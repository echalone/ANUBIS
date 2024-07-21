#!/bin/bash

echo "ANUBIS startup procedure initiated..."
echo "Enter root password if prompted to do so! (some actions need sudo privileges)"
echo "------------------------------"

echo "starting time-sync and resyncing..."
sudo timedatectl set-ntp true
sudo timedatectl
echo "...time-sync started and resynced"

echo "------------------------------"

echo "remounting any cifs folders..."
sudo mount -av
echo "...done remounting any cifs folders"

echo "------------------------------"

echo "making ANUBISConsole and USBswitchCmd executable..."
curdir=$(dirname $0)
sudo chmod a+x "${curdir}/../ANUBISConsole"
sudo chmod a+x "${curdir}/../USBswitchCmd"
echo "...done making ANUBISConsole and USBswitchCmd executable"

echo "------------------------------"

echo "launching ANUBISConsole..."
cd "${curdir}/.."
sudo ./ANUBISConsole
