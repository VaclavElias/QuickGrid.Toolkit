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

## How it works

You declare every possible column once in a `ColumnManager<T>` using strongly-typed helpers (`AddSimple`, `AddNumber`, `AddToggleColumn`, `AddTickColumn`, `AddImageColumn`, `AddIndexColumn`, …). The toolkit then renders those columns either into a `QuickGrid` you control, or into the all-in-one `QuickGridWrapper`. Because the column configuration is just data, the same setup can drive several grids that each show a different subset of columns.

## Run the samples

```bash
dotnet run --project src/QuickGrid.Samples
```

The demo app contains three example pages that share the same column setup and build on each other:

| Page | Route | Shows |
| --- | --- | --- |
| [QuickGrid + ColumnManager](src/QuickGrid.Samples/Pages/UsersGrid.razor) | `/users-grid` | The low-level pattern: your own `QuickGrid` + a `ColumnSelector` for show/hide |
| [QuickGridWrapper](src/QuickGrid.Samples/Pages/UsersGridWrapper.razor) | `/users-grid-wrapper` | The same columns in a component with toolbar, quick search and pagination |
| [Total Footer](src/QuickGrid.Samples/Pages/TotalFooterExample.razor) | `/total-footer-example` | An automatic, summed totals row on a `QuickGridWrapper` |

## Getting started

The snippets below assume you already use Blazor and QuickGrid. Each mirrors a sample page, open the linked source for the full, runnable version.

### 1. Direct `QuickGrid` with `ColumnManager<T>`

Full control of the `QuickGrid` markup while the toolkit manages columns and the selection UI. See [`UsersGrid.razor`](src/QuickGrid.Samples/Pages/UsersGrid.razor).

```razor
<ColumnSelector ColumnManager="_columnManager" SelectionChanged="SelectionChangedAsync" />

<QuickGrid @ref="_grid" Items="@_items.AsQueryable()" Theme="twentyAI"
           Class="table table-sm table-index table-striped small table-fit table-thead-sticky mb-0">
    @QuickGridColumns.Columns(_columnManager)
</QuickGrid>

@code {
    private List<UserDto> _items = new();
    private ColumnManager<UserDto> _columnManager = new();
    private QuickGrid<UserDto>? _grid;

    protected override void OnInitialized()
    {
        _columnManager.AddIndexColumn();
        _columnManager.AddSimple(p => p.Name, fullTitle: "Name");
        _columnManager.AddToggleColumn(p => p.RemoteWorking, "Remote", fullTitle: "Remote Working", onChange: ToggleChange);
        _columnManager.AddCountry();

        _items = UserService.GetUsers();
    }

    private async Task SelectionChangedAsync() // call after the selection changes
    {
        if (_grid is not null) await _grid.RefreshDataAsync();
    }

    private async Task ToggleChange(UserDto user) { /* ... */ }
}
```

Key points:
- `ColumnManager<T>` defines all possible columns (predefined helpers like `AddCountry()` plus custom ones like `AddToggleColumn(...)`).
- `ColumnSelector` renders the show/hide UI; call `RefreshDataAsync` when the selection changes.
- `QuickGridColumns.Columns(_columnManager)` renders the currently visible columns.

### 2. `QuickGridWrapper`

When several grids share similar data but different columns, the wrapper centralizes the grid markup, toolbar, quick search and pagination, you keep just the per-page column configuration. See [`UsersGridWrapper.razor`](src/QuickGrid.Samples/Pages/UsersGridWrapper.razor).

```razor
<QuickGridWrapper Items="@_items.AsQueryable()" ColumnManager="_columnManager" />
```

You pass `Items` and a configured `ColumnManager<T>`; the column setup is identical to example 1.

### 3. Total footer

Add `TotalFooter` and an `Id` to a `QuickGridWrapper` to get an automatic totals row, numeric columns are summed for you. See [`TotalFooterExample.razor`](src/QuickGrid.Samples/Pages/TotalFooterExample.razor).

```razor
<QuickGridWrapper Items="@_items.AsQueryable()"
                  ColumnManager="_columnManager"
                  TotalFooter="_totalFooter"
                  Id="id-total-footer-example" />

@code {
    private TotalFooter _totalFooter = new() { IsTotalFooter = true };
    // numeric columns (AddNumber / AddStyledNumber) are totalled automatically;
    // set CalculateTotal on a column to force a total on or off.
}
```

The footer is rendered by a small ES module shipped with the toolkit (`quickGridToolkit.js`) that the wrapper imports automatically. It only appears when the wrapper has an `Id`, because that becomes the grid table's `id`.

## Utility CSS classes

- `table-index`: adds a compact index column when used with `AddIndexColumn()`.
- `table-fit`: reduces padding for dense layouts.
- `table-thead-sticky`: keeps the header row sticky.

## Known issues

- The `Format` property is not working for `object` type (formatting is instead applied inside the column's rendered content).

