# QuickGrid.Toolkit

**▶ Live demo: <https://vaclavelias.github.io/QuickGrid.Toolkit/>**

[![Build](https://github.com/VaclavElias/QuickGrid.Toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/VaclavElias/QuickGrid.Toolkit/actions/workflows/build.yml)

QuickGrid.Toolkit extends the Blazor QuickGrid with reusable, dynamic column management and small UI utilities. It is especially useful when you render the same kind of data in multiple places but need different visible columns per grid: you declare every possible column once in a `ColumnManager<T>`, and because the column configuration is just data, the same setup can drive several grids that each show a different subset.

## Examples

Eleven example pages, from the low-level building blocks to a full application-grade setup. Every page runs in the [live demo](https://vaclavelias.github.io/QuickGrid.Toolkit/); sources live under [`src/QuickGrid.Samples.Shared/Pages/Examples`](src/QuickGrid.Samples.Shared/Pages/Examples) and are registered in [`ExampleRegistry.cs`](src/QuickGrid.Samples.Shared/Core/ExampleRegistry.cs).

| Example | Shows | Source |
| --- | --- | --- |
| [QuickGrid + ColumnManager](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid) | Your own `<QuickGrid>` with columns rendered from a `ColumnManager`, plus a `ColumnSelector` - the low-level pattern | [UsersGrid.razor](src/QuickGrid.Samples.Shared/Pages/Examples/UsersGrid.razor) |
| [QuickGridWrapper](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid-wrapper) | The same columns in one component with toolbar, quick search and a column selector | [UsersGridWrapper.razor](src/QuickGrid.Samples.Shared/Pages/Examples/UsersGridWrapper.razor) |
| [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) | Every column helper side by side: text, dates, numbers, ticks, toggles, markup, images, templates and actions | [ColumnTypes.razor](src/QuickGrid.Samples.Shared/Pages/Examples/ColumnTypes.razor) |
| [Formatting & Styling](https://vaclavelias.github.io/QuickGrid.Toolkit/formatting-styling) | Format strings, conditional cell styling with `CellStyleMap`, row classes, shared `ColumnInfo` definitions | [FormattingStyling.razor](src/QuickGrid.Samples.Shared/Pages/Examples/FormattingStyling.razor) |
| [Loading, Paging & Refresh](https://vaclavelias.github.io/QuickGrid.Toolkit/loading-paging) | `IsLoading`, pagination, and keeping the grid in step with data that changes underneath it (`ItemsVersion`) | [LoadingPaging.razor](src/QuickGrid.Samples.Shared/Pages/Examples/LoadingPaging.razor) |
| [Search & Filtering](https://vaclavelias.github.io/QuickGrid.Toolkit/search-filtering) | Quick search across every column, exact match, nested properties, and your own filter panel | [SearchFiltering.razor](src/QuickGrid.Samples.Shared/Pages/Examples/SearchFiltering.razor) |
| [Row Selection](https://vaclavelias.github.io/QuickGrid.Toolkit/row-selection) | Selecting rows with `ISelectionDto` and acting on the selection from the toolbar | [RowSelection.razor](src/QuickGrid.Samples.Shared/Pages/Examples/RowSelection.razor) |
| [Footers & Totals](https://vaclavelias.github.io/QuickGrid.Toolkit/footers-totals) | Automatic totals for numeric columns, or hand-built footer cells for full control | [FootersTotals.razor](src/QuickGrid.Samples.Shared/Pages/Examples/FootersTotals.razor) |
| [Export](https://vaclavelias.github.io/QuickGrid.Toolkit/export) | Wiring the export events to produce a CSV of what the user is currently looking at | [Export.razor](src/QuickGrid.Samples.Shared/Pages/Examples/Export.razor) |
| [Saved Views & Icons](https://vaclavelias.github.io/QuickGrid.Toolkit/saved-views) | Column layouts as named views, and swapping the toolbar icons for your own | [SavedViews.razor](src/QuickGrid.Samples.Shared/Pages/Examples/SavedViews.razor) |
| [Your Own Grid Component](https://vaclavelias.github.io/QuickGrid.Toolkit/app-quickgrid) | Subclassing `QuickGridWrapper` once to fix the styling and wire the events for a whole application | [AppQuickGridExample.razor](src/QuickGrid.Samples.Shared/Pages/Examples/AppQuickGridExample.razor) |

## Features

**Legend:** ⏳ planned. The *Example* column links to the live demo page showing the feature.

### Column management

| Feature | Example |
| --- | --- |
| Add columns dynamically at runtime with `ColumnManager<T>` | [QuickGrid + ColumnManager](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid) |
| One column setup reused across grids - each grid shows a different subset | [QuickGridWrapper](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid-wrapper) |
| Show/hide column selection UI (`ColumnSelector`) | [QuickGrid + ColumnManager](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid) |
| Predefined, strongly-typed helpers via extension methods (e.g. `AddCountry()`) | [QuickGrid + ColumnManager](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid) |
| Sorting for added columns | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Per-column visibility, alignment, format and CSS class | [Formatting & Styling](https://vaclavelias.github.io/QuickGrid.Toolkit/formatting-styling) |
| Saved column views / layouts (durable persistence is the host's job, via `Events`) | [Saved Views & Icons](https://vaclavelias.github.io/QuickGrid.Toolkit/saved-views) |

### Built-in column types

| Column type | Example |
| --- | --- |
| Index column (`AddIndexColumn`) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Value column (`AddSimple`) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Number column, `int` / `double` / `decimal` (`AddNumber`) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Styled number with conditional cell styling (`AddStyledNumber`) | [Formatting & Styling](https://vaclavelias.github.io/QuickGrid.Toolkit/formatting-styling) |
| Date column (`AddSimpleDate`) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Tick / boolean column (`AddTickColumn`, with true/false styling) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Toggle column (`AddToggleColumn`, with change callback) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Image column (`AddImageColumn`) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Template column (`AddTemplateColumn`, custom `RenderFragment`) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Markup column (`AddMarkup`, raw HTML) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |
| Clickable / action columns with callbacks (`AddAction`) | [Column Types](https://vaclavelias.github.io/QuickGrid.Toolkit/column-types) |

### Styling

| Feature | Example |
| --- | --- |
| Conditional cell styling (`CellStyleMap`) | [Formatting & Styling](https://vaclavelias.github.io/QuickGrid.Toolkit/formatting-styling) |
| Value markers by sign - `AddStyledNumber` marks each value `negative` / `positive` / `zero`; [your CSS supplies the colours](#styling-values-by-their-nature) | [Formatting & Styling](https://vaclavelias.github.io/QuickGrid.Toolkit/formatting-styling) |
| Choose which markers a grid emits (`ColumnManager.ValueStyles`) | [Formatting & Styling](https://vaclavelias.github.io/QuickGrid.Toolkit/formatting-styling) |
| Custom column styling (CSS class per column) | [Formatting & Styling](https://vaclavelias.github.io/QuickGrid.Toolkit/formatting-styling) |
| Custom row styling (row classes, CSS `:has()`) | [Formatting & Styling](https://vaclavelias.github.io/QuickGrid.Toolkit/formatting-styling) |
| Utility CSS classes: `table-index`, `table-fit`, `table-thead-sticky`, `table-no-empty-lines` | - |

### `QuickGridWrapper` (all-in-one grid)

| Feature | Example |
| --- | --- |
| Quick search across all columns | [QuickGridWrapper](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid-wrapper) |
| Nested / child-property search | [Search & Filtering](https://vaclavelias.github.io/QuickGrid.Toolkit/search-filtering) |
| Exact-match toggle | [Search & Filtering](https://vaclavelias.github.io/QuickGrid.Toolkit/search-filtering) |
| Preset / external search value (`QuickSearch`) | [Search & Filtering](https://vaclavelias.github.io/QuickGrid.Toolkit/search-filtering) |
| Custom filter panel (`FilterSection`) | [Search & Filtering](https://vaclavelias.github.io/QuickGrid.Toolkit/search-filtering) |
| EF-backed server filtering (`FilterCriteria`) | ⏳ example planned |
| Pagination, loading indicator, data refresh (`ItemsVersion`) | [Loading, Paging & Refresh](https://vaclavelias.github.io/QuickGrid.Toolkit/loading-paging) |
| Row selection (`ISelectionDto`) with bulk actions | [Row Selection](https://vaclavelias.github.io/QuickGrid.Toolkit/row-selection) |
| Toolbar with settings menu | [QuickGridWrapper](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid-wrapper) |
| Pluggable icons (`IQuickGridIconProvider`, Bootstrap Icons by default) | [Saved Views & Icons](https://vaclavelias.github.io/QuickGrid.Toolkit/saved-views) |
| App-wide defaults by subclassing the wrapper | [Your Own Grid Component](https://vaclavelias.github.io/QuickGrid.Toolkit/app-quickgrid) |

### Footers

| Feature | Example |
| --- | --- |
| Automatic total footer - sums numeric columns (`TotalFooter`) | [Footers & Totals](https://vaclavelias.github.io/QuickGrid.Toolkit/footers-totals) |
| Per-column total control (`CalculateTotal`) | [Footers & Totals](https://vaclavelias.github.io/QuickGrid.Toolkit/footers-totals) |
| Manual footer cells (`AddFooterColumn`, `AddFooterColumnWithSum`) | [Footers & Totals](https://vaclavelias.github.io/QuickGrid.Toolkit/footers-totals) |
| Column header tooltips from full titles (needs the wrapper `Id`) | [Footers & Totals](https://vaclavelias.github.io/QuickGrid.Toolkit/footers-totals) |

### Export

| Feature | Example |
| --- | --- |
| Export to CSV (the wrapper raises `Events`; writing the file is up to the host) | [Export](https://vaclavelias.github.io/QuickGrid.Toolkit/export) |
| Export selected columns only | [Export](https://vaclavelias.github.io/QuickGrid.Toolkit/export) |
| ⏳ Export to JSON | - |

## Requirements

- .NET 10
- Bootstrap 5
- Icons: either include Bootstrap Icons
  - `<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css">`
  - or provide your own implementation of `IQuickGridIconProvider`
- Toolkit CSS (Static Web Asset):
  - `<link rel="stylesheet" href="@Assets["_content/QuickGrid.Toolkit/app.css"]" />`
  - it covers layout, the toolbar and utility classes only; colouring values by their nature is opt-in, see [Styling values by their nature](#styling-values-by-their-nature)

## Getting started

The snippets below assume you already use Blazor and QuickGrid. Each mirrors a sample page, open the linked source for the full, runnable version.

### 1. Direct `QuickGrid` with `ColumnManager<T>`

Full control of the `QuickGrid` markup while the toolkit manages columns and the selection UI. See [`UsersGrid.razor`](src/QuickGrid.Samples.Shared/Pages/Examples/UsersGrid.razor) · [live](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid).

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

<img width="610" height="344" alt="image" src="https://github.com/user-attachments/assets/9d6c2476-f023-499c-8c25-9b780f1a51a7" />

### 2. `QuickGridWrapper`

When several grids share similar data but different columns, the wrapper centralizes the grid markup, toolbar, quick search and pagination, you keep just the per-page column configuration. See [`UsersGridWrapper.razor`](src/QuickGrid.Samples.Shared/Pages/Examples/UsersGridWrapper.razor) · [live](https://vaclavelias.github.io/QuickGrid.Toolkit/users-grid-wrapper).

```razor
<QuickGridWrapper Items="@_items.AsQueryable()" ColumnManager="_columnManager" />
```

You pass `Items` and a configured `ColumnManager<T>`; the column setup is identical to example 1.

<img width="435" height="369" alt="image" src="https://github.com/user-attachments/assets/23e067fd-a273-49e5-a099-ddac8a8af795" />

### 3. Total footer

Add `TotalFooter` and an `Id` to a `QuickGridWrapper` to get an automatic totals row, numeric columns are summed for you. See [`FootersTotals.razor`](src/QuickGrid.Samples.Shared/Pages/Examples/FootersTotals.razor) · [live](https://vaclavelias.github.io/QuickGrid.Toolkit/footers-totals).

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

<img width="556" height="438" alt="image" src="https://github.com/user-attachments/assets/aa9eb31d-3ea9-47d8-92f0-29557e7bcdc1" />

## Run the samples locally

```bash
dotnet run --project src/QuickGrid.Samples        # Blazor Server (matches how the toolkit is typically consumed)
dotnet run --project src/QuickGrid.Samples.Wasm   # standalone WebAssembly (what GitHub Pages hosts)
```

Both hosts render the same example pages from the shared `QuickGrid.Samples.Shared` library. The live demo is published by the [deploy-pages workflow](.github/workflows/deploy-pages.yml).

## Utility CSS classes

- `table-index`: adds a compact index column when used with `AddIndexColumn()`.
- `table-fit`: reduces padding for dense layouts.
- `table-thead-sticky`: keeps the header row sticky.

## Styling values by their nature

`AddStyledNumber` wraps its value in `<span content="...">`, where the content describes the value's nature: `negative`, `positive` or `zero`. The toolkit ships **no colours** for these - styling them is opt-in, so a grid shows only the colours the application actually asked for. Add the natures you want to your own stylesheet:

```css
td span[content="negative"] {
    color: #b02a37;
}

td span[content="positive"] {
    color: #146c43;
}

td span[content="zero"] {
    color: #6c757d;
}
```

Leave a rule out and that nature renders as ordinary text. A common choice is colouring negatives and greying zeros as noise while leaving positives plain. Scope the selectors - `.my-report td span[content="negative"]` - to vary the palette per grid.

To stop a grid emitting the markers at all, set `ColumnManager.ValueStyles`. A nature that is switched off produces no `<span>`, so there is nothing for CSS to undo:

```csharp
_columnManager.ValueStyles = GridValueStyles.Negative | GridValueStyles.Zero;
```

A `CellStyleMap` supplies your own content names instead of these three, and those are never suppressed.

## Known issues

- The `Format` property is not working for `object` type (formatting is instead applied inside the column's rendered content).
