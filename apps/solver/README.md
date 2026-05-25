# schedule_solver (C++ CLI)

Изолированный backend-компонент: принимает нормализованный JSON, возвращает JSON. Без БД, без UI, без Prisma.

## Сборка

```powershell
cmake -S apps/solver -B apps/solver/build
cmake --build apps/solver/build --config Release
```

```bash
cmake -S apps/solver -B apps/solver/build
cmake --build apps/solver/build
```

## Запуск (stub)

```powershell
apps\solver\build\Release\schedule_solver.exe `
  --input data\samples\synthetic-small\input.json `
  --output tmp\solver-output.json `
  --mode diagnostic
```

Или из корня репозитория:

```powershell
.\scripts\run-solver-dev.ps1
```

## CLI

```
schedule_solver --input <path> --output <path> [--mode diagnostic]
```

## CMake options

| Option | Default | Описание |
|--------|---------|----------|
| `SCHED_ENABLE_ORTOOLS` | `OFF` | Заготовка под OR-Tools CP-SAT |

## Структура исходников

- `src/main.cpp` — CLI
- `src/json_io.*` — чтение/запись файлов
- `src/solver_engine.h` / `solver_engine_stub.cpp` — заглушка движка
- `src/diagnostic_report.h` — формат diagnostic output

## TODO

- Валидация input по JSON Schema из `packages/shared-contracts`
- Реальный CP-SAT engine при `SCHED_ENABLE_ORTOOLS=ON`
- Расширенная диагностика infeasibility
