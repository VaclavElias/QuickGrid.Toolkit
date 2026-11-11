# Plan — Version 2.0 (breaking changes)

Status tracking document for the v2.0 major release. Prerequisite: the relevant parts of
[plan-version-1.md](plan-version-1.md) — in particular the test project (A1), the `_filteredItems` refactor (C1)
and the `[Obsolete]` markers (D8) — should land on 1.x first so v2 refactors regress loudly, not silently.

Breaking changes are acceptable here, including significant ones. Every item must still ship with a migration
note; the **consumer call-site counts** in plan-version-1.md are the budget for judging migration cost. Both known
consumers use project references, so "breaking" means source-level: their builds fail immediately and visibly.

Migration guide: maintain `docs/migration-v2.md` alongside the work — one section per item below, written when the
item lands, with before/after snippets keyed to the real consumer patterns (`AppQuickGrid` subclass, positional
`Add*` calls, `FullTitle`-keyed saved views, manual footer loops).

---

## 1. Self-rendering columns (issue #7 §2)

**Today:** `QuickGridColumns.razor` dispatches on `col.ColumnType == typeof(...)` through an if/else chain;
adding a column type means editing `QuickGridColumns.razor` + `DynamicColumn` + `ColumnManager` together, and
consumers cannot add column types at all.

**Change:** `DynamicColumn<TGridItem>` gets a `virtual RenderFragment Render()` (default: today's fallback
`PropertyColumn` rendering); built-in types (`TickPropertyColumn`, toggle, image, empty/index, template) override
it. `QuickGridColumns.Columns()` becomes a plain `foreach { @col.Render() }`. Custom column types become a
supported extension point.

**Breaks:** direct `ColumnType = typeof(...)` object-initializer construction — Twenty360 has **34 sites**
(their `ColumnManagerEntityExtensions` build `DynamicColumn` by hand), the samples 2, Invoicing 0. Keep
`ColumnType` as an `[Obsolete]` shim mapped to the built-in overrides for one minor release, then remove.
Migration table required listing each built-in `ColumnType` → new subclass/factory.

- [ ] Implement `Render()` on `DynamicColumn` + built-ins
- [ ] Rewrite `QuickGridColumns.Columns()` (add `@key` per column while here)
- [ ] `[Obsolete]` shim for `ColumnType`, then removal
- [ ] Migration guide section + custom-column sample page

## 2. Data-source split: `Items` / `QueryableItems` (issue #7 §7)

**Today:** `Items` is `IQueryable<T>`; quick search builds an expression calling a component method, which throws
at runtime on a real EF source. `QueryableItems` exists but is dead. Reality check: **every known consumer call
site passes `List.AsQueryable()`** — nobody uses the EF path yet.

**Change:** `Items` becomes `IEnumerable<T>` (in-memory: quick search, footers, selection all valid);
`QueryableItems` becomes the real EF-backed path (used with `FilterCriteria`, server-side evaluation, no
reflection search). Setting both is an error. `.AsQueryable()` call sites still compile against `IEnumerable<T>`,
so consumer migration is mostly "delete `.AsQueryable()` at your leisure".

- [ ] Retype `Items`; wire `QueryableItems` through search/footer/export paths with EF-safe evaluation
- [ ] Analyzer-friendly guardrails: throw with a clear message when quick search is enabled on `QueryableItems`
      without `FilterCriteria`, etc.
- [ ] EF sample page exercises the `QueryableItems` path (extends v1 F1)

## 3. Overload collapse on `ColumnManager` (issue #7 §4)

**Today:** 25 `Add*` overloads; `AddSimple` forwards verbatim to `Add`; most helpers exist twice
(string params vs `ColumnInfo`); the parameter matrix is ragged (`AddTickColumn` lacks
`propertyName`/`sortBy`/`calculateTotal`, `AddToggleColumn` lacks `visible`/`propertyName`, `AddImageColumn`
lacks `fullTitle`/`visible`, `AddTemplateColumn` calls its class parameter `cssClass`).

