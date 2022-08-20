using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using FidelityFundApi;
using FundData;
using MorningStar;
using Newtonsoft.Json;

Console.WriteLine($"Program executed with {JsonConvert.SerializeObject(args)}");
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// // get list of resources from fidelity
// if (args.ToList().Any(a => a.StartsWith("--getlist")))
// {
//     var fidelityResponse = await new HttpClient().PostAsJsonAsync(
//         AppConsts.fidelityApi,
//         new FundListRequest()
//     );
//     var fidelityBytes = await fidelityResponse.Content.ReadAsByteArrayAsync();
//     await File.WriteAllBytesAsync(AppConsts.fidelityOutputFile, fidelityBytes);
// }
var stream = new FileStream(AppConsts.fidelityOutputFile, FileMode.Open);
var reader = ExcelReaderFactory.CreateReader(stream);
var dataSet = reader.AsDataSet(
    new ExcelDataSetConfiguration()
    {
        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
    }
);

var funds = new List<Fund>();
foreach (DataRow row in dataSet.Tables[0].Rows)
{
    var rowData = row[dataSet.Tables[0].Columns[0]].ToString();
    var formatted = rowData.Replace(",", "");
    var ticker = new Regex(@"\((.*?)\)").Match(formatted);
    if (ticker.Length == 0)
        break;
    funds.Add(new Fund() { name = formatted, ticker = ticker.Value.Trim('(').Trim(')') });
}

// // get morningstart data for each fund
// var rawTasks = funds
//     .Select(async f =>
//     {
//         var client = new HttpClient();
//         var url = AppConsts.GetMorningStarRaw(f.ticker.ToLower());
//         var request = new HttpRequestMessage(HttpMethod.Get, url);
//         request.Headers.Authorization = new AuthenticationHeaderValue(
//             "Bearer",
//             AppConsts.morningstarToken
//         );
//         var rsp = await client.SendAsync(request);
//         var raw = await rsp.Content.ReadAsStringAsync();
//         f.morningstarRaw = new Regex(@"(?<=byId:)(.*?)}").Match(raw).Value;
//         f.morningstarCode = new Regex(@"(?<=,)(.*?)(?=:)").Match(f.morningstarRaw).Value;
//         return f;
//     })
//     .ToList();
// var rsps = await Task.WhenAll(rawTasks);

// write the csv
// var fundsCsv =
//     "name,ticker,morningstarCode\r\n"
//     + string.Join(
//         "\r\n",
//         funds
//             .Where(f => f.ticker.Length == 5)
//             .Select(f => $"{f.name},{f.ticker},{f.morningstarCode}")
//             .ToList()
//     );
// await File.WriteAllTextAsync(AppConsts.fundListFile, fundsCsv);

// read funds.csv
var fundsFinal = (await File.ReadAllLinesAsync(AppConsts.fundListFile))
    .Skip(1)
    .Select(l =>
    {
        var split = l.Split(",");
        return new Fund()
        {
            name = split[0],
            ticker = split[1],
            morningstarCode = split[2]
        };
    })
    .ToList();
var returnTasks = fundsFinal
    .Select(async f =>
    {
        var client = new HttpClient();
        var url = AppConsts.GetMorningStarApi(f.morningstarCode.ToLower(), "d");
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            AppConsts.morningstarToken
        );
        var rsp = await client.SendAsync(request);
        var s = await rsp.Content.ReadAsStringAsync();
        var series = JsonConvert.DeserializeObject<MorningStarSeries[]>(s.Replace("\\\"", "\""));
        f.Series = series.First();
        return f;
    })
    .ToList();
var returnsComplete = await Task.WhenAll(returnTasks);
await File.WriteAllTextAsync(AppConsts.returnsFile, JsonConvert.SerializeObject(returnsComplete));
Console.WriteLine("*** Complete ***");
