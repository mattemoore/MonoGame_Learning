---
name: diff-stack
description: Split a large uncommitted working-tree change set into a sequence of stacked PRs using the official `gh stack` GitHub CLI extension. The assistant builds careful local commits and hands the user the `gh stack` commands(submit, rebase, push, sync)to create, update,and propagate the stack - without the assistant ever touching the user's remote.
---

Split large multi-concern working-tree changes into an ordered stack of small PRs that can be reviewed and merged one after another, using the official [`gh stack`](https://github.com/github/gh-stack) extension. It automates the push/PR bookkeeping - pushing branches, creating one PR per branch with the correct base, linking them as a Stack on GitHub - and it solves the propagation problem: after editing an early PR, `gh stack rebase` + `gh stack push`(or just `gh stack submit`)replays your change down the whole chain in one command. The assistant's job stays the same: group the diff, build the local commit chain,and verify every snapshot compiles and tests green. All remote operations are delegated to a user-run step:the assistant never runs `gh stack submit`, `gh stack push`, or `git push`.

## Prerequisites

- GitHub CLI `gh` v2.0+ authenticated(`gh auth login` / `gh auth status`).
- The extension, installed once by the user:

  ```sh
  gh extension install github/gh-stack
  ```

- Optional but handy: `gh stack alias` installs the short `gs` alias。

## When to use

- The working tree has many modified/untracked files(roughly 10+)spanning multiple independent concerns.
- The user wants sequential small code reviews rather than one giant PR.
- The user asks about stacked PRs, PR stacks, splitting a diff into PRs, or review-by-PR ordering.

## When NOT to use

- A single coherent change or a small handful of files.
- User says just commit everything(use the `diff-commit` skill instead)。

## How the stack works

- A stack is an ordered list of branches, each building on the one below it. The bottom branch is based on the trunk(default:the repo's default branch);the top layer is furthest from trunk。
- Each branch becomes its own PR;GitHub sets each PR's base to the branch below it,so each PR shows only that layer's diff。
- Stack metadata lives in `.git/gh-stack`(local JSON;never committed;sent to the remote only when you push)。

## Steps

1. **Inventory**
   - `git status --short` and `git diff --stat HEAD` to see the full change set。
   - Confirm the base: `git log --oneline main..HEAD` and `git branch --show-current`. Note whether HEAD is exactly at the base(worktree-only changes)or ahead(existing commits)。

2. **Group the diff into logical PRs**
   - Cluster files by concern(e.g., content/atlas renames, sprite split, combat/animation fix, level wiring, tests, docs/config)。
   - **Order by compile dependency**,not by size:things that introduce new types/assets first;consumers later. Example ordering: content+atlas → sprite classes → combat defs/fixes → entity/enemy wiring → tests → docs。
   - **Some groups cannot split** - when a branch removes an API the old version of the next branch still calls(and vice versa),both must land in the same PR or no snapshot compiles. Detect these keystone couplings during grouping,and merge the entangled groups into one keystone PR;the rest of the chain then peels cleanly。
   - Put each group's file paths in a small plan(this is your PR table: name,files,one-line title,base)。
   - First PR's base = `main`;every later PR's base = the previous PR's branch。

3. **Build the local chain from the working tree SAFELY**
   - The current working tree still holds ALL changes. Work the chain in order committing from it:

   ```sh
   git switch -c <branch1> <base-or-current>      # keeps uncommitted tree
   git add -- <group1 files>
   git commit -m "..."
   # verify: dotnet build --warnaserror && dotnet test
   git switch -c <branch2>                           # continues from branch1 tip
   git add -- <group2 files>
   git commit -m "..."
   ```

   - Untracked files must be listed explicitly only in their group(`git add -u` would miss them;plain `git add <path>` for new files)。
   - Switching branches while other changes are uncommitted is fine as long as the next branch is created from the current tip - git carries untouched files across。
   - **Empirically validate each checkpoint**: after committing group N, run `dotnet build --warnaserror` and `dotnet test` on that snapshot before moving on. If a later branch is a required dependency, order must make every branch buildable。
   - The chain is cumulative,so the full changeset is always preserved locally. To review a single layer in isolation: `git diff <prev-branch>..<branch-N>`。

4. **Track the chain with `gh stack`**
   - From the trunk(e.g., `main`),adopt the branches into a tracked stack,bottom-to-top:

   ```sh
   gh stack init <branch1> <branch2> ... <branchN>
   ```

   - `init` adopts existing branches automatically,and creates any missing ones. If the trunk isn't the repo's default branch, pass `--base <trunk>`。Metadata stays local in `.git/gh-stack`。
   - Verify the tracked tree: `gh stack view`(or `--short`);each layer shows its parent - branch1's parent is the trunk,branch2's parent is branch1,and so on。

5. **Deliver instructions:the user runs the remote step**
   - Show the PR table(branch → title → base → files included)and the exact command:

   ```sh
   gh stack submit --auto        # create PRs(drafts)for every layer;push all branches;link them as a Stack
   # or: gh stack submit --open   # same,but PRs start ready for review
   ```

   - `submit` creates one PR per branch with the correct base,pushes every branch with per-branch `--force-with-lease`,and links the PRs into a Stack on GitHub. It is idempotent:rerun it after edits to update everything。
   - Tell the user:review/merge the PRs strictlyin order,bottom-to-top. After each merge,run `gh stack sync` to retarget the remaining PRs to the new base,prune merged branches,and restack。

。

于

## Keeping the stack in sync

- A change to an early layer(e.g., PR #1)does not appear on downstream PRs until you restackand resubmit。

- From the edited branch, propagate the change through the whole chain:

  ```sh
  gh stack rebase      # cascade-rebase every descendant onto the edited branch tip
  gh stack push         # push all branches(force-with-lease;fix rejected refs first,then rerun)
  ```

- Or,in one command:

  ```sh
  gh stack submit       # restack,push,and update all PRs
  ```

- On rebase conflict:resolve the conflicts,`git add`,then `gh stack rebase --continue`;`--abort`restores every branch to its pre-operation state。
- `gh stack undo` restores branches to the state before the last destructive operation(restack/submit/sync)。
- After a PR has been merged,run `gh stack sync`(or `gh stack sync --prune` to prune automatically):it fetches,fast-forwards the trunk,retargets orphaned children to the trunk,and restacks all。

## Useful `gh stack` commands

| Command | Description |
| --- | --- |
| `gh stack init` | Start or adopt a stack locally(bottom-to-top) |
| `gh stack view`(or `log`) | Show the stack tree with layers,and PR links |
| `gh stack add <name>` | Add a new layer on top of the current stack |
| `gh stack rebase` | Cascade-rebase the branch and its descendants onto their parents |
| `gh stack push` | Push all active branches with force-with-lease checks |
| `gh stack submit` | Restack,push,create/update PRs,and link the Stack on GitHub |
| `gh stack sync` | Fetch,prune merged,retarget to trunk,restack,push,and sync PR state |
| `gh stack continue` / `gh stack abort` | Resume / cancel a rebase or submit after conflicts |
| `gh stack undo` | Restore branches to pre-operation state |
| `gh stack checkout <pr-or-branch>` | Check out a stack by PR number,URL,or branch |
| `gh stack merge` | Merge PRs up to a chosen layer in one operation |
| `gh stack modify` | TUI to reorder,fold,insert,or rename layers |
| `gh stack up` / `down` / `top` / `bottom` / `trunk` / `switch` | Navigate between stack layers |

## Pitfalls to call out to the user

- **Keep the whole chain in one stack** - `gh stack` sets each PR's base to its parent layer automatically,and fixes it during rebase/sync. Do not manually retarget PRs to `main`,or the stack collapses。
- **Order by compile dependency;never build a group that doesn't compile** - merge entangled keystone groupsrather than producing a broken intermediate branch。
- If any group includes binary/assets(`*.png`),it must sit with the content-pipeline change that references it,or the `.mgcb` references a missing file。
- `gh stack submit` in an interactive terminal opens a full-screen editor;use `--auto`(or run from CI)to skip it. `--auto` PRs are drafts;pass `--open` to make them ready for review。
- Stack metadata is local-only;nothing touches the remote until you run `submit`/`push`。
- The assistant still never pushes or creates PRs:those commands are for the user to run。

## Definition of done

- Local branch chain exists:branch1 = main + group1,branch2 = branch1 + group2,and so on;each compiles and passes tests。
- `gh stack init` has adopted the chain,and `gh stack view` shows the expected tree(started locally by the assistant or user)。
- Working tree returns to a clean state(all groups committed across the chain)。
- User confirmed `gh stack` is installed/authenticated,and will run `gh stack submit` themselves(them,not you)。
