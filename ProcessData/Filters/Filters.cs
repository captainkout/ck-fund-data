namespace ProcessData;

public class Filters
{
    public static List<string> Cash()
    {
        return new List<string>() { "VWSTX", "DLTLX" };
    }

    // Straight Eq
    public static List<string> DomEq()
    {
        return new List<string>() { "NMFAX", "VFIAX", "CHTRX" };
    }

    public static List<string> IntEq()
    {
        return new List<string>() { "SLSSX", "SUIAX", "NWHJX" };
    }

    // Allocation
    public static List<string> DomAllo()
    {
        return new List<string>() { "RPBAX", "FKIQX", "MDCPX" };
    }

    public static List<string> GlobalAllo()
    {
        return new List<string>() { "KTRAX", "FSTBX", "MDPCX" };
    }

    //Bond
    public static List<string> LongBnd()
    {
        return new List<string>() { "VWESX", "LMSFX" };
    }

    public static List<string> InterBnd()
    {
        return new List<string>() { "FUSGX", "NERYX", "PRCIX" };
    }

    public static List<string> HighBnd()
    {
        return new List<string>() { "FHQRX", "VWEHX", "MHIIX" };
    }
}

// // Get Fund Groups
// var fundGroups = fundlist.GroupBy(f => f.category).OrderBy(g => g.Key).ToList();

// fundGroups.ForEach(g =>
// {
//     Console.WriteLine($"\r\n\r\n{g.Key}\t{g.Count()}");
//     var categoryFunds = g.OrderBy(f => f.inception).ToList();
//     categoryFunds.ForEach(f => Console.WriteLine($"{f.name}\t{f.inception}"));
// });

// var bonds = fundlist.Where(f => f.category.ToLower().Contains("bond")).OrderBy(f => f.inception);
// bonds
//     .ToList()
//     .ForEach(f =>
//     {
//         Console.WriteLine($"{f.category}\t{f.name}\t{f.inception}");
//     });

// // Get Old Distinct Funds
// var oldUnique = fundlist
//     .Where(f => f.inception < new DateTime(1980, 1, 1))
//     .Aggregate(
//         new List<Fund>(),
//         (p, c) =>
//         {
//             if (p.Any(pf => pf.name[0..^10] == c.name[0..^10]))
//                 return p;
//             p.Add(c);
//             return p;
//         }
//     )
//     .OrderBy(f => f.inception)
//     .ToList();
// oldUnique.ForEach(f =>
// {
//     Console.WriteLine(f.ToString());
// });
