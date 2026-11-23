# Kredvanta – Installation and Setup Guide

## 1. Introduction

This guide explains how to configure, install and run Kredvanta in a local development environment.

Kredvanta is an ASP.NET Core MVC application using Microsoft SQL Server and Entity Framework Core.

## 2. System Requirements

Before installing the application, ensure that the following tools are available:

| Software | Requirement |
|---|---|
| Operating System | Windows 10 or later |
| IDE | Visual Studio 2022 or later |
| .NET SDK | .NET 10 SDK |
| Database | Microsoft SQL Server |
| Version Control | Git |
| Browser | Microsoft Edge, Google Chrome or Firefox |
| Package Manager | NuGet |

## 3. Clone the Repository

Open PowerShell, Command Prompt or the Visual Studio terminal.

Run:

```powershell
git clone https://github.com/YOUR-USERNAME/Kredvanta.git
```

Navigate to the repository:

```powershell
cd Kredvanta
```

Navigate into the application project directory:

```powershell
cd Kredvanta
```

## 4. Restore Dependencies

Restore the required NuGet packages:

```powershell
dotnet restore
```

## 5. Configure the Database

Kredvanta uses Microsoft SQL Server.

Create or identify a SQL Server database instance that will be used for development.

Configure the `DefaultConnection` connection string using ASP.NET Core User Secrets. From the application project directory, initialize secrets if needed and set the key used by the application:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>"
```

### Configure the Initial Administrator

At startup, Kredvanta seeds identity roles. Administrator seeding is optional: if `SeedAdmin:Email` is missing, the application logs a warning and continues without creating or assigning an administrator account. Configure both values in User Secrets from the application project directory to seed one:

```powershell
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "<a strong initial password>"
```

Replace the example email and password with your own values. Keep secrets out of source control. The password is needed only when the administrator account is first created. If an email is configured for a new administrator but the password is missing, administrator seeding is skipped with a warning; the application still starts. Ensure an administrator account is provisioned before relying on staff-only features.

## 6. Install Entity Framework Core Tools

If the Entity Framework Core command-line tools are not installed, run:

```powershell
dotnet tool install --global dotnet-ef
```

If already installed, update them when necessary:

```powershell
dotnet tool update --global dotnet-ef
```

## 7. Apply Database Migrations

From the application project directory, run:

```powershell
dotnet ef database update
```

This applies the existing Entity Framework Core migrations to the configured database.

Do not generate a new initial migration if the repository already contains the correct migration history.

## 8. Build the Application

Compile the application:

```powershell
dotnet build
```

Resolve any build errors before proceeding.

## 9. Run the Application

Start the application using:

```powershell
dotnet run
```

Alternatively, open `Kredvanta.sln` in Visual Studio and run the application using the configured HTTPS launch profile.

Open the local address displayed in the terminal or browser launch output.

## 10. Troubleshooting

### Database Connection Errors

Check:

- SQL Server is running.
- The connection string is correct.
- The database server is accessible.
- The configured SQL Server authentication method is supported.

### Missing NuGet Packages

Run:

```powershell
dotnet restore
```

### Database Schema Errors

Check that:

- Existing migrations are included.
- The configured database is correct.
- The migrations have been applied.
- The application and database schema are compatible.

### Port Conflicts

Check the configured application launch settings and use an available local development port.

## 11. Development Environment Notes

Kredvanta is under active development. Some features may require additional configuration or may not yet be available.

Third-party integrations, production payment services and deployment configurations should not be assumed to work from a fresh local installation.

---

**Project:** Kredvanta  
**Framework:** ASP.NET Core MVC  
**Database:** Microsoft SQL Server  
**Runtime:** .NET 10  
**Guide Status:** Initial Development Setup Guide