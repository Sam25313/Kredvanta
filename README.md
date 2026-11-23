# Kredvanta – Digital Microfinance Management System

<p align="center">
  <strong>A modern digital platform for applicant onboarding and loan management.</strong>
</p>

Kredvanta is a web-based microfinance management application built with ASP.NET Core MVC and Microsoft SQL Server. It aims to digitize lending operations by providing a centralized platform for applicant management, loan applications, document handling and administrative operations.

**Project Status:** Under Active Development

---

## Project Overview

Kredvanta is designed to simplify microfinance operations by bringing essential lending activities into one digital platform.

The long-term goal is to provide an accessible, secure and efficient lending management solution without depending on physical branches.

## Application Preview
## Home Page
![Kredvanta Home Page](Screenshots/HomePage.png)
## Login Page
![Kredvanta Login Page](Screenshots/Login.png)
## Registration Page
![Kredvanta Registration Page](Screenshots/Onboarding.png)
## Admin Dashboard	
![Kredvanta Dashboard](Screenshots/AdminDashboard.png)
## ProductPage
![Kredvanta Product Page](Screenshots/ProductSection.png)


### Applicant Onboarding Workflow

![Applicant Management](Screenshots/Step1.png)
![Applicant Management](Screenshots/Step2.png)
![Applicant Management](Screenshots/Step3.png)
![Applicant Management](Screenshots/Review.png)

![Applicant Management](Screenshots/BorrowerDashboard.png)

### Mobile Interface

![Kredvanta Mobile View](Screenshots/MobilePrev.png)

## Footer
![Applicant Management](Screenshots/Footer.png)

*Screenshots will be added as the corresponding interfaces are completed.*

## Technology Stack

| Technology | Purpose |
|---|---|
| C# | Backend programming |
| ASP.NET Core MVC | Web application framework |
| .NET 10 | Application runtime |
| Entity Framework Core | Database ORM |
| Microsoft SQL Server | Database management |
| ASP.NET Core Identity | Authentication |
| Razor Views | Server-side user interface |
| Bootstrap | Responsive styling |
| JavaScript | Client-side functionality |
| Git and GitHub | Version control |

## Application Modules

The planned application modules include:

- User authentication and authorization.
- Role-based access control.
- Applicant management.
- Multi-step applicant onboarding.
- Loan product management.
- Loan application management.
- Supporting document uploads.
- Administrative dashboard.
- Loan repayment management.
- Financial reporting and analytics.

See [FEATURES.md](docs/FEATURES.md) for implementation status.

## Project Architecture

Kredvanta follows the Model-View-Controller (MVC) architectural pattern.

The application separates its responsibilities into:

- Models – Application entities and domain data.
- Views – Razor-based user interfaces.
- Controllers – Request handling and application coordination.
- Data – Database context and migrations.
- ViewModels – Data transfer between controllers and views.
- Services – Reusable application and business logic.

See [ARCHITECTURE.md](docs/ARCHITECTURE.md) for more information.

## Getting Started

### Prerequisites

- Visual Studio 2022 or later.
- .NET 10 SDK.
- Microsoft SQL Server.
- Git.
- Entity Framework Core CLI tools.

### Installation

1. Clone the repository:

   ```
   git clone https://github.com/Sam25313/Kredvanta.git
   ```

2. Navigate into the application directory:

   ```bash
   cd Kredvanta/Kredvanta
   ```

3. Restore NuGet dependencies:

   ```bash
   dotnet restore
   ```

4. Configure SQL Server connection string using ASP.NET Core User Secrets.

5. Apply the database migrations:

   ```bash
   dotnet ef database update
   ```

6. Run the application:

   ```bash
   dotnet run
   ```

See [INSTALLATION.md](docs/INSTALLATION.md) for detailed setup instructions.

## Security

Kredvanta is being developed with security considerations including:

- Authentication and authorization.
- Role-based access restrictions.
- Secure configuration management.
- Validation of user-submitted data.
- Controlled document upload handling.

See [SECURITY.md](docs/SECURITY.md).

## Development Roadmap

The application is under active development. Planned improvements include:

- Completing applicant onboarding.
- Loan application processing workflows.
- Loan approval and disbursement.
- Repayment management.
- Financial reporting.
- Payment gateway and mobile money integration.
- Automated testing and production deployment.

See [ROADMAP.md](docs/ROADMAP.md).

## Documentation

| Document | Description |
|---|---|
| [Architecture](docs/ARCHITECTURE.md) | Application architecture |
| [Database](docs/DATABASE.md) | Database design |
| [Installation](docs/INSTALLATION.md) | Setup instructions |
| [Features](docs/FEATURES.md) | Application functionality |
| [Security](docs/SECURITY.md) | Security considerations |
| [Roadmap](docs/ROADMAP.md) | Development progress |

## Author

**Samuel Barasa**

Software Developer | ASP.NET Core Developer

GitHub:https://github.com/Sam25313

## Disclaimer

Kredvanta is a software development project intended for educational, demonstration and portfolio purposes. It should not be considered a licensed financial institution or a production-ready lending service.

