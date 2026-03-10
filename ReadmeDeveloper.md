# P3-Final - ArtemisBanking 🏦

Sistema bancario desarrollado en **ASP.NET Core 8** utilizando **Onion Architecture**, como proyecto final académico.

El sistema simula las operaciones principales de un banco digital:

* Gestión de usuarios
* Administración de cuentas de ahorro
* Tarjetas de crédito
* Préstamos
* Transacciones financieras
* Operaciones de cajero
* API REST segura con JWT

---

## 🏗 Arquitectura

El proyecto utiliza **Onion Architecture** para separar responsabilidades en capas:

```
Domain          ← Núcleo: entidades y reglas del negocio
Application     ← Servicios, DTOs, interfaces
Shared          ← Capa transversal: Email, Helpers, Extensions
Infrastructure  ← EF Core, repositorios, Hangfire, MailKit
WebApp          ← Interfaz MVC (Admin, Cliente, Cajero)
WebAPI          ← API REST con JWT
```

### Domain

Núcleo del sistema. Contiene entidades del negocio, enumeraciones y la clase base de entidad. **Sin dependencias externas.**

### Application

Servicios de aplicación, DTOs, interfaces de repositorios y servicios. Depende únicamente de `Domain`. Define el contrato `IEmailService` que la capa `Shared/Infrastructure` implementa.

### Shared ⭐ Nueva capa

Capa **transversal** que provee funcionalidades reutilizables en todo el sistema:

> La capa `Shared` **no depende** de `Infrastructure` ni de `Application`. Es referenciada por `Infrastructure` (implementación del email) y por `WebApp`/`WebAPI` (configuración de DI).

### Infrastructure

Implementa los contratos de `Application`. Usa EF Core, ASP.NET Identity, MailKit (implementa `IEmailService` usando `EmailRequest` de `Shared`) y Hangfire para jobs en segundo plano.

### WebApp

Interfaz web MVC con tres paneles:
- **Admin** → Dashboard, usuarios, préstamos, tarjetas, cuentas
- **Cliente** → Home, beneficiarios, transacciones, pagos
- **Cajero** → Depósitos, retiros, operaciones

### WebAPI

API REST protegida con **JWT**. Documentada con Swagger.

---

## 👨‍💻 Equipo de Desarrollo

Proyecto desarrollado por **3 desarrolladores** con distribución modular.

| Dev   | Área                  |
| ----- | --------------------- |
| Dev 1 Albertson | Backend · Auth · API  |
| Dev 2 Gerald | Admin Panel           |
| Dev 3 Darwin | Cliente · Cajero · QA |

---

## 🌿 Estrategia de Branching

Se utiliza una estrategia basada en **GitFlow simplificado**.

```
main       ← producción
develop    ← integración continua
feature/*  ← desarrollo de funcionalidades
hotfix/*   ← correcciones urgentes
```

Flujo de trabajo:

```
feature → develop → main
```

---

## 🌿 Ramas por desarrollador

### Dev 1: Albertson - Backend

```
feature/foundation
feature/auth-system
feature/loans-logic
feature/background-jobs
feature/web-api
```

Responsabilidades:

* Arquitectura Onion
* Identity
* Generic Repository
* Email Service (Shared layer + Infrastructure)
* Lógica de préstamos
* Hangfire jobs
* API REST

---

### Dev 2: Gerald - Admin Panel

```
feature/admin-dashboard
feature/admin-users
feature/admin-loans
feature/admin-cards
feature/admin-accounts
```

Responsabilidades:

* Dashboard
* CRUD usuarios
* Gestión de préstamos
* Tarjetas de crédito
* Cuentas de ahorro

---

### Dev 3: Darwin - Cliente y Cajero

```
feature/client-home
feature/client-beneficiaries
feature/client-transactions
feature/cashier-module
feature/qa-deploy
```

Responsabilidades:

* Operaciones del cliente
* Transferencias
* Pagos
* Avances de efectivo
* Operaciones de cajero
* QA y deploy

---

## 🏷 Labels del Proyecto

| Label    | Uso                   |
| -------- | --------------------- |
| setup    | configuración inicial |
| backend  | lógica y servicios    |
| frontend | vistas y UI           |
| api      | endpoints REST        |
| auth     | autenticación         |
| payments | transacciones         |
| infra    | infraestructura       |
| bugfix   | correcciones          |
| testing  | QA                    |

---

## 🎯 Milestones

| Milestone | Objetivo    |
| --------- | ----------- |
| M1        | Fundamentos |
| M2        | Admin Panel |
| M3        | Cliente     |
| M4        | Cajero      |
| M5        | API + QA    |

---

## 📊 Flujo Kanban

El proyecto utiliza **GitHub Projects Kanban** con las columnas:

```
Backlog
Ready
In Progress
In Review
Done
```

Cada issue representa una **tarea del sistema**.

---

## 🔐 Seguridad

El sistema incluye:

* ASP.NET Identity
* Autenticación JWT
* Autorización por roles
* CVC de tarjetas cifrado SHA256 (vía `Shared.Helpers.CryptoHelper`)
* Validación de transacciones

---

## ⚙ Tecnologías

* ASP.NET Core 8
* Entity Framework Core
* ASP.NET Identity
* Hangfire
* MailKit
* AutoMapper
* Swagger
* JWT Authentication

---

## 🚀 Deploy

El proyecto incluye:

* Testing E2E
* Bug fixing
* Tag **v1.0.0**

---

## 📜 Licencia

Proyecto académico desarrollado para fines educativos.