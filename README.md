# EduBoost

**Identify slow learners for remedial teaching and capacity building with innovative methods and 24/7 AI assistance.**

EduBoost is an ASP.NET Core MVC (.NET 10) platform built with Entity Framework Core, SQL Server LocalDB, ASP.NET Core Identity, OpenAPI/Swagger, and Google Gemini / Anthropic Claude AI integrations. Teachers record students and assessment marks; the system calculates a weighted risk score, flags slow learners, recommends VARK-matched innovative teaching methods, provides a 24/7 AI Teaching Assistant chatbot, and generates personalized 4-week remedial plans.

> For full architecture diagrams, service internals, and developer guides, see [DOCUMENTATION.md](DOCUMENTATION.md) or visit `/Documentation` inside the running app.

## Requirements

- Visual Studio 2022/2026 with the **ASP.NET and web development** workload
- **.NET 10 SDK**
- SQL Server LocalDB (installed with Visual Studio)

## Run the project

1. Open `EduBoost.slnx` in Visual Studio.
2. Press **F5** (or `dotnet run` from this folder).
3. The application automatically:
   - creates/updates the `EduBoostDb` LocalDB database using EF Core migrations,
   - creates the `Admin` and `Teacher` roles,
   - seeds demo students, subjects, assessments, the innovative methods library, and a sample remedial plan,
   - runs slow-learner detection once so the dashboard has data.
4. Log in or register. Development demo accounts:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@eduboost.local` | `Admin@123` |
| Teacher | `teacher@eduboost.local` | `Teacher@123` |

New self-registered accounts receive the **Teacher** role. Admin sees the **Subjects** administration menu and Admin-only method management actions..

## Features

- **24/7 AI Assistant Chatbot (Google Gemini)**: Floating bottom-right widget (`_ChatbotWidget.cshtml`, `chatbot.js`) backed by `GeminiChatService` and `/api/chatbot/message` with a 3-tier pipeline (sub-20ms instant engine → live Gemini API with 6s timeout & 3-minute cached school stats → pedagogical offline fallback).
- **AI Remedial Plans (`AiPlanService`)**: Multi-provider 4-week plan generation (Google Gemini primary → Anthropic Claude secondary → deterministic VARK fallback).
- **Slow-learner risk engine (`Services/SlowLearnerService.cs`)**: Weighted 60% marks deficit, 25% attendance deficit, and 15% declining trend (`Math.Max(0, -(last - first))`). Flagged when average < 40% or risk ≥ 45.
- **Students & Assessments CRUD**: Searchable roster, attendance tracking, VARK learning style diagnostic quiz, and percentage-validated assessment recording.
- **Progress logs**: Score-after tracking, improvement badges, and plan completion lifecycle.
- **Dashboard, Landing Showcase & Executive KPIs**: Default `/Dashboard` with Chart.js bar/doughnut visualizations, `/Home/Index` landing showcase, Light (Off-White) & Dark themes persisted in `localStorage`, and collapsible navigation sidebar.
- **Live Notifications (`/api/notifications`)**: Topbar bell dropdown surfacing high-risk student alerts, active remedial plans, and low assessment warnings.
- **In-App Documentation Hub (`/Documentation`) & Swagger UI (`/swagger`)**: Interactive docs portal and OpenAPI specification.
- **Modern Auth & Google OAuth**: Split-card Login/Register pages (`_AuthLayout.cshtml`), simulated social SSO modal, optional live Google OAuth, and device/URL avatar uploads.
- **SQL Reporting Objects**: `vw_StudentPerformance` view and `sp_GetSlowLearners` stored procedure (`Data/Sql/ReportingObjects.sql`), plus CSV export and `/SlowLearners/FromStoredProcedure`.
- **Custom request logging middleware & xUnit tests**: `Middleware/RequestLoggingMiddleware.cs` and `tests/EduBoost.Tests/SlowLearnerRiskTests.cs`.

## AI & OAuth Configuration (optional)

Configure Google Gemini (used by both the 24/7 Chatbot and Remedial Plan Generator) and/or Anthropic Claude in `appsettings.json` or `dotnet user-secrets`:

```json
"Gemini": {
  "ApiKey": "YOUR-GEMINI-API-KEY",
  "Model": "gemini-3.5-flash-lite",
  "FallbackModel": "gemini-3.5-flash"
},
"Ai": {
  "ApiKey": "sk-ant-...",
  "Model": "claude-sonnet-4-5"
},
"Authentication": {
  "Google": {
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

Without external API keys, both `GeminiChatService` (instant engine + pedagogical fallback) and `AiPlanService` (deterministic VARK fallback) continue working offline.

## Database & Tests

- Connection string: `appsettings.json` → `DefaultConnection` (`(localdb)\MSSQLLocalDB`, database `EduBoostDb`).
- Migrations: `Data/Migrations/`.
- Apply manually or run tests:

```powershell
dotnet ef database update
dotnet test
```

To reset the demo data, delete the `EduBoostDb` database and run the app again.

## REST API Quick Reference

```http
GET    http://localhost:5168/api/students
GET    http://localhost:5168/api/students/slow
GET    http://localhost:5168/api/students/1
POST   http://localhost:5168/api/students
PUT    http://localhost:5168/api/students/1
DELETE http://localhost:5168/api/students/1
POST   http://localhost:5168/api/chatbot/message
GET    http://localhost:5168/api/chatbot/status
GET    http://localhost:5168/api/notifications
GET    http://localhost:5168/swagger
```

POST `/api/chatbot/message` body example:

```json
{
  "message": "How is the slow learner risk score calculated in EduBoost?",
  "history": []
}
```

POST/PUT `/api/students` body example:

```json
{
  "rollNo": "S20",
  "fullName": "New Student",
  "className": "9-B",
  "parentEmail": "parent@example.com",
  "attendancePercent": 80,
  "learningStyle": "Visual"
}
```

## Project Structure

```text
Areas/Identity/   Split-card Login/Register pages & Manage Profile (avatar uploads)
Controllers/      MVC controllers, NotificationsController, + Api/ (StudentsApiController, ChatbotController)
Data/             DbContext, DbSeeder, migrations, SQL reporting objects
Middleware/       Request logging middleware
Models/           Domain models, StudentPerformanceReport, and DTOs
Services/         SlowLearnerService, AiPlanService, GeminiChatService
ViewModels/       Dashboard view model
Views/            Razor MVC views, Documentation Hub, _Layout, _AuthLayout, _ChatbotWidget
tests/            xUnit test project (EduBoost.Tests)
wwwroot/          Bootstrap, jQuery, site/home/auth/chatbot CSS & JS, uploaded avatars
```
