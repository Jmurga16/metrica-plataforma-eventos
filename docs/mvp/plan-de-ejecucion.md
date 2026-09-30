# Plan de ejecución del reto (2 días)

> Orden de construcción recomendado para el MVP. El objetivo es tener **un flujo de punta a punta funcionando al final del día 1** e invertir el día 2 en seguridad, frontend, pruebas y pulido.
> Diseño detallado: [especificacion-tecnica.md](especificacion-tecnica.md).

## Estado al 30 de septiembre de 2026

El flujo principal del MVP, la infraestructura Compose, ambas APIs y el frontend están implementados. Las pruebas actuales pasan y el recorrido evento → outbox → RabbitMQ → notificación → Mailpit fue validado desde un clon limpio. Quedan como cierre prioritario las pruebas automatizadas de integración de EventService y la demostración reproducible de reintentos/DLQ. OpenTelemetry/Jaeger y el seed son mejoras opcionales.

## Principios
1. **Primero el esqueleto que camina:** el flujo completo mínimo (POST → cola → consumidor → BD → correo) antes de refinar cualquier parte.
2. **Commits pequeños y frecuentes** con Conventional Commits (`feat(event-service): ...`) para que la historia del repositorio muestre el proceso.
3. **Documentar mientras se construye:** actualizar el README y `docs/` al cerrar cada bloque, no al final.
4. **Línea de corte explícita:** si el tiempo no alcanza, se recorta en el orden definido abajo, nunca lo obligatorio.

## Día 1: backend y mensajería

| Bloque | Duración | Entregable | Criterio de salida |
|---|---|---|---|
| 1. Base del repositorio | 1 h | `.slnx`, `Directory.Build.props`, `Directory.Packages.props`, estructura de carpetas, `.gitignore`, `.editorconfig`, `docker-compose.yml` con `db`, `rabbitmq`, `redis` y `mailpit`, `db/init.sql` | `docker compose up -d` deja la infraestructura `healthy` |
| 2. EventService: dominio y aplicación | 2 h | Agregado `Event`/`Zone` con invariantes, `CreateEventCommand` + validador, `GetEventsQuery`, pruebas unitarias T01–T05 | Pruebas de dominio en verde |
| 3. EventService: infraestructura y API | 2 h | `EventsDbContext`, migración inicial, repositorio, endpoints `POST /events`, `GET /events`, `GET /events/{id}`, `ProblemDetails`, OpenAPI | `POST` persiste el evento y sus zonas en una transacción; `GET` pagina |
| 4. Mensajería con outbox + caché | 1,5 h | Contrato `EventCreated` v1, MassTransit + outbox EF + RabbitMQ, Redis cache-aside con invalidación por versión | El mensaje aparece en el exchange `event-created.v1`; `X-Cache` HIT/MISS |
| 5. NotificationService | 2,5 h | `NotificationsDbContext`, consumidor idempotente, reintentos, consumidor de `Fault`, `EmailSender` con MailKit, `GET /notifications` | Correo en Mailpit; un duplicado no genera correo; con SMTP caído el mensaje termina en `_error` |
| **Hito día 1** | | **Flujo de punta a punta en local** | Commit etiquetado `mvp-e2e` |

## Día 2: seguridad, frontend, calidad y entrega

| Bloque | Duración | Entregable | Criterio de salida |
|---|---|---|---|
| 6. Seguridad | 1,5 h | JWT (validación + `dev-token`), políticas por rol, rate limiting, `IExceptionHandler`, Serilog con filtros de datos sensibles, `X-Correlation-Id` | T07, T09 y T10 en verde |
| 7. Frontend | 2,5 h | Proyecto Vite + TS + Tailwind, selector de rol de demo, formulario con zonas dinámicas, validación Zod, estados de carga, error y éxito, mapeo de errores 400 | Registro exitoso desde el navegador; pruebas T15–T17 |
| 8. Pruebas de integración | 1,5 h | Testcontainers (PostgreSQL, RabbitMQ, Redis) para T06 y T08; MassTransit Test Harness para T11–T14 | `dotnet test` en verde |
| 9. Contenedores y compose final | 1 h | Dockerfiles *multi-stage* sin root, servicios `api-event`, `api-notifications` y `web` con *healthchecks* y `depends_on` | `docker compose up -d --build` desde un **clon limpio** funciona |
| 10. Documentación y cierre | 1 h | README final (ejecución, migraciones, seed, URLs, demo), revisión de `docs/` contra lo construido, capturas opcionales | Checklist de entrega (abajo) completo |
| 11. Ensayo de la demo | 0,5 h | Recorrer el guion de §16 de la especificación | Sin sorpresas en la presentación |

## Línea de corte (qué se recorta primero)

Si el tiempo se acaba, se sacrifica en este orden (de lo menos a lo más valioso):
1. Jaeger / trazas exportadas (se mantienen los logs con `correlationId`).
2. `POST /events/{id}/publish` y `EventPublished`.
3. `Idempotency-Key` en `POST /events`.
4. Pruebas de integración con Testcontainers (se mantienen las unitarias y el Test Harness del consumidor).
5. Rate limiting.

**Nunca se recorta:** outbox, idempotencia del consumidor, reintentos + DLQ, caché en `GET /events`, validación del frontend, Dockerfiles, compose ni README.

## Checklist de entrega

- [x] Repositorio público (o con acceso otorgado) en GitHub, GitLab o Bitbucket.
- [x] `docs/architecture.md` con diagramas, lista de microservicios, flujos sync/async, seguridad y sustentación.
- [x] `docs/backlog/Backlog-Roadmap-Plataforma-Eventos.xlsx` + `docs/backlog-roadmap.md`.
- [x] Código de `src/` (2 APIs) y `frontend/web-admin`.
- [x] `db/init.sql` + migraciones EF + scripts idempotentes en `db/scripts/`.
- [x] `Dockerfile` por API y para el frontend; `docker-compose.yml` en la raíz.
- [x] README con instrucciones de ejecución, migración y estrategia de datos iniciales.
- [x] `.env.example` sin secretos reales.
- [x] Pruebas actuales en verde.
