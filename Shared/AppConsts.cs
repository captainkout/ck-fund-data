namespace Shared;

public static class AppConsts
{
    public const string fundListFile = "..\\data\\FundList.json";

    public static string GetReturnFile(string ticker)
    {
        return $"..\\returns\\{ticker}.json";
    }

    public static string GetStrategyFile(string riskone, string risktwo, DateTime start)
    {
        return $"..\\strategy\\{riskone}_{risktwo}_{start:yyyy}.csv";
    }
}
