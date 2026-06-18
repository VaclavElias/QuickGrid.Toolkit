# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`QuickGrid.Toolkit` is a Blazor library that extends the official [QuickGrid](https://aspnet.github.io/quickgridsamples/) with **runtime, data-driven column management**, a show/hide column-selection UI, custom column types, in-memory quick search, and footer totals. Its primary use case: rendering the same data type in multiple grids while exposing different visible columns per grid.

The solution (`QuickGrid.Toolkit.slnx`) has two projects:
- `src/QuickGrid.Toolkit` — the library (`Microsoft.NET.Sdk.Razor`, ships static web assets).
- `src/QuickGrid.Samples` — a Blazor Web App (Interactive Server) demo. `Pages/UsersGrid.razor` and `Pages/UsersGridWrapper.razor` are the two reference usage patterns.

Both target **.NET 10**. (The `.github/copilot-instructions.md` is stale — it says .NET 9 and references a `BlazorApp1` project that no longer exists. Trust this file and the `.csproj` files instead.)

## Commands

```bash
dotnet build QuickGrid.Toolkit.slnx          # build everything
dotnet run --project src/QuickGrid.Samples    # run the demo app
```

There is **no test project** in this repo — do not look for or invent a test runner.

## Core architecture

The whole library revolves around describing columns as data, then translating that data into QuickGrid components at render time.

**`DynamicColumn<TGridItem>`** (`Columns/DynamicColumn.cs`) is the central data model. It is a plain class (NOT a QuickGrid `ColumnBase` — QuickGrid columns carry `[Parameter]` attributes that prevent inheriting from them, see the comment in the file). It holds everything needed to render one column: `Property` expression (always `Func<TGridItem, object?>`), `ColumnType` (the QuickGrid component type to emit), `Title`/`FullTitle`, `Visible`, `Align`, `Format`, `Class`, `CalculateTotal`, `OnActionAsync`, etc.

**`ColumnManager<TGridItem>`** (`ColumnManager.cs`) is the public API surface — the object callers build up. It owns `List<DynamicColumn>` plus `List<FooterColumn>`. Its many `Add*` methods (`Add`, `AddSimple`, `AddNumber`, `AddStyledNumber`, `AddAction`, `AddTickColumn`, `AddToggleColumn`, `AddImageColumn`, `AddTemplateColumn`, `AddIndexColumn`, `AddMarkup`, `AddFooterColumn*`) are the intended entry points.
- `Add(...)` is the single funnel: it assigns sequential `Id` and derives `Title`/`PropertyName` from the expression when absent. **Always route additions through `Add` — never `Columns.AddRange` directly** (the XML doc on `AddRange` spells out why: it would bypass ID/title initialization).
- `Get()` returns only `Visible` columns.

**`ColumnBuilder<TGridItem>`** (`Builders/ColumnBuilder.cs`) constructs the `DynamicColumn` instances and, crucially, builds each column's `ChildContent` `RenderFragment` by compiling the property expression and emitting cells via `RenderTreeBuilder`. This is where formatting, cell styling (`CellStyleMap`), click handlers, and markup rendering are wired. New column flavors are added here, then surfaced through a `ColumnManager.Add*` method.

**`QuickGridColumns.Columns(columnManager)`** (`QuickGridColumns.razor`) is the renderer: a static `RenderFragment` that loops visible columns and `switch`es on `ColumnType` to emit the matching real component (`EmptyColumn`, `ImageColumn`, `TickColumn`, `ToggleColumn`, `TemplateColumn`, or fallback `PropertyColumn`). When you add a new `ColumnType`, you must add a branch here too.

### Two consumption patterns
1. **Bare `QuickGrid` + `ColumnManager`** (`UsersGrid.razor`): caller owns the `<QuickGrid>` markup and drops in `@QuickGridColumns.Columns(_columnManager)`, plus an optional `<ColumnSelector>` for show/hide UI. Caller calls `grid.RefreshDataAsync()` on selection change.
2. **`QuickGridWrapper<TGridItem>`** (`QuickGridWrapper.razor` + `.razor.cs`): a batteries-included component that encapsulates the grid, toolbar, search box, column selector, pagination, and footer. You pass `Items` (in-memory `IQueryable`) and a configured `ColumnManager`.

### QuickGridWrapper specifics worth knowing
- **In-memory vs EF search**: `FilterCriteria` being `null` means in-memory quick search via `QuickSearchUtility` (reflection over public properties, with a property-info cache and configurable depth/exact-match). Non-null `FilterCriteria` switches to EF-translatable expression filtering against `Items`.
- **`ItemsVersion`**: callers must increment this `long` whenever the `Items` collection mutates — it's how the wrapper detects changes to invalidate search caches and refresh footer totals.
- **Footer totals are rendered via JS interop, not Blazor.** `AddOrUpdateFooterAsync` and `RefreshColumnTitlesAsync` call `JS.InvokeVoidAsync("addOrUpdateFooter", ...)` and `"setColumnTitles", ...`. These JS functions are **not defined in this repo** and only fire when the wrapper's `Id` parameter is set; the host app must provide them. The interop is wrapped in try/catch and logged as non-critical, so a missing function degrades gracefully rather than crashing.
- `IQuickGridIconProvider` is resolved lazily from DI; if unregistered it falls back to `DefaultQuickGridIconProvider`. Override it to swap out Bootstrap Icons.

## Conventions

- File-scoped namespaces; one public type per file; nullable + implicit usings enabled. Library-wide usings live in `GlobalUsings.cs` (e.g. `Microsoft.AspNetCore.Components.QuickGrid`, `System.Linq.Expressions`) — new files inherit them.
- Components use `.razor` + code-behind `.razor.cs` partial classes for anything non-trivial (see `QuickGridWrapper`).
- Property expressions are normalized to `Expression<Func<TGridItem, object?>>` via `ExpressionHelper.ConvertToObjectExpression`; property names are extracted with `ExpressionHelper.GetPropertyName` / `GetSafePropertyName`.
- The library leans on raw `RenderTreeBuilder` and string-built HTML cells (footers) rather than reflection-heavy abstractions — match that style.

## Known issues / sharp edges (from README and code comments)

- The `Format` property does **not** work for `object`-typed columns — passing `Format="@col.Format"` to the fallback `PropertyColumn` in `QuickGridColumns.razor` crashes Blazor, so it's intentionally omitted (formatting is instead applied inside `ColumnBuilder`'s `ChildContent`).
- `QuickGridColumns.GetActionColumn` is non-functional (render-handle error with `@onclick` in a static method) — use the `AddAction` builder path instead.
- Several `// ToDo` markers flag intended refactors (e.g. merging the `AddNumber`/`AddStyledNumber` overloads, renaming `AddSimpleDate` to `AddDate`, supporting both `Items` and `QueryableItems`). Treat these as direction, not done work.
