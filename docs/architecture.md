# Архитектура schedule-solver-platform

## Компоненты

| Компонент | Роль | Технологии |
|-----------|------|------------|
| `apps/web` | Управляющий слой: справочники, audit, manual review, экспорт JSON | Next.js, Prisma, Ant Design |
| `apps/solver` | Stateless CLI: JSON in → JSON out | C++20, CMake |
| `packages/shared-contracts` | Граница контрактов solver input/output | JSON Schema, TypeScript |

## Главная граница

```
[ Web + SQLite/DB ]  --export-->  input.json  --CLI-->  [ C++ solver ]  ---->  output.json
                                      ^                           |
                                      |                           v
                              shared-contracts              import to DB
```

- **Web** владеет данными, файлами, ручными решениями и подготовкой нормализованного input.
- **Solver** не имеет доступа к БД, Prisma, Next.js и XLSX.
- **Shared contracts** — единственное место описания формата обмена.

## Solver: stateless

- Запускается как отдельный процесс `schedule_solver`.
- Получает пути к файлам (normalized input, rule config, solver config — в одном JSON на текущем этапе).
- Возвращает JSON: `status`, `diagnostics`, `schedule`, `warnings`, `artifacts`, …
- Не хранит состояние между запусками.

## Что пока не реализовано

- HTTP-обёртка над solver (только CLI).
- CP-SAT модель OR-Tools.
- Реальный вызов solver из UI.
- Импорт Excel / сложная нормализация.
