$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "" 
Write-Host "CONFIGURACION DEL CORREO REAL - MICROBENEFICIO SAN GABRIEL" -ForegroundColor Cyan
Write-Host "Este asistente guardara la clave mediante User Secrets; no la escribe en appsettings.json." -ForegroundColor DarkGray
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "No se encontro el comando dotnet. Instale el SDK de .NET 8 o abra el proyecto con Visual Studio 2022 actualizado." -ForegroundColor Red
    Read-Host "Presione Enter para cerrar"
    exit 1
}

$correoPredeterminado = "soportemicrobeneficiosg@gmail.com"
$correo = Read-Host "Correo remitente [$correoPredeterminado]"
if ([string]::IsNullOrWhiteSpace($correo)) {
    $correo = $correoPredeterminado
}

$securePassword = Read-Host "Pegue la contrasena de aplicacion de Google (16 caracteres)" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
}

$password = ($password -replace '\s', '')
if ($password.Length -ne 16) {
    Write-Host "La contrasena de aplicacion debe tener 16 caracteres despues de quitar espacios." -ForegroundColor Red
    Read-Host "Presione Enter para cerrar"
    exit 1
}

try {
    dotnet user-secrets set "EmailSettings:Enabled" "true" | Out-Null
    dotnet user-secrets set "EmailSettings:Mail" $correo | Out-Null
    dotnet user-secrets set "EmailSettings:DisplayName" "Microbeneficio San Gabriel" | Out-Null
    dotnet user-secrets set "EmailSettings:Password" $password | Out-Null
    dotnet user-secrets set "EmailSettings:Host" "smtp.gmail.com" | Out-Null
    dotnet user-secrets set "EmailSettings:Port" "587" | Out-Null
    dotnet user-secrets set "EmailSettings:EnableSsl" "true" | Out-Null

    # Borra valores del modo local que pudieran quedar de una version anterior.
    dotnet user-secrets remove "EmailSettings:Mode" 2>$null | Out-Null
    dotnet user-secrets remove "EmailSettings:DevelopmentOutputPath" 2>$null | Out-Null

    Write-Host "" 
    Write-Host "Correo SMTP configurado correctamente para $correo." -ForegroundColor Green
    Write-Host "Cierre y vuelva a abrir Visual Studio; despues use Reenviar correo en la cuenta pendiente." -ForegroundColor Yellow
}
catch {
    Write-Host "No fue posible guardar la configuracion: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    $password = $null
}

Read-Host "Presione Enter para cerrar"
