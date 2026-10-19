# MessageHub

Email microservices on .NET 8 with a **message-broker abstraction** that can run on **RabbitMQ** or **Kafka**, through **MassTransit** or **CAP**, and records every published message in an **outbox**.

> **Status: reference skeleton, not a finished product.** The solution builds (`dotnet build`: 0 errors) and the receiving side (HTTP → MongoDB → broker) is implemented. The sending side is intentionally unfinished — see [What is and isn't implemented](#what-is-and-isnt-implemented). Not run end to end in a container.

## Architecture

```
POST /api/email ──► EmailReceiver.API ──► MongoDB (email document)
                         │
                         └─ IMessageBroker.PublishAsync("email-created")
                                 │  1. write OutboxMessage (Pending)  [MassTransit adapters]
                                 │  2. publish to RabbitMQ / Kafka
                                 │  3. mark Processed (or Failed + rethrow)
                                 ▼
                    RabbitMQ / Kafka ──► EmailSender.API ──► IEmailService (SMTP)
```

```
src/
  BuildingBlocks/MessageBroker/
    MessageBroker.Common          IMessageBroker, OutboxMessage, IOutboxStore, OutboxProcessor (BackgroundService)
    MessageBroker.Infrastructure  EF Core outbox store (DbOutboxStore) + OutboxMessageBroker
    MessageBroker.RabbitMQ        MassTransit and CAP adapters (+ Mongo outbox store)
    MessageBroker.Kafka           MassTransit (Kafka rider) and CAP adapters (+ Mongo outbox store)
  Services/
    EmailReceiver.{API,Application,Domain,Infrastructure}   accepts and stores emails, publishes EmailMessageCreatedEvent
    EmailSender.{API,Application,Domain,Infrastructure}     consumes the event, SMTP service, Mongo repository
```

The transport is chosen at start-up from configuration:

| Setting | Values |
|---|---|
| `MessageBroker:Type` | `RabbitMQ` or `Kafka` |
| `MessageBroker:Library` | `MassTransit` or `CAP` |

## What is and isn't implemented

| Area | State |
|---|---|
| `POST /api/email` and `GET /api/email/{id}` (EmailReceiver) | Implemented. Validates the recipient, stores the email in MongoDB, publishes `EmailMessageCreatedEvent` to topic `email-created`. |
| Broker adapters (RabbitMQ/Kafka × MassTransit/CAP) | Implemented as extension methods behind `IMessageBroker`. |
| Outbox on the MassTransit paths | Every publish is journaled in a MongoDB `Outbox` collection (`Pending` → `Processed` / `Failed`). |
| Outbox on the CAP paths | Delegated to CAP, which keeps its own published-message table (SQL Server, see `ConnectionStrings:SqlDb`); no `OutboxMessage` row is written. |
| `OutboxProcessor` (polls every 10 s, republishes `Pending` rows) | Implemented with an EF Core store, but **not registered** by either service (`AddMessageBroker()` is never called), and rows marked `Failed` are not retried. Treat it as a building block, not an active relay. |
| `EmailMessageCreatedConsumer` (EmailSender) | **Placeholder** — it receives the event and does nothing yet. |
| `SmtpEmailService` | Implemented (`System.Net.Mail`, settings from `SmtpSettings`) but not yet called by the consumer. |
| EmailSender with RabbitMQ + CAP | Not wired in `Program.cs` (MassTransit and Kafka are). |
| Automated tests | None yet. |

## Run locally

Prerequisites: .NET 8 SDK (the solution targets `net8.0`), Docker for the infrastructure.

1. Start only the infrastructure (the compose file also defines the two services, but their build contexts are unverified):
   ```bash
   cd MessageHub/src
   docker compose up mongodb rabbitmq
   ```
   The compose file uses the development defaults `guest`/`guest` for RabbitMQ — **local use only**, never reuse them elsewhere.
2. Provide your own settings with .NET user-secrets or environment variables (do not commit them):
   ```bash
   export ConnectionStrings__MongoDb="mongodb://localhost:27017"
   export MessageBroker__Type=RabbitMQ
   export MessageBroker__Library=MassTransit
   # EmailSender also needs SMTP settings:
   export SmtpSettings__Host=...  SmtpSettings__Port=587  SmtpSettings__Username=...  SmtpSettings__Password=...  SmtpSettings__EnableSsl=true  SmtpSettings__FromEmail=...
   ```
   The CAP adapters additionally read `ConnectionStrings:SqlDb` (SQL Server) for CAP's own storage.
3. Run the services in two terminals:
   ```bash
   dotnet run --project Services/EmailReceiver/EmailReceiver.API
   dotnet run --project Services/EmailSender/EmailSender.API
   ```
4. In Development, Swagger UI is served at `/swagger`. Send a test request:
   ```bash
   curl -X POST https://localhost:<port>/api/email -H "Content-Type: application/json" \
        -d '{"recipientEmail":"someone@example.com","subject":"Hi","body":"<b>Hello</b>"}'
   ```

## Design notes and trade-offs
- **Outbox first, then publish.** Journaling the message before it leaves the process means a broker outage leaves an auditable `Failed` record instead of silently dropping the email. A full transactional outbox would write the email and the outbox row in one transaction and let a relay publish asynchronously; this repo has the pieces (`OutboxProcessor`, `DbOutboxStore`) but does not connect them yet.
- **Reflection in the relay.** `OutboxProcessor` resolves the message type from its stored name and invokes `PublishAsync<T>` through reflection. Fine for a demo; production code should use an explicit type registry.
- **Two outbox stores.** EF Core (`DbOutboxStore`) and MongoDB (`MongoOutboxStore`, one per adapter) exist side by side — consolidating them is part of the roadmap.

## Roadmap
- [ ] Implement `EmailMessageCreatedConsumer` → `IEmailService.SendEmailAsync` with retry/backoff and dead-lettering
- [ ] Register the relay (single outbox store) and retry `Failed` rows
- [ ] Idempotent consumer (de-duplicate by message id)
- [ ] Unit tests for the adapters and integration tests with Testcontainers (MongoDB, RabbitMQ, Kafka)
- [ ] Fix the compose build contexts and add a one-command demo
- [ ] Move all connection strings and SMTP credentials to user-secrets/environment variables only

## License
No license file yet — all rights reserved by default. Open an issue if you want to reuse the code.
