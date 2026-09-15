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

- .NET SDK 8.0.4xx (pinned in `global.json`)
- Docker, for PostgreSQL, RabbitMQ and the container-backed tests

## Running it

### 1. Start PostgreSQL and RabbitMQ

```bash
docker run -d --name finbeat-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=finbeat_taskmanagement \
  -p 5432:5432 postgres:16-alpine

docker run -d --name finbeat-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management-alpine
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

## Configuration

Every setting can be supplied as an environment variable, with `__` for nesting.

| Setting | Environment variable | Default |
|---|---|---|
| `ConnectionStrings:TaskManagement` | `ConnectionStrings__TaskManagement` | none — startup fails without it |
| `RabbitMq:Host` | `RabbitMq__Host` | `localhost` |
| `RabbitMq:Port` | `RabbitMq__Port` | `5672` |
| `RabbitMq:VHost` | `RabbitMq__VHost` | `/` |
| `RabbitMq:User` / `RabbitMq:Pass` | `RabbitMq__User` / `RabbitMq__Pass` | `guest` / `guest` |
| `OpenTelemetry:OtlpEndpoint` | `OTEL_EXPORTER_OTLP_ENDPOINT` | unset — tracing is exported only when set |

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

Statuses travel as names — `New`, `InProgress`, `Completed`, `Archived` — in requests, responses and
events alike. Failures are RFC 9457 problem details carrying a stable `code` extension; validation
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

- **Dockerfile and docker compose.** The assignment lists them as optional; the `docker run`
  commands above are what this repository has been run with.
- **Task ownership.** The assignment says "a user's tasks", but there is no authentication here and
  no `UserId` on the aggregate, so every task is visible to every caller. Adding it means choosing
  where identity comes from, which is a decision rather than an omission.
- **Consumer-side deduplication.** The outbox gives at-least-once delivery, and the listener owns no
  database to hold an inbox, so a redelivery is logged twice.
