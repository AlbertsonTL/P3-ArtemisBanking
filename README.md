# 🏦 Mini Proyecto: Artemis Banking

Sistema bancario en línea desarrollado con **ASP.NET Core 8** siguiendo **Onion Architecture**.  

Desarrollar una plataforma de banca en línea sobre ASP.NET Core MVC (versión 8) que permita gestionar de manera integral los préstamos, administrar tarjetas de crédito, operar cuentas de ahorro, procesar pagos de préstamos y tarjetas, así como realizar provisión de fondos y transferencias entre cuentas, todo ello dentro de un marco seguro y basado en roles para administrador, cajero y cliente.

---

## 🏗 Arquitectura

```
ArtemisBanking/
└── src/
    ├── ArtemisBanking.Domain          ← Entidades, Enums (sin dependencias externas)
    ├── ArtemisBanking.Application     ← Interfaces, DTOs, contratos de servicios
    ├── ArtemisBanking.Shared          ← Helpers, modelos transversales (sin dependencias)
    ├── ArtemisBanking.Infrastructure  ← EF Core, Identity, Repositorios, AutoMapper, Email
    ├── ArtemisBanking.WebApp          ← MVC (Admin · Cliente · Cajero)
    ├── ArtemisBanking.WebAPI          ← REST API con JWT + Swagger
    └── ArtemisBanking.Functions       ← Jobs programados para cuotas atrasadas
```

**Flujo de dependencias:**
```
WebApp / WebAPI
      ↓
Infrastructure  →  Shared
      ↓
Application
      ↓
Domain
```

---

## 👨‍💻 Equipo

| Dev | Área | Ramas |
|-----|------|-------|
| Dev 1 — Albertson | Backend · Auth · API | `feature/foundation` `feature/auth-system` `feature/web-api` |
| Dev 2 — Gerald | Admin Panel | `feature/admin-dashboard` `feature/admin-users` |
| Dev 3 — Darwin | Cliente · Cajero · QA | `feature/client-home` `feature/cashier-module` |

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

### 2. Configurar la configuración local

Los archivos `appsettings.json` incluidos en el repositorio contienen únicamente
valores de desarrollo y placeholders.

**`src/ArtemisBanking.WebApp/appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ArtemisBankingDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "MailSettings": {
    "Host": "EMAIL_HOST",
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

Configura la clave JWT mediante User Secrets antes de iniciar el WebAPI:

```powershell
dotnet user-secrets set "Jwt:Key" "<clave-local-segura-de-al-menos-32-caracteres>" `
  --project .\src\ArtemisBanking.WebAPI
```

Para configurar las credenciales de correo sin guardarlas en Git, utiliza el
mismo mecanismo:

```powershell
dotnet user-secrets set "MailSettings:Host" "<servidor-smtp>" `
  --project .\src\ArtemisBanking.WebAPI
dotnet user-secrets set "MailSettings:Port" "587" `
  --project .\src\ArtemisBanking.WebAPI
dotnet user-secrets set "MailSettings:SenderEmail" "<correo>" `
  --project .\src\ArtemisBanking.WebAPI
dotnet user-secrets set "MailSettings:UserName" "<usuario-smtp>" `
  --project .\src\ArtemisBanking.WebAPI
dotnet user-secrets set "MailSettings:Password" "<contraseña-o-app-password>" `
  --project .\src\ArtemisBanking.WebAPI
```

### 3. Generar migración local

```powershell
dotnet ef migrations add InitialCreate `
  --project .\src\ArtemisBanking.Infrastructure `
  --startup-project .\src\ArtemisBanking.WebApp
```

### 4. Aplicar migración y crear la base de datos

```powershell
dotnet ef database update `
  --project .\src\ArtemisBanking.Infrastructure `
  --startup-project .\src\ArtemisBanking.WebApp
```

### 5. Correr la aplicación

**WebApp (MVC):**
```bash
dotnet run --project src/ArtemisBanking.WebApp
```
`Disponible en: https://localhost:5291`

**WebAPI:**
```bash
dotnet run --project src/ArtemisBanking.WebAPI
```
`Swagger UI en: https://localhost:5018/swagger`

---

## 🔐 Usuarios por defecto (Seeder)

Al iniciar la aplicación por primera vez se crean automáticamente:

| Usuario | Contraseña | Rol |
|---------|-----------|-----|
| `admin` | `Admin@12345` | Admin |
| `cajero` | `Cajero@12345` | Cajero |
| `cliente` | `Cliente@12345` | Cliente |

> El seeder es **idempotente**: si los usuarios ya existen, no los duplica.  
> Además de los usuarios, el seeder prepara datos demo idempotentes para probar el flujo completo:
> cuenta secundaria `200000002`, cliente receptor `cliente2` (cuenta `300000003`),
> tarjeta `4111111111111111`, comercio, consumo aprobado, beneficiario y transferencias.
> La tarjeta usa CVC de prueba `123` (se guarda únicamente su hash).

## 🛠 Tecnologías

| Tecnología | Uso |
|-----------|-----|
| ASP.NET Core 8 MVC | Interfaz web |
| ASP.NET Core 8 Web API | API REST |
| Entity Framework Core 8 | ORM — Code First |
| ASP.NET Identity | Autenticación y roles |
| AutoMapper 13 | Mapeo Entity ↔ DTO ↔ ViewModel |
| JWT | Autenticación y seguridad API |
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

**Flujo estándar:**
```bash
git checkout develop
git pull origin develop
git checkout -b feature/nombre-funcionalidad

# ... trabajar y hacer commits ...

git push origin feature/nombre-funcionalidad
# Abrir Pull Request hacia develop en GitHub
```

**Nunca hacer push directo a `main` o `develop`.**

---

## 📊 Milestones

| Milestone | Objetivo | Dev responsable |
|-----------|---------|----------------|
| M1 | Fundamentos (Onion, Identity, Domain) | Dev 1 |
| M2 | Admin Panel | Dev 2 |
| M3 | Módulo Cliente | Dev 3 |
| M4 | Módulo Cajero | Dev 3 |
| M5 | API REST + QA + Deploy | Dev 1 + Dev 3 |

---

## 📜 Notas importantes

- Todos los montos financieros usan `decimal(18,2)` — nunca `float` ni `double`.
- El CVC de tarjetas se almacena mediante un **hash SHA-256** (nunca en texto plano).
- Los números de cuenta (9 dígitos) y tarjeta (16 dígitos) son únicos en todo el sistema.

---

*Proyecto Programación 3 — ITLA 2026 © Artemis Banking Team*