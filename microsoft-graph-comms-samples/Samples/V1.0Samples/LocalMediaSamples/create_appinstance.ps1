<#

.SYNOPSIS
Create an Application Instance.

.DESCRIPTION
This script performs a couple tasks to create an Application Instance.
- Imports SkypeOnlineConnector.
- Log into Azure.
- Creates Application Instance.

.PARAMETER UserPrincipalName
Enter your User Principal Name.

.PARAMETER DisplayName
Enter your Bot's Display name (can be obtained from the Azure Portal).

.PARAMETER BotAppId
Enter your Bot's Application Id (can be obtained from the Azure Portal).


#>

param(
    [parameter(Mandatory=$false,HelpMessage="Enter your User Principal Name.")][alias("u")] $UserPrincipalName,
    [parameter(Mandatory=$false,HelpMessage="Enter your Bot's Display name (can be obtained from the Azure Portal).")][alias("name")] $DisplayName,
    [parameter(Mandatory=$false,HelpMessage="Enter your Bot's Application Id (can be obtained from the Azure Portal).")][alias("id")] $BotAppId,
)

Write-Output 'Policy Recording Bot - Creating an Application Instance'


if (-not $UserPrincipalName) {
    $UserPrincipalName = (Read-Host 'Enter your User Principal Name.').Trim()
}

if (-not $DisplayName) {
    $DisplayName = (Read-Host 'Enter your Bot'' Display name (can be obtained from the Azure Portal).').Trim()
}

if (-not $BotAppId) {
    $BotAppId = (Read-Host 'Enter your Bot''s Application Id (can be obtained from the Azure Portal).').Trim()
}


Write-Output "Log in to Azure..."
Import-Module SkypeOnlineConnector
$userCredential = Get-Credential
$sfbSession = New-CsOnlineSession -Credential $userCredential -Verbose
Import-PSSession $sfbSession

Write-Output "Creating an Application Instance..."
$results = New-CsOnlineApplicationInstance -UserPrincipalName $UserPrincipalName -DisplayName $DisplayName -ApplicationId $BotAppId
foreach ($name in $results) {
    if ( $name.name_with_namespace -match ".*object.*" ) {    
        write-host "$($name.name_with_namespace) - $($name.id)"               
    }   
}

#Sync-CsOnlineApplicationInstance -ObjectId <objectId>

Write-Output "Update Complete."
