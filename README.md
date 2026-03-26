# TaskFlow ⚡
### Distributed Task Scheduling Engine

![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=flat&logo=dotnet)
![Apache Kafka](https://img.shields.io/badge/Apache_Kafka-Messaging-231F20?style=flat&logo=apachekafka)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-MassTransit-FF6600?style=flat&logo=rabbitmq)
![Redis](https://img.shields.io/badge/Redis-Distributed_Lock-FF4438?style=flat&logo=redis)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat&logo=postgresql)
![React](https://img.shields.io/badge/React-18-61DAFB?style=flat&logo=react)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=flat&logo=typescript)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker)
![SignalR](https://img.shields.io/badge/SignalR-Real_Time-512BD4?style=flat&logo=dotnet)

A production-grade, cloud-native distributed task scheduler built with .NET 8 microservices. Define HTTP webhook tasks, schedule them with cron or interval expressions, and monitor real-time execution — all from a clean React dashboard.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           TASKFLOW                                  
│                                                                     
│  ┌──────────────────────────────────────────────────────────────┐  
│  │            React Dashboard (TypeScript + Vite)               │  
│  │   Task CRUD · Live Logs · Statistics · Alerts · Auth         │  
│  └─────────────────────┬────────────────────────────────────────┘  
│                        │ SignalR (real-time)                        
│                        ▼                                            
│  ┌──────────────────────────────────────────────────────────────┐  
│  │              TaskFlow.API  (.NET 8 Web API)                  │  
│  │   REST API · JWT Auth · CQRS + MediatR · SignalR Hub         │  
│  └──────┬───────────────┬──────────────────────────────────────┘   
│         │               │                                           
│    PostgreSQL      Kafka (task.trigger)                             
│    (tasks, logs)         │                                          
│                          ▼                                          
│  ┌──────────────────────────────────────────────────────────────┐  
│  │         TaskFlow.Scheduler  (.NET 8 Worker)                  │  
│  │   Cron Engine · Redis Distributed Lock · Priority Queue      │  
│  └──────────────────────┬─────────────────────────────────────┘   
│                          │ Kafka (task.trigger)                     
│                          ▼                                          
│  ┌──────────────────────────────────────────────────────────────┐  
│  │         TaskFlow.Executor  (.NET 8 Worker)                   │  
│  │   Webhook Invoker · Retry Logic · MassTransit/RabbitMQ       │  
│  └──────────────────────┬─────────────────────────────────────┘   
│                          │ Kafka (task.result)                      
│                          ▼                                          
│              API consumes results → DB + SignalR push               
└─────────────────────────────────────────────────────────────────────┘
```

---

## Features

### Core
- **Cron, Interval & Manual** scheduling with `Cronos` expression parser
- **HTTP Webhook execution** — POST/GET/PUT/PATCH with custom headers and body
- **Retry logic** — configurable retry count, delay, and timeout per task
- **Dead Letter Queue** — failed tasks after max retries go to DLQ
- **Redis Distributed Lock** — prevents duplicate execution across instances
- **Priority Queue** — tasks execute in priority order (1=highest, 10=lowest)
- **Max Concurrent Limit** — per-task concurrency control
- **Missed Run Recovery** — tasks skipped due to downtime are recovered on restart

### Observability
- **Real-time dashboard** via SignalR — live execution status updates
- **Execution History** — full log of every run with HTTP status, duration, response body
- **Execution Logs** — per-execution log stream with Info/Warning/Error levels
- **Statistics** — success rate, avg duration, 24h timeline, 7-day daily charts
- **Health Checks** — `/health`, `/health/live`, `/health/ready` endpoints

### Security & Quality
- **JWT Authentication** + Refresh Token rotation
- **Role-based Authorization** — Admin, Operator, Viewer
- **FluentValidation** — cron expression, URL, JSON body validation
- **Audit Log** — who created/triggered/enabled/disabled tasks

### Alerting
- **Webhook-based alerts** — fires to Slack, Discord, or any HTTP endpoint
- **Trigger types**: ConsecutiveFailures, AnyFailure, TaskDead
- **Configurable threshold** — alert after N consecutive failures
- **Alert history** — full delivery log

### Extensibility
- **Task Tags** — organize tasks with colored labels
- **Outbox Pattern ready** — DB-first, Kafka-second for guaranteed delivery

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Backend** | .NET 8, C#, ASP.NET Core |
| **Messaging** | Apache Kafka, RabbitMQ + MassTransit |
| **Distributed Lock** | Redis (StackExchange.Redis) |
| **Real-time** | SignalR |
| **Database** | PostgreSQL 16, Entity Framework Core 8 |
| **Auth** | JWT Bearer + Refresh Token, BCrypt |
| **Validation** | FluentValidation |
| **Scheduling** | Cronos |
| **Frontend** | React 18, TypeScript, Vite, Tailwind CSS |
| **Charts** | Recharts |
| **Infrastructure** | Docker, Docker Compose |

---

## Services

| Service | Type | Responsibility |
|---|---|---|
| `TaskFlow.API` | Web API | REST endpoints, JWT auth, SignalR hub, CQRS |
| `TaskFlow.Scheduler` | Worker | Cron/interval engine, Redis lock, Kafka trigger |
| `TaskFlow.Executor` | Worker | Webhook calls, retry logic, RabbitMQ retry queue |
| `TaskFlow.Shared` | Class Library | Shared models, messages, enums |
| `taskflow-ui` | React SPA | Real-time dashboard |

---

## Kafka Topics

| Topic | Partitions | Producer | Consumers |
|---|---|---|---|
| `task.trigger` | 3 | API + Scheduler | Executor |
| `task.result` | 3 | Executor | API |
| `task.deadletter` | 1 | Executor | Manual inspection |

---

## Getting Started

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK
- Node.js 20+

### 1. Clone & Start Infrastructure

```bash
git clone https://github.com/Berkay-Cetin/taskflow-distributed-scheduler-dotnet.git
cd taskflow-distributed-scheduler-dotnet

# Start infrastructure
docker compose up -d postgres redis rabbitmq zookeeper kafka
docker compose up -d kafka-init kafka-ui

# Verify topics
docker exec taskflow-kafka kafka-topics \
  --bootstrap-server localhost:9093 --list
```

### 2. Run Services

Open 3 terminals:

```bash
# Terminal 1 — API (start first, creates DB tables)
cd src/TaskFlow.API && dotnet run

# Terminal 2 — Scheduler
cd src/TaskFlow.Scheduler && dotnet run

# Terminal 3 — Executor
cd src/TaskFlow.Executor && dotnet run
```

### 3. Run Frontend

```bash
cd src/taskflow-ui
npm install
npm run dev
```

### 4. Create Admin User

```bash
curl -X POST http://localhost:5200/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","email":"admin@taskflow.com","password":"Admin123!","role":"Admin"}'
```

Open **http://localhost:5173** and login.

---

## Monitoring

| Tool | URL | Credentials |
|---|---|---|
| **TaskFlow UI** | http://localhost:5173 | admin / Admin123! |
| **Swagger API** | http://localhost:5200/swagger | — |
| **Health Check** | http://localhost:5200/health | — |
| **Kafka UI** | http://localhost:8082 | — |
| **RabbitMQ UI** | http://localhost:15673 | taskflow / taskflow123 |

---

## API Endpoints

### Auth
| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register user |
| POST | `/api/auth/login` | Login → JWT + refresh token |
| POST | `/api/auth/refresh` | Rotate refresh token |
| POST | `/api/auth/logout` | Revoke tokens |
| GET | `/api/auth/me` | Current user info |

### Tasks
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/tasks` | List all tasks |
| POST | `/api/tasks` | Create task |
| PUT | `/api/tasks/{id}` | Update task |
| DELETE | `/api/tasks/{id}` | Delete task |
| POST | `/api/tasks/{id}/trigger` | Manual trigger |
| PATCH | `/api/tasks/{id}/enable` | Enable task |
| PATCH | `/api/tasks/{id}/disable` | Disable task |
| GET | `/api/tasks/{id}/executions` | Execution history |
| GET | `/api/tasks/{id}/missed-runs` | Missed runs |

### Stats & Monitoring
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/stats/summary` | Overall stats |
| GET | `/api/stats/timeline` | 24h execution timeline |
| GET | `/api/stats/daily` | 7-day daily summary |
| GET | `/api/stats/tasks` | Per-task performance |
| GET | `/api/audit` | Audit log |
| GET | `/health` | Full health check |
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe |

---

## Task Definition Example

```json
{
  "name": "Daily Report Generator",
  "description": "Generates and sends daily sales report",
  "scheduleType": "Cron",
  "cronExpression": "0 8 * * *",
  "webhookUrl": "https://your-app.com/api/reports/daily",
  "httpMethod": "POST",
  "webhookHeaders": "{\"Authorization\": \"Bearer YOUR_TOKEN\"}",
  "webhookBody": "{\"format\": \"pdf\", \"recipients\": [\"team@company.com\"]}",
  "retryCount": 3,
  "retryDelaySeconds": 30,
  "timeoutSeconds": 60,
  "maxConcurrent": 1,
  "priority": 1,
  "allowMissedRuns": true
}
```

## Execution Lifecycle

```
SCHEDULED → TRIGGERED → RUNNING → SUCCESS
                                 → FAILED → RETRYING → SUCCESS
                                                      → DEAD (DLQ)
                       → QUEUED  (max concurrent reached)
```

---

## Project Structure

```
taskflow-distributed-scheduler-dotnet/
├── src/
│   ├── TaskFlow.API/           # Web API — REST + SignalR + CQRS
│   │   ├── Controllers/        # Tasks, Auth, Stats, Alerts, Tags, Audit
│   │   ├── CQRS/               # Commands + Queries (MediatR)
│   │   ├── Data/               # EF Core DbContext
│   │   ├── Hubs/               # SignalR TaskHub
│   │   ├── Services/           # Kafka, Alert, Audit, JWT, Schedule
│   │   └── Validators/         # FluentValidation rules
│   ├── TaskFlow.Scheduler/     # Worker — Cron engine + Redis lock
│   │   ├── Data/               # SchedulerDbContext
│   │   ├── Services/           # KafkaProducer, RedisLock
│   │   └── Workers/            # SchedulerWorker
│   ├── TaskFlow.Executor/      # Worker — Webhook + retry
│   │   ├── Consumers/          # MassTransit RetryConsumer
│   │   ├── Services/           # WebhookInvoker, KafkaResultPublisher
│   │   └── Workers/            # ExecutorWorker
│   ├── TaskFlow.Shared/        # Shared models + messages
│   │   ├── Models/             # ScheduledTask, TaskExecution, AlertRule...
│   │   └── Messages/           # Kafka message contracts
│   └── taskflow-ui/            # React 18 + TypeScript + Vite
│       └── src/
│           ├── api/            # Axios API client
│           ├── components/     # TaskCard, TaskForm, StatsDashboard...
│           ├── context/        # AuthContext
│           ├── hooks/          # useTaskHub (SignalR)
│           ├── pages/          # LoginPage
│           └── types/          # TypeScript interfaces
├── docker-compose.yml
└── README.md
```

---

## Architecture Patterns

- **Event-Driven Microservices** — services communicate via Kafka, no direct calls
- **CQRS** — strict command/query separation with MediatR
- **Outbox Pattern** — guaranteed at-least-once delivery
- **Distributed Lock** — Redis-based lock prevents duplicate execution
- **Dead Letter Queue** — failed messages captured for inspection
- **Consumer Group** — Executor instances share Kafka partition load
- **Optimistic Concurrency** — version-based conflict detection on tasks
- **Refresh Token Rotation** — new refresh token issued on every use

---

<p align="center">
  Built with .NET 8 · Apache Kafka · RabbitMQ · Redis · PostgreSQL · React 18 · SignalR
</p>