param([int]$Port = 14333)
$ErrorActionPreference = 'Stop'
$container = 'nhat-tests-' + [Guid]::NewGuid().ToString('N')
$password = 'Tests!aA1' + [Guid]::NewGuid().ToString('N')
$previousConnection = $env:STUDENT_TEST_SQL
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    docker run --detach --name $container -e ACCEPT_EULA=Y -e MSSQL_PID=Developer `
        -e "MSSQL_SA_PASSWORD=$password" -p "127.0.0.1:${Port}:1433" mcr.microsoft.com/mssql/server:2022-latest | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start isolated SQL Server.' }
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        docker exec $container sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -I -S localhost -U sa -C -b -Q "SELECT 1"' 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'SQL Server did not become ready.' }
    $env:STUDENT_TEST_SQL = "Server=127.0.0.1,$Port;Database=master;User ID=sa;Password=$password;Encrypt=True;TrustServerCertificate=True"
    dotnet build --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    dotnet test --no-build --nologo --logger trx
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
finally {
    $env:STUDENT_TEST_SQL = $previousConnection
    docker rm --force $container 2>$null | Out-Null
    Pop-Location
}
