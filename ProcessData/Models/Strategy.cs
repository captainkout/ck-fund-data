using System.Collections.Generic;
using System.Security.AccessControl;
using FidelityFundApi;
using MorningStar;

namespace ProcessData;

public class Strategy
{
    private Fund RiskOne;
    private List<TickerChange> RiskOneChanges;
    private Fund RiskTwo;
    private List<TickerChange> RiskTwoChanges;
    private Fund Cash;
    private List<TickerChange> CashChanges;
    private List<StrategyChange> StrategyChanges;

    public Strategy(Fund riskOne, Fund riskTwo, Fund cash)
    {
        RiskOne = riskOne;
        RiskTwo = riskTwo;
        Cash = cash;
    }

    private List<TickerChange> aggfun(List<TickerChange> p, Series c)
    {
        double? lastvalue = p.Any() ? p.Last().Value : null;
        double? change = p.Any() ? (c.totalReturn - p.Last().Value) / c.totalReturn : null;

        double? lastnormvalue = p.Any() ? p.Last().NormValue : 100;
        double? lastyearvalue = p.LastOrDefault(ps => ps.Date < c.date.AddYears(-1))?.Value;

        var ticker = new TickerChange()
        {
            Date = c.date,
            Value = (double)c.totalReturn,
            Change = change,
            LastYearChange = lastyearvalue != null ? c.totalReturn - lastyearvalue : null,
            NormValue = change != null ? lastnormvalue + lastnormvalue * change : 100
        };

        p.Add(ticker);
        return p;
    }

    public async Task CombineReturns()
    {
        var t = new List<Task<List<TickerChange>>>()
        {
            Task.Run(
                () => RiskOne.Series.series.Aggregate(new List<TickerChange>(), aggfun).ToList()
            ),
            Task.Run(
                () => RiskTwo.Series.series.Aggregate(new List<TickerChange>(), aggfun).ToList()
            ),
            Task.Run(() => Cash.Series.series.Aggregate(new List<TickerChange>(), aggfun).ToList())
        };
        var series = await Task.WhenAll(t);
        RiskOneChanges = series[0];
        RiskTwoChanges = series[1];
        CashChanges = series[2];
    }

    public void CalcStrategy()
    {
        var changes = RiskOneChanges
            .Join(RiskTwoChanges, r1 => r1.Date, r2 => r2.Date, (r1, r2) => new { r1.Date, r1, r2 })
            .Join(CashChanges, r => r.Date, c => c.Date, (r, c) => new { r.Date, r.r1, r.r2, c })
            .ToList();
        StrategyChanges = changes.Select(c =>
        {

            return new StrategyChange();
        }).ToList();
    }
}
