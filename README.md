# TermProject

A Unity game project for 2110511 Game Programming.  
Unity Version: 6.3 LTS (6000.3.7f1)

---

## Getting Started

1. Clone the repo
```bash
   git clone https://github.com/ImSoRichGovePlsStop/TermProject.git
```
2. Open in Unity Hub
   - Unity Hub → Open → select the `TermProject` folder
3. Create a new branch for your feature
```bash
   git checkout dev
   git pull origin dev
   git checkout -b <your-branch-name>
```
4. Work on your feature, then commit
```bash
   git add .
   git commit -m "<commit-name>"
   git push origin <your-branch-name>
```
5. Open a Pull Request on GitHub
   - Go to the repo on GitHub
   - Click **"Compare & pull request"**
   - Set base branch to `dev`
   - Add a description and request a reviewer
   - Click **"Create pull request"**

---

## Keeping Your Branch Up to Date
Pull the latest changes from `dev` into your branch regularly:
```bash
   git checkout dev
   git pull origin dev
   git checkout <your-branch-name>
   git merge dev
```
---

## Contributing

### Branch Naming Scheme

Follow this format: `<name>/<type>/<short-description>`

**Example:** `gong/feat/player-movement`

| Type | Description |
|------|-------------|
| `feat/` | New features |
| `fix/` | Bug fixes |
| `chore/` | Maintenance tasks, dependency updates, config changes |
| `refactor/` | Code refactoring without changing functionality |
| `remove/` | Removing unused code, dependencies, or features |

### Branch Protection Rules

The `dev` and `main` branches are protected with the following rules:

1. **Require a pull request before merging**
   - All changes must be submitted via pull request (PR).
2. **Require branches to be up to date before merging**
   - The PR branch must be rebased or merged with the latest `dev` before merging.

### Best Practices
- **PR to `dev` as soon as your feature is complete** — don't let your branch fall too far behind.
- **Pull from `dev` frequently** to keep your branch up to date and reduce conflicts.
- **Avoid working on the same scene or prefab as others** — Unity scene and prefab files are binary-like and extremely hard to resolve when conflicted.
- If you must work in the same scene, **communicate with your teammate first** and coordinate who edits it at what time.
- **Never force push** to `dev` or `main`.
