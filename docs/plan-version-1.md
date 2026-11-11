# Plan — Version 1.x (non-breaking improvements)

The detail behind the queue. **Open first: [now.md](now.md)** — one line per remaining task, in order, linking
back here. This file holds the constraints every 1.x change must respect, the reasoning behind each open item, and
a short record of what landed and the decisions it left behind. Companion: [plan-version-2.md](plan-version-2.md)
(breaking changes for v2.0).

Sources: [GitHub issue #7](https://github.com/VaclavElias/QuickGrid.Toolkit/issues/7) (full architecture review — read it
first, this plan builds on it and does not repeat its analysis), plus an August 2026 usage analysis of the two
production consumers (both consume the toolkit by **project reference**, so source compatibility is what matters;
binary compatibility does not).

- **Twenty360.Web** (`D:\Projects\twentyAI\Twenty360\Twenty360.Web`): ~150 grid instances in 62 razor files, all via an
  `AppQuickGrid : QuickGridWrapper<T>` subclass; 35 app-side `ColumnManager` extension methods (~1000 lines);
  saved views persisted in EF and matched by `FullTitle`; manual footer loops with live copy-paste bugs;
  `ItemsVersion` is their #1 ergonomic complaint; uses raw QuickGrid where virtualization is needed.
  Call sites: `AddSimple` 455 · `AddStyledNumber` ~339 · `AddNumber` 183 · `AddIndexColumn` 112 · `AddAction` 84 ·
  `AddSimpleDate` 68 · `AddTickColumn` 52 · `AddTemplateColumn` 12 · `AddFooterColumn` 15.
- **TwentyAi.Invoicing** (`D:\Projects\twentyAI\TwentyAi.Invoicing`): 201 `Add*` call sites in 12 files, same
  subclass pattern (`AppQuickGrid`); always in-memory data (`List.AsQueryable()`), never EF; one `TotalFooter`
  instance shared by 5 grids; `AddTickColumn2` extension exists only to supply default tick icon classes;
  toggle `onChange` handler flips the bound value itself; `ColumnManager.Columns` is cleared and rebuilt after
  first render on one page; grid `Id`s are duplicated across different pages.

Verification loop for every item: `dotnet build QuickGrid.Toolkit.slnx` (warning-clean) → `dotnet test` → run
`src/QuickGrid.Samples` and check the affected example pages → build both consumer projects:

```bash
dotnet build D:\Projects\twentyAI\Twenty360\Twenty360.Web\Twenty360.Web.csproj
dotnet build D:\Projects\twentyAI\TwentyAi.Invoicing\src\TwentyAi.Invoicing\TwentyAi.Invoicing.csproj
```

---

## Frozen API constraints (do not violate in 1.x)

These are hard rules derived from real consumer code. Any 1.x change must respect all of them.

1. **Parameter names AND order** of `AddSimple`, `AddNumber`, `AddStyledNumber`, `AddAction`, `AddSimpleDate`,
   `AddTickColumn` are frozen. Hundreds of call sites pass `title`/`fullTitle` positionally; Invoicing's
   `AddTickColumn2` passes `trueClass`, `falseClass`, `showOnlyTrue` **positionally after `visible:`** — inserting a
   parameter there compiles but silently binds the wrong icons. New parameters go at the end only, and remember
   optional-parameter additions are still source-compatible but change overload resolution — build both consumers.
2. **`QuickGridWrapper<T>` subclassing surface**: stays non-sealed, parameterless ctor, `virtual OnInitializedAsync`
   callable via `base`, and `Class`, `Theme`, `IsExportEnabled`, `IsColumnItemsSelection`, `DownloadFileName`,
   `Events` settable from a subclass ctor / `OnInitializedAsync`. `Events.OnManageColumns` stays `EventCallback?`
   (consumers null-check it); the other five callbacks stay non-nullable `EventCallback` (consumers use `HasDelegate`).
3. **`ColumnManager.Columns` stays a public mutable `List<>`** that the wrapper re-reads on render — Invoicing
   clears and rebuilds it after first render (`EmployeeViewModel`). No one-time snapshotting, no `IReadOnlyList`.
4. **`ToggleColumn` must never write the bound property before invoking `onChange`** — Invoicing's handler flips the
   value itself; a toolkit-side write means a double toggle with no compile error.
5. **`TotalFooter` must stay free of per-grid mutable state** — Invoicing shares one instance across 5 grids on one page.
   The same rule now covers `QuickSearchOptions` passed through `SearchOptions` (D15): the wrapper copies, never writes.
