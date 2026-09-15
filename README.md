# FinBeat Task Management

A task management service: create, read, update and delete tasks over HTTP, with every change
published as an integration event and logged by a separate listener.

## Layout

```
src/
  FinBeat.TaskManagement.Domain          the aggregate and its rules. Zero dependencies.
  FinBeat.TaskManagement.Contracts       the wire format. Zero dependencies.
  FinBeat.TaskManagement.Application     use cases, on Result<T>. EF Core and nothing else.
  FinBeat.TaskManagement.Infrastructure  PostgreSQL, MassTransit, the transactional outbox.
  FinBeat.TaskManagement.Api             minimal API endpoints, validation, Swagger.
  FinBeat.TaskManagement.Listener        consumes the events and logs them. Contracts only.
sql/                                     Задание 2, the daily-payments table function.
tests/                                   architecture, unit and integration suites.
```

Dependencies point inward. `Listener` is a separate deployable and shares only the wire format with
the API, which is why it references `Contracts` and nothing else.

## Prerequisites

- Docker, with Compose v2 - enough on its own to run the whole stack
- .NET SDK 8.0.4xx (pinned in `global.json`) - only to run the hosts from source, or the tests

## Running it

### Everything in containers

```bash
docker compose up -d --build
```

| | |
|---|---|
| API | http://localhost:8080 — Swagger at `/swagger` |
| RabbitMQ management | http://localhost:15672 — `guest` / `guest` |
| Jaeger | http://localhost:16686 |

Compose starts them in the order the system needs rather than all at once:

1. `postgres`, `rabbitmq` and `jaeger` come up, the first two with health checks.
2. `migrator` waits for PostgreSQL to be **healthy**, applies the migrations and exits. Schema
   changes stay a deploy step: no host migrates on boot, so none needs DDL rights at runtime and
   two instances cannot race each other.
3. `listener` waits for RabbitMQ, and declares the consumer queues.
4. `api` waits for the migrator to have **exited successfully** and for the listener to have
   started — so the schema is there before the first request, and the earliest events it publishes
   reach a bound queue rather than the `unroutable` one.

Every port and credential has a default compiled into `docker-compose.yml`, so no `.env` is needed.
Copy `.env.example` to `.env` to change one — most often a port already taken by something you
started by hand.

```bash
docker compose logs -f api listener   # both hosts also write a rolling file to /app/logs
docker compose down                   # add -v to drop the database volume too
```

The two host images publish framework-dependent onto the runtime images and run as the non-root
`app` user. The listener's base is `runtime` rather than `aspnet`: it is a worker that references no
web framework. `migrator` is the odd one out at about 1.6 GB, because `dotnet ef` needs the SDK —
which is the reason it is a container that exits rather than anything the running hosts carry.

## Running the hosts from source

### 1. Start PostgreSQL, RabbitMQ and Jaeger

```bash
docker run -d --name finbeat-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=finbeat_taskmanagement \
  -p 5432:5432 postgres:16-alpine

docker run -d --name finbeat-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine

docker run -d --name finbeat-jaeger -p 16686:16686 -p 4317:4317 -p 4318:4318 jaegertracing/jaeger:2.21.0
```

### 2. Apply the migrations

Schema changes are a deploy step, never applied at startup — a host that migrates on boot races
every other instance and needs DDL rights at runtime.

```bash
dotnet tool restore

ConnectionStrings__TaskManagement="Host=localhost;Port=5432;Database=finbeat_taskmanagement;Username=postgres;Password=postgres" \
dotnet ef database update \
  --project src/FinBeat.TaskManagement.Infrastructure \
  --startup-project src/FinBeat.TaskManagement.Infrastructure \
  --context ApplicationDbContext
```

### 3. Run the listener, then the API

Order matters on a cold broker. Consumer queues and their bindings are declared by the **listener**
at startup, and a fanout exchange with nothing bound to it discards what it cannot route — silently,
with no error returned to the publisher.

```bash
dotnet run --project src/FinBeat.TaskManagement.Listener   # logs each event it receives
dotnet run --project src/FinBeat.TaskManagement.Api        # Swagger UI at /swagger
```

`appsettings.Development.json` already points both at the containers above, so no environment
variables are needed for a local run. The API starts even when RabbitMQ is down: publishes go to the
outbox table, and delivery retries in the background.

### The `unroutable` queue

Start them the other way round anyway and nothing is lost. Every event exchange names an alternate
exchange, so an event published while no consumer queue is bound is diverted to the durable
`unroutable` queue instead of being dropped:

```bash
curl -s -u guest:guest http://localhost:15672/api/queues/%2F/unroutable
```

Messages sitting there mean events were published with nothing listening. Nothing drains that queue
automatically — it is a place to look, not a recovery mechanism.

