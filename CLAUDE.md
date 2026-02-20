# CLAUDE.md

This file provides guidance for AI assistants (Claude and others) working with this repository.

## Project Overview

**okozukai** (お小遣い) is a pocket money / personal allowance management application. The project name comes from the Japanese word for pocket money.

> Note: This repository is in its initial state. Update this file as the project evolves.

## Repository State

This repository is currently empty (no source files). This CLAUDE.md will be updated as the codebase grows.

When adding the initial codebase, update the following sections to reflect the actual technology choices, directory structure, and conventions used.

---

## Directory Structure

```
okozukai/
├── CLAUDE.md          # This file
├── README.md          # Project overview and setup guide (create when ready)
└── ...                # Source directories (add as project grows)
```

Update this section once the project structure is established.

---

## Technology Stack

Document the chosen stack here once decided. Common considerations for a pocket money app:

- **Frontend**: (e.g., React, Vue, Next.js, plain HTML)
- **Backend**: (e.g., Node.js/Express, Python/FastAPI, Go, Rails)
- **Database**: (e.g., SQLite, PostgreSQL, MySQL)
- **Package manager**: (e.g., npm, yarn, pnpm, pip, cargo)

---

## Development Workflow

### Initial Setup

```bash
# Clone the repository
git clone <repo-url>
cd okozukai

# Install dependencies (update command once tech stack is chosen)
# npm install
# pip install -r requirements.txt
# go mod download
```

### Running the Application

```bash
# Update these commands to match your actual stack
# npm run dev
# python main.py
# go run .
```

### Running Tests

```bash
# Update these commands to match your actual test framework
# npm test
# pytest
# go test ./...
```

### Building for Production

```bash
# Update these commands to match your build process
# npm run build
# docker build -t okozukai .
```

---

## Git Conventions

### Branch Naming

- `main` or `master` — stable production code
- `feature/<short-description>` — new features
- `fix/<short-description>` — bug fixes
- `chore/<short-description>` — maintenance, dependency updates, config changes
- `docs/<short-description>` — documentation-only changes
- `claude/<description>-<session-id>` — branches created by Claude AI assistant

### Commit Messages

Follow conventional commits format:

```
<type>(<scope>): <short summary>

<optional body>
```

Types: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `style`, `perf`

Examples:
```
feat(budget): add monthly spending limit feature
fix(auth): correct session expiration handling
docs: update setup instructions in README
chore: upgrade dependencies to latest versions
```

### Pull Requests

- Keep PRs focused on a single concern
- Include a clear description of what changed and why
- Reference related issues with `Closes #<issue-number>`

---

## Code Conventions

Update this section as the project develops to reflect actual patterns used.

### General Principles

- Write self-documenting code; add comments only where intent is non-obvious
- Keep functions small and single-purpose
- Prefer explicit over implicit behavior
- Delete unused code rather than commenting it out

### Naming Conventions

Update with language-specific conventions once the stack is chosen:

- **Variables/functions**: camelCase (JS/TS), snake_case (Python/Ruby), camelCase (Go)
- **Classes/types**: PascalCase across most languages
- **Constants**: SCREAMING_SNAKE_CASE or language idiom
- **Files**: match language convention (kebab-case for JS, snake_case for Python, etc.)

### Error Handling

- Handle errors explicitly; avoid silent failures
- Return meaningful error messages to users
- Log errors with enough context to diagnose issues

---

## Key Domain Concepts

Since this is a pocket money management application, expect these core concepts:

- **Transaction** — a record of money received or spent
- **Category** — classification of a transaction (food, transport, entertainment, etc.)
- **Budget** — a spending limit for a given period or category
- **Balance** — current available amount
- **Period** — a time range for reporting (daily, weekly, monthly)

---

## Environment Variables

Document required environment variables here as they are added. Use an `.env.example` file in the repository to show required keys without actual values.

```bash
# Example (update once actual env vars are determined)
# DATABASE_URL=
# SECRET_KEY=
# PORT=3000
```

Never commit real credentials, API keys, or secrets.

---

## CI/CD

Document the CI/CD pipeline here once configured. Common checks to add:

- Linting
- Type checking
- Unit tests
- Integration tests
- Build verification

---

## AI Assistant Notes

When working on this codebase as an AI assistant:

1. **Read before editing** — always read existing files before modifying them
2. **Stay focused** — make only the changes requested; avoid unrequested refactors
3. **Match existing style** — follow conventions already present in the file being edited
4. **No over-engineering** — prefer simple, direct solutions over clever abstractions
5. **Test your changes** — run the test suite after making changes when possible
6. **Update this file** — keep CLAUDE.md current as the project evolves

### Branch Requirements

When making changes as Claude:
- Develop on branches prefixed with `claude/`
- Branch names must end with the session ID (e.g., `claude/add-feature-FyRYx`)
- Always push with `git push -u origin <branch-name>`
- Never push directly to `main`/`master` without explicit permission
