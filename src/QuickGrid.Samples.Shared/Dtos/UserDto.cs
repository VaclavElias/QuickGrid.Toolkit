namespace QuickGrid.Samples.Dtos;

public class UserDto : ICountryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "n/a";
    public string Country { get; set; } = "n/a";
    public int Age { get; set; }
    public float Weight { get; set; }
    public bool RemoteWorking { get; set; }
}