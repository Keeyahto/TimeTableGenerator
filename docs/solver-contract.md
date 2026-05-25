# Solver contract (CLI)

## Исполняемый файл

`schedule_solver`

## Аргументы

```
schedule_solver --input <path> --output <path> [--mode diagnostic]
```

| Аргумент | Обязательный | Описание |
|----------|--------------|----------|
| `--input` | да | Путь к normalized JSON (`SolverInput`) |
| `--output` | да | Путь для записи `SolverOutput` |
| `--mode` | нет | Сейчас поддерживается только `diagnostic` |

## Вход

Минимальный пример: `data/samples/synthetic-small/input.json`

Схема: `packages/shared-contracts/solver-input.schema.json`

## Выход (stub)

Текущая заглушка всегда пишет diagnostic JSON:

```json
{
  "schema_version": "0.1",
  "status": "STUB",
  "feasible": null,
  "solver_status": "NOT_RUN",
  "objective_value": null,
  "best_objective_bound": null,
  "gap": null,
  "enabled_rules": [],
  "warnings": [
    {
      "code": "SOLVER_NOT_IMPLEMENTED",
      "message": "C++ CP-SAT solver is not implemented yet"
    }
  ],
  "schedule": null,
  "artifacts": []
}
```

Схема: `packages/shared-contracts/solver-output.schema.json`

## Коды возврата (stub)

| Code | Причина |
|------|---------|
| 0 | Успех |
| 1 | Ошибка CLI |
| 2 | Нельзя записать output |
| 3 | Нельзя прочитать input |
| 4 | Input не JSON |
| 5+ | Ошибки движка (зарезервировано) |

## TODO

- Валидация input по JSON Schema перед запуском engine.
- Режимы `solve` / `prove` при появлении CP-SAT.
- Поля `infeasibility_candidates`, `diagnostics` в полном объёме.
