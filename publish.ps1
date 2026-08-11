<#
.SYNOPSIS
    Produit PokerRanges.exe en version autonome.

.DESCRIPTION
    Un exécutable unique, sans .NET à installer sur la machine cible. Les tests passent d'abord :
    publier un binaire qu'on n'a pas vérifié, c'est se préparer à le rappeler.

.PARAMETER SkipTests
    Publie sans lancer les tests. Pour un aller-retour rapide, pas pour une version qu'on garde.

.PARAMETER ReadyToRun
    Précompile en code natif : démarrage nettement plus vif, fichier plus gros. Le binaire devient
    spécifique à win-x64, ce qu'il est déjà de toute façon.
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$ReadyToRun
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$solution = Join-Path $root 'PokerRanges.slnx'
$project = Join-Path $root 'src\PokerRanges.App\PokerRanges.App.csproj'
$output = Join-Path $root 'publish\win-x64'

if (-not $SkipTests) {
    Write-Host 'Tests...' -ForegroundColor Cyan
    dotnet test $solution --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Tests en échec : rien n''est publié.' }
}

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

Write-Host 'Publication...' -ForegroundColor Cyan

$arguments = @($project, '-p:PublishProfile=win-x64', '--nologo')
if ($ReadyToRun) { $arguments += '-p:PublishReadyToRun=true' }

dotnet publish @arguments
if ($LASTEXITCODE -ne 0) { throw 'La publication a échoué.' }

$exe = Join-Path $output 'PokerRanges.exe'
if (-not (Test-Path $exe)) { throw "Publication terminée mais $exe est introuvable." }

$size = [math]::Round((Get-Item $exe).Length / 1MB, 1)

Write-Host ''
Write-Host "PokerRanges.exe — $size Mo" -ForegroundColor Green
Write-Host $exe
Write-Host ''
Write-Host 'Copiable tel quel : aucune installation de .NET requise sur la machine cible.'
Write-Host 'Réglages et charts se créent au premier lancement dans %APPDATA%\PokerRanges.'
