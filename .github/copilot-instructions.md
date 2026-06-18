# Copilot instructions for QuickGrid.Toolkit

These repository instructions guide GitHub Copilot (and other AI assistants) when working in the QuickGrid.Toolkit library and its demo app.

## Project overview

`QuickGrid.Toolkit` extends the official Blazor [QuickGrid](https://aspnet.github.io/quickgridsamples/) with runtime, data-driven column management: dynamically built columns, a show/hide column-selection UI, custom column types, in-memory quick search, and footer totals. The main use case is rendering the same data type across multiple grids while exposing different visible columns per grid.

The solution (`QuickGrid.Toolkit.slnx`) contains two projects:

- `src/QuickGrid.Toolkit` — the reusable library. SDK `Microsoft.NET.Sdk.Razor`; ships static web assets (`wwwroot/app.css` served as `_content/QuickGrid.Toolkit/app.css`).
- `src/QuickGrid.Samples` — a demo Blazor Web App using Interactive Server render mode, showcasing the toolkit. The `Users*` pages (`Pages/UsersGrid.razor`, `Pages/UsersGridWrapper.razor`) are the reference usage patterns.

There is no domain/infrastructure layering and no `DbContext` in this repository — sample data comes from an in-memory `UserService`.

## Target framework & dependencies

- **.NET 10** for both projects.
- Library depends on `Microsoft.AspNetCore.Components.Web` and `Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter`. EF Core APIs (e.g. `ToListAsync`) are used in `QuickGridWrapper` to support EF-backed grids in consuming apps, but no EF model or `DbContext` exists in this repo.
- Frontend: Blazor with minimal JS interop. Bootstrap 5 + Bootstrap Icons (or a custom `IQuickGridIconProvider`) are expected in the host app.

## Build & run

- Build: `dotnet build QuickGrid.Toolkit.slnx`
- Run the demo: `dotnet run --project src/QuickGrid.Samples`
- There is **no test project** — do not assume one exists.

## Architecture

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
- **Footer totals and column titles are rendered via JS interop, not Blazor.** `AddOrUpdateFooterAsync` / `RefreshColumnTitlesAsync` call `addOrUpdateFooter` / `setColumnTitles` from the toolkit's own ES module `wwwroot/quickGridToolkit.js` (imported on demand via `InvokeModuleVoidAsync` from `_content/QuickGrid.Toolkit/quickGridToolkit.js` — no host-app script setup needed). They only run when the wrapper's `Id` is set, because the QuickGrid `<table>` is rendered with that `id`. Interop is try/caught and logged as non-critical, so failures degrade gracefully.
- `IQuickGridIconProvider` is resolved lazily from DI; if unregistered it falls back to `DefaultQuickGridIconProvider`. Override it to swap out Bootstrap Icons.

## Coding style & conventions

- Prefer modern C#: file-scoped namespaces, pattern matching, collection expressions (`[]`), primary constructors where suitable.
- One public type per file; avoid `#region`; write self-explanatory code.
- Async-first: `async`/`await`; pass `CancellationToken` for I/O; avoid sync-over-async.
- Library-wide `global using`s live in `GlobalUsings.cs` — rely on them rather than re-importing.
- Components: `.razor` markup with code-behind `.razor.cs` partial classes for anything non-trivial (see `QuickGridWrapper`).
- Use QuickGrid / QuickGrid.Toolkit for data grids; do not introduce other grid libraries. Keep JS interop minimal and prefer native Blazor/.NET patterns.
- Prefer constructor injection; register new services via `IServiceCollection` (use extension methods for cross-cutting concerns). The toolkit resolves `IQuickGridIconProvider` lazily from DI and falls back to a default if unregistered.

## Best practices

- Avoid premature abstraction; wait for proven duplication.
- Favor explicitness over reflection-heavy patterns unless already established (the toolkit deliberately uses compiled expressions and `RenderTreeBuilder` over reflection in hot paths).

## Change scope (important)

When asked for a refactor or improvement:

- If you spot minor related cleanups nearby, list them as suggestions with rationale and effort — do not apply them unasked.
- Only proceed with broader refactors after explicit confirmation.
- Keep unrelated formatting or XML-doc churn out of focused changes.
- If a fix reduces risk (obvious bug, disposal, async correctness), flag its severity clearly.

## Maintenance

Keep this document accurate. Update it when the solution structure changes, when target frameworks shift, or when new patterns are adopted; remove guidance that no longer matches the code.
