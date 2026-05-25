# Solver (.NET) — запланирован

Решатель будет **отдельным .NET console-приложением** с [Google.OrTools](https://developers.google.com/optimization/install/dotnet/pkg_windows) (NuGet).

## Статус

- Код solver **не реализован** — только контракты в `packages/shared-contracts`.
- Дизайн: `docs/superpowers/specs/2026-05-26-csharp-solver-design.md`

## Планируемый контракт (без изменений границы)

- Вход: JSON-файл (`SolverInput`)
- Выход: JSON-файл (`SolverOutput` / diagnostic v0.2)
- Без БД, без UI, без XLSX

## Пока не начинать здесь

Дождаться отдельного плана на C# solver. См. `docs/architecture.md`.
