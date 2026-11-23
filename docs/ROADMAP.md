# Kredvanta – Development Roadmap

## 1. Project Vision

Kredvanta aims to become a comprehensive digital microfinance management platform that simplifies applicant onboarding, loan processing, disbursement and repayment management.

The long-term vision is to provide an integrated lending platform that reduces reliance on traditional branch-based operations.

## 2. Development Phases

### Phase 1: Project Foundation

**Focus:** Core application infrastructure.

- [x] ASP.NET Core MVC project setup.
- [x] Microsoft SQL Server integration.
- [x] Entity Framework Core integration.
- [x] ASP.NET Core Identity integration.
- [x] Custom ApplicationUser model.
- [x] Initial database architecture.
- [x] Role-based authorization foundation.
- [ ] Complete automated testing setup.

### Phase 2: Applicant Management

**Focus:** Applicant registration and onboarding.

- [x] Borrower entity design.
- [x] Guarantor entity design.
- [x] Borrower profile fields.
- [x] Multi-step onboarding foundation.
- [x] Borrower verification document model.
- [ ] Complete and test applicant onboarding.
- [ ] Complete document upload and review workflows.
- [ ] Complete applicant profile management.

### Phase 3: Loan Product Management

**Focus:** Configurable lending products.

- [x] Loan product entity.
- [x] Loan product required document entity.
- [x] Loan product validation constraints.
- [ ] Complete loan product administration.
- [ ] Test product configuration workflows.

### Phase 4: Loan Application Management

**Focus:** Applicant loan requests.

- [x] Loan application entity.
- [x] Loan application document entity.
- [x] Loan application guarantor entity.
- [x] Application status history entity.
- [ ] Complete loan application submission.
- [ ] Complete document review workflows.
- [ ] Implement application tracking.
- [ ] Complete officer assignment workflows.

### Phase 5: Credit Assessment and Approval

**Focus:** Responsible lending decisions.

- [x] Credit assessment entity.
- [x] Loan approval entity.
- [ ] Implement credit assessment calculations.
- [ ] Implement credit assessment workflows.
- [ ] Implement approval levels.
- [ ] Complete approval and rejection workflows.
- [ ] Implement decision audit trails.

### Phase 6: Loan Disbursement

**Focus:** Issuing approved loans.

- [x] Loan entity.
- [x] Loan disbursement entity.
- [x] Create a loan and repayment schedule after a verified payout.
- [x] Complete the finance disbursement workflow.
- [x] Track unique transaction references and idempotent payout requests.
- [ ] Add automated tests for disbursement validation.

### Phase 7: Repayment Management

**Focus:** Loan repayment and financial tracking.

- [x] Repayment schedule entity.
- [x] Repayment entity.
- [x] Repayment allocation entity.
- [ ] Implement repayment schedule generation.
- [ ] Implement repayment recording.
- [ ] Implement repayment allocation.
- [ ] Implement outstanding balance calculations.
- [ ] Implement overdue payment tracking.
- [ ] Test financial calculations.

### Phase 8: Dashboard and Reporting

**Focus:** Operational visibility.

- [ ] Complete administrative dashboard.
- [ ] Implement applicant statistics.
- [ ] Implement loan portfolio summaries.
- [ ] Implement repayment performance reports.
- [ ] Implement financial reporting.
- [ ] Implement downloadable reports.

### Phase 9: Notifications and Audit Management

**Focus:** Operational accountability.

- [x] Notification entity.
- [x] AuditLog entity.
- [ ] Complete notification workflows.
- [ ] Implement email notifications.
- [ ] Complete audit logging integration.
- [ ] Implement audit log viewing and filtering.

### Phase 10: Security and Quality Assurance

**Focus:** Production readiness.

- [ ] Complete authorization testing.
- [ ] Complete document upload security testing.
- [ ] Implement automated unit tests.
- [ ] Implement integration tests.
- [ ] Perform application security testing.
- [ ] Conduct performance testing.
- [ ] Test database backup and recovery.

### Phase 11: Deployment and Integrations

**Focus:** Production deployment and external services.

- [ ] Configure production hosting.
- [ ] Configure production SQL Server.
- [ ] Configure HTTPS and domain.
- [ ] Implement M-Pesa integration.
- [ ] Implement payment reconciliation.
- [ ] Implement production monitoring.
- [ ] Complete deployment verification.

## 3. Future Enhancements

Potential future improvements include:

- Mobile-friendly applicant self-service portal.
- Automated credit scoring.
- Advanced financial analytics.
- SMS notifications.
- M-Pesa payment integration.
- Automated repayment reminders.
- Loan restructuring management.
- Advanced fraud detection.
- Mobile application integration.

## 4. Roadmap Status

The roadmap is a development planning document.

A checked item indicates that the corresponding foundational code or database design has been established. It does not necessarily mean the complete user-facing workflow has been implemented, tested or approved for production.

Development status should be updated as functionality is completed and verified.

---

**Project:** Kredvanta  
**Development Status:** Active Development  
**Roadmap:** Subject to change as project requirements evolve.