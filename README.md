# CampFix — Campus Facility Incident & Triage Platform

A real-time web application engineered on the **.NET 8 ecosystem** for reporting, triaging, and tracking campus infrastructure faults across lecture halls, computer laboratories, and student residences.

Built for **DCIT 318 (Programming II / Systems Development)**.

---

## 🏛️ System Architecture

CampFix is structured using modern, pragmatic .NET patterns:

- **Backend:** ASP.NET Core Minimal APIs with strongly typed DTOs and EF Core.
- **Database Engine:** Dual-mode persistence:
  - **Cloud:** **Neon Serverless PostgreSQL** (via `Npgsql.EntityFrameworkCore.PostgreSQL`)
  - **Local Fallback:** **SQLite** (`campfix.db`) automatically enabled if no cloud connection string is provided.
- **Real-Time Layer:** **ASP.NET Core SignalR WebSockets** for bi-directional event broadcasts (incident creation, status updates, and triage comments).
- **Interactive API Documentation:** **Swagger / OpenAPI 3.0** at `/swagger`.
- **Frontend:** Responsive, modern dashboard in `wwwroot/` styled with Tailwind CSS, Font Awesome icons, and native SignalR client sync.

```
                                CampFix Architecture
                                
  ┌───────────────────────────────────────────────────────────────────────────┐
  │                           Frontend (wwwroot)                              │
  │   • Tailwind CSS + Vanilla JS Single Page Application                     │
  │   • Live KPI Analytics Bar (Total, Pending, In Progress, Resolved)        │
  │   • Incident Reporting Modal with Category & Building Selectors           │
  │   • Real-Time WebSockets Listener (Live updates without page refresh)     │
  └────────────────────────┬──────────────────────────────────▲───────────────┘
                           │ HTTP REST (JSON)                 │ SignalR WebSockets
                           ▼                                  │
  ┌───────────────────────────────────────────────────────────┴───────────────┐
  │                         ASP.NET Core Backend (net8.0)                     │
  │                                                                           │
  │  ├── Endpoints:     RESTful Minimal APIs (/api/issues, /api/meta)         │
  │  ├── SignalR Hub:   Live Event Broadcasts (/hubs/issues)                  │
  │  ├── Data Layer:    EF Core + Automatic Seeding                          │
  │  └── Database:      Neon Cloud PostgreSQL  (Fallback: Local SQLite)       │
  └───────────────────────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start Guide

### 1. Prerequisites
- **.NET 8.0 SDK** (Installed in `~/.dotnet` or system path)

### 2. Configure Database (Optional — Neon PostgreSQL)
CampFix runs out-of-the-box using local SQLite (`campfix.db`). 

To connect to your **Neon Cloud PostgreSQL** database:
1. Create a `.env` file in the root directory:
   ```bash
   cp .env.example .env
   ```
2. Paste your Neon connection string into `.env`:
   ```env
   DATABASE_URL=postgresql://neondb_owner:YOUR_PASSWORD@ep-example-12345.us-east-2.aws.neon.tech/neondb?sslmode=require
   ```

*If `.env` is omitted or `DATABASE_URL` is empty, CampFix automatically falls back to local SQLite.*

### 3. Run the Application
```bash
~/.dotnet/dotnet run
# Or if dotnet is in your PATH:
# dotnet run
```

Access the application in your browser:
* **Web UI Dashboard:** [http://localhost:5000](http://localhost:5000)
* **Interactive Swagger Docs:** [http://localhost:5000/swagger](http://localhost:5000/swagger)

---

## 🛠️ REST API Specification

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/issues` | List issues with optional filters (`status`, `category`, `building`, `search`) |
| `GET` | `/api/issues/stats` | Aggregate metrics (Total, Reported, In Progress, Resolved, Critical) |
| `GET` | `/api/issues/{id}` | Detailed ticket view with activity audit trail |
| `POST` | `/api/issues` | Submit a new incident report (broadcasts via SignalR) |
| `PATCH` | `/api/issues/{id}/status` | Triage ticket: transition status, assign staff, add notes |
| `POST` | `/api/issues/{id}/comments` | Post a follow-up comment or progress report |
| `DELETE` | `/api/issues/{id}` | Cancel/delete an issue ticket |
| `GET` | `/api/meta` | Available categories, buildings, priorities, and statuses |

---

## ⚡ Real-Time SignalR Events

Connected clients subscribe to `/hubs/issues` and react to:
* `IssueCreated`: Prepends new incident to feed & updates KPI counts.
* `IssueStatusUpdated`: Transitions ticket status badge and animates changes.
* `CommentAdded`: Appends message to the active ticket timeline.
* `IssueDeleted`: Removes ticket card from display.

---
