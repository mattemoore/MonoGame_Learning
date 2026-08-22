# Code Review Process — Stacked PRs via `/diff-stack`

This describes how to review a large change that has been split into a **stack of PRs** using the
`/diff-stack` skill (see `.kilo/skills/diff-stack/SKILL.md`).

A stack is a chain of branches: PR #1 is based on `main`, PR #2 on the branch of PR #1, PR #3 on
the branch of PR #2, and so on. Each PR therefore contains the whole preceding stack plus one new
slice — but because of how GitHub compares a PR against its base, **each PR can be reviewed as one
small, isolated diff**.

---

## Review sequence

### 1. Setup — run the generated script once

The script created by `/diff-stack` (in `/tmp/kilo/diff-stack.sh` — it is never committed) pushes
every branch and opens each PR:

- PR #1: base `main`, head `branch-1`
- PR #2: base `branch-1`, head `branch-2`
- PR #3: base `branch-2`, head `branch-3`
- …and so on.

> Run the script once and only once. It uses `set -euo pipefail` and a `gh auth status` guard, so
> it is safe to inspect before running. If something fails you can re-run the failed step manually.

---

### 2. **Review PR #1** (the only PR based directly on `main`)

- Open PR #1. Its *Files changed* shows **only slice #1** (e.g. content/atlas assets, sprite
  renames) — nothing else.
- This slice is fully self-contained: the skill orders groups so every slice compiles on its own.
- When you are satisfied: **merge it into `main`** (squash or merge — pick your usual style).

> Because PR #1 is the base of the whole chain, it is the one that can "move the ground" for every
> other PR. Review it extra carefully.

---

### 3. **Retarget PR #2 to `main`**

PR #2 was created with base `branch-1`, but `branch-1` has now been merged into `main`. Before
reviewing PR #2:

```shell
git switch branch-2
git rebase main            # or: git merge main
git push --force-with-lease origin branch-2
gh pr edit branch-2 --base main
```

Now GitHub compares `branch-2` against `main`, so **PR #2's *Files changed* shows only slice #2**
— the diff is the change since the cherry-pick of slice #1, not the accumulated stack.

---

### 3. **Review PR #2, merge it too**

- The diff is small and isolated again.
- When reviewed, **merge PR #2 into `main`**.

---

### 4. **Repeat: retarget → review → merge**

For each remaining PR `#N`:

1. Rebase `branch-N` onto `main` (as above), and set its base to `main`.
2. Review just slice `#N`.
3. Merge into `main`.

The final merged PR represents the whole original change — the cumulative diff of the last branch
vs `main` equals the entire working-tree change under review.

---

## Key consequences to plan for

- **Each review is genuinely small.** Because the base is always moved up after each merge, you
  never review the accumulated stack — only your group.
- **A fix made while reviewing PR #2 automatically propagates into PR #3+.** Their branches still
  contain PR #2's history. Re-push PR #2 and later PRs pick it up.
- **Reordering or rewriting an earlier PR forces a rebase cascade** on all later PRs. Keep the
  sequence mechanical: review in order, merge clean, then retarget the next.
- **Review strictly bottom-up.** Never jump ahead to a higher PR before the current one is merged —
  otherwise your review target base is stale and the later PR cannot cleanly rebase.
- **A clean review is all you need** — every slice was already validated to build (`dotnet build
  --warnaserror`) and pass tests (`dotnet test`) when the skill created the chain, so you should
  not hit a compile break mid-review.

---

## Summary

1. PR #1 base `main` → review → merge.
2. `git rebase main` branch-2 → `--force-with-lease` push → set base `main` → PR #2 now shows only slice #2 → review → merge.
3. Repeat for #3, #4, … until the whole stack is in `main`.
4. Delete the stack branches locally (`git branch -d branch-1 ...`) and on the remote (`git push origin --delete branch-1 ...`).
