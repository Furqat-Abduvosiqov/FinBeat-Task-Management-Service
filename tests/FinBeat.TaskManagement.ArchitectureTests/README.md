# Architecture tests

Executable rules describing the shape of this solution. Unit tests check what code *does*; these
check how the projects are *allowed to depend on each other*, so a violation breaks a test run
instead of surviving until someone notices it in review.

They live in their own project because the rules must reference every layer in the solution. That is
the correct shape for a suite whose subject is the whole dependency graph, and the wrong shape for a
unit test project — which is why these moved out of `FinBeat.TaskManagement.UnitTests`.

## The architecture being enforced

```
Api ────────> Infrastructure ────> Application ──┬──> Domain
                                                 │
Listener ────────────────────────────────────────┴──> Contracts
```

`Api` also references `Application` directly. Both hosts are composition roots.

| Project | May reference | Why |
|---|---|---|
| `Domain` | nothing | The centre. Entities, value objects, domain events, repository contracts. |
| `Contracts` | nothing | The published wire format: flat integration-event records. |
| `Application` | `Domain`, `Contracts` | Use cases. Owns the domain-event → integration-event mapping. |
| `Infrastructure` | `Application` | Adapters: persistence, messaging. Implements contracts the inner layers declare. |
| `Api` | `Application`, `Infrastructure` | HTTP delivery and composition root. |
| `Listener` | `Contracts` | A **separate deployable**. Shares the wire format and nothing else. |

Two rows carry real decisions rather than following from the layer ordering:

- **`Api` names both `Application` and `Infrastructure`**, skipping a rank. A composition root is the
  one place allowed to know the concrete adapters — that is where the container binds them.
- **`Listener` names only `Contracts`.** The specification requires a *separate* listener service.
  Giving it `Application` or `Infrastructure` would hand a log-only service the aggregate, the
  repository and the production database, making "separate service" a naming convention rather than
  something the build enforces.

`Contracts` is deliberately separate from `Domain`: domain events carry value objects with private
constructors, which serialize but cannot deserialize, and putting the aggregate on the wire would
couple two independently deployable services to each other's internals.

## Changing the architecture

Edit `ArchitectureModel.cs`. Nothing else. Every rule and every test case is projected from the
tables in that one file.

## Why three different angles

Each catches something the others cannot.

| Angle | Reads | Catches |
|---|---|---|
| `.csproj` XML | project files on disk | a reference that is declared but not yet used by any code |
| Compiled IL | assemblies, via NetArchTest | a dependency that arrives without ever being declared |
| Package allow-list | `PackageReference` / `FrameworkReference` | technology that arrives as a package, not a project reference |

The first matters most right now. A reference added in the IDE but unused leaves **no trace in the
compiled output** — the compiler omits it — so while the layers are still thin, reading the `.csproj`
is the only angle that sees anything at all.

## Files

| File | Lines | Role |
|---|---|---|
| `ArchitectureModel.cs` | 130 | The architecture, written down once. Layer names and the reference tables. |
| `SolutionLayout.cs` | 102 | Resolves project files by convention and reads what they declare. |
| `ArchitectureData.cs` | 39 | Plumbing: xUnit's `[MemberData]` needs `TheoryData`, not `string[]`. |
| `ProjectReferenceTests.cs` | 57 | Rules over the declared dependency graph. |
| `LayerPurityTests.cs` | 64 | Keeps third-party technology out of the inner layers. |
| `NamespaceConventionTests.cs` | 43 | Keeps namespaces aligned with assemblies. |
| `LayerDependencyTests.cs` | 25 | Rules over compiled IL. |

Two implementation notes worth knowing:

- **`AllowedReferences` is computed**, as the transitive closure of `RequiredReferences`. A second
  hand-kept table would fail *open*: widening a row silently generates fewer test cases, and a theory
  producing fewer cases reports no error — the suite would go greener while getting weaker.
- **Projects resolve by convention** (`<root>/src/<name>/<name>.csproj`) rather than by searching the
  tree. Searching breaks when a second checkout of the repository sits inside it, which a git
  worktree under the repository root produces: every project name then matches twice.

## The rules

**`ProjectReferenceTests` — 14 cases.** The load-bearing ones today.

- `Layer_declares_every_reference_the_architecture_requires` (6) — a required reference is missing.
- `Layer_declares_no_reference_the_architecture_forbids` (6) — dependencies point inward only. This
  is the rule that keeps the Listener a separate service.
- `Dependency_free_layer_declares_no_project_references` (2) — `Domain` and `Contracts` depend on
  nothing.

**`LayerPurityTests` — 5 cases.** Layer ordering cannot stop an ORM or a broker, because those arrive
as packages.

- `Inner_layer_declares_no_unapproved_third_party_reference` (3) — an **allow-list**, so it fails
  *closed*. A deny-list passes whatever nobody thought to forbid, and nobody adding a package goes
  looking for a list of banned ones. Covers `FrameworkReference` too: one
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />` pulls all of ASP.NET Core into a layer
  without a single `PackageReference`.
- `Dependency_free_layer_references_only_the_base_class_library` (2) — asks the runtime which
  directory an assembly actually loaded from, rather than matching a `System.` prefix. That prefix is
  a poor proxy: `System.Data.SqlClient` and `System.Reactive` are ordinary NuGet packages.

**`NamespaceConventionTests` — 4 cases.** Load-bearing, not cosmetic: NetArchTest **cannot see a
dependency on a type in the global namespace**, because forbidden names are matched as namespace
prefixes and such a type has none. Keeping every type under its layer namespace is what closes that
hole. Hosts are exempt — top-level statements put the entry point in the global namespace.

**`LayerDependencyTests` — 20 cases.** One case per forbidden (layer, dependency) edge, so a failure
names both ends rather than reporting that a layer depends on something it shouldn't.

## What these rules do *not* yet prove

Worth stating plainly: **the 20 IL-level cases are vacuous today.** They read compiled assemblies,
and the layers currently hold template files that do not reference each other. They are guardrails
for code about to be written and start doing real work the day the domain lands. Of 43 cases, 23
are checking something real right now.

Two known gaps:

- A `PackageReference` injected from `Directory.Build.props` is invisible to a `.csproj` read.
  Closing it means parsing `obj/project.assets.json`, which is more machinery than the risk warrants.
- The `TimeProvider` mandate (the domain must never read an ambient clock) is not enforced here.
  NetArchTest works on type dependencies, and this is a call to a property getter — below this
  suite's resolution. `Microsoft.CodeAnalysis.BannedApiAnalyzers` is the right tool.

## Running them

```bash
dotnet test tests/FinBeat.TaskManagement.ArchitectureTests
```

**These rules are not wired into CI.** Until they are, they break nobody's build but the author's,
which is the single largest thing missing from this suite.

## Changing a rule

When you add or change a rule, check that it can actually fail. A rule that cannot go red is worse
than no rule, because it reports success. The practice used here is to introduce the violation
deliberately, confirm the expected test fails and names the offender, then revert — for example
adding `Listener -> Application` and confirming
`Layer_declares_no_reference_the_architecture_forbids` fails while everything else stays green.
