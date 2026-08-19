namespace QuickGrid.Samples.Core
{
    /// <summary>
    /// A set of columns defined once and reused by several grids, pulled in with <c>ColumnManager.AddRange</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="Instance"/> is deliberately a single shared instance: every page that reuses these columns reuses
    /// the same objects. That is safe because <c>AddRange</c> copies each column into the target manager, so hiding
    /// a column on one page does not hide it on another and the shared set is never renumbered.
    /// </remarks>
    public class SharedUserColumnManager : ColumnManager<UserDto>
    {
        public static SharedUserColumnManager Instance { get; } = new();

        public SharedUserColumnManager()
        {
            AddSimple(p => p.Age);
            AddSimple(p => p.Weight, fullTitle: "Weight (kg)", format: "N2", visible: false);
            AddTickColumn(p => p.RemoteWorking, "Remote", fullTitle: "Remote Working");
        }
    }
}
