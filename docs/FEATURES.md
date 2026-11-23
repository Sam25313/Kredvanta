# Kredvanta – Application Features

## 1. Overview

Kredvanta is designed as an integrated digital microfinance management platform.

The application aims to centralise applicant registration, lending operations, loan processing, repayment tracking and administrative management.

This document describes the application's functional modules and development scope.

## 2. User Authentication and Account Management

The authentication module uses ASP.NET Core Identity.

Its responsibilities include:

- User registration.
- User login and logout.
- Password management.
- Authentication.
- Account management.
- Role-based authorization.
- External authentication support where configured.

## 3. Applicant Management

The applicant management module maintains applicant information.

Its intended capabilities include:

- Applicant registration.
- Personal information management.
- Identification information.
- Contact and residential information.
- Employment information.
- Income information.
- Applicant profile management.
- Applicant record searching.
- Applicant verification document management.

## 4. Applicant Onboarding

Kredvanta uses a multi-step applicant onboarding approach.

The onboarding process is intended to collect information in manageable stages.

These include:

- Personal information.
- Contact and residential details.
- Employment and financial details.
- Supporting verification documents.
- Profile completion.

## 5. Loan Product Management

The loan product module supports the management of lending products.

Product configuration includes:

- Product codes.
- Product names.
- Product categories.
- Minimum and maximum loan amounts.
- Annual interest rates.
- Loan terms.
- Processing fees.
- Required supporting documents.

## 6. Loan Application Management

The loan application module manages requests from applicants.

Its intended capabilities include:

- Loan application submission.
- Loan product selection.
- Requested loan amount.
- Loan purpose.
- Application status tracking.
- Officer assignment.
- Supporting document uploads.
- Application history.

## 7. Document Management

The application supports document metadata management for applicant verification and loan applications.

Document information includes:

- Original file name.
- Storage key.
- File extension.
- Content type.
- File size.
- Uploading user.
- Reviewing user where applicable.
- Review comments where applicable.

Actual document storage and download functionality must be configured and secured separately.

## 8. Credit Assessment

The credit assessment module is designed to support evaluation of applicants' repayment capacity.

Its data model includes:

- Monthly income.
- Monthly expenses.
- Existing monthly loan payments.
- Other monthly obligations.
- Disposable income.
- Proposed monthly instalment.
- Debt service ratio.
- Assessing officer.

## 9. Loan Approval and Processing

The loan processing module supports the transition from an application to an issued loan.

Finance Officers can review signed offers waiting for payout, record the payment method, recipient destination and provider reference, and confirm the amount sent. The amount must match the signed offer's principal less its processing fee. A successful record creates the loan and repayment schedule and moves the application to Disbursed in one transaction.

Payout records are audited, provider references are unique, and idempotency keys protect against duplicate submissions. This workflow records staff-confirmed payouts; direct payment-provider integration is not included.

The database includes:

- Loan approval records.
- Approval levels.
- Reviewing officers.
- Issued loan records linked to their accepted offers.
- Loan financial snapshots.
- Loan disbursement records with recipient, reference and idempotency tracking.

## 10. Loan Repayment Management

The repayment module supports the management of loan repayment activities.

Its data model includes:

- Repayment schedules.
- Installment amounts.
- Principal due.
- Interest due.
- Fees due.
- Amount paid.
- Repayment transactions.
- Receipt numbers.
- External transaction references.
- Repayment allocations.

## 11. Guarantor Management

The guarantor module supports the association of guarantors with loan applications.

It includes:

- Guarantor personal information.
- Identification details.
- Contact information.
- Guaranteed amounts.
- Loan application associations.

## 12. Dashboard and Reporting

The dashboard is intended to provide an overview of important microfinance operations.

Potential dashboard information includes:

- Total applicants.
- Total loan applications.
- Loans issued.
- Outstanding loan balances.
- Repayment performance.
- Application processing status.
- Operational activity.

Reporting capabilities will be expanded as the financial modules mature.

## 13. Notifications

The notification module stores user-specific notifications.

It includes notification titles, messages, read status and user associations.

Notification delivery mechanisms may be extended to support email and other channels.

## 14. Audit Logging

The audit logging module records important system activities.

Its data model supports information such as:

- User responsible for an action.
- Action performed.
- Affected entity.
- Entity identifier.
- Action date.
- IP address.
- User agent.

## 15. Development Status

Kredvanta is under active development.

The existence of a database entity or controller does not automatically mean its complete user workflow has been implemented or tested.

The current implementation status of individual features should be updated as development and testing progress.

---

**Project:** Kredvanta  
**Application Type:** Digital Microfinance Management System  
**Status:** Active Development