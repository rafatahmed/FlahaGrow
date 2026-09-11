[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Epw,
    [string]$RadianceBin = 'C:\Program Files\ladybug_tools\radiance\bin',
    [switch]$Keep
)

$ErrorActionPreference = 'Stop'
$Epw = [IO.Path]::GetFullPath($Epw)
$RadianceBin = [IO.Path]::GetFullPath($RadianceBin)
if (-not (Test-Path -LiteralPath $Epw)) { throw "EPW was not found: $Epw" }
foreach ($tool in 'epw2wea','gendaymtx','oconv','rfluxmtx','dctimestep','rmtxop','cnt','rcalc','rcontrib') {
    if (-not (Test-Path -LiteralPath (Join-Path $RadianceBin "$tool.exe"))) { throw "Radiance executable was not found: $tool.exe" }
}
$library = Join-Path (Split-Path -Parent $RadianceBin) 'lib'
foreach ($cal in 'reinsrc.cal','reinhart.cal') { if (-not (Test-Path -LiteralPath (Join-Path $library $cal))) { throw "Radiance calculation file was not found: $cal" } }

$root = Join-Path ([IO.Path]::GetTempPath()) ('FlahaGrow-annual-reference-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$succeeded = $false
try {
    Copy-Item -LiteralPath $Epw -Destination (Join-Path $root 'weather.epw')
    Set-Content -LiteralPath (Join-Path $root 'envelope.mat') -Value @'
void plastic ground
0
0
5 0.2 0.2 0.2 0 0
'@
    Set-Content -LiteralPath (Join-Path $root 'envelope.blk') -Value @'
void plastic ground
0
0
5 0 0 0 0 0
'@
    Set-Content -LiteralPath (Join-Path $root 'envelope.rad') -Value @'
ground polygon ground
0
0
12 -20 -20 0 20 -20 0 20 20 0 -20 20 0
'@
    Set-Content -LiteralPath (Join-Path $root '0.pts') -Value '0 0 1 0 0 1'
    Set-Content -LiteralPath (Join-Path $root 'skyglow.rad') -Value @'
#@rfluxmtx u=+Y h=u
void glow groundglow
0
0
4 1 1 1 0
groundglow source ground
0
0
4 0 0 -1 180
#@rfluxmtx u=+Y h=r1
void glow skyglow
0
0
4 1 1 1 0
skyglow source sky
0
0
4 0 0 1 180
'@
    $batch = @"
@echo off
setlocal EnableExtensions DisableDelayedExpansion
set "PATH=$RadianceBin;%PATH%"
set "RAYPATH=.;$library"
epw2wea weather.epw Weather.wea || exit /b 1
gendaymtx -m 1 Weather.wea > Weather.smx || exit /b 1
oconv envelope.mat envelope.rad > amodel.oct || exit /b 1
rfluxmtx -I+ -y 1 -ab 1 -ad 128 -lw 0.01 - skyglow.rad -i amodel.oct < 0.pts > illum.mtx || exit /b 1
dctimestep illum.mtx Weather.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualR.ill || exit /b 1
oconv envelope.blk envelope.rad > bmodel.oct || exit /b 1
rfluxmtx -I+ -y 1 -ab 0 -ad 128 -lw 0.01 - skyglow.rad -i bmodel.oct < 0.pts > billum.mtx || exit /b 1
gendaymtx -m 1 -d Weather.wea > Weatherd.smx || exit /b 1
dctimestep billum.mtx Weatherd.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualRd.ill || exit /b 1
echo void light solar 0 0 3 1e6 1e6 1e6 > suns.rad
cnt 577 | rcalc -e MF:2 -f reinsrc.cal -e Rbin=recno -o "solar source sun 0 0 4 `${Dx} `${Dy} `${Dz} 0.533" >> suns.rad || exit /b 1
oconv -f envelope.blk envelope.rad suns.rad > sun.oct || exit /b 1
rcontrib -I+ -ab 1 -y 1 -n 1 -ad 64 -lw 1e-2 -dc 1 -dt 0 -dj 0 -fa -e MF:2 -f reinhart.cal -b rbin -bn Nrbins -m solar sun.oct < 0.pts > sun.mtx || exit /b 1
gendaymtx -5 0.533 -d -m 2 Weather.wea > WeatherSun.smx || exit /b 1
dctimestep sun.mtx WeatherSun.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualRs.ill || exit /b 1
rmtxop -fa annualR.ill + -s -1 annualRd.ill + annualRs.ill > annualRfinal.ill || exit /b 1
"@
    Set-Content -LiteralPath (Join-Path $root 'reference.bat') -Value $batch -Encoding ascii
    Push-Location -LiteralPath $root
    try {
        # Radiance may write non-fatal diagnostics to stderr. The batch exit code is authoritative.
        $previousErrorAction = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try { & (Join-Path $env:SystemRoot 'System32\cmd.exe') /d /c reference.bat; $batchExit = $LASTEXITCODE }
        finally { $ErrorActionPreference = $previousErrorAction }
        if ($batchExit -ne 0) { throw "Reference annual Radiance batch failed with exit code $batchExit. Retained at $root" }
    }
    finally { Pop-Location }
    $numbers = Get-Content -LiteralPath (Join-Path $root 'annualRfinal.ill') | Where-Object { $_ -notmatch '^[#A-Za-z]' -and $_ -notmatch '^$' } | ForEach-Object { [double]::Parse($_, [Globalization.CultureInfo]::InvariantCulture) }
    if ($numbers.Count -ne 8760) { throw "Reference produced $($numbers.Count) final samples, expected 8760. Retained at $root" }
    if (@($numbers | Where-Object { $_ -ne $_ -or [double]::IsInfinity($_) -or $_ -lt 0 }).Count -ne 0) { throw "Reference final illuminance contains a negative/nonfinite value. Retained at $root" }
    [pscustomobject]@{ Fixture = 'one upward sensor above an unobstructed diffuse ground'; Samples = $numbers.Count; MinimumLux = ($numbers | Measure-Object -Minimum).Minimum; MaximumLux = ($numbers | Measure-Object -Maximum).Maximum; Folder = $(if ($Keep) { $root } else { '<removed after successful validation>' }) } | Format-List
    $succeeded = $true
}
finally {
    if ($succeeded -and -not $Keep -and (Test-Path -LiteralPath $root)) { Remove-Item -LiteralPath $root -Recurse -Force }
}
