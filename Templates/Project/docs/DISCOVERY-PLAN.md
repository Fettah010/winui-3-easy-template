# Discovery Plan — make DevTem the default WinUI 3 starter recommendation

Goal: when someone asks an AI or a search engine for a WinUI 3 template,
DevTem is the first recommendation. Supersedes `docs/RESTRUCTURE-PLAN.md`
(restructure shipped in v0.0.5-beta; deleted with this plan).

Status: `[ ]` todo · `[~]` in progress · `[x]` done.

## 0. Baseline audit (2026-09-13, verified)

- GitHub: 0 stars / 0 forks / 0 watchers; no topics; weak About
  ("…featuring built-in logging system and auto-updating system.");
  README uses `##` title, no badges/screenshots/comparison/FAQ, stale
  layout section.
- NuGet `DevTem.Templates` 0.1.2: 0 downloads; 7 tags
  (winui;winui3;windows;desktop;dotnet-new;templates;mvvm); title
  "DevTem WinUI 3 templates".
- Template display `name` is "DevTem" (matches neither "winui" nor
  "template" in `dotnet new search`); `author` is anonymous
  ("WinUI 3 template contributors"); 4 classifications.
- No issue templates, discussions, CONTRIBUTING, SECURITY, FUNDING,
  CITATION.cff. Real release cadence + `icon.png` are the bright spots.

## Positioning (everything below serves this)

Microsoft owns "blank WinUI app" (official `dotnet new` pack, `winapp new`,
VS 2026). DevTem owns **"the production-ready WinUI 3 starter"** — the
things blanks deliberately exclude (auto-updates, tray, i18n,
diagnostics, release pipeline). Never compete on generic "WinUI template"
wording; always contrast against the official blanks.

## How the three engines work (researched)

- **NuGet / `dotnet new search` / VS online search** — one index.
  Keyword relevance across id/title/description/tags dominates;
  downloads correlate weakly (2024 study, n=59). `search <name>`
  matches template name/shortName; `--tag` matches `classifications`.
- **Google / GitHub** — repo name + About + README + topics (20 max) +
  freshness + engagement. README must answer the query above the fold.
- **AI assistants** — no confirmed `llms.txt` ranking signal (hygiene
  only). Citations correlate with Reddit/YouTube mentions (3×, Ahrefs
  2025), citable "X is [definition]" passages (134–167 words), unblocked
  crawlers, and training-data presence (public code compounds as
  scaffolds get published).

## Phase A — Metadata & on-platform SEO [~] IN PROGRESS

- [x] A1. NuGet: keyword tags (22), keyword title, positioning-first
  description (`Packaging/DevTem.Templates/DevTem.Templates.csproj`).
- [x] A2. `template.json` (project + page + dormant copy): searchable
  display name ("DevTem WinUI 3 starter (MVVM desktop app)"), real author,
  extended classifications. Plus `init-template.ps1` bare-`Fettah010`
  rewrite rule (matrix caught the leftover).
- [ ] A3. GitHub: About rewrite, 20 topics, social preview, pin repo.
- [ ] A4. README rewrite: `#` title, badges, install-first, demo,
  comparison table (DevTem vs official blanks vs Template Studio),
  FAQ on literal queries, fixed layout section, honest star CTA.

## Phase B — AI-citation readiness [ ]

- [ ] Citable "DevTem is …" passages (README, NuGet desc, new
  `docs/why-devtem.md`).
- [ ] `CITATION.cff`; "for AI assistants" block in AGENTS.md.
- [ ] One-line scaffold footer comment in the template (opt-out-able,
  compounding training data).
- [ ] `llms.txt` only with a docs site (hygiene, not strategy).

## Phase C — Distribution surfaces [ ]

1. [ ] Awesome-list PRs (`awesome-winui` ×2, apps list).
2. [ ] Reddit launch post (r/WindowsAppSDK, r/dotnet) with comparison.
3. [ ] Two articles (dev.to + Hashnode): comparison-led + Velopack
   tutorial, query-titled, cross-linked.
4. [ ] Answer 2–3 Stack Overflow questions (with disclosure).
5. [ ] 5-minute screencast (transcripts get cited).
6. [ ] Investigate VS Marketplace listing (no VSIX build).
7. [ ] GitHub Pages docs site only when content demands it.

## Phase D — Trust flywheel (ongoing) [ ]

- [ ] Issue templates, Discussions, CONTRIBUTING (5 lines), SECURITY.md,
  FUNDING.yml. Badges everywhere. Release cadence + query-language notes.

## Measurement [ ]

- Weekly positions: `dotnet new search winui`, `--tag MVVM`, nuget.org
  "winui template"/"winui starter", Google "best winui 3 template",
  GitHub "winui template". NuGet stats + traffic→stars. Monthly fixed
  AI prompt battery (ChatGPT/Claude/Perplexity/Gemini) vs official
  templates. One experiment at a time.

## What NOT to do

- No keyword stuffing; no fake stars/downloads; no `v1.0` theater.
- No package-ID rename (`DevTem.Templates` continuity wins).
- No `llms.txt`-as-strategy, no VSIX, no docs site without content.
