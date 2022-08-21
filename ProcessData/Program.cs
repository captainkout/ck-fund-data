// See https://aka.ms/new-console-template for more information
using System.Linq;
using FidelityFundApi;
using Newtonsoft.Json;
using ProcessData;
using Shared;

Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tBegin");

var fundlistFile = await File.ReadAllTextAsync(AppConsts.fundListFile);
var fundlist = JsonConvert.DeserializeObject<Fund[]>(fundlistFile);

// getReturns
var cash = await GetReturns.GetByTicker(Filters.Cash().First(), new DateTime(1980, 1, 1));
var domeq = await GetReturns.GetByTicker(Filters.Cash().First(), new DateTime(1980, 1, 1));
var inteq = await GetReturns.GetByTicker(Filters.Cash().First(), new DateTime(1980, 1, 1));

var strat = new Strategy(domeq, inteq, cash);
await strat.CombineReturns();
strat.CalcStrategy();

Console.WriteLine($"{DateTime.Now:yyyy-MM-ddTHH:mm:ssK} \tComplete");
