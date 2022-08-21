namespace ProcessData;

public class TickerChange
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
    public double? Change { get; set; }
    public double? LastYearChange { get; set; }
    public double? NormValue { get; set; }
}
