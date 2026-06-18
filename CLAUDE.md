# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Start here

Read [`.github/copilot-instructions.md`](.github/copilot-instructions.md) first — it covers the project overview, target framework (.NET 10), build/run commands, coding conventions, and change-scope rules. This file does **not** repeat those; it adds the deeper architecture detail and the sharp edges that aren't obvious from reading any single file.

Quick reminders that matter most often:
- Build: `dotnet build QuickGrid.Toolkit.slnx` · Run demo: `dotnet run --project src/QuickGrid.Samples`
- There is **no test project** — don't look for or invent a test runner.

## Architecture

The full architecture lives in [`.github/copilot-instructions.md`](.github/copilot-instructions.md) — the column-as-data pipeline (`ColumnManager` → `DynamicColumn` → `ColumnBuilder` → `QuickGridColumns`), the two consumption patterns, and the `QuickGridWrapper` specifics (in-memory vs EF search, `ItemsVersion`, JS-interop footers, icon-provider DI). Read that section before changing column rendering or the wrapper. The gotchas below are the failure modes that aren't visible from the code alone.

The footer/column-title JS interop is self-contained: the wrapper imports `_content/QuickGrid.Toolkit/quickGridToolkit.js` (source: `src/QuickGrid.Toolkit/wwwroot/quickGridToolkit.js`) on demand. It runs only when the wrapper's `Id` is set. The three sample pages under `src/QuickGrid.Samples/Pages` (`UsersGrid`, `UsersGridWrapper`, `TotalFooterExample`) are the canonical, working usage references and share a header via the `ExampleInfo` component.

## Known issues / sharp edges (from README and code comments)

- The `Format` property does **not** work for `object`-typed columns — passing `Format="@col.Format"` to the fallback `PropertyColumn` in `QuickGridColumns.razor` crashes Blazor, so it's intentionally omitted (formatting is applied inside `ColumnBuilder`'s `ChildContent` instead).
- `QuickGridColumns.GetActionColumn` is non-functional (render-handle error with `@onclick` in a static method) — use the `AddAction` builder path instead.
- Several `// ToDo` markers flag intended refactors (merging the `AddNumber`/`AddStyledNumber` overloads, renaming `AddSimpleDate` to `AddDate`, supporting both `Items` and `QueryableItems`). Treat these as direction, not done work.