The alternate exchange is an *exchange argument*, so it is fixed when the exchange is first declared.
Pointing this build at a broker that already carries exchanges declared without it fails the publish
with `PRECONDITION_FAILED - inequivalent arg 'alternate-exchange'`; the event stays in the outbox and
retries. Delete the four `FinBeat.TaskManagement.Contracts.Tasks:*` exchanges and it recovers on the
next attempt.

### Tracing

Both hosts export OpenTelemetry traces over OTLP, and `appsettings.Development.json` points them at
the Jaeger container above — a local run is traced with no further setup. Leave `OtlpEndpoint` empty
and nothing is exported at all, rather than every span failing against a collector that is not there.

Open http://localhost:16686 and pick `FinBeat.TaskManagement.Api`. One request spans both services:

```
PUT /tasks/{id:guid}/status       FinBeat.TaskManagement.Api
  finbeat_taskmanagement          Npgsql, once per statement
  outbox send                     the publish, inside the transaction
  outbox process                  delivery to the broker, after the commit
  TaskStatusChanged send
  task-status-changed receive     FinBeat.TaskManagement.Listener
  task-status-changed process
```

Trace context survives both the outbox and the broker, so what the listener does is joined to the
request that caused it instead of surfacing as an unrelated trace. The API traces ASP.NET Core,
HttpClient, Npgsql and MassTransit; the listener owns no database, so it traces MassTransit alone.

Jaeger v2 serves the UI on 16686 and speaks OTLP on 4317 (gRPC, what the exporter defaults to) and
4318 (HTTP). Its query API is `/api/v3/...`; the v1 `/api/services` path is gone.

## Configuration

Every setting can be supplied as an environment variable, with `__` for nesting.

| Setting | Environment variable | Default |
|---|---|---|
| `ConnectionStrings:TaskManagement` | `ConnectionStrings__TaskManagement` | none — startup fails without it |
| `RabbitMq:Host` | `RabbitMq__Host` | `localhost` |
| `RabbitMq:Port` | `RabbitMq__Port` | `5672` |
| `RabbitMq:VHost` | `RabbitMq__VHost` | `/` |
| `RabbitMq:User` / `RabbitMq:Pass` | `RabbitMq__User` / `RabbitMq__Pass` | `guest` / `guest` |
| `OpenTelemetry:OtlpEndpoint` | `OTEL_EXPORTER_OTLP_ENDPOINT` | unset outside Development, where it points at Jaeger; tracing is exported only when set |

The connection string is validated at startup, so a missing one fails immediately and names the key
rather than surfacing as a null reference on the first query.

## API

| | | |
|---|---|---|
| `POST` | `/tasks` | 201 with a `Location` header |
| `GET` | `/tasks?status=&page=&pageSize=` | 200, one page, oldest first |
| `GET` | `/tasks/{id}` | 200 / 404 |
| `PUT` | `/tasks/{id}` | 200 / 400 / 404 |
| `PUT` | `/tasks/{id}/status` | 200 / 400 / 404 / 409 |
| `DELETE` | `/tasks/{id}` | 204 / 404 |

Statuses travel as numbers — `1` New, `2` InProgress, `3` Completed, `4` Archived — in requests and
responses. The OpenAPI document spells out what each number means, taken from the summaries on the
enum itself, so the two cannot drift; Swagger UI shows them under the field. The `?status=` query
also accepts the name, since that binder parses both, but the number is what is documented.

Integration events are the exception and carry the **name**: the API is versioned and documented,
while consumers are not redeployed alongside it, and a name survives a renumbering they never hear
about.

Failures are RFC 9457 problem details carrying a stable `code` extension; validation
failures add per-field `errors`.

Swagger UI is served in Development only, at `/swagger`.

## Tests

```bash
dotnet test -c Release                                # everything
dotnet test -c Release --filter "Category!=RequiresDocker"   # no Docker needed
```

The container-backed suites start their own PostgreSQL through Testcontainers; they do not use the
container from step 1.

## Задание 2 — daily payments

`sql/postgresql/client_daily_payments.sql` is the table function, with its table and index. Its
output was checked against both worked examples in the assignment. `sql/sqlserver/` carries the same
logic in the T-SQL types the assignment states.

```bash
docker exec -i finbeat-postgres psql -U postgres -d finbeat_taskmanagement < sql/postgresql/client_daily_payments.sql
docker exec finbeat-postgres psql -U postgres -d finbeat_taskmanagement \
  -c "SELECT * FROM client.get_daily_payments(1, '2022-01-02', '2022-01-07');"
```

## Not included

- **Task ownership.** The assignment says "a user's tasks", but there is no authentication here and
  no `UserId` on the aggregate, so every task is visible to every caller. Adding it means choosing
  where identity comes from, which is a decision rather than an omission.
- **Consumer-side deduplication.** The outbox gives at-least-once delivery, and the listener owns no
  database to hold an inbox, so a redelivery is logged twice.
