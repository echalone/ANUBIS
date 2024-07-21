#!/bin/bash

if [ -f /usr/local/anubis/ANUBISConsole ]; then
	echo "Found ANUBISConsole on this system, trying to launch gnome launcher now..."

	if [ -f /usr/local/anubis/launchers/launch-gnome.sh ]; then
		echo "Making gnome launcher executable and launching with sudo privileges. Enter sudo password if prompted."
		sudo chmod a+x /usr/local/anubis/launchers/launch-gnome.sh

		echo "Launching ANUBIS gnome launcher now..."
		/usr/local/anubis/launchers/launch-gnome.sh
		exit 0
	else
		echo "ANUBIS gnome launcher was not found on this system, install newest version with update-raspberrypi.sh"
		exit 2
	fi
else
	echo "ANUBIS was not found on this system, install it with update-raspberrypi.sh"
	exit 1
fi
