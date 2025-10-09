# QuickGrid.Toolkit

QuickGrid.Toolkit extends the Blazor QuickGrid with reusable, dynamic column management and small UI utilities. It is especially useful when you render the same kind of data in multiple places but need different visible columns per grid.

- QuickGrid.Toolkit: a library that adds to the official [QuickGrid](https://aspnet.github.io/quickgridsamples/)
  - ✅ Dynamically add columns at runtime
  - ✅ Column selection UI (show/hide)
  - ✅ Predefined, strongly-typed helpers (e.g., `AddCountry()`)
  - ✅ Sorting support for added columns
  - ✅ Utility CSS classes: `table-index`, `table-fit`, `table-thead-sticky`
  - ✅ Custom `ImageColumn`
    - ⏳ Example
  - ✅ Custom `TickColumn`
    - ⏳ Example
  - ✅ Custom `ToggleColumn`
    - ⏳ Example
  - ✅ Clickable columns with callbacks
    - ⏳ Example
  - ✅ Custom column styling
    - ⏳ Example
  - ✅ Custom row styling (css `:has()`)
    - ⏳ Example
  - ⏳ Export to CSV and Json
    - ⏳ Example
  - ✅ Quick search across all columns (`QuickGridWrapper`)
    - ✅ Example
    - ✅ Pass search value
      - ⏳ Example
    - ✅ Exact Match option (`QuickGridWrapper`)
      - ✅ Example
- QuickGrid.Samples: a demo app showcasing the toolkit (see the Users pages)

## Requirements

- .NET 10
- Bootstrap 5
- Icons: either include Bootstrap Icons
  - `<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css">`
  - or provide your own implementation of `IQuickGridIconProvider`
- Toolkit CSS (Static Web Asset):
  - `<link rel="stylesheet" href="@Assets["_content/QuickGrid.Toolkit/app.css"]" />`

## Getting started

Below are two small examples. We assume you already use Blazor and QuickGrid and that you are familiar with their concepts.

### Example 1: Direct `QuickGrid` with `ColumnManager<T>` (UsersGrid.razor)

This pattern gives you full control of a `QuickGrid` while the toolkit manages the columns and selection UI.

```razor
<h1>Users Grid</h1>
<div class="my-3">
    <ColumnSelector ColumnManager="_columnManager" SelectionChanged="SelectionChangedAsync" />
</div>
<QuickGrid @ref="_grid" Items="@_items.AsQueryable()" Class="table table-sm table-index table-striped small table-fit table-thead-sticky mb-0" Theme="twentyAI">
    @QuickGridColumns.Columns(_columnManager)
</QuickGrid>

@code {
    private List<UserDto> _items = new();
    private ColumnManager<UserDto> _columnManager = new();
    private QuickGrid<UserDto>? _grid;

    protected override void OnInitialized()
    {
        var sharedManager = new SharedUserColumnManager();

        _columnManager.AddIndexColumn();
        _columnManager.Add(new() { Property = p => p.Id });
        _columnManager.AddSimple(p => p.Name, fullTitle: "Name");
        _columnManager.AddRange(sharedManager.Columns);
        _columnManager.AddToggleColumn(p => p.RemoteWorking, "Remote", fullTitle: "Remote Working", onChange: ToggleChange);
        _columnManager.AddCountry();

        _items = UserService.GetUsers();
    }

    private async Task SelectionChangedAsync()
    {
        if (_grid is null) return;
        await _grid.RefreshDataAsync();
    }

    private async Task ToggleChange(UserDto user)
    {
        user.RemoteWorking = !user.RemoteWorking;
        await Task.CompletedTask;
        StateHasChanged();
    }
}
```

Key points:
- `ColumnManager<T>` defines all possible columns (including predefined helpers like `AddCountry()` and custom ones like `AddToggleColumn(...)`).
- `ColumnSelector` renders a simple UI to show/hide columns; call `RefreshDataAsync` when the selection changes.
- `QuickGridColumns.Columns(_columnManager)` renders the current set of visible columns.

### Example 2: `QuickGridWrapper` (UsersGridWrapper.razor)

When you have multiple grids with similar data but different columns, the wrapper centralizes the grid markup while you keep per-page column configuration.

```razor
<QuickGridWrapper
    Items="@_items.AsQueryable()"
    ColumnManager="_columnManager" />

@code {
    private List<UserDto> _items = new();
    private ColumnManager<UserDto> _columnManager = new();

    protected override void OnInitialized()
    {
        var sharedManager = new SharedUserColumnManager();

        _columnManager.AddIndexColumn();
        _columnManager.Add(new() { Property = p => p.Id });
        _columnManager.AddSimple(p => p.Name, fullTitle: "Name");
        _columnManager.AddRange(sharedManager.Columns);
        _columnManager.AddToggleColumn(p => p.RemoteWorking, "Remote", fullTitle: "Remote Working", onChange: ToggleChange);
        _columnManager.AddCountry();

        _items = UserService.GetUsers();
    }

    private async Task ToggleChange(UserDto user)
    {
        user.RemoteWorking = !user.RemoteWorking;
        await Task.CompletedTask;
        StateHasChanged();
    }
}
```

Notes:
- `QuickGridWrapper` encapsulates the base grid and styling. You pass `Items` and a configured `ColumnManager<T>`.
- The wrapper will gain additional features over time. This README will be updated accordingly.

## Utility CSS classes

- `table-index`: adds a compact index column when used with `AddIndexColumn()`.
- `table-fit`: reduces padding for dense layouts.
- `table-thead-sticky`: keeps the header row sticky.

## Known issues

- The `Format` property is not working for `object` type.

## Samples

Explore the `QuickGrid.Samples` project for working pages and configurations.

