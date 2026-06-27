---
name: commit
description: Stage all tracked+untracked changes and commit with a concise, descriptive message generated from the diff.
---

Stage all changes (modified tracked files + new untracked files) and commit with a message that summarizes the changes.

1. Run `git status --short` to see what's staged and unstaged.
2. Run `git diff --stat HEAD` and `git diff HEAD` (or `git diff --cached` if something is already staged) to understand the full scope of changes.
3. Stage everything: `git add -A`
4. Generate a commit message using these rules:
   - Start with a short 50-72 char summary line describing the logical change
   - For mixed changes, use a category prefix like "Extract", "Remove", "Add", "Refactor"
   - Include a blank line then bullet points if multiple distinct changes exist
   - Reference what was extracted/removed/added and which files
   - Keep it factual, no emoji, no markdown formatting
5. Commit with `git commit -m "..."` (or `-m` with `-m` for body)
6. Show the resulting commit with `git log --oneline -3`