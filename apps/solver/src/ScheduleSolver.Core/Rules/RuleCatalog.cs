namespace ScheduleSolver.Core.Rules;

public static class RuleCatalog
{
    private static readonly RuleDefinition[] Seed =
    [
        Def("R00", "PRECHECK_INPUT", RuleClass.PRECHECK, 0, EnforcementStatus.Enforced),
        Def("R01", "LESSON_STATE_VALID", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R02", "SLOT_VALID", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R03", "GROUP_NO_OVERLAP", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R04", "TEACHER_NO_OVERLAP", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R05", "ROOM_NO_OVERLAP", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R06", "ACTIVE_WEEKS_SATURDAY", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R07", "UNSCHEDULED_PENALTY", RuleClass.RELAXED_HARD, 1000, EnforcementStatus.Enforced),
        Def("R08", "VIRTUAL_TEACHER", RuleClass.RELAXED_HARD, 500, EnforcementStatus.Enforced),
        Def("R09", "VIRTUAL_ROOM", RuleClass.RELAXED_HARD, 500, EnforcementStatus.Enforced),
        Def("R10", "RELAXED_10", RuleClass.RELAXED_HARD, 900, EnforcementStatus.Stub),
        Def("R11", "RELAXED_11", RuleClass.RELAXED_HARD, 850, EnforcementStatus.Stub),
        Def("R12", "RELAXED_12", RuleClass.RELAXED_HARD, 800, EnforcementStatus.Stub),
        Def("R13", "RELAXED_13", RuleClass.RELAXED_HARD, 750, EnforcementStatus.Stub),
        Def("R14", "RELAXED_14", RuleClass.RELAXED_HARD, 700, EnforcementStatus.Stub),
        Def("R15", "RELAXED_15", RuleClass.RELAXED_HARD, 650, EnforcementStatus.Stub),
        Def("R16", "RELAXED_16", RuleClass.RELAXED_HARD, 600, EnforcementStatus.Stub),
        Def("R17", "RELAXED_17", RuleClass.RELAXED_HARD, 550, EnforcementStatus.Stub),
        Def("R18", "RELAXED_18", RuleClass.RELAXED_HARD, 500, EnforcementStatus.Stub),
        Def("R19", "RELAXED_19", RuleClass.RELAXED_HARD, 450, EnforcementStatus.Stub),
        Def("R20", "RELAXED_20", RuleClass.RELAXED_HARD, 400, EnforcementStatus.Stub),
        Def("R21", "SOFT_STRONG_21", RuleClass.SOFT_STRONG, 300, EnforcementStatus.Stub),
        Def("R22", "SOFT_STRONG_22", RuleClass.SOFT_STRONG, 280, EnforcementStatus.Stub),
        Def("R23", "SOFT_STRONG_23", RuleClass.SOFT_STRONG, 260, EnforcementStatus.Stub),
        Def("R24", "SOFT_STRONG_24", RuleClass.SOFT_STRONG, 240, EnforcementStatus.Stub),
        Def("R25", "LANGUAGE_PARALLEL", RuleClass.MODEL_HARD, 0, EnforcementStatus.Stub),
        Def("R26", "LANGUAGE_LINK", RuleClass.MODEL_HARD, 0, EnforcementStatus.Stub),
        Def("R27", "GYM_CUMULATIVE", RuleClass.MODEL_HARD, 0, EnforcementStatus.Stub),
        Def("R28", "GYM_NO_OVERLAP", RuleClass.MODEL_HARD, 0, EnforcementStatus.Stub),
        Def("R29", "SOFT_MEDIUM_29", RuleClass.SOFT_MEDIUM, 120, EnforcementStatus.Stub),
        Def("R30", "SOFT_MEDIUM_30", RuleClass.SOFT_MEDIUM, 100, EnforcementStatus.Stub),
        Def("R31", "SOFT_MEDIUM_31", RuleClass.SOFT_MEDIUM, 80, EnforcementStatus.Stub),
        Def("R32", "SOFT_MEDIUM_32", RuleClass.SOFT_MEDIUM, 60, EnforcementStatus.Stub),
        Def("R33", "SOFT_WEAK_33", RuleClass.SOFT_WEAK, 40, EnforcementStatus.Stub),
        Def("R34", "SOFT_WEAK_34", RuleClass.SOFT_WEAK, 30, EnforcementStatus.Stub),
        Def("R35", "SOFT_WEAK_35", RuleClass.SOFT_WEAK, 20, EnforcementStatus.Stub),
        Def("R36", "SOFT_WEAK_36", RuleClass.SOFT_WEAK, 10, EnforcementStatus.Stub),
        Def("R37", "UNRESOLVED_37", RuleClass.UNRESOLVED, 0, EnforcementStatus.Disabled),
        Def("R38", "UNRESOLVED_38", RuleClass.UNRESOLVED, 0, EnforcementStatus.Disabled),
        Def("R39", "DOMAIN_FACT_39", RuleClass.DOMAIN_FACT, 0, EnforcementStatus.Disabled),
        Def("R40", "DIAGNOSTIC_POLICY_40", RuleClass.DIAGNOSTIC_POLICY, 0, EnforcementStatus.Disabled),
    ];

    public static IReadOnlyList<RuleDefinition> All => Seed;

    private static RuleDefinition Def(
        string id,
        string code,
        RuleClass ruleClass,
        int penalty,
        EnforcementStatus status) =>
        new(id, code, ruleClass, penalty, status);
}
