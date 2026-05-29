# Generates git-safe stress-medium-v1_1 (no PD). Uses handoff only for counts when present.
param(
    [string]$Output = "",
    [int]$DemandTarget = 135
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $Output) {
    $Output = Join-Path $repo "data\samples\stress-medium-v1_1\input.json"
}

$handoff = Join-Path $repo "data\solver_agent_full_handoff_v2\02_canonical_solver_input_v1_1\solver_input_real_v1\variant_A_no_merge_bakirova_valieva.json"
$gymShare = 0.08
$langShare = 0.12
$classHourShare = 0.03
if (Test-Path $handoff) {
    $h = Get-Content $handoff -Raw | ConvertFrom-Json
    $total = [math]::Max($h.lesson_demands.Count, 1)
    $gymShare = ($h.lesson_demands | Where-Object { $_.room_id -match 'gym|спорт' }).Count / $total
    $langShare = ($h.lesson_demands | Where-Object { $_.lesson_type -eq 'foreign_language' }).Count / $total
    Write-Host "Handoff ratios: gym=$([math]::Round($gymShare,3)) lang=$([math]::Round($langShare,3))"
}

$days = @('mon', 'tue', 'wed', 'thu', 'fri')
$parities = @('upper', 'lower')
$slots = [System.Collections.Generic.List[object]]::new()
foreach ($parity in $parities) {
    foreach ($day in $days) {
        for ($li = 1; $li -le 6; $li++) {
            $slots.Add([ordered]@{
                    slot_id    = "${parity}_${day}_$li"
                    week_parity = $parity
                    day_id     = $day
                    lesson_index = $li
                    shift_id   = if ($li -le 4) { 'first' } else { 'second' }
                })
        }
    }
    foreach ($li in 1..4) {
        $slots.Add([ordered]@{
                slot_id    = "${parity}_sat_$li"
                week_parity = $parity
                day_id     = 'sat'
                lesson_index = $li
                shift_id   = 'first'
            })
    }
}

$groupCount = 28
$teacherCount = 45
$roomCount = 22
$groups = 1..$groupCount | ForEach-Object {
    [ordered]@{
        id = ('g{0:D2}' -f $_)
        course_year = if ($_ % 4 -eq 1) { 1 } else { 2 }
        max_lessons_per_day = 4
        class_teacher_id = ('t{0:D2}' -f (($_ - 1) % $teacherCount + 1))
    }
}
$teachers = 1..$teacherCount | ForEach-Object { [ordered]@{ id = ('t{0:D2}' -f $_) } }
$rooms = 1..($roomCount - 1) | ForEach-Object { [ordered]@{ id = ('r{0:D2}' -f $_); capacity = 30 } }
$rooms += [ordered]@{ id = 'gym'; room_type = 'gym'; max_parallel_groups = 2; capacity = 50 }
$subjects = 1..15 | ForEach-Object { [ordered]@{ id = ('sub{0:D2}' -f $_) } }

$demands = [System.Collections.Generic.List[object]]::new()
$rng = [System.Random]::new(42)
$id = 0
while ($demands.Count -lt $DemandTarget) {
    $id++
    $g = $groups[$rng.Next($groupCount)]
    $t = $teachers[$rng.Next($teacherCount)]
    $roll = $rng.NextDouble()
    $room = $rooms[$rng.Next($rooms.Count - 1)]
    $lessonType = $null
    $parallelKey = $null
    $weekParity = if ($rng.Next(2) -eq 0) { 'upper' } else { 'lower' }
    if ($roll -lt $classHourShare) {
        $lessonType = 'class_hour'
        $t = [ordered]@{ id = $g.class_teacher_id }
        $weekParity = 'upper'
    }
    elseif ($roll -lt $classHourShare + $langShare) {
        $lessonType = 'foreign_language'
        $parallelKey = "lang-$($g.id)"
    }
    elseif ($roll -lt $classHourShare + $langShare + $gymShare) {
        $room = $rooms[-1]
    }
    $demands.Add([ordered]@{
            id = ('ld_{0:D4}' -f $id)
            group_id = $g.id
            subject_id = $subjects[$rng.Next($subjects.Count)].id
            teacher_id = $t.id
            room_id = $room.id
            lesson_type = $lessonType
            language_parallel_key = $parallelKey
            week_parity = $weekParity
            hours_per_week = 1
        })
}

$weeks = 1..4 | ForEach-Object {
    [ordered]@{
        week_index = $_
        week_parity = if ($_ % 2 -eq 1) { 'upper' } else { 'lower' }
    }
}

$doc = [ordered]@{
    schema_version = 'real_candidate_v1_1'
    calendar = [ordered]@{
        weeks = $weeks
        slots = $slots
    }
    groups = $groups
    teachers = $teachers
    rooms = $rooms
    subjects = $subjects
    lesson_demands = $demands
    rules = @(
        [ordered]@{ id = 'R19'; enabled = $true; params = [ordered]@{ max_lessons_per_day = 4 } }
    )
    solver_config = [ordered]@{ mode = 'solve'; time_limit_sec = 30 }
}

New-Item -ItemType Directory -Force -Path (Split-Path $Output) | Out-Null
$doc | ConvertTo-Json -Depth 12 | Set-Content $Output -Encoding UTF8
Write-Host "Wrote $Output demands=$($demands.Count) slots=$($slots.Count)"
