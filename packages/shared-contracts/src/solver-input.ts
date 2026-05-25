/**
 * Normalized solver input contract (web export -> C++ CLI).
 * TODO: tighten entity shapes after data-quality / manual-review stage.
 */

export const SOLVER_INPUT_SCHEMA_VERSION = "0.1" as const;

export type SolverInputSchemaVersion = typeof SOLVER_INPUT_SCHEMA_VERSION;

/** TODO: define calendar slots, week model, holidays */
export type SolverCalendar = Record<string, unknown>;

/** TODO: canonical group rows */
export type SolverGroup = Record<string, unknown>;

/** TODO: canonical teacher rows */
export type SolverTeacher = Record<string, unknown>;

/** TODO: canonical room rows */
export type SolverRoom = Record<string, unknown>;

/** TODO: canonical subject rows */
export type SolverSubject = Record<string, unknown>;

/** TODO: lesson demand rows derived from web normalization */
export type SolverLessonDemand = Record<string, unknown>;

/** TODO: hard/soft constraints bundle */
export type SolverConstraints = Record<string, unknown>;

/** TODO: engine tuning (time limit, workers, etc.) */
export type SolverConfig = Record<string, unknown>;

/** TODO: optional rule_config mirror for future split */
export type SolverRuleConfig = Record<string, unknown>;

export interface SolverInput {
  schema_version: SolverInputSchemaVersion;
  calendar: SolverCalendar;
  groups: SolverGroup[];
  teachers: SolverTeacher[];
  rooms: SolverRoom[];
  subjects: SolverSubject[];
  lesson_demands: SolverLessonDemand[];
  constraints: SolverConstraints;
  solver_config: SolverConfig;
  rule_config?: SolverRuleConfig;
}
