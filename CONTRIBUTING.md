# Contributing

## Commit authorship

**Commit messages must not attribute authorship to an AI assistant.** No `Co-Authored-By: Claude`,
no `Generated with ...`, no session links, no `noreply@anthropic.com`, no `🤖` attribution lines -
regardless of which tools were involved in producing the change.

The reasoning is simple: the author of a commit is the person who is accountable for it. Whoever
runs `git commit` has read the change, understands it, and answers for it in review. Tooling that
helped produce the diff is no more part of the authorship record than the IDE, the compiler, or the
documentation that was consulted along the way.

Nothing enforces this automatically. The `.githooks/commit-msg` hook that used to reject such
commits was removed in `645f140`, so the rule now rests on review.

## Commit messages

[Conventional Commits](https://www.conventionalcommits.org/): `type(scope): subject`.

Types in use here: `feat`, `fix`, `refactor`, `test`, `build`, `chore`, `docs`, `perf`.

- Subject in the imperative mood, under 72 characters, no trailing period.
- Explain **why** in the body, not just what - the diff already shows what changed. Where a
  non-obvious approach was chosen, say what the obvious alternative was and why it was rejected.
- State what was verified, with the actual result (`dotnet test` counts, build warning counts).

## Code conventions

Enforced by the build rather than by review:

- `Directory.Build.props` sets `GenerateDocumentationFile`, so a missing XML `<summary>` on a public
  member in `src/` is a **warning** (CS1591). It is not an error: this repository has no
  `TreatWarningsAsErrors`, no `EnforceCodeStyleInBuild` and no `.editorconfig`, so a zero-warning
  build is a convention whoever runs it upholds, not something the build can fail on. Keep it at
  zero regardless - every commit here states its warning count.
- Analyzer suppressions are scoped to the specific file that needs them, never applied repo-wide.
- `Directory.Packages.props` owns every package version (central package management).
  `PackageReference` elements carry no `Version=` attribute.
- `global.json` pins the SDK to 8.0.425.

## Architecture

`FinBeat.TaskManagement.Domain` must depend on **nothing** - zero `PackageReference`, zero
`ProjectReference`, base class library only. This is asserted by tests in
`tests/FinBeat.TaskManagement.ArchitectureTests/`, which read the `.csproj` files directly so
the rule holds even while a layer is still empty.

The domain never reads the ambient clock. Time arrives through a `TimeProvider` parameter, which is
what allows tests to assert timestamps by exact equality instead of tolerance.
