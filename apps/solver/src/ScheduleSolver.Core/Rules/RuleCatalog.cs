namespace ScheduleSolver.Core.Rules;

public static class RuleCatalog
{
    private static readonly RuleDefinition[] Seed =
    [
        Def("R00", "input_references_valid", RuleClass.PRECHECK, 0, EnforcementStatus.Enforced),
        Def("R01", "lesson_state_exactly_one", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R02", "valid_slot_duration", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R03", "group_no_overlap", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R04", "teacher_no_real_overlap", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R05", "room_real_capacity", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R06", "group_active_weeks", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R07", "unscheduled_lesson", RuleClass.RELAXED_HARD, 1_000_000, EnforcementStatus.Enforced),
        Def("R08", "virtual_teacher_non_vacancy", RuleClass.RELAXED_HARD, 300_000, EnforcementStatus.Enforced),
        Def("R09", "virtual_room", RuleClass.RELAXED_HARD, 200_000, EnforcementStatus.Enforced),
        Def("R10", "teacher_unavailable", RuleClass.RELAXED_HARD, 80_000, EnforcementStatus.Enforced),
        Def("R11", "teacher_thu_1_meeting", RuleClass.RELAXED_HARD, 90_000, EnforcementStatus.Enforced),
        Def("R12", "first_course_first_shift", RuleClass.RELAXED_HARD, 80_000, EnforcementStatus.Enforced),
        Def("R13", "graduation_first_shift", RuleClass.RELAXED_HARD, 80_000, EnforcementStatus.Enforced),
        Def("R14", "first_course_no_saturday", RuleClass.RELAXED_HARD, 70_000, EnforcementStatus.Enforced),
        Def("R15", "saturday_slots_1_4", RuleClass.MODEL_HARD, 0, EnforcementStatus.Enforced),
        Def("R16", "class_hour_monday_1_or_5", RuleClass.RELAXED_HARD, 60_000, EnforcementStatus.Stub),
        Def("R17", "class_hour_teacher", RuleClass.RELAXED_HARD, 90_000, EnforcementStatus.Stub),
        Def("R18", "class_hour_day_max_3_other_pairs", RuleClass.SOFT_STRONG, 10_000, EnforcementStatus.Stub),
        Def("R19", "group_max_4_per_day", RuleClass.RELAXED_HARD, 50_000, EnforcementStatus.Enforced),
        Def("R20", "group_no_gaps", RuleClass.SOFT_STRONG, 20_000, EnforcementStatus.Enforced),
        Def("R21", "teacher_no_gaps", RuleClass.SOFT_MEDIUM, 1_000, EnforcementStatus.Enforced),
        Def("R22", "admin_no_saturday", RuleClass.RELAXED_HARD, 50_000, EnforcementStatus.Enforced),
        Def("R23", "room_manager_own_room", RuleClass.SOFT_STRONG, 30_000, EnforcementStatus.Stub),
        Def("R24", "subject_max_once_per_day", RuleClass.SOFT_WEAK, 1_000, EnforcementStatus.Stub),
        Def("R25", "language_parallel_subgroups", RuleClass.SOFT_STRONG, 40_000, EnforcementStatus.Stub),
        Def("R26", "language_different_teachers", RuleClass.RELAXED_HARD, 70_000, EnforcementStatus.Stub),
        Def("R27", "gym_two_groups_max", RuleClass.MODEL_HARD, 0, EnforcementStatus.Stub),
        Def("R28", "gym_parallel_different_teachers", RuleClass.RELAXED_HARD, 60_000, EnforcementStatus.Stub),
        Def("R29", "room_203_blocked_wed_thu", RuleClass.RELAXED_HARD, 50_000, EnforcementStatus.Stub),
        Def("R30", "room_305_blocked_wed_fri", RuleClass.RELAXED_HARD, 50_000, EnforcementStatus.Stub),
        Def("R31", "upper_lower_week_pattern", RuleClass.RELAXED_HARD, 70_000, EnforcementStatus.Stub),
        Def("R32", "teacher_specific_days", RuleClass.RELAXED_HARD, 80_000, EnforcementStatus.Stub),
        Def("R33", "teacher_multiple_subjects", RuleClass.DOMAIN_FACT, 0, EnforcementStatus.Disabled),
        Def("R34", "subject_different_teachers_by_group", RuleClass.DOMAIN_FACT, 0, EnforcementStatus.Disabled),
        Def("R35", "different_group_week_counts", RuleClass.DOMAIN_FACT, 0, EnforcementStatus.Disabled),
        Def("R36", "vacant_hours_virtual_teacher", RuleClass.DIAGNOSTIC_POLICY, 0, EnforcementStatus.Disabled),
        Def("R37", "building_transition", RuleClass.UNRESOLVED, 0, EnforcementStatus.Disabled),
        Def("R38", "lab_same_day_subgroups", RuleClass.UNRESOLVED, 0, EnforcementStatus.Disabled),
        Def("R39", "technology_lab_capacity", RuleClass.UNRESOLVED, 0, EnforcementStatus.Disabled),
        Def("R40", "practice_rooms", RuleClass.UNRESOLVED, 0, EnforcementStatus.Disabled),
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
