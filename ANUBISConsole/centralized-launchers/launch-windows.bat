@echo off
if exist C:\anubis\ANUBISConsole.exe (
	echo Found ANUBISConsole on this system, trying to launch windows launcher now...

  if exist C:\anubis\launchers\launch-windows.bat (
		echo Launching ANUBIS windows launcher now...
		C:\anubis\launchers\launch-windows.bat
		exit 0
	else (
		echo ANUBIS windows launcher was not found on this system, install newest version with update-windows.bat
		exit 2
  )
) else (
	echo ANUBIS was not found on this system, install it with update-windows.bat
	exit 1
)
