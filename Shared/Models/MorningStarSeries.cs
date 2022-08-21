namespace MorningStar;

public class MorningStarSeries
{
    public string queryKey { get; set; }
    public List<Series> series { get; set; }
}

public class Series
{
    public double? nav { get; set; }
    public DateTime date { get; set; }
    public double? totalReturn { get; set; }
}
