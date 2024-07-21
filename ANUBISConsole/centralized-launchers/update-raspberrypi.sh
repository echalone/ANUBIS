#!/bin/bash

sharesRoot="/media/shares"
anubisReleaseMachine="anubis-release-machine"
anubisReleaseShare="ANUBISReleases"
anubisReleaseVersion="ANUBIS_Console_RPiOS"
shareLocation="${sharesRoot}/${anubisReleaseMachine}/${anubisReleaseShare}/"
fullShareLocation="${shareLocation}${anubisReleaseVersion}/"

echo "Looking for remote update location..."
if [ -f "${fullShareLocation}ANUBISConsole" ]; then
  echo "Found ANUBIS at update location"

  echo "Updating ANUBIS installation, enter sudo password if prompted to do so..."

  if [ -d /usr/local/anubis ]; then
    echo "Removing current ANUBIS installation..."
    sudo rm -rf /usr/local/anubis
  fi

  echo "Creating anubis folder..."
  sudo mkdir -p /usr/local/anubis

  echo "Copying anubis files..."
  sudo cp -R "${fullShareLocation}/." /usr/local/anubis
  sudo chmod a+x /usr/local/anubis/scripts/*.sh
  sudo chmod a+x /usr/local/anubis/launchers/*.sh
  sudo chmod a+x /usr/local/anubis/centralized-launchers/*.sh

  echo "ANUBIS has been updated"
  exit 0
else
  echo "Didn't find ANUBIS at update location, not updating!"
  echo "The '${anubisReleaseVersion}' directory containing the Raspberry Pi version must be present in the '${anubisReleaseShare}' ANUBIS release share of the '${anubisReleaseMachine}' ANUBIS release machine. That share must be mounted at '${shareLocation}'"
  exit 1
fi

