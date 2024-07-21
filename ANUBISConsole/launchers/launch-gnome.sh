#!/bin/bash

echo "starting ANUBIS Console in gnome..."
curdir=$(dirname $0)
echo "Making linux startup script executable..."
sudo chmod a+x "${curdir}/../scripts/start-linux.sh"

echo "Starting console logging output in gnome terminal..."
gnome-terminal --hide-menubar -- /usr/local/powershell/pwsh -Command ""${curdir}/../scripts/Show-ANUBISLogMessages.ps1""

echo "Waiting 5 seconds for the console logging output to be ready for first logging output"
sleep 5

echo "Starting ANUBIS Console in gnome terminal..."
gnome-terminal --hide-menubar --full-screen -- "${curdir}/../scripts/start-linux.sh"

echo "All done, exiting startup script now"
exit
