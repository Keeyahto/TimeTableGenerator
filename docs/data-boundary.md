# Граница данных

## Контур web / data-audit

Следующее остаётся **только** во frontend и слое управления данными:

- загрузка XLSX и других «грязных» источников;
- парсинг Excel;
- data quality checks;
- ручная валидация и `ManualDecision`;
- исправление ФИО, дублей, опечаток;
- формирование audit-отчётов (`DataAuditRun`).

## Контур solver

Solver принимает **только** нормализованный контракт `SolverInput` (см. `packages/shared-contracts`):

- `schema_version`
- `calendar`, `groups`, `teachers`, `rooms`, `subjects`, `lesson_demands`
- `constraints`, `solver_config` (и опционально `rule_config`)

Solver **не должен**:

- читать XLSX;
- подключаться к БД;
- применять ручные решения напрямую (они уже отражены в normalized input web-слоем);
- «чинить» ФИО или справочники на лету.

## Поток

1. Web сохраняет исходный файл (`DataSourceFile`).
2. Web запускает audit → `DataAuditRun.reportJson`.
3. Оператор принимает решения → `ManualDecision`.
4. Web экспортирует `input.json` для solver.
5. Web запускает CLI (позже) и сохраняет `SolverRun` + `SolverArtifact`.

## TODO

- Детальная схема нормализации export pipeline.
- Версионирование контрактов при изменении полей.
