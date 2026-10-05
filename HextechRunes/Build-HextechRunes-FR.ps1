$ErrorActionPreference = "Stop"

# ============================================================
# HextechRunes - Build FR PCK (Windows)
# Génère uniquement HextechRunes.pck à partir des assets.
# Aucune recompilation C# n'est nécessaire pour une traduction.
# ============================================================

$RepoRoot = "D:\ARAM-MAYHEM-MOD\HextechRunes"
$GameDir  = "F:\SteamLibrary\steamapps\common\Slay the Spire 2"
$GodotDir = "C:\Users\Arcegis\Downloads\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64"

# Détecte automatiquement si le projet est directement à la racine
# ou dans le sous-dossier HextechRunes du dépôt Git.
if (Test-Path (Join-Path $RepoRoot "HextechRunes\tools\pack_mod.gd")) {
    $Root = Join-Path $RepoRoot "HextechRunes"
}
elseif (Test-Path (Join-Path $RepoRoot "tools\pack_mod.gd")) {
    $Root = $RepoRoot
}
else {
    throw "Impossible de trouver tools\pack_mod.gd dans $RepoRoot"
}

$GodotExe = Join-Path $GodotDir "Godot_v4.5.1-stable_mono_win64_console.exe"
$GameExe  = Join-Path $GameDir "SlayTheSpire2.exe"

$Tools         = Join-Path $Root "tools"
$Assets        = Join-Path $Root "assets"
$BuildRoot     = Join-Path $Root ".build_windows_fr"
$ImportProject = Join-Path $BuildRoot "import_project"
$ModRoot       = Join-Path $ImportProject "HextechRunes"
$Dist          = Join-Path $Root "dist_fr"
$Manifest      = Join-Path $Assets "HextechRunes.json"
$OutputPck     = Join-Path $Dist "HextechRunes.pck"

Write-Host ""
Write-Host "=== HextechRunes - Build FR ===" -ForegroundColor Cyan
Write-Host "Projet : $Root"
Write-Host "Godot  : $GodotExe"
Write-Host "Jeu    : $GameExe"
Write-Host ""

foreach ($Required in @(
    $GodotExe,
    $GameExe,
    (Join-Path $Tools "project.godot"),
    (Join-Path $Tools "pack_mod.gd"),
    $Manifest
)) {
    if (-not (Test-Path $Required)) {
        throw "Fichier introuvable : $Required"
    }
}

Write-Host "[1/4] Nettoyage..." -ForegroundColor Yellow
Remove-Item $BuildRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $Dist      -Recurse -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Force -Path $ModRoot | Out-Null
New-Item -ItemType Directory -Force -Path $Dist    | Out-Null

Write-Host "[2/4] Préparation des assets..." -ForegroundColor Yellow
Copy-Item (Join-Path $Tools "project.godot") (Join-Path $ImportProject "project.godot") -Force

# Copie toutes les ressources du mod dans le projet d'import temporaire.
Copy-Item (Join-Path $Assets "*") $ModRoot -Recurse -Force

# Le manifeste reste à l'extérieur du PCK, comme dans le script officiel.
Remove-Item (Join-Path $ModRoot "HextechRunes.json") -Force -ErrorAction SilentlyContinue

Write-Host "[3/4] Import Godot des ressources..." -ForegroundColor Yellow
& $GodotExe --headless --path $ImportProject --import
if ($LASTEXITCODE -ne 0) {
    throw "Godot a échoué pendant l'import des ressources (code $LASTEXITCODE)."
}

Copy-Item $Manifest (Join-Path $Dist "HextechRunes.json") -Force

Write-Host "[4/4] Création de HextechRunes.pck avec le runtime de Slay the Spire 2..." -ForegroundColor Yellow
& $GameExe `
    --headless `
    --path $Tools `
    -s "res://pack_mod.gd" `
    -- `
    $Manifest `
    $OutputPck `
    $ImportProject

if ($LASTEXITCODE -ne 0) {
    throw "Slay the Spire 2 a échoué pendant la création du PCK (code $LASTEXITCODE)."
}

if (-not (Test-Path $OutputPck)) {
    throw "Le processus s'est terminé sans créer $OutputPck"
}

$Size = (Get-Item $OutputPck).Length
if ($Size -le 0) {
    throw "HextechRunes.pck a été créé mais il est vide."
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host "SUCCÈS !" -ForegroundColor Green
Write-Host "PCK créé :" -ForegroundColor Green
Write-Host $OutputPck -ForegroundColor White
Write-Host "Taille : $([math]::Round($Size / 1MB, 2)) Mo"
Write-Host "==============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Remplace maintenant le HextechRunes.pck du mod installé par celui-ci."
Write-Host "Conseil : garde une copie de l'ancien PCK au cas où."
Write-Host ""
