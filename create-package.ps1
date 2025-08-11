rd ./package -Force -Recurse | Out-Null
md ./package | Out-Null
dotnet build -c Release .\src\HarmonyPatchChangeParser.csproj -o ./package

Compress-Archive ./package/* HarmonyPatchChangeParser.zip  -Force
