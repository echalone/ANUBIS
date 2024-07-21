echo "Removing previous compilations..."
rmdir "%~dp0release" /S /Q

echo "Compiling for windows..."
dotnet publish "%~dp0ANUBISConsole\ANUBISConsole.csproj" --configuration release --runtime win-x64 --self-contained --output "%~dp0release\ANUBIS_Console_WinOS" -p:Platform="Any CPU"

echo "Compiling for linux..."
dotnet publish "%~dp0ANUBISConsole\ANUBISConsole.csproj" --configuration release --runtime linux-arm64 --self-contained --output "%~dp0release\ANUBIS_Console_RPiOS" -p:Platform=ARM64

echo "Archiving for windows..."
7z a -tzip "%~dp0release\ANUBIS_Console_WinOS.zip" "%~dp0release\ANUBIS_Console_WinOS"

echo "Archiving for linux..."
7z a -tzip "%~dp0release\ANUBIS_Console_RPiOS.zip" "%~dp0release\ANUBIS_Console_RPiOS"

echo ""
echo "done"
pause