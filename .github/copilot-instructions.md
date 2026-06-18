# Copilot instructions for QuickGrid.Toolkit

These repository instructions guide GitHub Copilot (and other AI assistants) when working in the QuickGrid.Toolkit library and its demo app.

## Project overview

`QuickGrid.Toolkit` extends the official Blazor [QuickGrid](https://aspnet.github.io/quickgridsamples/) with runtime, data-driven column management: dynamically built columns, a show/hide column-selection UI, custom column types, in-memory quick search, and footer totals. The main use case is rendering the same data type across multiple grids while exposing different visible columns per grid.

The solution (`QuickGrid.Toolkit.slnx`) contains two projects:

- `src/QuickGrid.Toolkit` — the reusable library. SDK `Microsoft.NET.Sdk.Razor`; ships static web assets (`wwwroot/app.css` served as `_content/QuickGrid.Toolkit/app.css`).
- `src/QuickGrid.Samples` — a demo Blazor Web App using Interactive Server render mode, showcasing the toolkit. The `Users*` pages (`Pages/UsersGrid.razor`, `Pages/UsersGridWrapper.razor`) are the reference usage patterns.

There is no separate `BlazorApp1` project, no domain/infrastructure layering, and no `DbContext` in this repository. Sample data comes from an in-memory `UserService`.

## Target framework & dependencies

- **.NET 10** for both projects.
- Library depends on `Microsoft.AspNetCore.Components.Web` and `Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter`. EF Core APIs (e.g. `ToListAsync`) are used in `QuickGridWrapper` to support EF-backed grids in consuming apps, but no EF model or `DbContext` exists in this repo.
- Frontend: Blazor with minimal JS interop. Bootstrap 5 + Bootstrap Icons (or a custom `IQuickGridIconProvider`) are expected in the host app.

## Build & run

- Build: `dotnet build QuickGrid.Toolkit.slnx`
- Run the demo: `dotnet run --project src/QuickGrid.Samples`
- There is **no test project** — do not assume one exists.

## How the toolkit works (essentials)

Columns are described as data, then translated into QuickGrid components at render time:

- `ColumnManager<TGridItem>` (`ColumnManager.cs`) is the public API callers build up via its `Add*` methods. Every addition must flow through `Add(...)`, which assigns the sequential `Id` and derives `Title`/`PropertyName` from the expression. Never call `Columns.AddRange` directly (it bypasses that initialization).
- `DynamicColumn<TGridItem>` (`Columns/DynamicColumn.cs`) is the per-column data model (deliberately not a QuickGrid `ColumnBase`).
- `ColumnBuilder<TGridItem>` (`Builders/ColumnBuilder.cs`) compiles the property expression and builds each column's `ChildContent` via `RenderTreeBuilder`. New column flavors are implemented here.
- `QuickGridColumns.Columns(columnManager)` (`QuickGridColumns.razor`) renders visible columns, switching on `ColumnType` to emit the matching component. A new `ColumnType` needs a branch here too.
- `QuickGridWrapper<TGridItem>` is the batteries-included component (grid + toolbar + search + column selector + pagination + footer).

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
