<#
.SYNOPSIS
	Mutation testing for the simulation with Stryker.NET. See docs/TestPlan.md.

.DESCRIPTION
	Stryker changes the simulation code in small ways (e.g. < into <=, + into -) and runs the unit tests against
	every change. A change no test notices "survived": a check is missing, or the change makes no difference.
	Prints a table per file and, with -Survivors, every surviving change with its line.

.EXAMPLE
	powershell -ExecutionPolicy Bypass -File tools/MutationTesting/run.ps1
	Everything (about 4 minutes).

.EXAMPLE
	powershell -ExecutionPolicy Bypass -File tools/MutationTesting/run.ps1 Cats/Skill.cs Health/* -Survivors
	Only these files (paths below scripts/Simulation), and list what survived.

.EXAMPLE
	powershell -ExecutionPolicy Bypass -File tools/MutationTesting/run.ps1 -LastReport -Survivors
	Don't run anything, show the last report again.
#>
param(
	[Parameter(Position = 0, ValueFromRemainingArguments = $true)]
	[string[]] $Files,
	[switch] $Survivors,
	[switch] $LastReport
)

$ErrorActionPreference = 'Stop'
$testsDir = Join-Path $PSScriptRoot 'Tests'
$outputDir = Join-Path $testsDir 'StrykerOutput'

if (-not $LastReport) {
	Push-Location $testsDir
	try {
		# The Stryker version is pinned in dotnet-tools.json next to this script
		dotnet tool restore | Out-Null
		$strykerArgs = @()
		foreach ($file in $Files) {
			$strykerArgs += '--mutate'
			$strykerArgs += "**/Simulation/$file"
		}
		dotnet tool run dotnet-stryker @strykerArgs
		if ($LASTEXITCODE -ne 0) {
			exit $LASTEXITCODE
		}
	}
	finally {
		Pop-Location
	}
}

$run = Get-ChildItem $outputDir -Directory -ErrorAction SilentlyContinue | Sort-Object Name | Select-Object -Last 1
if ($null -eq $run) {
	Write-Host 'No report yet. Run without -LastReport first.'
	exit 1
}
$reportPath = Join-Path $run.FullName 'reports\mutation-report.json'
$report = Get-Content -Raw -Encoding UTF8 $reportPath | ConvertFrom-Json

# Killed and Timeout count as found by the tests. Ignored (Tuning.cs, text) and CompileError don't count at all.
$rows = @()
$totalKilled = 0
$totalMissed = 0
foreach ($file in $report.files.PSObject.Properties) {
	$mutants = @($file.Value.mutants)
	$killed = @($mutants | Where-Object { $_.status -in 'Killed', 'Timeout' }).Count
	$survived = @($mutants | Where-Object { $_.status -eq 'Survived' }).Count
	$noCoverage = @($mutants | Where-Object { $_.status -eq 'NoCoverage' }).Count
	$counted = $killed + $survived + $noCoverage
	if ($counted -eq 0) {
		continue
	}
	$totalKilled += $killed
	$totalMissed += $survived + $noCoverage
	$rows += [pscustomobject]@{
		File = ($file.Name -replace '^.*[\\/]Simulation[\\/]', '')
		Score = '{0,3}%' -f [math]::Round(100 * $killed / $counted)
		Killed = $killed
		Survived = $survived
		NoCoverage = $noCoverage
	}
}

$rows | Sort-Object File | Format-Table -AutoSize | Out-Host
if ($totalKilled + $totalMissed -gt 0) {
	Write-Host ('Total: {0}% ({1} of {2} found by the tests)' -f `
		[math]::Round(100 * $totalKilled / ($totalKilled + $totalMissed)), $totalKilled, ($totalKilled + $totalMissed))
}

if ($Survivors) {
	foreach ($file in $report.files.PSObject.Properties | Sort-Object Name) {
		$missed = @($file.Value.mutants | Where-Object { $_.status -in 'Survived', 'NoCoverage' } |
			Sort-Object { $_.location.start.line })
		if ($missed.Count -eq 0) {
			continue
		}
		$lines = $file.Value.source -split "`n"
		Write-Host ''
		Write-Host ('=== ' + ($file.Name -replace '^.*[\\/]Simulation[\\/]', ''))
		foreach ($mutant in $missed) {
			$line = $mutant.location.start.line
			$status = if ($mutant.status -eq 'NoCoverage') { 'no test' } else { 'survived' }
			$replacement = ($mutant.replacement -replace '\s+', ' ')
			Write-Host ('{0,4} {1,-8} {2}' -f $line, $status, $lines[$line - 1].Trim())
			Write-Host ('              -> {0}' -f $replacement)
		}
	}
}

Write-Host ''
Write-Host "Full report (open in a browser): $(Join-Path $run.FullName 'reports\mutation-report.html')"
