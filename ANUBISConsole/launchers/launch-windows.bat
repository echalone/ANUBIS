echo Starting ANUBIS Console in Windows Terminal (make sure this path doesn't have any spaces in it and that this script is run as administrator)...

echo Starting windows time service (needs admin privileges)...
net start w32time

echo Resyncing time (needs admin privileges)...
w32tm /resync /rediscover

echo Starting console logging output in Windows Terminal...
wt -p PowerShell -d %~dp0../scripts pwsh Show-ANUBISLogMessages.ps1

echo Waiting 5 seconds for the console logging output to be ready for first logging output
timeout 5

echo Starting ANUBIS Console in Windows Terminal...
wt --fullscreen -p PowerShell -d %~dp0.. %~dp0../ANUBISConsole.exe

echo All done, exiting startup script now
exit
