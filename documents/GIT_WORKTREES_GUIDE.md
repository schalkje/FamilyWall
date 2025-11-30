# Git Worktrees Workflow Guide

This guide explains how to work with git worktrees in the FamilyWall project while using Claude Code effectively.

## Table of Contents
- [Overview](#overview)
- [Why Worktrees?](#why-worktrees)
- [Directory Structure](#directory-structure)
- [Creating Worktrees](#creating-worktrees)
- [Syncing Branches](#syncing-branches)
- [Working with Claude Code](#working-with-claude-code)
- [Cleaning Up](#cleaning-up)
- [Best Practices](#best-practices)

## Overview

Git worktrees allow you to work on multiple branches simultaneously without switching branches or stashing changes. Each worktree is a separate working directory linked to the same git repository.

**Current Setup:**
```
C:/repo/FamilyWall/                      (main branch - primary worktree)
C:/repo/FamilyWall/worktrees/feat-design (feature/design branch)
C:/repo/FamilyWall/worktrees/feat-mvp    (feature/mvp branch)
```

## Why Worktrees?

**Benefits:**
- Work on multiple features/branches simultaneously
- No need to stash changes when switching contexts
- Build/test different branches in parallel
- Each worktree can have its own IDE instance with Claude Code

**Use Cases:**
- Developing multiple features in parallel
- Testing fixes while continuing feature work
- Reviewing PRs while keeping your work untouched
- Running different builds simultaneously

## Directory Structure

```
FamilyWall/
├── .git/                  # Shared git repository
├── src/                   # Main worktree (main branch)
├── docs/
├── worktrees/             # All feature worktrees go here
│   ├── feat-mvp/          # feature/mvp branch
│   ├── feat-design/       # feature/design branch
│   └── feat-speckit/      # feature/speckit branch
└── README.md
```

**Naming Convention:**
- Use `feat-<name>` for feature branches
- Use `fix-<name>` for bugfix branches
- Use `docs-<name>` for documentation branches

## Creating Worktrees

### 1. Create New Feature Branch with Worktree

```bash
# From main worktree (C:/repo/FamilyWall)
git worktree add worktrees/feat-<feature-name> -b feature/<feature-name>
```

**Example:**
```bash
git worktree add worktrees/feat-photo-upload -b feature/photo-upload
```

This creates:
- A new branch `feature/photo-upload`
- A new worktree at `worktrees/feat-photo-upload`

### 2. Create Worktree from Existing Branch

```bash
# Checkout existing branch into new worktree
git worktree add worktrees/feat-<name> feature/<name>
```

**Example:**
```bash
git worktree add worktrees/feat-calendar feature/calendar
```

### 3. List All Worktrees

```bash
git worktree list
```

## Syncing Branches

### Scenario 1: Merge MVP Code into Design Branch

You have `feature/mvp` with implementation code, and `feature/design` with documentation. You want to bring MVP code into design branch.

**Option A: Merge (Recommended for ongoing work)**

```bash
# Navigate to design worktree
cd worktrees/feat-design

# Ensure you're on feature/design
git status

# Pull latest changes from remote (if any)
git pull origin feature/design

# Merge feature/mvp into current branch (feature/design)
git merge feature/mvp

# Resolve conflicts if any
# ... conflict resolution ...

# Commit the merge
git commit -m "Merge feature/mvp into feature/design"

# Push to remote
git push origin feature/design
```

**Option B: Rebase (For clean linear history)**

```bash
# Navigate to design worktree
cd worktrees/feat-design

# Rebase feature/design on top of feature/mvp
git rebase feature/mvp

# Resolve conflicts if any
# ... conflict resolution ...

# Force push (only if branch is not shared!)
git push --force-with-lease origin feature/design
```

**Option C: Cherry-pick Specific Commits**

```bash
# Navigate to design worktree
cd worktrees/feat-design

# Find commit hashes from feature/mvp
git log feature/mvp --oneline

# Cherry-pick specific commits
git cherry-pick <commit-hash>

# Push changes
git push origin feature/design
```

### Scenario 2: Keep Feature Branch Updated with Main

```bash
# From your feature worktree
cd worktrees/feat-<name>

# Fetch latest changes
git fetch origin

# Option A: Merge main into feature
git merge origin/main

# Option B: Rebase feature on main (cleaner history)
git rebase origin/main
```

### Scenario 3: Sync All Branches with Remote

```bash
# From any worktree
git fetch --all

# Then in each worktree, merge/rebase as needed
```

## Working with Claude Code

### Opening Worktrees in VSCode

Each worktree can be opened as a separate VSCode workspace:

```bash
# Open specific worktree
code worktrees/feat-mvp

# Or from inside worktree
cd worktrees/feat-mvp
code .
```

### Claude Code Considerations

1. **Independent Sessions:** Each worktree can have its own Claude Code session
2. **Context Isolation:** Claude Code sees only the files in the current worktree
3. **Git Operations:** Claude Code will operate on the current worktree's branch

### Recommended Workflow

```bash
# Terminal 1: Main branch (code reviews, quick fixes)
cd C:/repo/FamilyWall
code .

# Terminal 2: Feature MVP
cd C:/repo/FamilyWall/worktrees/feat-mvp
code .

# Terminal 3: Feature Design
cd C:/repo/FamilyWall/worktrees/feat-design
code .
```

Each VSCode instance maintains its own:
- Claude Code context
- Git status
- Build outputs
- Terminal sessions

## Cleaning Up

### When to Remove Worktrees

Remove worktrees when:
- Feature is merged to main and no longer needed
- Experimentation is complete
- Branch is abandoned

### How to Remove Worktrees

```bash
# 1. Commit or stash any work in the worktree
cd worktrees/feat-<name>
git status
git add .
git commit -m "Final changes"

# 2. Go back to main worktree
cd C:/repo/FamilyWall

# 3. Remove the worktree
git worktree remove worktrees/feat-<name>

# 4. (Optional) Delete the branch if no longer needed
git branch -d feature/<name>              # Local delete (safe)
git branch -D feature/<name>              # Force delete local
git push origin --delete feature/<name>   # Delete remote branch
```

### Cleanup Stale Worktrees

If you manually deleted worktree folders:

```bash
# List worktrees (shows broken ones)
git worktree list

# Prune deleted worktrees
git worktree prune
```

## Best Practices

### When to Create Worktrees

**DO create worktrees for:**
- Long-running features (days/weeks)
- Parallel feature development
- Experimentation while keeping main work intact
- Testing different approaches simultaneously

**DON'T create worktrees for:**
- Quick fixes (just commit and switch branches)
- Short-lived branches (< 1 hour work)
- One-off tasks

### Branch Management

1. **Always branch from main:**
   ```bash
   git worktree add worktrees/feat-new -b feature/new main
   ```

2. **Keep features focused:**
   - One feature per worktree/branch
   - Merge to main when feature is complete

3. **Regular syncing:**
   ```bash
   # Daily: Update your feature with main
   cd worktrees/feat-<name>
   git fetch origin
   git merge origin/main  # or git rebase origin/main
   ```

### Naming Conventions

- **Branches:** `feature/<name>`, `fix/<name>`, `docs/<name>`
- **Worktrees:** `feat-<name>`, `fix-<name>`, `docs-<name>`
- Keep names short and descriptive

### Claude Code Integration

1. **One worktree = One VSCode instance** for clarity
2. **Inform Claude of context:** When starting work, mention the branch/feature
3. **Commit regularly:** Claude Code can help with git operations
4. **Push frequently:** Keep remote in sync

### Conflict Resolution

When merging branches:

```bash
# If conflicts occur during merge
git status  # Shows conflicted files

# Edit files to resolve conflicts
# Look for <<<<<<< HEAD markers

# After resolving
git add <resolved-files>
git commit  # Completes the merge
```

**Ask Claude Code to help:**
- "Show me the merge conflicts"
- "Help resolve conflicts in [file]"
- "Explain these merge conflicts"

## Common Workflows

### Starting a New Feature

```bash
cd C:/repo/FamilyWall
git worktree add worktrees/feat-<name> -b feature/<name>
cd worktrees/feat-<name>
code .
# Start coding with Claude Code
```

### Merging Completed Feature

```bash
# From main worktree
cd C:/repo/FamilyWall
git checkout main
git pull origin main
git merge feature/<name>
git push origin main

# Clean up
git worktree remove worktrees/feat-<name>
git branch -d feature/<name>
```

### Switching Context

No need to switch branches! Just switch VSCode windows:
- `Alt+Tab` between VSCode instances
- Each window = different worktree = different branch

### Keeping Multiple Features in Sync

```bash
# Update all worktrees with latest main
cd C:/repo/FamilyWall
git pull origin main

cd worktrees/feat-mvp
git merge main

cd ../feat-design
git merge main

cd ../feat-speckit
git merge main
```

## Troubleshooting

### Issue: "Cannot create worktree, branch already exists"

```bash
# Use existing branch
git worktree add worktrees/feat-<name> feature/<name>
```

### Issue: "Worktree already exists"

```bash
# Remove old worktree first
git worktree remove worktrees/feat-<name>
# Then recreate
git worktree add worktrees/feat-<name> feature/<name>
```

### Issue: Build conflicts between worktrees

Ensure each worktree has isolated build outputs:
- Each worktree has its own `bin/` and `obj/` directories
- Clean and rebuild when switching focus
- Consider using different ports for running applications

## Summary

**Key Takeaways:**
- Worktrees = parallel work on different branches
- Each worktree = separate VSCode + Claude Code session
- Merge/rebase to sync branches
- Clean up worktrees when features are complete
- Use descriptive names for easy identification

**Quick Reference:**
```bash
# Create
git worktree add worktrees/feat-<name> -b feature/<name>

# List
git worktree list

# Remove
git worktree remove worktrees/feat-<name>

# Sync branches
cd worktrees/feat-<name>
git merge <other-branch>
```

Happy coding with worktrees and Claude Code!
