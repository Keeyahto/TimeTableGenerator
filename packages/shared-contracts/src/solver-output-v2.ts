/**
 * Solver diagnostic output contract v0.2 (C++ CLI -> web import).
 * Matches solver-output-v2.schema.json (handoff diagnostic-first report).
 */

export const SOLVER_OUTPUT_V2_SCHEMA_VERSION = "0.2" as const;

export type SolverOutputV2SchemaVersion = typeof SOLVER_OUTPUT_V2_SCHEMA_VERSION;

export interface SolverOutputV2Warning {
  code: string;
  message: string;
  [key: string]: unknown;
}

/** Rule penalty row in objective breakdown */
export type SolverRulePenalty = Record<string, unknown>;

/** Unscheduled lesson demand reference */
export type SolverUnscheduledLesson = Record<string, unknown>;

/** Virtual resource usage row */
export type SolverVirtualResourceUsage = Record<string, unknown>;

/** RELAXED_HARD or SOFT violation row */
export type SolverViolation = Record<string, unknown>;

/** Data-quality warning (often passthrough from input) */
export type SolverDataQualityWarning = Record<string, unknown>;

/** Rule registry status buckets (enforced / stub / disabled, etc.) */
export type SolverRulesByStatus = Record<string, unknown>;

/** Assignment grid / per-group views; null when not solving */
export type SolverOutputV2Schedule = Record<string, unknown> | unknown[] | null;

export interface SolverOutputV2 {
  schema_version: SolverOutputV2SchemaVersion;
  status: string;
  cp_sat_status: string;
  objective_value: number | null;
  best_objective_bound: number | null;
  gap: number | null;
  wall_time_seconds: number;
  enabled_rules: string[];
  rule_penalties: SolverRulePenalty[];
  unscheduled_lessons: SolverUnscheduledLesson[];
  virtual_teachers_used: SolverVirtualResourceUsage[];
  virtual_rooms_used: SolverVirtualResourceUsage[];
  relaxed_hard_violations: SolverViolation[];
  soft_violations: SolverViolation[];
  data_quality_warnings: SolverDataQualityWarning[];
  rules_by_status: SolverRulesByStatus;
  schedule: SolverOutputV2Schedule;
  warnings: SolverOutputV2Warning[];
}

export function createEmptySolverOutputV2(
  overrides: Partial<Omit<SolverOutputV2, "schema_version">> = {},
): SolverOutputV2 {
  return {
    schema_version: SOLVER_OUTPUT_V2_SCHEMA_VERSION,
    status: "STUB",
    cp_sat_status: "NOT_RUN",
    objective_value: null,
    best_objective_bound: null,
    gap: null,
    wall_time_seconds: 0,
    enabled_rules: [],
    rule_penalties: [],
    unscheduled_lessons: [],
    virtual_teachers_used: [],
    virtual_rooms_used: [],
    relaxed_hard_violations: [],
    soft_violations: [],
    data_quality_warnings: [],
    rules_by_status: {},
    schedule: null,
    warnings: [],
    ...overrides,
  };
}
