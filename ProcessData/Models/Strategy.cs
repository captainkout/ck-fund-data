using System.Collections.Generic;
using System.Data.Common;
using System.Net;
using System.Security.AccessControl;
using System.Text;
using FidelityFundApi;
using MorningStar;

namespace ProcessData;

public class Strategy
{
    public Fund RiskOne;
    private List<TickerChange> RiskOneChanges;
    public Fund RiskTwo;
    private List<TickerChange> RiskTwoChanges;
    public Fund Cash;
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
        StrategyChanges = changes
            .Aggregate(
                new List<StrategyChange>(),
                (p, c) =>
                {
                    // if (p.Count % 25 == 0)
                    //     Console.WriteLine("kctest");

                    var last = p.LastOrDefault();
                    var tradingDay = !p.Any() || (last?.Date.Month != c.Date.Month);
                    var change =
                        last?.Allocation == EnAllocation.RiskOne
                            ? c.r1.Change
                            : last?.Allocation == EnAllocation.RiskTwo
                                ? c.r2.Change
                                : (c.c.Change ?? 0);
                    var newValue = last?.Value + last?.Value * change ?? 100;

                    var sc = new StrategyChange()
                    {
                        Date = c.Date,
                        Allocation = tradingDay
                            ? (
                                (
                                    c.r1.LastYearChange > c.r2.LastYearChange
                                    && c.r1.LastYearChange > c.c.LastYearChange
                                )
                                    ? EnAllocation.RiskOne
                                    : (
                                        c.r1.LastYearChange < c.r2.LastYearChange
                                        && c.r2.LastYearChange > c.c.LastYearChange
                                    )
                                        ? EnAllocation.RiskTwo
                                        : EnAllocation.Cash
                            )
                            : (last?.Allocation ?? EnAllocation.Cash), // same as yesterday
                        LastAllocation = last?.Allocation ?? EnAllocation.Cash,
                        LastValue = last?.Value ?? 100,
                        Value = newValue,
                        Change = (newValue - (last?.Value ?? 100)) / (last?.Value ?? 100)
                    };

                    p.Add(sc);
                    return p;
                }
            )
            .ToList();
    }

    public string ToCsv()
    {
        return RiskOneChanges
            .Join(RiskTwoChanges, r1 => r1.Date, r2 => r2.Date, (r1, r2) => new { r1.Date, r1, r2 })
            .Join(CashChanges, r => r.Date, c => c.Date, (r, c) => new { r.Date, r.r1, r.r2, c })
            .Join(
                StrategyChanges,
                r => r.Date,
                s => s.Date,
                (r, s) => new { r.Date, r.r1, r.r2, r.c, s }
            )
            .Skip(1)
            .Aggregate(
                new StringBuilder(
                    "Date,R1,R1_Change,R2,R2_Change,Cash,Cash_Change,Strategy_Allocation,Strategy,Strategy_Change,RiskAve_Change,AllAve_Change\r\n"
                ),
                (p, o) =>
                    p.AppendLine(
                        string.Join(
                            ",",
                            new List<string>()
                            {
                                o.Date.ToString("yyyy-MM-dd"),
                                o.r1.NormValue.ToString(),
                                o.r1.Change.ToString(),
                                o.r2.NormValue.ToString(),
                                o.r2.Change.ToString(),
                                o.c.NormValue.ToString(),
                                o.c.Change.ToString(),
                                Enum.GetName(o.s.Allocation),
                                o.s.Value.ToString(),
                                o.s.Change.ToString(),
                                ((o.r1.Change + o.r2.Change) / 2).ToString(),
                                ((o.r1.Change + o.r2.Change + o.c.Change) / 3).ToString()
                            }
                        )
                    )
            )
            .ToString();
    }
}
