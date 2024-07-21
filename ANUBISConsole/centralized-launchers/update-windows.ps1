try {
  $AnubisReleaseMachine = "babylon"
  $AnubisReleaseShare = "ANUBISReleases"
  $AnubisReleaseVersion = "ANUBIS_Console_WinOS"
  $SharePath = "\\$AnubisReleaseMachine\$AnubisReleaseShare\$AnubisReleaseVersion\"

  Write-Host "Looking for remote update location..."

  if (Test-Path -Path "$($SharePath)ANUBISConsole.exe") {
    Write-Host  "Found ANUBIS at update location"
  
    Write-Host "Updating ANUBIS installation..."
  
    if (Test-Path -Path "C:\anubis") {
      Write-Host "Removing current ANUBIS installation..."
      Remove-Item -Path C:\anubis -Recurse -Force -ErrorAction Stop
    }
  
    Write-Host "Creating anubis folder..."
    New-Item -ItemType Directory -Path C:\ -Name anubis -ErrorAction Stop | Out-Null

    Write-Host "Copying anubis files..."
    Copy-Item -Path "$($SharePath)*" -Destination "C:\anubis" -Recurse -Force -ErrorAction Stop
    
    Write-Host "ANUBIS has been updated"
    exit 0
  }
  else {
    Write-Host "Didn't find ANUBIS at update location, not updating!"
    Write-Host "The ""$AnubisReleaseVersion"" directory containing the windows version must be present in the ""$AnubisReleaseShare"" ANUBIS release share of the ""$AnubisReleaseMachine"" ANUBIS release machine"
    exit 1
  }    
}
catch {
  Write-Error "While trying to launch ANUBIS, $($_.Exception.GetType().FullName): $($_.Exception.Message)"   
}
