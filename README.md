# schedule-solver-platform

Monorepo для генерации расписаний: **web** управляет данными, **solver** (C++ CLI) решает задачу по JSON, **shared-contracts** фиксирует границу обмена.

## Структура

```
schedule-solver-platform/
  apps/
    web/                 # Next.js + Prisma + Ant Design
    solver/              # C++20 CMake CLI (stub)
  packages/
    shared-contracts/    # JSON Schema + TypeScript types
  data/
    samples/
      synthetic-small/
        input.json
  docs/
    architecture.md
    local-development.md
    solver-contract.md
    data-boundary.md
  scripts/
    run-solver-dev.ps1 | .sh
    validate-contracts.ps1 | .sh
```

## Границы компонентов

| Компонент | Может | Не может |
|-----------|-------|----------|
| **web** | БД (Prisma), UI, audit, export JSON | CP-SAT, чтение XLSX в solver |
| **solver** | JSON in/out, CLI | БД, UI, Prisma, XLSX |
| **shared-contracts** | Схемы и типы | Бизнес-логика |

## Быстрый старт

### 1. Установка

```powershell
cd D:\Files\TimeTableGenerator
npm install
```

### 2. Web

```powershell
cd apps/web
copy .env.example .env
npm run prisma:migrate
npm run dev
```

http://localhost:3000

### 3. Solver (stub)

```powershell
cmake -S apps/solver -B apps/solver/build
cmake --build apps/solver/build --config Release
.\scripts\run-solver-dev.ps1
```

Результат: `tmp/solver-output.json`

### 4. Контракты

```powershell
npm run validate:contracts
```

Подробнее: [docs/local-development.md](docs/local-development.md)

## Что реализовано

- Monorepo с npm workspaces
- Web: страницы-заглушки, Ant Design layout, Prisma schema (SQLite)
- Solver: CMake + C++20 stub CLI + nlohmann/json
- Shared contracts: JSON Schema + TS types
- Документация архитектуры и границ данных

## Handoff-данные (реальное учебное заведение)

Каталог `data/solver_agent_full_handoff_v2/` — аудиты, правила, candidate JSON (variant A/B).  
См. [docs/agent-handoff.md](docs/agent-handoff.md) и Cursor skill `reading-solver-handoff-v2`.

## Что НЕ реализовано

- OR-Tools CP-SAT (`SCHED_ENABLE_ORTOOLS=OFF`)
- Импорт Excel / XLSX
- Вызов solver из UI
- HTTP API для solver
- Авторизация и production deploy
- Полная доменная модель в схемах

## Лицензия

MIT — см. [LICENSE](LICENSE).