**Change:**
- Add `implicit operator ColumnInfo(string title)` so `AddSimple(expr, "Title")` binds the `ColumnInfo`
  overload; keep only the `ColumnInfo` overloads. **Keep the `AddSimple` name** and remove the `Add` alias
  (`AddSimple` has 400+ call sites vs `Add`'s handful; Twenty360's ~70 `Add(DynamicColumn)` calls are the
  *column-instance* overload, which stays).
- Normalize the parameter matrix: every helper gains the full set (`visible`, `propertyName`, `sortBy`,
  `calculateTotal` where meaningful), appended in a consistent order. A major version permits reordering existing
  parameters, but each reorder must be listed in the migration guide with its call-site count — prefer appending
  even in v2 (positional two-string calls `("Payroll.N.", "Payroll Number")` would silently swap on reorder,
  which is worse than an error).
- Rename `AddTemplateColumn`'s `cssClass` → `@class` (named-arg break; low count, 12+12 sites).

**Do not** merge the three `AddNumber` overloads (decided in v1; type inference makes it worse).

- [ ] Implicit conversion + overload pruning
- [ ] Parameter matrix normalization table (write it first, review, then implement)
- [ ] Migration guide section with per-method call-site counts

## 4. Column identity: `Key` everywhere (issue #7 §6, phase 2)

v1 D2 introduces `Key` with `FullTitle` fallback for saved views. v2 completes the cut-over:

- Footers keyed by `Key` instead of positional 1-based `Id` (`AddFooterColumn(int id, ...)` →
  `AddFooterColumn(string key, ...)`; keep `Id` overloads `[Obsolete]` one release). The `Id`-after-insertion
  assignment (`column.Id = Columns.Count`) stops being load-bearing.
- Saved views persist `Key`; drop the `FullTitle` fallback. Provide a one-shot migration helper that maps
  existing persisted `ColumnName` (FullTitle) values → keys given a `ColumnManager`.
- Export (`ExpandoObjectBuilder`, selected-columns export) keys on `Key` rather than `PropertyName`.

**Breaks:** Twenty360's manual footer loops (`AddFooterColumn(column.Id, ...)` — 15 sites, ideally already
replaced by v1 D6's declarative footers before this lands) and its persisted view rows (migration helper).

- [ ] Footer API on `Key`
- [ ] Saved-view persistence on `Key` + migration helper
- [ ] Export on `Key`

## 5. Saved views as a first-class contract

**Today:** the toolkit has `ColumnConfig`/`SelectedColumn` DTOs and view-selection UI, but persistence is the
consumer's problem: Twenty360 hand-maps an EF entity (`ColumnConfiguration`, which carries a
`// ToDo: This needs to be separated for QuickGrid...` comment) to `ColumnConfig` on every load, wires all six
`Events` callbacks, and re-implements dialog plumbing. `SelectedColumn.DisplayOrder` exists but reordering is
not implemented.

**Change:**
- `IColumnConfigStore` abstraction (`LoadAsync(tableId, userId)` / `SaveAsync(...)` / `DeleteAsync(...)`); the
  wrapper drives it directly when registered, `Events` callbacks remain as the manual escape hatch.
- Ship `ColumnConfig` as a clean, persistence-agnostic DTO (serialization-stable; `Key`-based after item 4).
- Implement **column reordering** end-to-end: `DisplayOrder` honored at render time, drag-and-drop in
  `ColumnSelector` (a commented-out "Drag and drop to reorder columns" hint already sits in that file).

**Breaks:** shape changes to `ColumnConfig`/`SelectedColumn`; Twenty360's `AppQuickGrid` mapping code (which it
wants to delete anyway — this item exists to absorb it).

This is the largest single contributor to the *"shrink the `AppQuickGrid` burden"* design goal in
[plan-version-1.md](plan-version-1.md) — read that section first for the stays-in-the-app vs. absorb-into-the-toolkit
split. Invoicing having commented the whole saved-view block out is the strongest signal available that the
current plumbing cost exceeds its value.

- [ ] `IColumnConfigStore` + wrapper integration
- [ ] Reordering (model + render + selector UI)
- [ ] Saved-views sample upgraded to real persistence (e.g. localStorage-backed store in samples)

