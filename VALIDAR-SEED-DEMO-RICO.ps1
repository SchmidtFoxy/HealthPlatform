param(
    [string]$Script = (Join-Path $PSScriptRoot 'POPULAR-LUCATTI-DEMO-RICO.ps1')
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Script)) { throw "Script nao encontrado: $Script" }

$tokens = $null
$errors = $null
[void][System.Management.Automation.Language.Parser]::ParseFile($Script, [ref]$tokens, [ref]$errors)

if ($errors.Count -gt 0) {
    Write-Host "Falha de sintaxe em $Script" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host ("  Linha {0}, coluna {1}: {2}" -f $e.Extent.StartLineNumber, $e.Extent.StartColumnNumber, $e.Message) -ForegroundColor Red
    }
    exit 1
}

$content = Get-Content -Raw $Script
$unsafeColon = [regex]::Matches($content, '\$[A-Za-z_][A-Za-z0-9_]*:')
if ($unsafeColon.Count -gt 0) {
    throw "Padrao potencialmente ambiguo de variavel seguido de ':' encontrado: $($unsafeColon[0].Value)"
}

Write-Host 'Seed demo rico: sintaxe PowerShell OK.' -ForegroundColor Green
Write-Host 'Nenhum padrao ambiguo $variavel: encontrado.' -ForegroundColor Green
