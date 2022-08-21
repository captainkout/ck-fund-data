namespace ProcessData;

public class StrategyChange
{
    public DateTime Date { get; set; }
    public EnAllocation Allocation { get; set; }
    public EnAllocation LastAllocation { get; set; }
    public double Value { get; set; }
    public double LastValue { get; set; }
    public double Change { get; set; }
}

public enum EnAllocation
{
    RiskOne,
    RiskTwo,
    Cash
}