## 6. Wrapper decomposition & UI modernization

Building on v1 C1/C2 (internal collaborators), make the UI itself composable:

- Toolbar, search box, settings menu, selection actions and column-layout dropdown become individual components
  (internal first; public once stable) so hosts can rearrange or replace pieces instead of `IsToolbar="false"`
  + rebuild-everything.
- **Localization**: all UI strings are hardcoded English ("quick search..", "Manage Columns", "Export CSV",
  "SELECTED", "Loading.." …). Introduce a string-resource hook (`IStringLocalizer` or a simple options-based
  strings class).
- **Theming**: replace hardcoded colors with CSS custom properties (dark mode currently broken in several spots);
  keep existing class names (frozen contract) but define variables under them.
- Replace the invalid `content="..."` span attribute with `data-content` — **dual-emit both attributes during
  late 1.x**, switch samples/docs/CSS to `data-content`, drop the old attribute in 2.0 (consumer CSS selects on
  `td span[content=...]`).

**Parameter surface consolidation** (follows v1 D9): remove the obsoleted standalone `ColumnSelectionChanged`
and `SearchResultChanged` parameters (their `Events` members become the only path); normalize
`QuickGridWrapperEvents` member shapes (today `OnManageColumns` is `EventCallback?` while the rest are
non-nullable `EventCallback` checked via `HasDelegate` — pick one convention; consumers null-check
`OnManageColumns` specifically, so this is a breaking signature change with two known call sites in each
consumer's `AppQuickGrid`).

**Parameter grouping — decided (2026-08, do not re-litigate without new evidence).** The test is "does the group
carry behaviour or mutual exclusivity together?", not "are these related?" — grouped objects cannot use `@bind-`,
cannot be set as subclass-ctor defaults (the `AppQuickGrid` pattern), and an object created inline in markup is a
new instance every render, defeating parameter change detection.

- **Group — data source**: `Items` + `QueryableItems` + `ItemsVersion` + `ItemKey` + the item-7 `ItemsProvider`
  are mutually exclusive *modes* pretending to be independent parameters. `GridDataSource<T>` (item 8) is the
  umbrella: it models mode selection in the type system and hosts change notification. Items 2, 7 and 8 converge
  here — design them as one abstraction, not three features.
- **Group — search knobs**: expose one optional `SearchOptions` parameter (wrapping the existing internal
  `QuickSearchOptions`: nested depth, case sensitivity, `MultiTermOperator`, `EnableExclusionTerms`,
  visible-columns-only, `IsNestedSearch` folds in) instead of a new flat flag per knob forever.
  `QuickSearch`/`ExactMatch` stay flat — `@bind-` requires it. **v1 D15 shipped this additively** (2026-08-26) — `SearchOptions` is
  the baseline, `IsNestedSearch` is already `[Obsolete]` — so v2 is only the removal of the flat shim.
- **Keep flat**: the boolean `Is*` feature toggles (`<AppQuickGrid IsPaginator ItemsPerPage="50">` is idiomatic
  Blazor); appearance (`Class`, `Theme`, `RowClass`, `Icons` — set in subclass ctors); all RenderFragments
  (`FilterSection`, `SelectedItemsActionDropDown`, `DropdownItems` — child-content syntax needs flat); small
  singles (`Id`, `DownloadFileName`). `TotalFooter` already shows object-parameters done right.
- Net effect: ~35 → ~28 parameters. The real reduction pressure comes from sub-componentization above, not
  grouping — grouping is only for type-level meaning.

- [ ] Sub-componentization
- [ ] Localization hook
- [ ] CSS custom properties + dark mode
- [ ] `data-content` cut-over (dual-emit starts in 1.x)
- [ ] Remove obsoleted standalone event parameters; normalize `Events` member shapes
- [ ] Remove the `[Obsolete]` `IsNestedSearch` shim (the `SearchOptions` parameter itself landed in v1 D15)
- [ ] Data-source grouping lands as part of items 2/7/8 (`GridDataSource<T>`), not separately

## 7. Virtualization / `ItemsProvider` support

Twenty360 falls back to raw `<QuickGrid ItemsProvider Virtualize>` because the wrapper only accepts materialized
items. Add first-class support: `Virtualize`/`ItemSize` parameters and an `ItemsProvider` source mode. Interacts
with search (server-side only in provider mode), footers (totals need a full-set aggregate — likely
provider-supplied), selection and export (both need explicit strategies in provider mode). Design doc first;
this is the largest net-new feature in v2.

- [ ] Design note covering search/footer/selection/export semantics in provider mode
- [ ] Implementation + sample page

## 8. `ItemsVersion` ergonomics

Keep `ItemsVersion` (29+ consumer usages, and QuickGrid fundamentally re-reads only on reference change), but
make most pages never need it: an opt-in data-source path that raises change notifications. Per the parameter-
grouping decision in item 6, the vehicle is `GridDataSource<T>` — the same abstraction that models the
in-memory / queryable / provider source modes for items 2 and 7 — with a `Refresh()` method and optional
`INotifyCollectionChanged` observation. Plus a clear doc page on when each mechanism applies. Twenty360
documents `ItemsVersion` as their #1 papercut in two places.

- [ ] Design `GridDataSource<T>` jointly with items 2 and 7 (one abstraction, three features)
- [ ] Implement + deprecate nothing (`ItemsVersion` stays supported)

## 9. Icon provider v2

Replace the "exhaustive switch or crash" contract: `QuickGridIconProviderBase` with one virtual method per icon
returning the Bootstrap default, so implementers override only what they customize and **new icons are additive,
not breaking**. `IQuickGridIconProvider` stays for full control but its doc contract states unknown members must
fall back (and the wrapper keeps the v1 B7 try/catch safety net). Also honor the `extraCss` parameter, which the
default provider currently ignores.

- [ ] Base class + default forwarding
- [ ] Migrate `DefaultQuickGridIconProvider`; update the SavedViews/icons sample

## 10. Removals (everything obsoleted in v1 D8)

`QuickGridColumns.GetActionColumn`, `QuickGridColumns.MarkupColumn`, `TogglePropertyColumn`,
`ColumnBuilder.BuildConditionalActionColumn`, `QuickGridWrapper.IsTableIndex()`, the `ColumnType` shim (item 1),
`Id`-keyed footer overloads (item 4). Keep `DownloadFileName` — Invoicing reads it (`AppQuickGrid.GetFileName`).
Re-verify each removal against both consumers at the time of removal, not against this document.

- [ ] Sweep + consumer builds green

### Review `AddRange` (decide, do not assume)

`ColumnManager.AddRange` had **zero** production callers when checked (2026-08-19) — 0 in Twenty360, 0 in
Invoicing, 0 inside the toolkit; only two sample pages. Both apps meet the same need with `ColumnManager`
extension methods, which are strictly better for the purpose (see the guidance note under v1 B5).

This is a *review* item, not a removal. "Unused by the two apps we can read" is not "unused by everyone" once the
package is on NuGet, and after B5 the method is correct and free to keep. Decide between:

- **Keep and de-emphasize** (favoured today) — document extension methods as the recommended way to share columns
  and stop leading the samples with `AddRange`.
- **Obsolete it** — only if telemetry or issues show nobody uses it, and only with the extension-method pattern
  documented as the replacement first.

- [ ] Re-check consumer usage at v2 time · [ ] Decide keep / obsolete · [ ] Sample text reflects the decision

---

## Sequencing sketch

1. Item 1 (self-rendering columns) and item 3 (overload collapse) — the two big structural breaks; land early
   so the rest of v2 is built on the new shapes.
2. Item 2 (data-source split) and item 4 (Key cut-over) — contract changes with mechanical migrations.
3. Items 5, 6 — saved views + UI decomposition (feature-heavy, benefit from the cleaner core).
4. Items 7, 8 — net-new capability.
5. Items 9, 10 — cleanup and release. Write `docs/migration-v2.md` sections as each item lands, not at the end.
