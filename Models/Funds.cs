using MorningStar;

namespace FidelityFundApi;

public class Fund
{
    public string name { get; set; }
    public string ticker { get; set; }
    public string morningstarCode { get; set; }
    public MorningStarSeries Series { get; set; }
}
