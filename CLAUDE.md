# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Start here

Read [`.github/copilot-instructions.md`](.github/copilot-instructions.md) first — it covers the project overview, target framework (.NET 10), build/run commands, coding conventions, and change-scope rules. This file does **not** repeat those; it adds the deeper architecture detail and the sharp edges that aren't obvious from reading any single file.

Quick reminders that matter most often:
- Build: `dotnet build QuickGrid.Toolkit.slnx` · Run demo: `dotnet run --project src/QuickGrid.Samples`
- There is **no test project** — don't look for or invent a test runner.

## Core architecture

The library describes columns as data, then translates that data into QuickGrid components at render time. Four types form the pipeline:

**`DynamicColumn<TGridItem>`** (`Columns/DynamicColumn.cs`) — the per-column data model. It is a plain class, NOT a QuickGrid `ColumnBase`: QuickGrid columns carry `[Parameter]` attributes that prevent inheriting from them (see the comment in the file). Holds `Property` (always normalized to `Func<TGridItem, object?>`), `ColumnType` (the component to emit), `Title`/`FullTitle`, `Visible`, `Align`, `Format`, `Class`, `CalculateTotal`, `OnActionAsync`, etc.

**`ColumnManager<TGridItem>`** (`ColumnManager.cs`) — the public API surface callers build up. Owns `List<DynamicColumn>` plus `List<FooterColumn>`. Entry points are the `Add*` methods (`Add`, `AddSimple`, `AddNumber`, `AddStyledNumber`, `AddAction`, `AddTickColumn`, `AddToggleColumn`, `AddImageColumn`, `AddTemplateColumn`, `AddIndexColumn`, `AddMarkup`, `AddFooterColumn*`).
- `Add(...)` is the single funnel: assigns sequential `Id`, derives `Title`/`PropertyName` from the expression when absent. **Always route additions through `Add` — never `Columns.AddRange` directly** (its XML doc explains why: it bypasses ID/title init).
- `Get()` returns only `Visible` columns.

**`ColumnBuilder<TGridItem>`** (`Builders/ColumnBuilder.cs`) — constructs `DynamicColumn` instances and builds each column's `ChildContent` `RenderFragment` by compiling the property expression and emitting cells via `RenderTreeBuilder`. Formatting, cell styling (`CellStyleMap`), click handlers, and markup rendering are wired here. New column flavors start here, then get surfaced through a `ColumnManager.Add*` method.

**`QuickGridColumns.Columns(columnManager)`** (`QuickGridColumns.razor`) — the renderer: a static `RenderFragment` that loops visible columns and `switch`es on `ColumnType` to emit the matching real component (`EmptyColumn`, `ImageColumn`, `TickColumn`, `ToggleColumn`, `TemplateColumn`, or fallback `PropertyColumn`). **Adding a new `ColumnType` requires a branch here too.**

### Two consumption patterns
1. **Bare `QuickGrid` + `ColumnManager`** (`Pages/UsersGrid.razor`): caller owns the `<QuickGrid>` markup and drops in `@QuickGridColumns.Columns(_columnManager)`, plus an optional `<ColumnSelector>`. Caller calls `grid.RefreshDataAsync()` on selection change.
2. **`QuickGridWrapper<TGridItem>`** (`QuickGridWrapper.razor` + `.razor.cs`): batteries-included — encapsulates grid, toolbar, search box, column selector, pagination, and footer. Pass `Items` (in-memory `IQueryable`) and a configured `ColumnManager`.

### QuickGridWrapper specifics worth knowing
- **In-memory vs EF search**: `FilterCriteria == null` → in-memory quick search via `QuickSearchUtility` (reflection over public properties, property-info cache, configurable depth/exact-match). Non-null `FilterCriteria` → EF-translatable expression filtering against `Items` (`ToListAsync`).
- **`ItemsVersion`**: callers must increment this `long` whenever the `Items` collection mutates — it's how the wrapper detects changes to invalidate search caches and refresh footer totals.
- **Footer totals and column titles are rendered via JS interop, not Blazor.** `AddOrUpdateFooterAsync` / `RefreshColumnTitlesAsync` call `JS.InvokeVoidAsync("addOrUpdateFooter", ...)` and `"setColumnTitles", ...`. These JS functions are **not defined in this repo** and only fire when the wrapper's `Id` parameter is set; the host app must provide them. The interop is try/caught and logged as non-critical, so a missing function degrades gracefully.
- `IQuickGridIconProvider` is resolved lazily from DI; if unregistered it falls back to `DefaultQuickGridIconProvider`. Override it to swap out Bootstrap Icons.

## Known issues / sharp edges (from README and code comments)

- The `Format` property does **not** work for `object`-typed columns — passing `Format="@col.Format"` to the fallback `PropertyColumn` in `QuickGridColumns.razor` crashes Blazor, so it's intentionally omitted (formatting is applied inside `ColumnBuilder`'s `ChildContent` instead).
- `QuickGridColumns.GetActionColumn` is non-functional (render-handle error with `@onclick` in a static method) — use the `AddAction` builder path instead.
- Several `// ToDo` markers flag intended refactors (merging the `AddNumber`/`AddStyledNumber` overloads, renaming `AddSimpleDate` to `AddDate`, supporting both `Items` and `QueryableItems`). Treat these as direction, not done work.
