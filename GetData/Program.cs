using System.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using FidelityFundApi;
using FundData;
using MorningStar;
using Newtonsoft.Json;

Console.WriteLine(
    $"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tProgram executed with {JsonConvert.SerializeObject(args)}"
);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var logs = new StringBuilder();

// get list of resources from fidelity
var fidelityResponse = await new HttpClient().PostAsJsonAsync(
    AppConsts.fidelityApi,
    new FundListRequest()
);
var fidelityBytes = await fidelityResponse.Content.ReadAsByteArrayAsync();
await File.WriteAllBytesAsync(AppConsts.fidelityOutputFile, fidelityBytes);
Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tFund List Received");

Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tParsing Fund List");
var stream = new FileStream(AppConsts.fidelityOutputFile, FileMode.Open);
var reader = ExcelReaderFactory.CreateReader(stream);
var dataSet = reader.AsDataSet(
    new ExcelDataSetConfiguration()
    {
        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
    }
);

var funds = new List<Fund>();
for (var i = 0; i < dataSet.Tables[0].Rows.Count; i++)
{
    var rowData = dataSet.Tables[0].Rows[i][dataSet.Tables[0].Columns[0]].ToString();
    var formatted = rowData.Replace(",", "");
    var ticker = new Regex(@"\((.*?)\)").Match(formatted);
    if (ticker.Length == 0)
        break;

    var inception = DateTime.Parse(
        dataSet.Tables[2].Rows[i][dataSet.Tables[2].Columns[2]].ToString()
    );
    var category = dataSet.Tables[0].Rows[i][dataSet.Tables[0].Columns[1]].ToString();
    var expenseRatio = 100.0;
    try
    {
        double.Parse(
            dataSet.Tables[2].Rows[i][dataSet.Tables[2].Columns[8]].ToString().Replace("%", "")
        );
    }
    catch (Exception) { }

    funds.Add(
        new Fund()
        {
            name = formatted,
            ticker = ticker.Value.Trim('(').Trim(')'),
            inception = inception,
            category = category,
            expenseRatio = expenseRatio / 100.0
        }
    );
}
Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tFund List Parsed");

Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tFetch and Decode MorningStar WebPage");

// get morningstart data for each fund
var rawTasks = funds
    .Where(f => f.ticker.Length == 5)
    .Select(
        async (f, i) =>
        {
            if (i % 100 == 0)
                Console.WriteLine(
                    $"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tFetch and Decode\t{i}/{funds.Count}"
                );

            var client = new HttpClient();
            var url = AppConsts.GetMorningStarRaw(f.ticker.ToLower());
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                AppConsts.morningstarToken
            );
            try
            {
                var rsp = await client.SendAsync(request);
                var raw = await rsp.Content.ReadAsStringAsync();
                var morningstarRaw = new Regex(@"(?<=byId:)(.*?)}").Match(raw).Value;
                f.morningstarCode = new Regex(@"(?<=,)(.*?)(?=:)").Match(morningstarRaw).Value;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return f;
        }
    )
    .ToList();
var rsps = await Task.WhenAll(rawTasks);
Console.WriteLine(
    $"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tFetch and Decode MorningStar WebPage Complete"
);
Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tStore Fund Details");
await File.WriteAllTextAsync(AppConsts.fundListFile, JsonConvert.SerializeObject(rsps));

Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tRead Fund Details");

// read funds json
var fundsFinal = JsonConvert
    .DeserializeObject<Fund[]>(await File.ReadAllTextAsync(AppConsts.fundListFile))
    .ToList()
    .Where(f => !File.Exists($"returns\\{f.ticker}.json"))
    .Where(f => f.morningstarCode != null && f.morningstarCode != string.Empty)
    .ToArray();

Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tFetch Returns");
var returnTasks = fundsFinal
    .Select(
        async (f, i) =>
        {
            Thread.Sleep(500); // sleep because they cancel requests if it goes any faster
            if (i % 50 == 0)
                Console.WriteLine(
                    $"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tFetch and Store Returns\t{i}/{fundsFinal.Length}"
                );

            var client = new HttpClient();
            var url = AppConsts.GetMorningStarApi(f.morningstarCode.ToLower(), "d");
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                AppConsts.morningstarToken
            );
            try
            {
                var rsp = await client.SendAsync(request);
                var s = await rsp.Content.ReadAsStringAsync();
                var series = JsonConvert.DeserializeObject<MorningStarSeries[]>(
                    s.Replace("\\\"", "\"")
                );
                f.Series = series.First();
            }
            catch (Exception)
            {
                logs.AppendLine($"{f.ticker},{f.morningstarCode},{url}");
                return null;
            }

            return f;
        }
    )
    .ToList();
var returnsComplete = await Task.WhenAll(returnTasks);
Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tStoring Returns");

var stored = returnsComplete
    .Where(f => f != null)
    .Select(
        async (f, i) =>
        {
            if (i % 100 == 0)
                Console.WriteLine(
                    $"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tFetch and Store Returns\t{i}/{returnsComplete.Length}"
                );
            await File.WriteAllTextAsync(
                AppConsts.GetReturnFile(f.ticker),
                JsonConvert.SerializeObject(f)
            );
            return true;
        }
    )
    .ToList();
var complete = await Task.WhenAll(stored);

Console.WriteLine($"*** {DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \t Complete ***");

// write Logs
if (logs.Length > 0)
    await File.WriteAllTextAsync(
        $"logs\\run_{DateTime.Now:yyyy_MM_dd__HH_mm}.txt",
        logs.ToString()
    );
