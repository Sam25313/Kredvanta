# Kredvanta – Application Architecture

## 1. Introduction

Kredvanta is a web-based microfinance management application developed using ASP.NET Core MVC and C#.

The application follows the Model-View-Controller (MVC) architectural pattern, separating application responsibilities into models, views and controllers.

The architecture is designed to support maintainability, modular development, security and future expansion.

## 2. Architectural Pattern

Kredvanta uses the Model-View-Controller architecture.

### Model

The Model layer represents application data and business entities.

Examples include:

- Borrower
- Guarantor
- LoanProduct
- LoanApplication
- Loan
- Repayment
- Notification

Models are organised into functional folders under the `Models` directory.

### View

The View layer provides the user interface.

Kredvanta uses Razor Views (`.cshtml`) to render dynamic HTML pages.

The Views directory contains interfaces for different application modules, including borrower management, loan processing and dashboards.

### Controller

Controllers handle incoming HTTP requests, coordinate application operations and return appropriate responses.

Examples include:

- HomeController
- DashboardController
- ProfileController
- BorrowersController

Controllers communicate with the database through Entity Framework Core and `ApplicationDbContext`.

## 3. Technology Architecture

| Layer | Technology |
|---|---|
| Presentation | Razor Views, HTML, CSS, Bootstrap |
| Client-side functionality | JavaScript |
| Application framework | ASP.NET Core MVC |
| Backend language | C# |
| Data access | Entity Framework Core |
| Database | Microsoft SQL Server |
| Authentication | ASP.NET Core Identity |
| Version control | Git and GitHub |

## 4. Project Directory Structure

The application project is organised as follows:

```text
Kredvanta/
│
├── Areas/
│   └── Identity/
│       └── Pages/
│
├── Controllers/
│   ├── HomeController.cs
│   ├── DashboardController.cs
│   └── Other application controllers
│
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
│
├── Models/
│   ├── Applications/
│   ├── Borrowers/
│   ├── Enums/
│   ├── Identity/
│   ├── Loans/
│   ├── Products/
│   ├── Repayments/
│   └── System/
│
├── ViewModels/
│
├── Views/
│   ├── Home/
│   ├── Dashboard/
│   ├── Borrowers/
│   └── Shared/
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── images/
│   └── lib/
│
├── Properties/
├── appsettings.json
├── Program.cs
└── Kredvanta.csproj
```

The directory structure may evolve as additional modules and application services are introduced.

## 5. Request Processing Workflow

The general request processing flow is:

1. A user interacts with the application through a web browser.
2. An HTTP request is sent to the ASP.NET Core application.
3. Middleware processes authentication, authorization and other request requirements.
4. Routing identifies the appropriate controller and action.
5. The controller processes the request.
6. Application data is accessed through Entity Framework Core and ApplicationDbContext where necessary.
7. SQL Server performs the requested database operations.
8. The controller returns a Razor View or another appropriate HTTP response.
9. The browser displays the result.

## 6. Database Architecture

Kredvanta uses Entity Framework Core for object-relational mapping.

The central database context is:

`Kredvanta.Data.ApplicationDbContext`

It inherits from:

`IdentityDbContext<ApplicationUser>`

This integrates application entities with ASP.NET Core Identity.

Database relationships, indexes, precision settings, check constraints and delete behaviours are configured through Entity Framework Core model configuration.

## 7. Authentication and Authorization

ASP.NET Core Identity provides account management and authentication.

The application uses role-based authorization to control access to protected functionality.

Application roles include:

- Administrator
- LoanOfficer
- CreditManager

Access to individual controllers and actions is governed by authorization attributes and policies.

## 8. Architectural Design Principles

The application aims to follow these principles:

- Separation of concerns.
- Reusable components.
- Centralised database access.
- Role-based access control.
- Consistent data validation.
- Maintainable and modular code.
- Secure configuration management.
- Scalable feature development.

## 9. Future Architectural Improvements

Potential improvements include:

- Introducing dedicated business services.
- Implementing repository abstractions where beneficial.
- Adding automated unit and integration testing.
- Separating file storage from the web application.
- Introducing background processing for notifications.
- Adding structured logging and monitoring.

These improvements will be introduced as the application evolves.

---

**Project:** Kredvanta  
**Framework:** ASP.NET Core MVC  
**Architecture:** Model-View-Controller (MVC)  
**Status:** Active Development