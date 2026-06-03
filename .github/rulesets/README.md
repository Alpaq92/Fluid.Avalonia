# Branch ruleset (as code)

`main-branch-protection.json` is the branch protection for `main`:

- squash-only, linear history, no force-push / deletion
- require the latest push to be approved (a human, a contributor, or CodeRabbit's bot review satisfies this — that's what lets the auto-merge flows ship)
- required status check: **Build (Fluid.Avalonia)** (CI)
- required CodeQL code-scanning at "high or higher"
- the **admin** repository role can bypass (so the owner can force-merge / merge without waiting for requirements when needed)

## Apply / update it

GitHub does not auto-apply ruleset files — import it once (token must be the repo admin; the `gh` CLI logged in as the owner works):

```bash
# create
gh api repos/Alpaq92/Fluid.Avalonia/rulesets --method POST --input .github/rulesets/main-branch-protection.json

# update later: find the id, then PUT
gh api repos/Alpaq92/Fluid.Avalonia/rulesets --jq '.[] | "\(.id)\t\(.name)"'
gh api repos/Alpaq92/Fluid.Avalonia/rulesets/<id> --method PUT --input .github/rulesets/main-branch-protection.json
```

Or via the UI: **Settings → Rules → Rulesets → New ruleset → Import**.
