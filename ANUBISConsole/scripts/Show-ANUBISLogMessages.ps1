# Shows you the console messages for ANUBIS in powershell core

Param
(
    [Parameter(Position=0)]
    [string] $PathToLogFile=$null,
    [Parameter(Position=1)]
    [ConsoleColor] $VerboseColor = [ConsoleColor]::DarkGray,
    [Parameter(Position=2)]
    [ConsoleColor] $DebugColor = [ConsoleColor]::Gray,
    [Parameter(Position=3)]
    [ConsoleColor] $InfoColor = [ConsoleColor]::White,
    [Parameter(Position=4)]
    [ConsoleColor] $WarningColor = [ConsoleColor]::Yellow,
    [Parameter(Position=5)]
    [ConsoleColor] $ErrorColor = [ConsoleColor]::DarkYellow,
    [Parameter(Position=6)]
    [ConsoleColor] $FatalColor = [ConsoleColor]::Red,
    [Parameter(Position=7)]
    [switch] $NoClearScreen,
    [Parameter(Position=8)]
    [switch] $IgnoreEmptyLines
)

$const_rx_Verbose = "^\d\d:\d\d:\d\d \[(VRB)\] "
$const_rx_Debug = "^\d\d:\d\d:\d\d \[(DBG)\] "
$const_rx_Info = "^\d\d:\d\d:\d\d \[(INF)\] "
$const_rx_Warning = "^\d\d:\d\d:\d\d \[(WRN)\] "
$const_rx_Error = "^\d\d:\d\d:\d\d \[(ERR)\] "
$const_rx_Fatal = "^\d\d:\d\d:\d\d \[(FTL)\] "

function Write-ANUBISLogMessage {
<#
    .SYNOPSIS
        Writes the log message to the console in color
    .PARAMETER Message
        The log message to write out
#>

    [CmdletBinding(SupportsShouldProcess=$true)]
    PARAM (
        [Parameter(Position=0, ValueFromPipeline=$true)]
        [string] $Message,
        [Parameter(Position=1)]
        [switch] $IgnoreEmptyLines
    )

    if(![string]::IsNullOrWhiteSpace($Message)) {
        if ($Message -match $const_rx_Fatal) {
            Write-Host -Message $Message -ForegroundColor $FatalColor
        }
        elseIf ($Message -match $const_rx_Error) {
            Write-Host -Message $Message -ForegroundColor $ErrorColor
        }
        elseIf ($Message -match $const_rx_Warning) {
            Write-Host -Message $Message -ForegroundColor $WarningColor
        }
        elseIf ($Message -match $const_rx_Info) {
            Write-Host -Message $Message -ForegroundColor $InfoColor
        }
        elseIf ($Message -match $const_rx_Debug) {
            Write-Host -Message $Message -ForegroundColor $DebugColor
        }
        elseIf ($Message -match $const_rx_Verbose) {
            Write-Host -Message $Message -ForegroundColor $VerboseColor
        }
        else {
            Write-Host -Message $Message
        }
    }
    else {
        if(!$IgnoreEmptyLines.IsPresent) {
            Write-Host ""
        }
    }
}

if(!$NoClearScreen.IsPresent) {
    Clear-Host
}

if([string]::IsNullOrWhiteSpace($PathToLogFile)) {
    $PathToLogFile = Join-Path -Path $PSScriptRoot -ChildPath ".." -AdditionalChildPath Logs, console.log
}

while (!(Test-Path -Path $PathToLogFile)) { }
Get-Content -Path $PathToLogFile -Tail 0 -Wait | ForEach-Object { $_ | Write-ANUBISLogMessage -IgnoreEmptyLines:$IgnoreEmptyLines }

