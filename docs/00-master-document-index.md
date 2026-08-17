# ECommerce Project — Master Document Index

## Overview

This document serves as the single entry point for all project documentation. It lists every document by number, title, current status, and a one-line summary of its contents.

---

## Document Registry

| # | Title | Status | Description |
|---|-------|--------|-------------|
| 00 | Master Document Index | **Exists** | This file — index of all project documentation. |
| 01 | Project Charter | **Exists** | Project goals, scope, stakeholders, and success criteria. |
| 02 | Glossary | **Exists** | Domain terms, definitions, and entity mappings across all bounded contexts. |
| 03 | Business Requirements | **Exists** | Business-level requirements and constraints for the ECommerce platform. |
| 03a | User Stories | **Exists** | Detailed user stories with acceptance criteria. |
| 03b | Product Backlog | **Exists** | Prioritised product backlog items. |
| 03c | Sprint Plan | **Exists** | Sprint-level planning and iteration breakdown. |
| 04 | Software Requirements Specification | **Exists** | Functional and non-functional software requirements. |
| 04a | Functional Requirements Specification | **Exists** | Granular functional requirements by feature area. |
| 05 | Non-Functional Requirements | **Exists** | Performance, scalability, security, and availability requirements. |
| 06 | System Architecture | **Exists** | High-level architecture, technology stack, and deployment topology. |
| 06a | Domain Model | **Exists** | Domain entities, value objects, aggregates, and invariants. |
| 06b | Event Storming | **Exists** | Event storming results — commands, events, policies, aggregates. |
| 06c | Bounded Contexts | **Exists** | Bounded context map, context relationships, and integration patterns. |
| 07 | Data Model / ERD | **Exists** | Entity-relationship diagrams and database schema. |
| 08 | API Design | **Exists** | REST API surface, endpoint contracts, and versioning strategy. |
| 09 | Security Architecture | **Exists** | Security controls, threat model, and compliance requirements. |
| 10 | Authentication & Authorization | **Exists** | JWT auth flow, refresh token rotation, RBAC, and permission-based authorization. |
| 11 | Identity, Roles & Permissions Matrix | **Exists** | Full permissions matrix — every permission constant, role mapping, and endpoint coverage. |
| 12 | Domain Events & Integration Patterns | **Planned** | Domain events catalog, event-driven integration, and async messaging design. |
| 13 | Cart & Checkout Flow | **Planned** | Cart lifecycle, checkout orchestration, and order placement flow. |
| 14 | Order Management | **Planned** | Order states, status transitions, cancellation, and reorder flow. |
| 15 | Pricing & Promotions | **Planned** | Pricing engine, promotion conditions/actions, coupon lifecycle, and tax calculation. |
| 16 | Payments & Refunds | **Planned** | Payment authorization, capture, refund workflow, and reconciliation. |
| 17 | Inventory & Warehouse | **Planned** | Stock management, warehouse operations, stock movements, and inter-warehouse transfers. |
| 18 | Fulfillment & Shipping | **Planned** | Pick/pack/ship workflow, fulfillment tasks, shipment tracking, and shipping rate quotes. |
| 19 | Catalog Management | **Planned** | Products, categories, brands, product imports, and search. |
| 20 | Invoicing & Credit Notes | **Planned** | Invoice generation, PDF export, credit note lifecycle. |
| 21 | Reviews & Moderation | **Planned** | Review submission, voting, moderation queue, publish/reject/remove flow. |
| 22 | Notifications | **Planned** | Notification preferences, channels, and notification dispatch. |
| 23 | Wishlist | **Planned** | Wishlist add/remove and guest-to-authenticated merge. |
| 24 | Feature Flags | **Planned** | Feature flag CRUD, evaluation, and platform-level toggle management. |
| 25 | Reporting & Analytics | **Planned** | Sales, inventory, finance, promotion, and fulfillment reports with CSV export. |
| 26 | Audit Trail | **Planned** | Audit chain, audit entries, and immutable audit log design. |
| 27 | Webhooks & Integrations | **Planned** | Webhook endpoint registration, delivery, secret rotation, and replay. |
| 28 | Profile & Account Management | **Planned** | Customer profile updates, account closure, and GDPR data erasure. |
| 29 | Error Handling & Problem Responses | **Planned** | RFC 7807 Problem Details, error types, and result monad pattern. |
| 30 | Test Strategy & Quality Gates | **Exists** | Unit, integration, and E2E test strategy with quality gates. |
| 31 | CI/CD Pipeline & Release Management | **Exists** | Build, test, deploy pipeline and release versioning. |
| 32 | Deployment Infrastructure & Runbooks | **Exists** | Infrastructure-as-code, deployment topology, and operational runbooks. |
| 33 | Observability & Monitoring | **Planned** | Structured logging, metrics, tracing, and alerting strategy. |
| 34 | Load & Performance Test Report | **Exists** | Performance baselines, load test results, and SLA validation. |
| 35 | Security Review | **Exists** | Security audit findings, remediation status, and compliance checklist. |
| 36 | Developer Onboarding Guide | **Exists** | Getting started, environment setup, coding standards, and workflows. |
| 36a | Performance Remediation Backlog | **Exists** | Tracked performance improvements and optimisation backlog. |
| 37 | Coding Standards | **Exists** | C# coding conventions, naming, project structure, and tooling. |
| 37a | Runbooks — Top 10 Failure Modes | **Exists** | Operational runbooks for the most common production failure scenarios. |
| 38 | Database Migration Strategy | **Planned** | EF Core migrations, versioning, rollback, and seed data strategy. |
| 39 | Caching Strategy | **Planned** | Cache layers, invalidation patterns, and cache configuration. |
| 40 | Internationalisation & Localisation | **Planned** | Multi-currency, locale handling, and product translation strategy. |
| 41 | OAuth/OIDC Design | **Exists** | Lightweight OAuth 2.0 implementation with client credentials, ROPC, and social login stubs. |
| 42 | YARP Gateway Design | **Exists** | YARP reverse proxy as BFF gateway with in-process routing. |
| 43 | SQL Server Provider | **Exists** | Multi-database support with EF Core and Dapper provider switching. |
| 44 | Guest-to-Authenticated Cart Merge | **Planned** | Anonymous cart persistence and merge-on-login flow. |
| 45 | GDPR & Data Privacy | **Planned** | Data retention, consent management, and right-to-erasure implementation. |
| 46 | Disaster Recovery Plan | **Planned** | Backup strategy, RTO/RPO targets, and recovery procedures. |

---

## Legend

| Status | Meaning |
|--------|---------|
| **Exists** | Document has been written and is available in the `docs/` directory. |
| **Planned** | Document is defined in scope but not yet written. |

---

## Quick Navigation

- **Architecture & Design:** 03–09
- **Identity & Security:** 02, 09–11
- **Domain & Business Flows:** 12–29
- **Quality & DevOps:** 30–37a
- **Operations & Compliance:** 38–43
