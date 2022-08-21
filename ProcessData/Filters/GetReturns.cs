using FidelityFundApi;
using Newtonsoft.Json;
using Shared;

namespace ProcessData;

public static class GetReturns
{
    public static async Task<Fund> GetByTicker(string ticker, DateTime startDate)
    {
        var str = await File.ReadAllTextAsync(AppConsts.GetReturnFile(ticker));
        var f = JsonConvert.DeserializeObject<Fund>(str);
        f.Series.series = f.Series.series.Where(d => d.date > startDate).ToList();
        return f;
    }
}