6. **No uniqueness requirement on `Id`** — consumers reuse the same `Id` string on different pages. The JS interop
   must keep tolerating absent/re-created elements (grids inside conditional panels).
7. **CSS class names and asset paths are contracts**: `_content/QuickGrid.Toolkit/app.css`,
   `_content/QuickGrid.Toolkit/quickGridToolkit.js`, and classes `table-index`, `table-fit`, `table-thead-sticky`,
   `table-no-empty-lines`, `table-bg-transparent`, `index-column`, `no-index`, `true-sign`, `false-sign`, `action`,
   `quick-search`, plus the `td span[content=...]` selector contract and `Theme` attribute handling. Both consumers
   deleted their local CSS copies and depend on the toolkit's. The *selector* is the contract, not the palette: D12
   removed the toolkit's default `negative`/`positive`/`zero` colours, leaving applications to supply their own.
8. **`IQuickGridIconProvider` consumers throw on unknown enum members** (Twenty360's provider ends with
   `_ => throw new NotImplementedException()`). ~~Never let a new `QuickGridIcon` member reach a consumer provider
   unguarded~~ — **lifted by B7**: the wrapper now renders every icon through `ResilientQuickGridIconProvider`, so a
   provider that does not handle a member falls back to the default glyph instead of crashing. New members are safe
   to add; the constraint is kept here as the reason the guard exists. See also B4.
9. **Defaults are behaviour**: `AddNumber` format `"N0"`, `AddSimpleDate` format `"dd/MM/yyyy"`, `AddToggleColumn`
   class `"text-center"`, `ItemsPerPage` 20, `QuickSearchOptions.MaxSearchDepth` 1. Changing a default is a silent
   behaviour change across hundreds of grids. Lesson from `MaxSearchDepth` going 1 → 2 → 1 in August 2026 (a grid
   needed depth 2 and the library default was the only lever until D15 made it per-grid): both moves left the
   suite red on `main`, because the depth boundary was pinned from one side only. `QuickSearchUtilityTests` now
   keeps a case on **both** sides of every level, so a default that moves fails a test whose name says so — do the
   same for any other default you change.

---

## Design goal — shrink the `AppQuickGrid` burden

Both production consumers wrote an `AppQuickGrid<T> : QuickGridWrapper<T>` subclass *before* the toolkit was
comfortable to use, and both wrote similar things in it. The subclass seam itself is correct and stays (it is even
a documented example page, `app-quickgrid`) — but roughly half of what sits in those classes today is there
because the toolkit does not provide something, not because the app is customizing anything. The target: a new
consumer can use `QuickGridWrapper` directly, and reaches for a subclass only for genuinely app-specific
behaviour.

**Stays in the app — do not move into the toolkit.** The auth-policy-driven `IsExportEnabled`, `WarningRequested`
→ the app's toast service, export activity logging, and the persistence backend for saved views (EF entity, user
identity, SignalR). Moving any of these would drag app concepts into a grid library.

