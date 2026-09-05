# 📋 Guía de Prioridades y Estimaciones de Issues

Este documento describe cómo se organizan y priorizan los **Issues** del proyecto **Artemis Banking** utilizando **GitHub Issues** y **GitHub Projects** para gestionar el trabajo del equipo.

---

# Priority (Prioridad)

La **prioridad** indica qué tan urgente o importante es resolver un issue dentro del proyecto.

| Prioridad | Descripción |
|-----------|-------------|
| **P0 – Critical** | Problema crítico que bloquea el sistema o rompe una funcionalidad principal. |
| **P1 – High** | Alta prioridad. Debe resolverse lo antes posible para continuar el desarrollo. |
| **P2 – Medium** | Prioridad media. Importante pero no bloquea el progreso inmediato. |
| **P3 – Low** | Baja prioridad. Mejoras menores o tareas opcionales. |

### Ejemplos

| Issue | Priority |
|------|---------|
Login no funciona | P0 |
CRUD de usuarios | P1 |
Dashboard indicadores | P2 |
Mejoras visuales UI | P3 |

Para este proyecto normalmente se utilizará:


High
Medium
Low


---

# Size (Tamaño del Issue)

El **size** representa el nivel de complejidad o el tamaño aproximado del trabajo requerido.

| Size | Descripción |
|-----|-------------|
| **XS** | Muy pequeño (10–30 minutos) |
| **S** | Pequeño (1–2 horas) |
| **M** | Mediano (medio día) |
| **L** | Grande (1 día de trabajo) |
| **XL** | Muy grande (varios días) |

### Ejemplos

| Issue | Size |
|------|------|
Fix validación de formulario | XS |
Login con Identity | M |
CRUD Usuarios | L |
Setup arquitectura Onion | M |

Para simplificar el trabajo del equipo se usará:


S
M
L


---

# Estimate (Estimación)

El **estimate** representa el esfuerzo estimado necesario para completar el issue.  
En este proyecto se utilizarán **Story Points**.

| Points | Esfuerzo aproximado |
|------|--------------------|
| **1** | Muy fácil |
| **2** | Fácil |
| **3** | Trabajo estándar |
| **5** | Complejo |
| **8** | Muy complejo |

### Ejemplos

| Issue | Estimate |
|------|----------|
Setup solución Onion | 3 |
CRUD Usuarios | 5 |
Dashboard Admin | 5 |
Transacción Express | 3 |
Hangfire Job cuotas vencidas | 3 |

---

# Ejemplo de Issue Configurado

Title:
Implement Login con ASP.NET Identity

Priority:
High

Size:
M

Estimate:
3

Assignee:
Dev1

Milestone:
M1 - Fundamentos

---

# Reglas de Uso en el Proyecto

1. Cada **issue debe tener prioridad asignada**.
2. El **size debe reflejar la complejidad real** de la tarea.
3. El **estimate debe definirse antes de comenzar el desarrollo**.
4. Los issues deben pertenecer a un **milestone correspondiente**.
5. Todo issue debe tener un **assignee responsable**.

---

# Objetivo

El uso de **Priority, Size y Estimate** permite:

- Mejor planificación del trabajo.
- Distribución equilibrada entre desarrolladores.
- Seguimiento claro del progreso del proyecto.
- Organización profesional del repositorio.