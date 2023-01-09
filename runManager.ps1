Function Test-CommandExists
{

 Param ($command)

 $oldPreference = $ErrorActionPreference

 $ErrorActionPreference = ‘stop’

 try {if(Get-Command $command){RETURN $true}}

 Catch {Write-Host “$command does not exist”; RETURN $false}

 Finally {$ErrorActionPreference=$oldPreference}

} #end function test-CommandExists

#Check Tools
If(-not(Test-CommandExists python3))
 {
  Write-Host "You have to install python3"
   return
 }

If(-not(Test-CommandExists dotnet))
 {
  Write-Host "You have to install dotnet"
   return
 }

 #Download data from git
 git pull

#Build and Run
cd $PSScriptRoot/CertificatesGenerator
dotnet build ConsoleManager --configuration Release

dotnet ./ConsoleManager/bin/Release/net6.0/ConsoleManager.dll

