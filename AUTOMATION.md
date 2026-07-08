# Automation &amp; PR flow

How a change moves from a pull request to a published release in this repo — the branch rules, the checks every PR must pass, the auto-merge behaviour per PR type, and what happens once it lands on `main`. It's all wired so that a green PR ships with minimal manual steps.

Everything reaches `main` through a pull request — a branch ruleset blocks direct pushes (the owner keeps an admin bypass for emergencies). The ruleset is versioned as code under [`.github/rulesets/`](.github/rulesets/), so the rules below are the source of truth, not screenshots.

## Opening a PR

Branch (or fork), make your change, and open a PR against `main`. Commit and PR titles follow [**Conventional Commits**](https://www.conventionalcommits.org/), because [release-please](https://github.com/googleapis/release-please) reads them to drive the version and changelog: `feat:` / `fix:` / `perf:` / `deps:` / `revert:` appear in the changelog (and `feat` / `fix` bump the version), while `docs:` / `chore:` / `refactor:` / `test:` / `build:` / `ci:` are silent. Merges are **squash**, so the **PR title becomes the commit message** — make it a valid Conventional Commit.

## What a PR must pass

| Gate | What it checks |
| --- | --- |
| **CI — `Build (Fluid.Avalonia)`** | `dotnet restore` + `build` of the whole solution (library + demo + Desktop + Browser/WASM heads) in Release, on .NET 8 &amp; 10 with the `wasm-tools` workload. |
| **CodeQL** | Code scanning must be clean at *high or higher*. |
| **One approving review** | [CodeRabbit](https://coderabbit.ai) auto-reviews every PR (`.coderabbit.yaml`) and its approval satisfies the rule; a human approval counts too. |

On top of those: squash-only, linear history, no force-push and no branch deletion.

## How PRs actually merge

Green PRs land on their own through GitHub auto-merge (always squash) — you rarely click *Merge* yourself. Which automation handles a PR depends on who opened it:

| PR source | Merges once… |
| --- | --- |
| **Maintainer** ([`auto-merge-trusted.yml`](.github/workflows/auto-merge-trusted.yml)) | the required checks pass — no review wait, no cool-off. |
| **External contributor** ([`auto-merge-approved.yml`](.github/workflows/auto-merge-approved.yml)) | it is approved, mergeable, checks are green, and it has been open **≥ 7 days** — a one-week sanity window (the job runs every 6 h; a maintainer can merge sooner by hand). |
| **Dependabot** ([`dependabot-auto-merge.yml`](.github/workflows/dependabot-auto-merge.yml)) | the required checks pass — no cool-off. |
| **release-please** release PR ([`release.yml`](.github/workflows/release.yml)) | checks and the review are green. |

The owner can always force-merge through the ruleset's admin bypass.

## After it lands on `main`

- **release-please** keeps a `chore(main): release X.Y.Z` PR up to date from the Conventional Commits since the last tag; merging it bumps `.release-please-manifest.json` + `CHANGELOG.md`, tags `vX.Y.Z`, cuts a GitHub Release, and **publishes the package to NuGet**. The publish is gated on release-please reporting a new release on the run that lands the release PR — if that's missed (e.g. the release PR is admin-merged and the tag already exists by the time a later run evaluates), the version tags but the package never pushes. Recover with the Release workflow's **`workflow_dispatch`**, which packs + pushes an existing tag by hand: `gh workflow run release.yml -f tag=vX.Y.Z`.
- **[`pages.yml`](.github/workflows/pages.yml)** redeploys the WebAssembly demo to GitHub Pages.