Styling values (`Theme = "twentyAI"`, the app's `Class` string) are app-specific too and stay app-owned — D3
changes only *where they are declared* (once at DI registration instead of in a subclass constructor), never who
owns them.

**Toolkit gaps that only look like customization — absorb these:**

| Boilerplate in `AppQuickGrid` today | Fixed by |
|---|---|
| `Events ??= new()` plus mixed `HasDelegate` / `is null` checks | **D9** (the `is null` check exists only because `OnManageColumns` is `EventCallback?` while the rest are not) |
| `GetFileName() => DownloadFileName ?? $"{typeof(TGridItem).Name}.csv"` | **D10** |
| Wiring `OnExport`/`OnSelectedColumnsExport` just to get a CSV file | **D10** |
| `ColumnConfig` ↔ EF entity hand-mapping (`ToColumnConfig`); Invoicing left the whole block commented out, arguably because the plumbing cost exceeded the value | **v2 item 5** (`IColumnConfigStore`) |
| Re-implementing how the column-selector dialog opens | **v2 item 5** / review whether the built-in selector covers the common case |
| `AddTickColumn2` extension (in *both* apps) supplying default tick icon classes | **D3** |
| `Class` / `Theme` / `ItemsPerPage` set in the subclass constructor | **D3** (app-wide defaults via DI, so the subclass becomes optional) |

**Precedence rule for any toolkit-supplied default (D3 especially).** Both consumers set `Class` in their subclass
constructor, so a naive "options always apply" implementation would silently override their styling with no
compile error. The order must be, and must be tested:

> explicit markup parameter → subclass constructor value → DI options → built-in default

D15 already solved the hard half of this for `SearchOptions`: a nullable parameter is what makes "unset" distinct
from "set to the default", the resolved value is a per-render copy, and change detection compares by value so an
instance allocated in markup every render is not a change. Reuse that shape.

**Expected end state.** Invoicing's `AppQuickGrid` drops to about the theme string plus toast wiring; Twenty360's
to theme plus auth plus its saved-view store. Whatever remains is unmistakably app-specific — which is the signal
that the split is right.

Note the 1.x constraint applies throughout: the subclass surface itself (frozen constraint 2) does not change, so
none of this breaks either app as it lands.

## Track A — Foundations

- [x] **A1. Test project.** *(2026-08)* `tests/QuickGrid.Toolkit.Tests` covers the pure logic: `ExpressionHelper`,
  `QuickSearchUtility`, `CellStyleHelper`/`CellStyleMap`, footer arithmetic, and since C2 the extracted `GridSearch`
  and `GridFooterBuilder`. On **xunit.v3** 4.0.1 since 2026-09-21: the project is an executable running on
  Microsoft.Testing.Platform, opted in by the root `global.json` — delete that file and `dotnet test` errors on the
  .NET 10 SDK. `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` are kept for Test Explorer backward
  compatibility, per the xunit migration guide; CI calls `dotnet test --solution`.
- [x] **A2. CI.** *(2026-08)* `.github/workflows/build.yml` — Release build with `/warnaserror`, tests, and a pack
  step to keep packaging valid; nothing is published.
- [x] **A3. NuGet metadata (pack, don't publish).** *(2026-08)* Metadata + `GenerateDocumentationFile` in the csproj,
  `PackageLicenseExpression=MIT`, README packed; `dotnet pack` verified. CS1591/CS1573 stay in `NoWarn` until the XML
  docs are complete. **Still open:** `LICENSE.txt` carries the MIT template placeholders `[year] [fullname]` — fill in
  before any real publish. Publishing is deliberately deferred; consumers stay on project references.
- [x] **A4. `.editorconfig`.** *(2026-08)* Root `.editorconfig` encoding the copilot-instructions style rules.
  `Directory.Build.props` skipped on purpose — one packable project, nothing to centralize yet.
- [x] **A5. Doc drift.** *(2026-08)* README/copilot-instructions/CLAUDE.md pointed at deleted paths. Pages live under
  `src/QuickGrid.Samples.Shared/Pages/Examples/`, registered in `Core/ExampleRegistry.cs`, which is the count to quote.

## Track B — Correctness fixes

- [x] **B1. Dead "Manage Columns" button.** *(2026-08)* `ManageColumns()` tested `Events?.OnResetViewToDefault is null`,
  true only when `Events` itself was null, so supplying `Events` without `OnManageColumns` made the button a no-op.
  Now: invoke `Events.OnManageColumns` when set, else open the built-in selector.
- [x] **B2. `ColumnInfo.PropertyName` dropped.** *(2026-08)* `Add<TValue>(expr, ColumnInfo, ...)` ignored
  `columnInfo.PropertyName`, silently breaking selected-columns export. Now `propertyName ?? columnInfo.PropertyName`,
  matching `AddAction`/`AddStyledNumber`.
- [x] **B3. Unencoded footer HTML.** *(2026-08)* `AddFooterColumn*` built raw `<td>` strings for `tfoot.innerHTML`;
  value and class are now HTML-encoded like the wrapper's own cells. C3 supersedes this entirely.
- [x] **B4. `DefaultQuickGridIconProvider` threw on `QuickGridIcon.ClearSearch`.** *(2026-08)* Mapping added; B7's
  completeness test stops it recurring.
- [x] **B5. `AddRange` shared mutable column instances / `Id` corruption.** *(2026-08-19, issue #7 §5)* `Add` ends
  with `column.Id = Columns.Count`, so adding the same instance to a second manager corrupted the first and shared
  `Visible` across grids. `AddRange` now copies each column in via a new `DynamicColumn.Clone()` built on
  `MemberwiseClone` — chosen because it preserves the runtime type (`TickPropertyColumn` survives; the renderer
  dispatches on it) and cannot go stale when properties are added. `SimpleClone()` delegates to it and has a live
  consumer (Twenty360 `ActivityTrendPage`). The copy is shallow: `Property`, `SortBy`, `ChildContent` and
  `OnActionAsync` are shared, and `ChildContent` closes over the original `Add*` arguments. **The wrapper itself
  deliberately does not clone** (frozen constraint 3). `AddRange` has **no production callers** — both apps share
  columns through `ColumnManager` extension methods, which is the recommended way; see v2 "Review `AddRange`".
  Tests: `ColumnCloneTests.cs`. Sample: F2.
- [ ] **B6. `FilterCriteria` search path robustness.** `SearchTextChanged` (wrapper ~L471-500): `IsLoading` is
  toggled around `await Items.ToListAsync()` without `StateHasChanged()` (spinner never shows), there is no
  try/catch, no debounce/cancellation, and a plain `List.AsQueryable()` combined with `FilterCriteria` throws
  (`ToListAsync` needs `IAsyncQueryProvider`). Fix: `StateHasChanged` around the await, guard non-async providers
  with a clear error (or fall back to sync `ToList`), catch + log. Note: neither consumer uses `FilterCriteria`
  yet, so this path has zero real-world coverage — pair with the new EF sample (F1). `GridSearchTests` pins the
  throw as a known defect (`RunFilterCriteriaSearchAsync_AboveTheMinimum_RequiresAnAsyncQueryProvider`); flip it
  when this lands.
- [x] **B7. Icon-provider resilience.** *(2026-08)* `Core/ResilientQuickGridIconProvider` decorates whichever
  provider is in use; an unhandled icon degrades to the default glyph with a logged warning, and a genuinely unknown
  value draws nothing. Only the `Render` *call* is guarded — a fragment that throws mid-execution cannot be recovered
  without leaving a partial subtree. **This lifted frozen constraint 8.** `IconProviderTests` includes a completeness
  test over every declared `QuickGridIcon` member.

## Track C — Internal architecture (non-breaking; issue #7 items 1, 3, 8, 10)

Public API is unchanged throughout this track.

- [x] **C1. Killed the side-effecting `_filteredItems` property.** *(signed off 2026-08-19, issue #7 §1)* The getter
  recomputed the search, mutated four caches and raised `SearchResultChanged` mid-render. Replaced by a pure
  `VisibleItems => _search.Result ?? Items`, one writer (`Recompute`) and one place that recomputes *and* raises
  the event (`RefreshSearchResultAsync`). **Load-bearing rule:** a changed `Items` *reference* does not trigger a
  recompute — `SearchFiltering.razor` allocates a new queryable every render, and recomputing on it would raise the
  event every render and loop with any handler that calls `StateHasChanged`. `ItemsVersion`/`RefreshDataAsync` are
  the signal for changed data. Two accepted behaviour changes, do not "restore": toggling exact match re-runs an
  active search, and `SearchResultChanged` also fires on an `ItemsVersion` bump with no search active.
- [x] **C2. Extracted collaborators from the wrapper.** *(2026-08-19, issue #7 §3)* `Core/GridSearch<T>` owns the
  query, options and result but raises no events and touches no rendering; `Core/GridFooterBuilder<T>` is a
  stateless `Build(columns, totalFooter, items)` → HTML function (the `_lastRenderedFooter` cache is a JS-interop
  concern and stays in the component). Both are covered by ordinary xUnit tests — the coverage the no-bUnit
  decision was missing, including cell-for-cell footer alignment. **Regression found in Twenty360 and fixed:** the
  first cut had `GridSearch` snapshot the item source in `SyncInputs`, but Blazor renders a component *before*
  `OnParametersSetAsync` has run whenever `OnInitializedAsync` returns an incomplete task — which both consumers'
  `AppQuickGrid` does — so every grid rendered empty. `GridSearch` no longer holds the source; rows are passed in.
  **Lesson for C3 and anything else that moves state out of the component: a collaborator may cache what it
  computes, never what it reads from a parameter.** The samples' `AppQuickGrid` is now async so they can catch this.
- [ ] **C3. Footer as a RenderFragment** (issue #7 §8). Today footer cells are hand-built HTML strings pushed via
  `tfoot.innerHTML`. Render the `<tfoot>` content as Blazor markup in a hidden container and have
  `quickGridToolkit.js` **move the node** into the table instead of assigning `innerHTML`. Blazor owns the content
  (encoding automatic, interactive footers become possible), and the footer cache dance shrinks. Makes B3 obsolete.
- [ ] **C4. Compiled accessors in `QuickSearchUtility`** (issue #7 §10). `property.GetValue(item)` runs per
  property × row × search term. Cache compiled `Func<object, object?>` getters next to the existing
  `PropertyInfo[]` cache. Self-contained, ~10-50× on the hottest path. Cover with A1 tests first. Two smaller
  inefficiencies from #6 belong with it: the filtered-property array allocated per item per term when
  `ColumnNames`/`ExcludedColumns` are set (reachable since D15), and `MatchesQuery` calling `ToString()` on values
  that are already strings.
- [x] **C5. Search parsed the query once per row.** *(2026-08-21)* `QuickSearch` split and deduped the query on every
  call, inside `items.Where(...)`. Split into internal `PrepareTerms(query, options)` + `Matches(item, terms, options)`;
  `GridSearch` parses once per run, and `Matches` uses a plain loop instead of `All`/`Any` closures. Public overloads
  delegate to the pair, pinned by an equivalence test. Found while triaging #6, which had blamed the `PropertyInfo[]`
  cache; that was not the cost — this was. C4 is still the big one.

## Track D — Non-breaking API additions

- [x] **D1. `Add*` returns the created column.** *(2026-08-21)* All 18 helpers return `DynamicColumn<TGridItem>`,
  with the subtype preserved (`AddTickColumn` returns the `TickPropertyColumn` the renderer dispatches on);
  `Add(DynamicColumn?)` is null-in/null-out; `AddRange` and `AddFooterColumn*` stay `void`. Source-compatible for
  all ~1,300 consumer call sites — only a method-group conversion to `Action` would break, and none exists.
  `ColumnReturnValueTests` calls every helper on one manager and asserts the returned list is reference-equal to
  `Columns`. Unblocks per-column options without touching the frozen signatures.
- [ ] **D2. Stable column `Key`** (issue #7 §6, phase 1). Add `string Key` to `DynamicColumn`, defaulting to
  `PropertyName` (fallback `FullTitle`). Saved-view matching (`SetColumnVisibility`, `SelectView`,
  `ColumnSelector`) matches by `Key` first, then falls back to `FullTitle` so existing persisted configs keep
  working. Full cut-over (footers, export, persistence format) is a v2 item. Beware: `GetSafePropertyName`'s
  hash fallback for computed expressions is not stable across processes — such columns need an explicit `Key`.
- [ ] **D3. `QuickGridToolkitOptions` via DI** (`services.AddQuickGridToolkit(o => ...)`), resolved lazily like the
  icon provider, all values defaulting to today's behaviour: default tick `TrueClass`/`FalseClass` (both consumers
  wrote an `AddTickColumn2` extension solely for this), default date format, default `ItemsPerPage`, default table
  `Class`. `ColumnManager` needs access — simplest is an optional static/ambient default plus a per-wrapper
  parameter; decide during implementation, but do not add a required constructor parameter to `ColumnManager`
  (both consumers construct it with `new()`). The precedence chain and its "unset vs default" problem are solved
  in D15 — reuse `bool?`/nullable parameters, per-render resolved copy, value comparison. The genuinely open part:
  the tick defaults are read inside a `ColumnBuilder` closure at `Add*` time, in page code that never sees DI
  (the wall D12 hit). Decide the delivery route — ambient static default is the obvious candidate and the one most
  likely to bite — before writing code.
- [ ] **D4. `SearchVisibleColumnsOnly` parameter** (issue #7 §9). Populate `QuickSearchOptions.ColumnNames` from
  visible columns' `PropertyName`. Default **false** in 1.x (today's behaviour: search hits hidden columns);
  flipping the default is a v2 item. Note `addToContent: true` columns (Invoicing uses them as hidden-but-
  searchable) must keep working — searching by rendered content is content-based, not property-based.
  After D15 this is one line writing `ColumnNames` onto the resolved `SearchOptions` baseline; value comparison of
  the list contents already exists, so hiding a column is noticed. Settle the naming hazard while here:
  `ExcludedColumns` (which *properties* are searched) and an excluded *term* (D14, which rows survive) are two
  unrelated meanings of "excluded" in one options object.
- [ ] **D5. `ResetSortAsync()`** public method on the wrapper. Twenty360 unmounts the entire grid
  (`_showGrid = false`) because there is no way to clear sort state. QuickGrid exposes per-column
  `SortByColumnAsync`; investigate the cleanest reset (may need to track the last sorted column).
- [ ] **D6. Declarative footer generation.** Twenty360 hand-writes three ~60-line loops walking
  `Columns` → `AddFooterColumn(column.Id, ...)` per column, and two of them contain live copy-paste bugs (wrong
  manager captured). Provide `ColumnManager.BuildFooterFromColumns(Func<DynamicColumn<T>, FooterCell?> selector)`
  (or similar) that owns the iterate/skip/label/sum mechanics driven by column metadata (`IsNumeric`,
  `CalculateTotal`, `Format`, `Class`), with per-column overrides for label cells and custom aggregations
  (their loops filter rows before summing — the aggregation delegate must receive the items).
- [ ] **D7. Post-data/render hook.** Both consumers use `await Task.Delay(20/100)` to wait for the grid to settle
  (column-selector open, footer recompute after visibility change). Add a reliable extension point — e.g.
  `EventCallback OnGridReady` fired after first successful render + JS init, and ensure
  `AddOrUpdateFooterAsync`/`RefreshColumnTitlesAsync` are safe to call at any time (queue until ready).
- [ ] **D8. Obsolete the dead API** (verified unused by both consumers; removal happens in v2):
  `QuickGridColumns.GetActionColumn` (documented broken), `QuickGridColumns.MarkupColumn`,
  `TogglePropertyColumn`, `ColumnBuilder.BuildConditionalActionColumn` (already `[Obsolete]` — add the removal
  note), `QuickGridWrapper.IsTableIndex()` (duplicate of `GetTableClass`). Delete outright (not public API /
  already dead): commented-out blocks in `EmptyColumn.cs`, `SelectedColumn.cs`, wrapper L253/503,
  `wwwroot/background.png`, the stale `// ToDo: Rename to AddDate()` comment (rename rejected — 60+ call sites,
  pure churn), and the `// ToDo` about `Items`/`QueryableItems` (moved to plan-version-2.md).
- [ ] **D9. Complete `QuickGridWrapperEvents`.** The `Events` object was introduced to consolidate wrapper
  callbacks but the job was left half-done: `ColumnSelectionChanged` and `SearchResultChanged` still live as
  standalone `[Parameter]`s (one consumer usage each — Twenty360's `AppQuickGrid` re-invokes
  `ColumnSelectionChanged`; `SearchResultChanged` has a single placeholder handler). Add `OnColumnSelectionChanged`
  and `OnSearchResultChanged` members to `QuickGridWrapperEvents`; the wrapper invokes both the new member and the
  old parameter, and the old parameters get `[Obsolete]` pointing at `Events` (removal is a v2 item).
  **Constraint:** `QuickSearchChanged` and `ExactMatchChanged` must stay standalone parameters forever —
  `@bind-QuickSearch`/`@bind-ExactMatch` require component-level `XChanged` parameters, and Invoicing uses both
  bind forms. Document that split on the `Events` class ("binding pairs stay as parameters; everything else lives
  here") so future callbacks land in the right place.
- [ ] **D10. Export that works out of the box.** Today the wrapper only *raises* `OnExport` /
  `OnSelectedColumnsExport`; writing the file is entirely the host's problem. Both consumers route those events to
  the **same** shared `ExportService` in a sibling library — evidence that the capability sits one level too high,
  since turning rows into CSV and handing the browser a download is generic. Ship a default CSV writer (build the
  text, trigger the download through the existing `quickGridToolkit.js` module) that runs **only when the
  corresponding event has no delegate**, so both apps keep their activity logging by staying wired and nothing
  changes for them. Also default `DownloadFileName` to `$"{typeof(TGridItem).Name}.csv"` (Invoicing wrote a
  `GetFileName()` helper for exactly this). Reuse the existing `ExpandoObjectBuilder` projection for the
  selected-columns path. Upgrades the README's "writing the file is up to the host" to a real out-of-the-box
  feature; add a sample-page section demonstrating the zero-wiring case. See the **Design goal** section above.
- [x] **D11. `ColumnManager.ValueStyles` — opt out of the built-in value colours.** *(2026-08-19)* `[Flags]`
  `GridValueStyles` (`None`/`Negative`/`Positive`/`Zero`/`All`) on the manager, proxied to its `ColumnBuilder`; a
  disabled nature emits no span at all, so nothing has to be unstyled and the v2 `data-content` move cannot break
  it. It lives on the manager, not the wrapper, because the span is baked into `ChildContent` by the builder at
  `Add*` time and the closure reads the builder's field as each cell renders. **Limitation, pinned by a test:** it
  belongs to a column *set* — two grids sharing one manager share it, and `AddRange` copies keep the styling of
  the manager that built them. Tests: `ValueStylesTests.cs`. Sample: checkbox row on `/formatting-styling`.
- [x] **D12. Value colours moved out of the toolkit's stylesheet.** *(2026-08-21)* The toolkit's `app.css` no longer
  colours `td span[content="negative"|"positive"|"zero"]`; the three rules live in the samples' stylesheet as a
  copyable palette and the README has a **Styling values by their nature** section. CSS was chosen over a DI
  option because `ColumnManager` is `new()`'d in page code and never sees DI, the decision sits inside a
  `ColumnBuilder` closure, and CSS gives per-grid scoping for free. The selector stays the contract (constraint 7).
  Not a break for Twenty360 (its own stylesheet defines all three); Invoicing pastes the three rules if it wants
  them back. **Still open:** `BuildSimpleColumn` (`ColumnBuilder.cs:89`) emits a `CellStyleMap` name without the
  `IsStyleEnabled` check that `BuildStyledNumberColumn` applies — one line to align.
- [x] **D13. `RefreshColumnsAsync()` — column changes made outside the wrapper.** *(2026-08-24)* The private
  `OnColumnVisibilityChangedAsync` became public and `ColumnSelector` binds straight to it, so a hand-rolled control
  runs exactly what the toolbar runs. Measured, not assumed: the rendered columns, footer and index column all
  self-heal on a re-render; only the **header tooltips** (latched by `_titlesLoaded`) and **`ColumnSelectionChanged`**
  need the call. **Fixed a live bug in the built-in selector on the way:** titles were pushed inside the event
  handler, before Blazor rendered the new header, so re-showing a column left the last `<th>` with no tooltip. The
  push is now deferred to `OnAfterRenderAsync` by clearing the latch, and `RefreshColumnsAsync` ends with
  `StateHasChanged()` so a caller with no render of its own still gets one. Sample: `/column-visibility`, with a
  checkbox that suppresses the call so the drift can be seen.
- [x] **D14. Exclusion terms in quick search (`-word`).** *(2026-08-25, `b6da0fa`)* `-manager` rejects rows it
  matches; `QuickSearchOptions.EnableExclusionTerms` (default **true**), internal `SearchTerm` record struct, 10
  tests, README row and sample prose. An exclusion vetoes in both operator modes, a query of only exclusions keeps
  everything it does not veto, and `alice -alice` matches nothing. Cheap only because C5 had already moved the parse
  out of the per-row predicate. **Consumer-visible change:** a search for `-500` now excludes instead of finds;
  turn it off per grid via `SearchOptions` (D15).
- [x] **D15. `SearchOptions` parameter — `QuickSearchOptions` reachable per grid.** *(2026-08-26)* Six of the eight
  knobs (`MaxSearchDepth`, `CaseSensitive`, `MultiTermOperator`, `EnableExclusionTerms`, `ColumnNames`,
  `ExcludedColumns`) were unreachable from a grid because `GridSearch` built its options inline. Now
  `[Parameter] QuickSearchOptions? SearchOptions`, resolved by `ResolveSearchOptions()` into a **copy** per
  parameter set (the caller's instance is never written — constraint 5). Decisions that D3 and D4 build on:
  - **Which flat parameters stay flat:** the test is *does the wrapper write it*. `ExactMatch` and `QuickSearch` are
    written by the settings menu and are binding pairs, so they stay and `ExactMatch` overrules
    `SearchOptions.ExactMatch`. `IsNestedSearch` was a pure input, so it moved: now `bool?` + `[Obsolete]`, overriding
    `IncludeChildProperties` only when set. The `bool?` is what makes "unset" distinct from "set to the default" —
    without it the precedence is unimplementable.
  - **Live, compared by value.** Read-once was rejected (it would break the sample's live checkbox, re-open the C2
    first-render hazard, and make D4 impossible). `QuickSearchOptions.ValuesEqual` compares scalars and list
    *contents* (null and empty are the same), with no reference-equality fast path — an in-place edit to a held
    instance must be noticed, and an instance allocated in markup every render must not count as a change.
    Invoicing does exactly the latter (`SearchOptions="Settings.GetSearchOptions()"`, depth 3).
  - **Drift guards:** `SearchOptionsTests` walks every public property of `QuickSearchOptions` by reflection and
    fails if `ValuesEqual` or `Clone` misses one.
  - `MaxSearchDepth` default is back to **1**; grids that need more set it themselves. Sample: `/search-filtering`
    drives a live options object (nesting, depth against `Owner.Office.City`, exclusions).

## Track E — C# 14 / .NET 10 modernization

Apply only where it genuinely simplifies; the library already targets net10.0/C# 14 and uses the `field` keyword
(`DynamicColumn.FullTitle`). No churn-only rewrites.

- [ ] **E1.** Collection expressions and `params` collections where applicable (e.g. `CellStyleMap.AddRange`,
  internal list building).
- [ ] **E2.** `field`-backed properties to eliminate remaining explicit backing fields where the property has
  logic (candidates in the wrapper now that C1/C2 have landed).
- [ ] **E3.** C# 14 `extension` blocks — **additive only**: new grouped helper APIs (e.g. future convenience
  helpers over `ColumnManager`) may ship as extension members; never move existing instance methods (source
  breaking for `override`/reflection and churn for callers).
- [ ] **E4.** `SearchValues` / span-based matching in `QuickSearchUtility` string comparison hot path (pairs
  with C4; measure before/after).
- [ ] **E5.** If any locking is introduced (C2 collaborators, caches), use `System.Threading.Lock`.

Explicitly out of scope (decided, do not revisit): merging the three `AddNumber` overloads (generic inference
fails on non-nullable properties — comment in `ColumnManager.cs` explains; tried and reverted), renaming
`AddSimpleDate` → `AddDate`.

## Track F — Samples & docs

The example pages are the de-facto test suite; every new feature above ships with a page, registered in
`src/QuickGrid.Samples.Shared/Core/ExampleRegistry.cs`.

- [ ] **F1. EF / `FilterCriteria` example** — the server-side search path currently has **zero** coverage anywhere
  (no sample, no consumer). Needs an EF-backed source (SQLite in-memory or EF InMemory provider in the samples
  app). Pair with B6.
- [x] **F2. Shared ColumnManager done right.** *(with B5)* `SharedUserColumnManager` is one static `Instance` genuinely
  shared by `UsersGrid` and `UsersGridWrapper`; before, each page built its own and the sample never exercised the bug.
- [ ] **F3. Auto-footer example** — showcase D6 next to the existing manual footer example.
- [ ] **F4. Global options + custom tick icons example** — showcase D3 (the `AddTickColumn2` scenario).
- [ ] **F5. Search options example** — mostly landed with **D15**: `/search-filtering` now drives a live
  `QuickSearchOptions` from the page, with controls for `IncludeChildProperties`, `MaxSearchDepth` (against a
  three-level source — `ProjectDto.Owner.Office.City`, added for it) and `EnableExclusionTerms`. Still to add
  once they exist: `SearchVisibleColumnsOnly` (D4), an `ExactMatch` preset, and `addToContent`
  hidden-but-searchable columns. Extend that page rather than writing a second search example.
- [ ] **F6. Dark-mode fix** — `wwwroot/app.css` `.table-thead-sticky` hardcodes `background-color: white`; use a
  Bootstrap variable (`var(--bs-body-bg)`) so dark themes work. Also document (or stop using) `btn-xs`, which is
  not a Bootstrap 5 class and currently must come from the host app.
- [x] **F7. README feature-table upkeep.** *(2026-08)* Live-demo link on top, every example in one table, every
  feature row linking to the page that shows it. Keep the links accurate as F1-F5 land — the remaining ⏳ rows are
  the EF/`FilterCriteria` example and JSON export.
- [x] **F8. Samples on GitHub Pages.** *(2026-08)* Pages and services live in the `src/QuickGrid.Samples.Shared` RCL;
  `QuickGrid.Samples` is a thin Blazor Server head (the higher-fidelity one — both consumers run Server) and
  `src/QuickGrid.Samples.Wasm` a thin WebAssembly head deployed by `.github/workflows/deploy-pages.yml` (Pages
  source must be "GitHub Actions"). A new page in the RCL lights up in both.
- [x] **F9. Example count visible on both heads.** *(2026-08-24)* `ExampleRegistry.Count`/`Position(route)` surfaced
  as an **EXAMPLES · n** nav heading, a badge and numbered list on the home page, and **Example n of N** in
  `ExampleNav`.

## Working agreement

**Decided (2026-08-19): no bUnit.** The A1 tests cover pure logic only and `QuickGridWrapper` has no automated
render coverage, but the owner prefers to verify component behaviour himself — so wrapper changes ship in
reviewable chunks and are signed off by hand against the sample pages and both consumer apps rather than by a
component-test suite. Practical consequence: **stop and hand over for manual testing at each item boundary**, keep
each change small enough to eyeball, and say plainly which pages and which behaviours need clicking. Where wrapper
logic can be pulled out into a plain testable type (as C2 did with `GridSearch<T>` and `GridFooterBuilder<T>`),
cover *that* with ordinary xUnit tests — it is the cheapest coverage available under this decision.

The order of the remaining work is kept in [now.md](now.md), not here.
