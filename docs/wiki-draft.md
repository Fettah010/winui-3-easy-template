# Wiki / site draft (v1.0 — publish checklist, unpublished until v1.0 ships)

> Status: **draft for v1.0**. Nothing here is published — no wiki enabled,
> no site DNS, no announcement. v1.0 owns the publish; this file only fixes
> the structure so v1.0 is assembly, not authorship.

## Proposed home

- GitHub wiki (preferred): zero hosting, versioned with the repo, searchable
  from the repo front page. Fallback: `docs/`-rendered static page if the
  wiki stays disabled.
- Source of truth stays in `docs/` (`GETTING-STARTED.md`,
  `TEMPLATE-GUIDE.md`, `MAINTAINERS.md`, `why-devtem.md`); the wiki links
  in, never forks.

## Proposed structure (mirrors the doc split)

1. **Start** → `GETTING-STARTED.md` (15 minutes: scaffold → rename → run).
2. **Daily** → `TEMPLATE-GUIDE.md` (pages, settings, layout, data).
3. **Maintain** → `MAINTAINERS.md` (matrix, releases, signing, Store).
4. **Why** → `why-devtem.md` (head-to-head, honest non-goals).
5. **Reference** → `docs/diagnostics.md`, `docs/accessibility.md`,
   `docs/feature-guides/*`, `docs/DECISIONS.md` (append-only rationale).

## v1.0 publish checklist (not started)

- [ ] Enable the wiki (or reserve the site path) + link from README.
- [ ] One page per structure item above (link, don't copy).
- [x] Screenshots: the six re-verified at 1.0.0 (about/settings/diagnostics
  re-captured with 1.0.0 version text, rest carried) + `demo-tour.png` strip.
- [ ] Announcement kit stays separate (marketplace post, awesome-winui PR,
      video) — the wiki is documentation, not launch.
