dotnet build -c Release

$OutDir = "./package/SolastaSpellManager"
$BuildDir = "./SolastaSpellManager/bin/Release/net452"

New-Item -Force $OutDir

Copy-Item -Force "${BuildDir}/SolastaSpellManager.dll" $OutDir
Copy-Item -Force ./Info.json $OutDir

Compress-Archive -Force -Path $OutDir -DestinationPath SolastaSpellManager.zip 

Remove-Item -Recurse package
