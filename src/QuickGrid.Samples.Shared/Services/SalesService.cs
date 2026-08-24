namespace QuickGrid.Samples.Services;

public static class SalesService
{
    public static List<SalesDto> GetSales() =>
    [
        new() { Id = 10245, Region = "North America", Product = "Laptops", TotalAmount = 12450.50m, Units = 83 },
        new() { Id = 10246, Region = "Europe", Product = "Monitors", TotalAmount = 8450.00m, Units = 145 },
        new() { Id = 10247, Region = "Asia", Product = "Keyboards", TotalAmount = 2150.75m, Units = 310 },
        new() { Id = 10248, Region = "South America", Product = "Mice", TotalAmount = 980.00m, Units = 245 },
        new() { Id = 10249, Region = "Europe", Product = "Docking Stations", TotalAmount = 4325.25m, Units = 96 },
        new() { Id = 10250, Region = "North America", Product = "Headsets", TotalAmount = 1890.00m, Units = 63 },
        new() { Id = 10251, Region = "Asia", Product = "Webcams", TotalAmount = 2675.30m, Units = 71 },
        new() { Id = 10252, Region = "Australia", Product = "Tablets", TotalAmount = 7150.00m, Units = 22 },
        new() { Id = 10253, Region = "Africa", Product = "Accessories", TotalAmount = 1240.40m, Units = 188 },
        new() { Id = 10254, Region = "Europe", Product = "Servers", TotalAmount = 18600.00m, Units = 12 },
        new() { Id = 10255, Region = "North America", Product = "Printers", TotalAmount = 0m, Units = 0 }
    ];
}