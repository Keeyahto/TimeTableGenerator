# Solver contract (CLI)

> **Стек:** .NET console (план). Исполняемый файл и флаги уточнятся при старте C#-проекта.

## Граница

- Вход: normalized JSON (`SolverInput` / `real_candidate_v1_1`)
- Выход: diagnostic JSON (`SolverOutput` v0.2)
- Без БД, без UI

## Планируемый интерфейс

```text
schedule-solver --input <path> --output <path> --mode {validate|profile|diagnostic|solve}
```

Режимы и поля — как в handoff `AGENT_START_PROMPT.md` и `packages/shared-contracts`.

## Stub

Пока solver **не реализован**. Для проверки контрактов:

```powershell
npm run validate:contracts
```

## Схемы

- `packages/shared-contracts/solver-input.schema.json` (0.1)
- `packages/shared-contracts/solver-input-v1_1.schema.json` (real candidate)
- `packages/shared-contracts/solver-output-v2.schema.json` (diagnostic)
