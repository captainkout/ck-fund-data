// See https://aka.ms/new-console-template for more information
using System.Linq;
using System.Reflection;
using FidelityFundApi;
using Newtonsoft.Json;
using ProcessData;
using Shared;

Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tBegin");

var fundlistFile = await File.ReadAllTextAsync(AppConsts.fundListFile);
var fundlist = JsonConvert.DeserializeObject<Fund[]>(fundlistFile);

var argStart = args.FirstOrDefault(a => a.StartsWith("--start="));
var startDate =
    argStart != null ? DateTime.Parse(argStart.Split("=")[1]) : new DateTime(1990, 1, 1);

if (args.Any(a => a.StartsWith("--list")))
{
    // Get Old Distinct Funds
    var oldUnique = fundlist
        .Where(f => f.inception < startDate)
        .Aggregate(
            new List<Fund>(),
            (p, c) =>
            {
                if (p.Any(pf => pf.name[0..^15] == c.name[0..^15]))
                    return p;
                p.Add(c);
                return p;
            }
        )
        .OrderBy(f => f.inception)
        .ToList();
    oldUnique.ForEach(f =>
    {
        Console.WriteLine(f.ToString());
    });
    Environment.Exit(0);
}

var argR1 = args.FirstOrDefault(a => a.StartsWith("--r1="));
var r1 = argR1 != null ? argR1.Split("=")[1] : "VFIAX";

var argR2 = args.FirstOrDefault(a => a.StartsWith("--r2="));
var r2 = argR2 != null ? argR2.Split("=")[1] : "NHWJX";

var argCash = args.FirstOrDefault(a => a.StartsWith("--cash="));
var c = argCash != null ? argCash.Split("=")[1] : "VWSTX";

// getReturns
var cash = await GetReturns.GetByTicker(c, startDate);
var domeq = await GetReturns.GetByTicker(r1, startDate);
var inteq = await GetReturns.GetByTicker(r2, startDate);

var strat = new Strategy(domeq, inteq, cash);
await strat.CombineReturns();
strat.CalcStrategy();
var str = await Task.Run(() => strat.ToCsv());
await File.WriteAllTextAsync(
    AppConsts.GetStrategyFile(strat.RiskOne.ticker, strat.RiskTwo.ticker, startDate),
    str
);

Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tComplete");
