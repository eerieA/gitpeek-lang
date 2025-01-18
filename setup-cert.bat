@echo off
setlocal EnableDelayedExpansion

echo Please enter a password for your development certificate:
set /p CERT_PASSWORD="Password: "

rem Validate password is not empty
if "!CERT_PASSWORD!"=="" (
    echo Password cannot be empty!
    exit /b 1
)

echo.
echo Generating development certificate...
dotnet dev-certs https --clean
dotnet dev-certs https -ep "./aspnetapp.pfx" -p !CERT_PASSWORD!
dotnet dev-certs https --trust

rem Check if certificate was created
if exist ".\aspnetapp.pfx" (
    echo.
    echo Certificate generated successfully!
    echo.
    echo You can now run the application with:
    (
        echo docker run -p 8080:80 -p 8443:443
        echo   -e ASPNETCORE_Kestrel__Certificates__Default__Password="!CERT_PASSWORD!"
        echo   -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
        echo   -v "%CD%:/https/"
        echo   gitpeek-lang
    )
    echo.
    echo Please save this command for future use - you'll need the same password.
) else (
    echo Failed to generate certificate!
    exit /b 1
)