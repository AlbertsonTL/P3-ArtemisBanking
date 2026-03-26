# 🏦 Artemis Banking - P3-Proyecto Final


## Descripcion
Mini proyecto final: Artemis Banking

Sistema bancario en línea desarrollado con **ASP.NET Core 8** siguiendo **Onion Architecture**.  

Desarrollar una plataforma de banca en línea sobre ASP.NET Core MVC (versión 8 o 9) que permita gestionar de manera integral los préstamos, administrar tarjetas de crédito, operar cuentas de ahorro, procesar pagos de préstamos y tarjetas, así como realizar provisión de fondos y transferencias entre cuentas, todo ello dentro de un marco seguro y basado en roles para administrador, cajero y cliente.

`Proyecto Final Programación 3 — ITLA 2026.`

---

## 🏗 Arquitectura

```
ArtemisBanking/
└── src/
    ├── ArtemisBanking.Domain          ← Entidades, Enums (sin dependencias externas)
    ├── ArtemisBanking.Application     ← Interfaces, DTOs, contratos de servicios
    ├── ArtemisBanking.Shared          ← Helpers, modelos transversales (sin dependencias)
    ├── ArtemisBanking.Infrastructure  ← EF Core, Identity, Repositorios, AutoMapper, Email
    ├── ArtemisBanking.Functions       ← Azure Function con CRON JOB
    ├── ArtemisBanking.WebApp          ← MVC (Admin · Cliente · Cajero)
    └── ArtemisBanking.WebAPI          ← REST API con JWT + Swagger
```

---

## 👨‍💻 Equipo

| Dev | Área  |
|-----|-------|
| Dev 1 — Albertson | Backend · Auth · API | 2024-1949 |
| Dev 2 — Darwin | Cliente · Cajero · QA | 2024-0044 |

---

## ⚙️ Requisitos previos

Asegúrate de tener instalado:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [SQL Server](https://www.microsoft.com/es-es/sql-server/sql-server-downloads) o SQL Server LocalDB (incluido con Visual Studio)
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

```bash
# Instalar EF Core CLI global (solo una vez)
dotnet tool install --global dotnet-ef
```

---

## 🚀 Configuración y primer arranque

### 1. Clonar el repositorio

```bash
git clone https://github.com/Alb3rtsonTL/P3-Final-ArtemisBanking.git
cd P3-Final-ArtemisBanking
git checkout develop
```

### 2. Configurar connection string local

Cada desarrollador crea su propio archivo local (no se sube al repo):

El appsettings.json esta en el drive

**`src/ArtemisBanking.WebApp/appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ArtemisBankingDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "MailSettings": {
    "Host": "EMAIL_USER",
    "Port": "EMAIL_PORT",
    "SenderName": "Artemis Banking",
    "SenderEmail": "EMAIL_USER",
    "UserName": "EMAIL_USER",
    "Password": "EMAIL_PASSWORD"
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*"
}
```

**`src/ArtemisBanking.WebAPI/appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ArtemisBankingDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "ArtemisBanking_SuperSecretKey_2026_Min32Chars!",
    "Issuer": "ArtemisBankingAPI",
    "Audience": "ArtemisBankingClients",
    "ExpirationHours": 8
  },
  "MailSettings": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "SenderName": "Artemis Banking",
    "SenderEmail": "TU_CORREO@gmail.com",
    "UserName": "TU_CORREO@gmail.com",
    "Password": "TU_APP_PASSWORD"
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*"
}
```

> ⚠️ Estos archivos están en `.gitignore` — nunca se suben al repositorio.

### 3. Generar migración local

```bash
cd src/ArtemisBanking.WebApp

dotnet ef migrations add InitialCreate --project ../ArtemisBanking.Infrastructure
```

### 4. Aplicar migración y crear la base de datos

```bash
dotnet ef database update --project ../ArtemisBanking.Infrastructure
```

### 5. Correr la aplicación

**WebApp (MVC):**
```bash
dotnet run --project src/ArtemisBanking.WebApp
```
`Disponible en: https://localhost:5001`

**WebAPI:**
```bash
dotnet run --project src/ArtemisBanking.WebAPI
```
`Swagger UI en: https://localhost:7001/swagger`

---

## 🔐 Usuarios por defecto (Seeder)

Al iniciar la aplicación por primera vez se crean automáticamente:

| Usuario | Contraseña | Rol |
|---------|-----------|-----|
| `admin` | `Admin@12345` | Admin |
| `cajero` | `Cajero@12345` | Cajero |
| `cliente` | `Cliente@12345` | Cliente |

> El seeder es **idempotente**: si los usuarios ya existen, no los duplica.  
> El cliente demo tiene una cuenta de ahorro principal creada con saldo `$0.00`.

---

## 🛠 Tecnologías

| Tecnología | Uso |
|-----------|-----|
| ASP.NET Core 8 MVC | Interfaz web |
| ASP.NET Core 8 Web API | API REST |
| Entity Framework Core 8 | ORM — Code First |
| ASP.NET Identity | Autenticación y roles |
| AutoMapper 12 | Mapeo Entity ↔ DTO ↔ ViewModel |
| JWT Bearer | Seguridad API |
| MailKit | Envío de correos |
| Hangfire | Jobs en segundo plano (cuotas atrasadas) |
| Swagger / Swashbuckle | Documentación API |
| SQL Server / LocalDB | Base de datos |

---

## 🌿 Branching (GitFlow simplificado)

```
main          ← producción (solo via PR desde develop)
develop       ← integración continua
feature/*     ← desarrollo de funcionalidades
hotfix/*      ← correcciones urgentes en producción
```
---

*Proyecto Final Programación 3 — ITLA 2026 © Artemis Banking Team*