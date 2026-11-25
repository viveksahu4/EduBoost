# EduBoost — Comprehensive Project Documentation & Service Reference

> **EduBoost** is an enterprise-grade learning analytics and early intervention platform built on **ASP.NET Core 10**, designed to identify academically vulnerable or slow learners early, map their cognitive learning modalities (VARK), assist educators 24/7 via a **Google Gemini AI Chatbot**, and build personalized capacity through structured remedial teaching action plans.

---

## Table of Contents
1. [Architecture & System Overview](#1-architecture--system-overview)
2. [Teacher Operations Guide](#2-teacher-operations-guide)
3. [Administrator Governance Guide](#3-administrator-governance-guide)
4. [Diagnostic Risk Engine & Scoring Formula](#4-diagnostic-risk-engine--scoring-formula)
5. [24/7 AI Assistant Chatbot (Google Gemini)](#5-247-ai-assistant-chatbot-google-gemini)
6. [AI Remedial Action Planner](#6-ai-remedial-action-planner)
7. [UI/UX, Theme System & Landing Portal](#7-uiux-theme-system--landing-portal)
8. [Social Authentication & Live Google OAuth Setup](#8-social-authentication--live-google-oauth-setup)
9. [Developer & Technical Architecture Reference](#9-developer--technical-architecture-reference)
10. [REST API Endpoints](#10-rest-api-endpoints)
11. [Troubleshooting & Frequently Asked Questions](#11-troubleshooting--frequently-asked-questions)

---

## 1. Architecture & System Overview

EduBoost uses a modern multi-tiered **ASP.NET Core 10** architecture with **Entity Framework Core**, **SQL Server LocalDB**, **ASP.NET Core Identity**, **OpenAPI/Swagger**, and **Google Gemini / Anthropic Claude** AI integrations.

```text
EduBoost/
├── Areas/Identity/                    # ASP.NET Core Identity (Login, Register, Profile Management)
│   └── Pages/Account/
│       ├── Login.cshtml               # Split-card sign-in + 24/7 AI banner + social SSO modal
│       ├── Register.cshtml            # Split-card sign-up (auto-assigns Teacher role) + social SSO modal
│       └── Manage/Index.cshtml        # Profile management, password & device/URL avatar upload
├── Controllers/                       # MVC & REST API Controllers
│   ├── Api/
│   │   ├── ChatbotController.cs       # 24/7 Gemini AI chatbot API (/api/chatbot/message, /status)
│   │   └── StudentsApiController.cs   # Student CRUD REST API (/api/students)
│   ├── AssessmentsController.cs       # Mark recording & test history
│   ├── DashboardController.cs         # Analytics KPIs & Chart.js aggregates (default route)
│   ├── DocumentationController.cs     # In-app Documentation Hub (/Documentation)
│   ├── HomeController.cs              # Landing showcase (/Home/Index) & Privacy
│   ├── InnovativeMethodsController.cs # Pedagogical intervention catalog (VARK-mapped)
│   ├── NotificationsController.cs     # Live notification engine (/api/notifications)
│   ├── RemedialPlansController.cs     # 4-week remedial plan creation & progress logs
│   ├── SlowLearnersController.cs      # High-risk cohort analytics, CSV export & SP endpoint
│   ├── StudentsController.cs          # Student roster & VARK assessment quiz
│   └── SubjectsController.cs          # Academic subject catalog (Admin only)
├── Data/                              # EF Core DbContext, Seeder, Migrations & SQL objects
│   ├── ApplicationDbContext.cs        # Entity definitions, reporting view mapping & fluent API
│   ├── DbSeeder.cs                    # Demo accounts, sample students, methods & assessments
│   └── Sql/ReportingObjects.sql       # SQL view (vw_StudentPerformance) & sp_GetSlowLearners
├── Middleware/
│   └── RequestLoggingMiddleware.cs    # HTTP request timing & status logger
├── Models/                            # Core domain entities & DTOs
│   ├── Dtos/StudentDto.cs             # API DTOs & SlowLearnerSpResult
│   ├── Student.cs, Subject.cs, Assessment.cs, RemedialPlan.cs, ProgressLog.cs, InnovativeMethod.cs
│   └── StudentPerformanceReport.cs    # Keyless entity mapped to vw_StudentPerformance
├── Services/                          # Business logic & AI services
│   ├── ISlowLearnerService.cs / SlowLearnerService.cs  # Multi-factor predictive risk engine
│   ├── IAiPlanService.cs / AiPlanService.cs            # Gemini-first -> Claude -> fallback planner
│   └── IGeminiChatService.cs / GeminiChatService.cs    # 3-tier 24/7 Gemini chatbot engine
├── Views/
│   ├── Documentation/Index.cshtml     # Interactive in-app Documentation Hub
│   ├── Home/Index.cshtml              # Modernized landing page with 3D analytics card & Bento grid
│   └── Shared/
│       ├── _Layout.cshtml             # Primary SaaS shell (Light/Dark theme, collapsible sidebar)
│       ├── _AuthLayout.cshtml         # Dedicated authentication viewport layout
│       ├── _ChatbotWidget.cshtml      # Global floating 24/7 AI Assistant widget
│       ├── _NotificationDropdown.cshtml # Dynamic topbar notification center
│       └── _LoginPartial.cshtml       # Topbar user chip with live profile avatar
├── tests/EduBoost.Tests/              # xUnit test suite (SlowLearnerRiskTests.cs)
└── wwwroot/                           # Static assets (site.css, home.css, auth.css, chatbot.css/js, uploads)
```

---

## 2. Teacher Operations Guide

Teachers are the primary frontline users of EduBoost. Below is the standard operating workflow:

### A. Enrolling Students & Roster Management
- Navigate to **Students → + Add Student** (`/Students/Create` or the quick modal on `/Students`).
- Enter the student's **Roll Number**, **Full Name**, **Class/Section** (e.g. `10-A`), **Parent Email**, and initial **Attendance Percentage** (0–100%).
- *Tip:* Updating attendance regularly is critical because attendance below 75% significantly increases the student's vulnerability score.

### B. Recording Assessments
- Navigate to **Assessments → + Record Assessment** (`/Assessments/Create`).
- Select the student and academic subject (Mathematics, Science, English, etc.).
- Enter the **Exam Type** (e.g. *Surprise Test*, *Midterm*, *Weekly Quiz*), **Marks Obtained**, and **Maximum Marks**.
- The system automatically validates percentages and triggers risk score recalculation.

### C. Identifying Struggling Learners
- Visit **Slow Learners** (`/SlowLearners`).
- EduBoost automatically flags any student meeting either criterion:
  1. Average score across all recorded assessments is **below 40%**.
  2. Composite **Risk Score is 45% or higher**.

### D. Assessing Cognitive Learning Modality (VARK)
- Open the student's profile details (`/Students/Details/{id}`).
- Conduct the 4-question interactive VARK diagnostic quiz:
  - **Visual:** Learns through diagrams, color coding, and flowcharts.
  - **Auditory:** Learns through verbal explanations, peer discussion, and oral reviews.
  - **Reading / Writing:** Learns through notes, textbooks, and written summaries.
  - **Kinesthetic:** Learns through practice tests, hands-on tasks, and gamified quizzes.
- The identified learning style is saved to the student's record and automatically matches future remedial plans.

### E. Launching a Remedial Action Plan
- From the student profile, click **Create Remedial Plan** (`/RemedialPlans/Create?studentId={id}`).
- Choose an intervention strategy from the catalog or click **✨ Generate with AI**.
- The AI planner creates a tailored 4-week progression schedule based on the student's learning style, average score, and attendance.

### F. Tracking Weekly Progress Logs
- Open the active remedial plan (`/RemedialPlans/Details/{id}`).
- Click **+ Add Progress Log** after each intervention session.
- Record topics covered, teacher observations, and the **Score After** in post-intervention assessments.
- When the student consistently achieves passing marks, mark the plan as **Completed**.

### G. Consulting the 24/7 AI Assistant
- Click the floating **AI Assistant** launcher button in the bottom-right corner of any page (or click **✨ Ask 24/7 AI** on `/Home/Index`).
- Use quick suggestion chips or type any pedagogical question (e.g., kinesthetic science activities, attendance recovery strategies, or 4-week plan outlines).

---

## 3. Administrator Governance Guide

Administrators maintain institutional integrity, configure curriculum subjects, oversee pedagogical strategies, and execute cohort audits.

| Task | Route | Instructions |
| --- | --- | --- |
| **Manage Subjects** | `/Subjects` | Add, edit, or delete academic subjects taught at the institution (`Admin` only). |
| **Manage Methods** | `/InnovativeMethods` | Curate intervention methodologies and associate them with VARK modalities (`Admin` only for Create/Edit/Delete). |
| **Run Cohort Detection** | `POST /SlowLearners/RunDetection` | Executes a batch recalculation across all students and assessment records. |
| **Export Audit CSV** | `/SlowLearners/ExportCsv` | Generates a CSV data file from database view `vw_StudentPerformance`. |
| **Stored Proc Query** | `/SlowLearners/FromStoredProcedure?minRisk=45` | Executes SQL stored procedure `sp_GetSlowLearners` and returns JSON diagnostic output. |
| **User & Profile Audit** | `/Identity/Account/Manage` | Review accounts, manage profile avatars, and audit security settings. |

---

## 4. Diagnostic Risk Engine & Scoring Formula

The diagnostic engine is implemented in `Services/SlowLearnerService.cs` (`ApplyRiskCalculation`):

$$\text{RiskScore} = \text{Clamp}\Big((\text{MarksDeficit} \times 0.60) + (\text{AttendanceDeficit} \times 0.25) + (\text{DeclineDelta} \times 0.15),\ 0,\ 100\Big)$$

### Factor Breakdown:
1. **Marks Deficit (60% Weight):**
   $$\text{MarksDeficit} = 100 - \text{AveragePercentage}$$
2. **Attendance Deficit (25% Weight):**
   $$\text{AttendanceDeficit} = 100 - \text{AttendancePercent}$$
3. **Trajectory Trend Deficit (15% Weight):**
   - Chronologically orders assessments by `ExamDate` and compares the latest score (`marks.Last()`) against the earliest score (`marks.First()`):
     $$\text{trend} = \text{LastScore} - \text{FirstScore}$$
   - Applies a penalty only when performance is declining:
     $$\text{DeclineDelta} = \max(0,\ -\text{trend})$$
4. **No-Assessment Baseline:**
   - If a student has zero assessments recorded yet, `RiskScore` is computed from attendance alone: $(100 - \text{AttendancePercent}) \times 0.25$, rounded to 1 decimal place.

### Diagnostic Flag Threshold:
A student is flagged as a **Slow Learner** (`IsSlowLearner = true`) when:
$$\text{AveragePercentage} < 40.0\% \quad \text{OR} \quad \text{RiskScore} \ge 45.0$$
*(For students with zero assessments, `IsSlowLearner = RiskScore >= 45`.)*

---

## 5. 24/7 AI Assistant Chatbot (Google Gemini)

EduBoost features an embedded **24/7 AI Teaching Assistant** accessible from every page via a floating chat widget, backed by a low-latency 3-tier service pipeline.

### Architecture & Files
| Layer | File | Responsibility |
| --- | --- | --- |
| **Service Contract** | `Services/IGeminiChatService.cs` | Defines `ChatMessageDto`, `ChatRequestDto`, `ChatResponseDto`, and `IGeminiChatService.SendMessageAsync(...)`. |
| **Service Implementation** | `Services/GeminiChatService.cs` | Executes the 3-tier Fast Path → Live Gemini API → Pedagogical Fallback pipeline with 3-minute cached DB stats. |
| **REST Controller** | `Controllers/Api/ChatbotController.cs` | Exposes `POST /api/chatbot/message` and `GET /api/chatbot/status`, injecting authenticated user email & role (`Admin` / `Teacher` / `Staff`). |
| **UI Partial** | `Views/Shared/_ChatbotWidget.cshtml` | Floating launcher button, online pulse badge, header controls (clear/close), suggestion chips, typing indicator, and message form. |
| **Client Script** | `wwwroot/js/chatbot.js` | Manages toggle state, maintains up to 20 turns of client `conversationHistory`, renders sanitized Markdown (`**bold**`, `*italic*`, `` `code` ``, lists), and calls `/api/chatbot/message`. |
| **Stylesheet** | `wwwroot/css/chatbot.css` | Theme-aware (`light` & `dark`) fixed bottom-right widget styling and pulse animations. |

### 3-Tier Execution Pipeline (`GeminiChatService.SendMessageAsync`)
1. **Tier 1 — Instant Engine (`TryGetInstantResponse`, `< 20ms`):**
   - Matches common high-frequency queries locally without external network calls and sets `ModelUsed = "eduboost-instant-engine"`.
   - Covered topics: risk score calculation/formula, greetings, 4-week remedial plan outline, kinesthetic/innovative teaching methods, and low attendance + failing marks interventions.
2. **Tier 2 — Live Google Gemini API (`CallGeminiApiAsync`):**
   - Reads `Gemini:ApiKey` and `Gemini:Model` (defaults to `gemini-3.5-flash-lite`) from configuration.
   - Calls `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}` with a strict **6-second** `CancellationTokenSource` timeout (and `HttpClient` configured for 7 seconds in `Program.cs`).
   - Builds a context-aware `systemInstruction` via `GetOrBuildSystemPromptAsync` including EduBoost rules, the signed-in user's role/email, and live DB counts (`Total Students`, `Slow Learners`, `Active Plans`) cached in memory for **3 minutes** (`_cachedDbSummary`).
   - Sends up to the **last 4 turns** (`history.TakeLast(4)`) with `temperature = 0.6` and `maxOutputTokens = 450` for snappy responses.
3. **Tier 3 — Pedagogical Fallback (`BuildPedagogicalFallback`):**
   - If the Gemini API times out or throws an error, returns structured remedial guidance with `Success = true` and `ModelUsed = "edu-fast-fallback"` so the UI never breaks.

### Configuration (`appsettings.json` or User Secrets)
```json
"Gemini": {
  "ApiKey": "YOUR_GOOGLE_GEMINI_API_KEY",
  "Model": "gemini-3.5-flash-lite",
  "FallbackModel": "gemini-3.5-flash"
}
```
> **Security Tip:** Never commit real API keys to public repositories. Override `Gemini:ApiKey` using `dotnet user-secrets set "Gemini:ApiKey" "<key>"` or environment variables (`Gemini__ApiKey`) in production.

---

## 6. AI Remedial Action Planner

EduBoost generates tailored 4-week remedial plans via `Services/AiPlanService.cs` (registered with a 25-second `HttpClient` timeout in `Program.cs`).

### Multi-Provider Resolution Order
1. **Primary — Google Gemini (`Gemini:ApiKey`, `Gemini:Model`):**
   - If `Gemini:ApiKey` is configured, calls `generativelanguage.googleapis.com/v1beta/models/{model}:generateContent` to generate a concise 4-week plan containing weekly activities, 2 VARK-matched teaching methods, a weekly test idea, and parent tips.
2. **Secondary — Anthropic Claude (`Ai:ApiKey`, `Ai:Model`):**
   - If Gemini is unconfigured or fails, falls through to `https://api.anthropic.com/v1/messages` (e.g., `claude-sonnet-4-5`, `max_tokens = 800`).
3. **Offline Deterministic Fallback (`FallbackPlan`):**
   - If neither provider key is valid (e.g., `PUT-YOUR-KEY-HERE` or blank) or a network error occurs, returns a structured 4-week template customized to the subject and VARK learning style, guaranteeing 100% offline availability.

---

## 7. UI/UX, Theme System & Landing Portal

### SaaS App Shell & Theme Engine (`Views/Shared/_Layout.cshtml`, `wwwroot/css/site.css`)
- **Dual Theme Support:** Supports **Light (Off-White)** (`data-theme="light"`, default) and **Dark (Charcoal/Slate)** (`data-theme="dark"`).
- **Zero-Flash Persistence:** An inline pre-paint script reads `localStorage.getItem('eduboost-theme')` before CSS renders and syncs `data-theme` and `data-bs-theme`. Users toggle themes via `#themeToggleBtn` in the topbar.
- **Collapsible Sidebar:** Desktop sidebar collapse state persists in `localStorage` (`eduboost-sidebar-collapsed`). Includes a **Developer & Resources** section linking to `/Documentation` and `/swagger`.
- **Global Topbar:** Features live student search (`/Students?search=...`), theme switcher, real-time notification dropdown (`_NotificationDropdown.cshtml` polling `/api/notifications`), and user profile chip (`_LoginPartial.cshtml`).

### Executive KPI Views
- **Dashboard (`/Dashboard`), Students (`/Students`), Assessments (`/Assessments`), Slow Learners (`/SlowLearners`), Remedial Plans (`/RemedialPlans`), Innovative Methods (`/InnovativeMethods`), and Subjects (`/Subjects`)** share unified executive KPI cards, interactive filters, and glassmorphic tables.

### Modernized Landing Showcase (`Views/Home/Index.cshtml`, `wwwroot/css/home.css`)
- Includes a hero section with a 3D floating student recovery preview card, a direct **✨ Ask 24/7 AI** launcher button (`openChatbotFromHero()`), a **Quick Demo Access** bar with one-click clipboard copy chips for demo credentials, a **Core Capabilities Bento Grid**, and a **Platform Architecture Showcase**.

---

## 8. Social Authentication & Live Google OAuth Setup

EduBoost features split-card SaaS sign-in and registration views (`Areas/Identity/Pages/Account/Login.cshtml` and `Register.cshtml`, hosted inside `Views/Shared/_AuthLayout.cshtml` with `wwwroot/css/auth.css`) featuring a 24/7 AI Assistant highlight banner and an interactive simulated SSO modal for **Google**, **Apple**, **Facebook**, and **X**.

To enable **real live Google Cloud OAuth** (`Program.cs` automatically activates `.AddGoogle(...)` when both keys are populated):

### Step 1: Open Google Cloud Console
1. Go to [https://console.cloud.google.com/](https://console.cloud.google.com/).
2. Sign in and create a new project (e.g., `EduBoost-Auth`).

### Step 2: Configure OAuth Consent Screen
1. Go to **APIs & Services → OAuth consent screen**.
2. Select **External** and click **Create**.
3. Fill in **App name** (`EduBoost`), **User support email**, and **Developer contact information**, then click **Save and Continue**.

### Step 3: Create OAuth Client ID
1. Navigate to **APIs & Services → Credentials**.
2. Click **+ Create Credentials → OAuth client ID** (`Web application`).
3. Under **Authorized redirect URIs**, add:
   ```text
   http://localhost:5168/signin-google
   ```
4. Click **Create**.

### Step 4: Add Keys to `appsettings.json` (or User Secrets)
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "PASTE-YOUR-CLIENT-ID.apps.googleusercontent.com",
      "ClientSecret": "PASTE-YOUR-CLIENT-SECRET"
    }
  }
}
```
Once configured, ASP.NET Core automatically enables Google OAuth login redirecting to `accounts.google.com`.

---

## 9. Developer & Technical Architecture Reference

### Prerequisites
- **.NET 10 SDK**
- **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`)
- Visual Studio 2022/2026 or VS Code

### Key NuGet Packages (`EduBoost.csproj`)
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` & `Microsoft.AspNetCore.Identity.UI` (`10.0.0`)
- `Microsoft.AspNetCore.Authentication.Google` (`10.0.0-*`)
- `Microsoft.EntityFrameworkCore.SqlServer` (`10.0.0`) & `Microsoft.EntityFrameworkCore.Design` (`10.0.11`)
- `Swashbuckle.AspNetCore` (`11.0.1`) — OpenAPI / Swagger UI at `/swagger`

### Essential CLI Commands
```powershell
# Restore & build solution
dotnet build

# Run application
dotnet run

# Run xUnit test suite (tests/EduBoost.Tests)
dotnet test

# Apply database migrations manually
dotnet ef database update
```

### Startup Automation (`Program.cs`)
On startup, `Program.cs`:
1. Applies pending EF Core migrations (`db.Database.MigrateAsync()`).
2. Runs `DbSeeder.SeedAsync()` (seeds subjects, methods, demo students, assessments, and a sample remedial plan) and executes `ISlowLearnerService.RunDetectionAsync()` on initial seed.
3. Ensures `Admin` and `Teacher` roles exist.
4. In `Development`, ensures demo accounts exist:

| Role | Email | Password |
| --- | --- | --- |
| **Admin** | `admin@eduboost.local` | `Admin@123` |
| **Teacher** | `teacher@eduboost.local` | `Teacher@123` |

### Unit Testing (`tests/EduBoost.Tests/SlowLearnerRiskTests.cs`)
Covers `SlowLearnerService.ApplyRiskCalculation` (high performers, failing marks, declining trends + poor attendance, `[0, 100]` clamping) and `AiPlanService.GeneratePlanAsync` deterministic fallback verification.

---

## 10. REST API Endpoints

Interactive OpenAPI documentation is available at `/swagger`.

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/api/students` | Authenticated | List all enrolled students mapped to `StudentDto` |
| `GET` | `/api/students/slow` | Authenticated | List students flagged as slow learners (ordered by `RiskScore` desc) |
| `GET` | `/api/students/{id}` | Authenticated | Get single student DTO with assessment & active plan counts |
| `POST` | `/api/students` | Authenticated | Create a new student record (`CreateStudentDto`) |
| `PUT` | `/api/students/{id}` | Authenticated | Update student details (`UpdateStudentDto`) |
| `DELETE` | `/api/students/{id}` | Authenticated | Delete a student and cascade related assessments/plans |
| `POST` | `/api/chatbot/message` | Public / Role-aware | Send message + optional `history` to 24/7 Gemini AI Assistant |
| `GET` | `/api/chatbot/status` | Public / Role-aware | Chatbot service health check & current user/role metadata |
| `GET` | `/api/notifications` | Authenticated | Retrieve live high-risk alerts, active plans, low scores & system tips |
| `GET` | `/SlowLearners/FromStoredProcedure` | Authenticated | Execute `sp_GetSlowLearners` with optional `minRisk` (default `45`) |
| `GET` | `/swagger` | Public | Interactive OpenAPI Swagger UI |

### Example: Chatbot API Request (`POST /api/chatbot/message`)
```json
{
  "message": "Suggest 3 innovative teaching methods for Kinesthetic learners struggling in Science.",
  "history": [
    { "role": "user", "text": "Hello" },
    { "role": "model", "text": "Hello! How can I help you right now?" }
  ]
}
```
Response (`ChatResponseDto`):
```json
{
  "success": true,
  "reply": "Here are **3 Innovative Teaching Methods for Kinesthetic Learners**...",
  "modelUsed": "eduboost-instant-engine",
  "error": null
}
```

---

## 11. Troubleshooting & Frequently Asked Questions

### Q1: Port 5168 is busy or locked
**Fix:** Stop running background `dotnet run` instances or update `applicationUrl` in `Properties/launchSettings.json`.

### Q2: How do I reset database demo data?
**Fix:** Delete database `EduBoostDb` in SQL Server Object Explorer / LocalDB, then run `dotnet run`. `DbSeeder.cs` automatically re-creates the schema, demo students, assessments, and roles.

### Q3: How do avatars persist on device upload?
**Fix:** Uploaded images are stored in `wwwroot/uploads/avatars/` with unique GUID filenames. The file path is saved as a user claim (`ProfilePicture`), and `SignInManager.RefreshSignInAsync(user)` refreshes the authentication cookie immediately.

### Q4: How does the 24/7 AI Chatbot work if internet or Gemini API is unavailable?
**Fix:** `GeminiChatService` responds locally in `< 20ms` for common EduBoost queries via `TryGetInstantResponse`, enforces a strict 6-second timeout on live Gemini calls, and falls back gracefully to `BuildPedagogicalFallback` (`ModelUsed = "edu-fast-fallback"`) so educators always receive actionable guidance.

### Q5: Which AI provider does the Remedial Plan Generator use first?
**Fix:** `AiPlanService` checks `Gemini:ApiKey` first, falls back to Anthropic Claude (`Ai:ApiKey`) if Gemini is unavailable, and finally falls back to a built-in deterministic 4-week VARK template if neither key is configured.
