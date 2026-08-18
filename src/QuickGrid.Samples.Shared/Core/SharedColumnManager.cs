namespace QuickGrid.Samples.Core
{
    /// <summary>
    /// This is just for a demonstration of a shared column manager.
    /// </summary>
    public class SharedUserColumnManager : ColumnManager<UserDto>
    {
        public SharedUserColumnManager()
        {
            AddSimple(p => p.Age);
            AddSimple(p => p.Weight, fullTitle: "Weight (kg)", format: "N2", visible: false);
            AddTickColumn(p => p.RemoteWorking, "Remote", fullTitle: "Remote Working");
        }
    }
}