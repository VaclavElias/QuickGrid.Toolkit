namespace QuickGrid.Samples.Services;

public static class UserService
{
    // Add random remote working flag to some users
    public static List<UserDto> GetUsers() =>
    [
            new() { Id = 1078, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Jack", Country = "China", RemoteWorking = true },
            new() { Id = 1189, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Katherine", Country = "India", RemoteWorking = false },
            new() { Id = 1290, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Lucas", Country = "Spain", RemoteWorking = true },
            new() { Id = 1291, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Xavier", Country = "Spain", RemoteWorking = true },
            new() { Id = 1311, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Mia", Country = "Argentina", RemoteWorking = false },
            new() { Id = 1412, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Noah", Country = "Japan", RemoteWorking = true },
            new() { Id = 1513, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Olivia", Country = "Italy", RemoteWorking = false },
            new() { Id = 1614, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Patrick", Country = "South Korea", RemoteWorking = true },
            new() { Id = 1674, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Alice", Country = "USA", RemoteWorking = false },
            new() { Id = 1715, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Quinn", Country = "South Africa", RemoteWorking = true },
            new() { Id = 1816, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Ryan", Country = "Nigeria", RemoteWorking = false },
            new() { Id = 1917, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Sophia", Country = "Egypt", RemoteWorking = true },
            new() { Id = 2018, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Thomas", Country = "Kenya", RemoteWorking = false },
            new() { Id = 2753, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Bob", Country = "Canada", RemoteWorking = true },
            new() { Id = 3835, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Charlie", Country = "Mexico", RemoteWorking = true },
            new() { Id = 4653, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "David", Country = "Germany", RemoteWorking = true },
            new() { Id = 5536, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Emma", Country = "Germany", RemoteWorking = true },
            new() { Id = 5537, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Henry", Country = "United Kingdom", RemoteWorking = true },
            new() { Id = 5538, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Emma", Country = "France", RemoteWorking = true },
            new() { Id = 6634, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Frank", Country = "UK", RemoteWorking = true },
            new() { Id = 7666, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Grace", Country = "Australia", RemoteWorking = true },
            new() { Id = 7667, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Grace", Country = "Germany", RemoteWorking = true },
            new() { Id = 8267, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Sergej", Country = "Russia", RemoteWorking = true },
            new() { Id = 9676, Age = GetRandomAge(), Weight = GetRandomWeight(), Name = "Isabella", Country = "Brazil", RemoteWorking = true }
        ];

    public static int GetRandomAge() => Random.Shared.Next(18, 100);
    public static float GetRandomWeight() => (float)(Random.Shared.NextDouble() * 120.0 + 30.0);
}