# Contributing

## One-time setup after cloning

```bash
git config core.hooksPath .githooks
```

`core.hooksPath` is a per-clone setting and cannot be committed, so every clone has to run this
once. Without it the hooks in `.githooks/` are inert.

## Commit authorship

**Commit messages must not attribute authorship to an AI assistant.** No `Co-Authored-By: Claude`,
no `Generated with ...`, no session links, no `noreply@anthropic.com`, no `🤖` attribution lines —
regardless of which tools were involved in producing the change.

The reasoning is simple: the author of a commit is the person who is accountable for it. Whoever
runs `git commit` has read the change, understands it, and answers for it in review. Tooling that
helped produce the diff is no more part of the authorship record than the IDE, the compiler, or the
documentation that was consulted along the way.

This is enforced by `.githooks/commit-msg`, which rejects the commit and prints the offending lines.
Do not route around it with `git commit --no-verify`.

## Commit messages

[Conventional Commits](https://www.conventionalcommits.org/): `type(scope): subject`.

Types in use here: `feat`, `fix`, `refactor`, `test`, `build`, `chore`, `docs`, `perf`.

- Subject in the imperative mood, under 72 characters, no trailing period.
- Explain **why** in the body, not just what — the diff already shows what changed. Where a
  non-obvious approach was chosen, say what the obvious alternative was and why it was rejected.
- State what was verified, with the actual result (`dotnet test` counts, build warning counts).

## Code conventions

Enforced by the build rather than by review:

- `Directory.Build.props` sets `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild` and
  `GenerateDocumentationFile`. A missing XML `<summary>` on a public member in `src/` is a **build
  error** (CS1591), as is any style rule violation — `.editorconfig` rules carry `warning` severity
  deliberately, because a rule left at `suggestion` is IDE-only and can never fail a build.
- Analyzer suppressions are scoped to the specific file that needs them, never applied repo-wide.
- `Directory.Packages.props` owns every package version (central package management).
  `PackageReference` elements carry no `Version=` attribute.
- `global.json` pins the SDK to 8.0.425.

## Architecture

`FinBeat.TaskManagement.Domain` must depend on **nothing** — zero `PackageReference`, zero
`ProjectReference`, base class library only. This is asserted by tests in
`tests/FinBeat.TaskManagement.UnitTests/Architecture/`, which read the `.csproj` files directly so
the rule holds even while a layer is still empty.

The domain never reads the ambient clock. Time arrives through a `TimeProvider` parameter, which is
what allows tests to assert timestamps by exact equality instead of tolerance.
