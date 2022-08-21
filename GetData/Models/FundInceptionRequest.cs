namespace FidelityFundApi;

public class InceptionDate
{
    public string lowerRange { get; set; } = "35";
    public string upperRange { get; set; } = null;
}

public class FundListRequest
{
    public string businessChannel { get; set; } = "RETAIL";
    public int currentPageNumber { get; set; } = 1;
    public int noOfRowsPerPage { get; set; } = 10000;
    public SearchFilter searchFilter { get; set; } = new();
    public string sortBy { get; set; } = "grossXpnsRatio";
    public string sortOrder { get; set; } = "asc";
    public string subjectAreaCode { get; set; } =
        "fundInformation,fundPicks,mstarCatgAvgRiskRatings,volatility,bestAvailableMonthlyPerformance,mstarRatings,dailyPerformance,expensesAndFees,portfolioManager,totalNetAssets,holdingCharacteristics,fundFeatures,mstarRankings,fixedIncomeCharacteristics,monthlyYields,distributions,dailyNAV";
    public string tabNames { get; set; } =
        "Risk,Overview,ManagementAndFees,MorningstarRankings,IncomeCharacteristics,ShortTermPerformance,DailyPricingAndYields";
}

public class SearchFilter
{
    public string includeLeveragedAndInverseFunds { get; set; } = "N";
    public string openToNewInvestors { get; set; } = "OPEN,NEW,CLOSED";
    public string investmentTypeCode { get; set; } = "MFN";
    public string category { get; set; } =
        "XY,TU,RI,TE,CV,TA,TV,MA,XM,TJ,TN,AL,TH,TI,TK,TG,CA,IH,XQ,TD,TL";
    public InceptionDate inceptionDate { get; set; } = new();
}
