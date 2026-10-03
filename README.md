# ☕ SmartCafe Pro — Advanced Cafe Management System

**Production-Grade Windows Desktop Application**
Built with C# · .NET 10 · WinForms · SQL Server · 3-Tier Architecture

---

## 📋 Table of Contents
1. [Project Overview](#overview)
2. [Features](#features)
3. [Architecture](#architecture)
4. [OOP & Design Patterns](#oop)
5. [Database Schema](#database)
6. [Prerequisites](#prerequisites)
7. [Setup Guide](#setup)
8. [Module Guide](#modules)
9. [Default Credentials](#credentials)
10. [Project Structure](#structure)

---

## 🏆 Project Overview <a name="overview"></a>

SmartCafe Pro is a **production-level** Point-of-Sale and Cafe Management System
designed for real-world cafe and restaurant environments. Built as a University
Final Year Project, it demonstrates mastery of:

- Full **3-Tier Architecture** (Presentation → Business Logic → Data Access)
- Complete **OOP** implementation (all 11 concepts demonstrated)
- **5 Design Patterns** (Singleton, Repository, Factory, Strategy, Observer)
- **Advanced SQL Server** (Stored Procedures, Triggers, Views, Functions, Transactions)
- Modern, animated **WinForms UI** with dark/light theme support

---

## ✨ Features <a name="features"></a>

### Core Modules
| Module               | Features |
|----------------------|----------|
| 🔐 Authentication    | Login, roles, password hashing (SHA-256), remember-me, audit logs |
| 📊 Dashboard         | Live KPIs, bar charts, donut charts, top products, recent orders |
| 💳 Point of Sale      | Product catalog, cart, discounts, multi-payment, receipt printing |
| 🛒 Order Management  | Dine-in/Takeaway/Delivery, status tracking, order history |
| 👨‍🍳 Kitchen Display  | Kanban board (Pending → Preparing → Ready), real-time updates |
| 🪑 Table Management  | Visual table grid, live status, right-click context menu |
| 📅 Reservations      | Booking management, guest count, special requests |
| 👥 Customers         | CRM, loyalty tiers (Bronze→VIP), points, history |
| 🍽 Menu/Products     | Categories, pricing, availability toggle, combos |
| 📦 Inventory         | Stock levels, low-stock alerts, supplier tracking, adjustment |
| 🏢 Employees         | Records, attendance, salary calculation |
| 📈 Reports           | Daily/Monthly/Products/Payments, Excel & PDF export |
| 🔔 Notifications     | Observer pattern, real-time alerts, mark-read |
| ⭐ Feedback          | Customer ratings, complaints, resolve tracking |
| ⚙ Settings           | Theme, tax rate, cafe info, database backup |

### Advanced Technical Features
- ✅ Role-based access control (Admin / Manager / Cashier / Waiter / Chef)
- ✅ Animated sidebar with collapse/expand
- ✅ Smooth fade-in login with shake animation on failure
- ✅ Dark / Light theme toggle with live preview
- ✅ PDF invoice generation (iText7)
- ✅ Excel report export (EPPlus)
- ✅ QR code generation (ZXing.Net)
- ✅ One-click database backup to local path
- ✅ Auto-refresh dashboard and kitchen display
- ✅ Global exception logging to file

---

## 🏗 Architecture <a name="architecture"></a>

```
SmartCafePro/
│
├── 📁 PresentationLayer/          ← WinForms UI (Forms, Controls)
│   └── Forms/
│       ├── LoginForm.cs           Animated login screen
│       ├── MainForm.cs            Shell: animated sidebar + content host
│       ├── DashboardForm.cs       KPI cards + charts
│       ├── POSForm.cs             Full Point-of-Sale
│       ├── ModuleForms.cs         All 12 other modules
│       └── UtilityForms.cs        ChangePassword, About, SetupProgress
│
├── 📁 BusinessLayer/              ← Business Logic Layer (BLL)
│   └── Services.cs                AuthService, OrderService, DashboardService,
│                                  SettingsService, Strategy/Factory/Observer patterns
│
├── 📁 DataAccessLayer/            ← Data Access Layer (DAL)
│   └── Repositories.cs            Singleton DatabaseManager + all Repositories
│
├── 📁 Models/                     ← Domain Models (POCOs)
│   ├── Models.cs                  All 14 model classes with BaseModel
│   └── SupplementalModels.cs      Category, TableRepository additions
│
├── 📁 Interfaces/                 ← Contracts (ISP principle)
│   └── Interfaces.cs              IRepository<T>, all domain interfaces
│
├── 📁 Services/                   ← Application Services
│   ├── InitialSetupService.cs     First-run DB setup & password hashing
│   └── ReportExportService.cs     PDF (iText7) & Excel (EPPlus) export
│
├── 📁 Utilities/                  ← Cross-cutting concerns
│   └── Utilities.cs               Validator, ThemeManager, Formatter,
│                                  ReceiptPrinter, MessageHelper, ExceptionLogger
│
├── 📁 Database/                   ← SQL Scripts (run in order)
│   ├── 01_CreateDatabase.sql      Tables, indexes, constraints
│   ├── 02_StoredProcedures.sql    All 10 stored procedures
│   ├── 03_TriggersViewsFunctions.sql  Triggers, Views, TVFs, seed data
│   └── 04_SetupAdmin.sql          Users, employees, sample data
│
├── SmartCafePro.csproj            .NET 10 project file + NuGet packages
├── App.config                     Connection strings & settings
└── README.md                      This file
```

---

## 🎯 OOP & Design Patterns <a name="oop"></a>

### OOP Concepts Demonstrated

| Concept | Where |
|---------|-------|
| **Encapsulation** | All model properties with private setters, computed properties |
| **Inheritance** | `BaseModel → User/Customer/Product/Order/...` (14 child classes) |
| **Abstraction** | `BaseModel.Validate()` abstract method |
| **Polymorphism** | `GetDisplayName()` overridden in every model |
| **Method Overloading** | `AuthService.HashPassword()` variations |
| **Interfaces** | `IRepository<T>`, `IPaymentStrategy`, `IDiscountStrategy`, `INotificationObserver` |
| **Composition** | `Order` contains `List<OrderDetail>` |
| **Aggregation** | `Employee` references `User` (exists independently) |
| **Association** | `Order` → `Customer`, `Product`, `Employee` |
| **Dependency Injection** | `AuthService(IUserRepository)`, `OrderService(IOrderRepo, IProductRepo, INotifService)` |
| **SOLID Principles** | SRP (one class per concern), OCP (strategy pattern), LSP (all repos), ISP (split interfaces), DIP (inject abstractions) |

### Design Patterns

| Pattern | Implementation |
|---------|---------------|
| **Singleton** | `DatabaseManager.Instance`, `SessionManager.Instance` |
| **Repository** | `BaseRepository<T>` + `UserRepository`, `CustomerRepository`, etc. |
| **Factory** | `PaymentFactory.Create("Cash"|"Card"|"Online"|"Wallet")` |
| **Strategy** | `IDiscountStrategy` → `NoDiscount`, `PercentageDiscount`, `LoyaltyDiscount`, `HappyHourDiscount` |
| **Observer** | `INotificationSubject / INotificationObserver` in `NotificationService` |

---

## 🗄 Database Schema <a name="database"></a>

**20 Tables** normalized to 3NF:

```
Users ────────────── Roles
  │
Employees ─────────── Attendance
  │                     Salaries
  │
Orders ─────────────── OrderDetails ── Products ── Categories
  │                     Payments              │
  ├── Customers ─────── LoyaltyPoints    Inventory ── Suppliers
  │     └── Feedback        │           PurchaseOrders
  │                         │
  └── CafeTables ──── Reservations

Notifications
AuditLogs
Settings
```

**Advanced SQL Features Used:**
- 10 Stored Procedures with transactions & OUTPUT params
- 5 Triggers (audit logs, loyalty update, membership tier, low-stock, order completion)
- 5 Views (`vw_DailySalesSummary`, `vw_ProductPerformance`, `vw_InventoryStatus`, `vw_ActiveReservations`, `vw_KitchenQueue`)
- 1 Scalar Function (`fn_GetLoyaltyDiscount`)
- 1 Table-Valued Function (`fn_GetOrdersByDateRange`)
- 1 Sequence (`sq_OrderNumber`)
- 9 Indexes for query performance

---

## ⚙ Prerequisites <a name="prerequisites"></a>

| Requirement | Version | Notes |
|-------------|---------|-------|
| Windows OS | 10 / 11 | Required for WinForms |
| .NET SDK | 10.0 | `dotnet --version` to check |
| SQL Server | 2019+ | Express edition is free |
| SQL Server Management Studio (SSMS) | Latest | For running scripts |
| Visual Studio | 2022+ | Community edition works |

**Free Downloads:**
- .NET 10 SDK: https://dotnet.microsoft.com/download
- SQL Server Express: https://www.microsoft.com/sql-server/sql-server-downloads
- SSMS: https://aka.ms/ssmsfullsetup
- Visual Studio Community: https://visualstudio.microsoft.com/

---

## 🚀 Setup Guide <a name="setup"></a>

### Step 1 — Clone / Extract Project
```bash
# If using Git:
git clone https://github.com/yourname/SmartCafePro.git
cd SmartCafePro

# Or extract the ZIP and open the folder
```

### Step 2 — Run SQL Scripts in SSMS
Open SSMS → Connect to your SQL Server instance, then run in order:

```sql
-- Open and execute each file in SSMS:
01_CreateDatabase.sql      -- Creates all 20 tables, indexes
02_StoredProcedures.sql    -- 10 stored procedures
03_TriggersViewsFunctions.sql -- Triggers, views, seed data
04_SetupAdmin.sql          -- Admin user, sample data
```

**Quick method — run all at once in SSMS:**
```sql
:r C:\Path\To\SmartCafePro\Database\01_CreateDatabase.sql
:r C:\Path\To\SmartCafePro\Database\02_StoredProcedures.sql
:r C:\Path\To\SmartCafePro\Database\03_TriggersViewsFunctions.sql
:r C:\Path\To\SmartCafePro\Database\04_SetupAdmin.sql
```

### Step 3 — Configure Connection String
Edit `App.config` — change Server= to your SQL Server instance:
```xml
<add name="SmartCafeDB"
     connectionString="Server=YOUR_SERVER;Database=SmartCafeProDB;
                       Integrated Security=true;TrustServerCertificate=true;"
     providerName="Microsoft.Data.SqlClient" />
```

Common server names:
- `localhost` — default local instance
- `.\SQLEXPRESS` — SQL Server Express
- `localhost\SQLEXPRESS` — SQL Server Express (alternate)
- `COMPUTERNAME\MSSQLSERVER2022` — named instance

### Step 4 — Restore NuGet Packages
```bash
cd SmartCafePro
dotnet restore
```

### Step 5 — Build & Run
```bash
dotnet build
dotnet run
# OR open SmartCafePro.sln in Visual Studio and press F5
```

### Step 6 — First Login
The app will auto-hash passwords on first run. Log in with:
```
Username: admin
Password: Admin@123
```

**⚠ Important:** Change the admin password immediately after first login via Settings → Change Password.

---

## 📖 Module Guide <a name="modules"></a>

### Dashboard
- Refreshes every 60 seconds automatically
- Shows 6 KPI cards with live database data
- Weekly revenue bar chart (last 7 days)
- Payment breakdown donut chart
- Top 5 selling products (last 30 days)
- Recent orders table for today

### Point of Sale (POS)
1. Select Order Type (Dine-In / Takeaway / Delivery)
2. Optionally select Table and Customer
3. Click product cards to add to cart
4. Adjust quantities/discounts inline in cart grid
5. Select discount strategy from dropdown
6. Choose payment method
7. Click "PLACE ORDER" → receipt prints automatically

### Kitchen Display
- Auto-refreshes every 15 seconds
- Click "Start 🔥" to move item to Preparing
- Click "Ready ✅" to mark item as ready
- Red cards = waiting > 15 minutes (urgent)

### Reports
- Select report type from dropdown
- Set date range
- Click "▶ Run"
- Results shown in grid
- Use Export buttons for PDF/Excel (in ReportsForm extended version)

---

## 🔑 Default Credentials <a name="credentials"></a>

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | Admin (full access) |
| `manager1` | `Pass@123` | Manager |
| `cashier1` | `Pass@123` | Cashier |
| `waiter1` | `Pass@123` | Waiter |
| `chef1` | `Pass@123` | Kitchen Staff |

---

## 📁 File Count Summary <a name="structure"></a>

| Layer | Files | Lines (approx.) |
|-------|-------|-----------------|
| Database SQL | 4 files | ~1,200 lines |
| Models | 2 files | ~400 lines |
| Interfaces | 1 file | ~120 lines |
| Data Access Layer | 2 files | ~650 lines |
| Business Layer | 1 file | ~500 lines |
| Services | 2 files | ~450 lines |
| Utilities | 1 file | ~280 lines |
| Presentation (Forms) | 5 files | ~2,500 lines |
| Config & Project | 3 files | ~100 lines |
| **Total** | **~21 files** | **~6,200 lines** |

---

## 🛠 Technology Stack

```
Language:     C# 12
Framework:    .NET 10 Windows Forms
Database:     SQL Server 2019+ (T-SQL)
Data Access:  Microsoft.Data.SqlClient (ADO.NET)
PDF Export:   iText7
Excel Export: EPPlus 7
QR Codes:     ZXing.Net
JSON:         Newtonsoft.Json
Architecture: 3-Tier + Repository + Strategy + Observer + Factory + Singleton
```

---

## 📜 License

This project is developed as a **University Final Year Project** and is intended for
educational and portfolio purposes. Commercial use requires proper licensing of
third-party libraries (iText7 AGPL, EPPlus Commercial).

---

*SmartCafe Pro — Built with ❤ for Computer Science students*
