---
name: diff-stack
description: Split a large uncommitted working-tree change set into a sequence of stacked PRs (branch chain), building careful local commits and generating a runnable script that pushes each branch and opens PRs — without the assistant ever touching the user's remote.
---

Split large multi-concern working-tree changes into an ordered stack of small PRs that can be reviewed and merged one after another. All remote operations (push, PR creation) are delegated to a script the user runs themselves; never `git push`, never create PRs, never touch the remote directly.

## When to use
- The working tree has many modified/untracked files (roughly 10+) spanning multiple independent concerns.
- The user wants sequential small code reviews rather than one giant PR.
- The user asks about "stacked PRs", "PR stack", "splitting a diff into PRs", or review-by-PR ordering.

## When NOT to use
- A single coherent change or a small handful of files.
- User says "just commit everything" (use the `diff-commit` skill instead).

## Steps

1. **Inventory**
   - `git status --short` and `git diff --stat HEAD` to see the full change set.
   - Confirm the base: `git log --oneline main..HEAD` and `git branch --show-current`. Note whether HEAD is exactly at the base (worktree-only changes) or ahead (existing commits).

2. **Group the diff into logical PRs**
   - Cluster files by concern (e.g., "content/atlas renames", "sprite split", "combat/animation fix", "level wiring", "tests", "docs/config").
   - **Order by compile dependency**, not by size: things that produce new types/assets first; consumers later. Example ordering: content+atlas → sprite classes → combat defs/fixes → entity/enemy wiring → tests → docs/.editorconfig.
   - Put each group's file paths in a small plan (this is your PR table: name, files, one-line title, base).
   - First PR's base = `main`; every later PR's base = the previous PR's branch.

3. **Kick off from a clean-ish baseline SAFELY**
   - The current working tree still holds ALL changes. Work the chain in order committing from it:
   ```
   git switch -c <branch1> <base-or-current>      # keeps uncommitted tree
   git add -- <group1 files>
   git commit -m "..."
   # verify: dotnet build --warnaserror && dotnet test
   git switch -c <branch2>                           # continues from branch1 tip
   git add -- <group2 files>
   git commit -m "..."
   ```
   - Untracked files must be listed explicitly only in their group (`git add -u` would miss them; plain `git add <path>` for new files).
- Switching branches while other changes are uncommitted is fine as long as the next branch is created from the current tip — git will carry untouched files across.
    - **The chain is cumulative, so the full changeset is always preserved locally.** Branch N of the stack contains branch N-1's commits plus its own group. Therefore:
     - Checking out the **last** branch shows the complete combined diff vs `main` — it IS the "everything" branch.
     - Checking out branch N shows the repo state after groups 1..N (all changes up to that point).
     - To review a *single* changeset in isolation: `git diff <prev-branch>..<branch-N>` (same slice a PR shows, since each PR's base is the previous branch).
   - **Empirically validate each checkpoint**: after committing group N, run `dotnet build --warnaserror` and `dotnet test` on that snapshot before moving on. If a later branch is a required dependency, order must make each branch buildable.

4. **Generate the runner script**
   - Write it to a temp path OUTSIDE the repo (e.g. `/tmp/kilo/diff-stack.sh`) so it is not committed.
   - For each PR branch in order, emit:
     - `git push -u origin <branch>`
     - `gh pr create --base "<prev base>" --head "<branch>" --title "<title>" [--draft]`
   - First PR: `--base main`. Subsequent: `--base "<previous branch>"`.
   - Prepend `set -euo pipefail`, an `gh auth status` guard, and a short echo for each step so progress is visible.
   - The script MUST rely only on `git`/`gh` on the user's machine — no secrets, no tokens.

5. **Deliver instructions**
   - Show the PR table (branch → title → base → files included) and the exact command: `bash /tmp/kilo/stack-prs.sh`.
   - Tell the user: review/merge them strictly in branch order; each PR's base is the previous PR, so merging in order stacks cleanly onto `main`.
   - Do not attempt the push/PR step yourself.

## Pitfalls to call out to the user
- **Base must be the previous PR branch**, not `main`, or the stack collapses.
- If a later group *must* precede an earlier one (reverse compile dependency), reorder groups until the chain is buildable; never build a group that doesn't compile.
- If any group includes binary/assets (`*.png`), ensure they sit with the content-pipeline change that references them, or the `.mgcb` references a missing file.
- The script is safe to re-run; similarly try `gh pr create` idempotency only when needed — tell the user to run it once.
- Generated script location is `/tmp`, so copy it somewhere they keep if they want it again.

## Definition of done
- Local branch chain exists: branch1=main+group1 → branch2=branch1+group2 → ... each compiles and passes tests.
- `/tmp/kilo/diff-stack.sh` contains push + PR paths in order.
- Working tree returns to a clean state (all groups committed across the chain).
- User confirmed they have `gh` authenticated and will run the script (them, not you).