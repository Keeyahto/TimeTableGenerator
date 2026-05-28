# Kill leftover testhost / solver processes that hoard RAM after interrupted dotnet test.
param([switch]$WhatIf)

$killed = @()

foreach ($name in @("testhost", "ScheduleSolver.Cli", "MemoryHog")) {
    Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
        $mb = [int]($_.WorkingSet64 / 1MB)
        $killed += [pscustomobject]@{ Id = $_.Id; Name = $_.ProcessName; MB = $mb }
        if (-not $WhatIf) {
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    ($_.WorkingSet64 / 1MB) -ge 1024
} | ForEach-Object {
    $mb = [int]($_.WorkingSet64 / 1MB)
    $killed += [pscustomobject]@{ Id = $_.Id; Name = "dotnet"; MB = $mb }
    if (-not $WhatIf) {
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
}

if ($killed.Count -eq 0) {
    Write-Host "No heavy solver/testhost processes found."
} else {
    Write-Host $(if ($WhatIf) { "Would kill:" } else { "Killed:" })
    $killed | Format-Table -AutoSize
}
