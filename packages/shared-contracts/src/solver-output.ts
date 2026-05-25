/**
 * Solver output contract (C++ CLI -> web import).
 * TODO: expand schedule assignment model and infeasibility diagnostics.
 */

export const SOLVER_OUTPUT_SCHEMA_VERSION = "0.1" as const;

export type SolverOutputSchemaVersion = typeof SOLVER_OUTPUT_SCHEMA_VERSION;

export interface SolverWarning {
  code: string;
  message: string;
  [key: string]: unknown;
}

export interface SolverArtifactRef {
  type: string;
  path: string;
  [key: string]: unknown;
}

/** TODO: assignment grid / per-group views */
export type SolverSchedule = Record<string, unknown> | unknown[] | null;

/** TODO: structured infeasibility hints */
export type InfeasibilityCandidate = Record<string, unknown>;

export interface SolverOutput {
  schema_version: SolverOutputSchemaVersion;
  status: string;
  feasible: boolean | null;
  solver_status: string;
  objective_value: number | null;
  best_objective_bound: number | null;
  gap: number | null;
  enabled_rules: string[];
  warnings: SolverWarning[];
  schedule: SolverSchedule;
  artifacts: SolverArtifactRef[];
  infeasibility_candidates?: InfeasibilityCandidate[];
  diagnostics?: Record<string, unknown>;
}

export function createStubSolverOutput(): SolverOutput {
  return {
    schema_version: SOLVER_OUTPUT_SCHEMA_VERSION,
    status: "STUB",
    feasible: null,
    solver_status: "NOT_RUN",
    objective_value: null,
    best_objective_bound: null,
    gap: null,
    enabled_rules: [],
    warnings: [
      {
        code: "SOLVER_NOT_IMPLEMENTED",
        message: "C++ CP-SAT solver is not implemented yet",
      },
    ],
    schedule: null,
    artifacts: [],
  };
}
