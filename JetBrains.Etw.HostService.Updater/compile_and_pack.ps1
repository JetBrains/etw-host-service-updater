if ($PSVersionTable.PSVersion.Major -lt 3) {
	throw "PS Version $($PSVersionTable.PSVersion) is below 3.0."
}

Set-StrictMode -Version Latest
$ErrorActionPreference=[System.Management.Automation.ActionPreference]::Stop


[xml]$_Project=Get-Content JetBrains.Etw.HostService.Updater.csproj
$_Framework="net461"
$_PackageVersion=$_Project.Project.PropertyGroup.Version

Write-Host "Frameworks:" $_Framework
Write-Host "PackageVersion:" $_PackageVersion

function pack($_Runtime) {
  $_File='<?xml version="1.0" encoding="utf-8"?>
<package>
  <metadata>
    <id>JetBrains.Etw.HostService.Updater.' + $_Runtime + '</id>
    <version>' + $_PackageVersion + '</version>
    <title>JetBrains Etw Host Service Updater</title>
    <authors>Mikhail Pilin</authors>
    <copyright>Copyright © ' + $(get-date -Format yyyy) + ' JetBrains s.r.o.</copyright>
    <projectUrl>https://github.com/JetBrains/etw-host-service-updater</projectUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <license type="expression">Apache-2.0</license>
    <description>JetBrains Etw Host Service Updater</description>
  </metadata>
  <files>
    <file src="bin\Release\' + $_Framework + '\' + $_Runtime + '\publish\**\*" target="tools\' + $_Runtime + '" />
  </files>
</package>'

  $_NuSpec="Package.$_Runtime.nuspec"
  Out-File -InputObject $_File -Encoding utf8 $_NuSpec
  nuget pack $_NuSpec
}

function compileAndPack($_Runtime) {
  dotnet publish -f $_Framework -r $_Runtime -c Release --self-contained true
  pack $_Runtime
}

compileAndPack "win-x64"    
compileAndPack "win-x86"    
