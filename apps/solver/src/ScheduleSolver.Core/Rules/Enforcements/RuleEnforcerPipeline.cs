using ScheduleSolver.Core.Model;

namespace ScheduleSolver.Core.Rules.Enforcements;

public static class RuleEnforcerPipeline
{
    private static readonly IRuleEnforcer[] Enforcers =
    [
        new ModelStructureEnforcer(),
        new R07UnscheduledEnforcer(),
        new R08VirtualTeacherEnforcer(),
        new R09VirtualRoomEnforcer(),
        new R10TeacherUnavailableEnforcer(),
        new R19GroupMaxPerDayEnforcer(),
        new R11ThursdayMeetingEnforcer(),
        new R12FirstCourseShiftEnforcer(),
        new R13GraduationShiftEnforcer(),
        new R14FirstCourseNoSaturdayEnforcer(),
        new R22AdminNoSaturdayEnforcer(),
        new R20GroupNoGapsEnforcer(),
        new R21TeacherNoGapsEnforcer(),
        new R24SubjectMaxOncePerDayEnforcer(),
        new RoomBlockedDaysEnforcer("R29"),
        new RoomBlockedDaysEnforcer("R30"),
    ];

    public static void ApplyAll(SchedulingBuildContext ctx)
    {
        foreach (var enforcer in Enforcers)
        {
            if (!ctx.Registry.TryGet(enforcer.RuleId, out var def)
                || def.DefaultStatus != EnforcementStatus.Enforced)
            {
                continue;
            }

            enforcer.Apply(ctx);
            if (!ctx.EnforcedRuleIds.Contains(enforcer.RuleId, StringComparer.Ordinal))
            {
                ctx.EnforcedRuleIds.Add(enforcer.RuleId);
            }
        }
    }
}
