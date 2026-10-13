# Builds/runs using the .NET SDK installed in .\.dotnet, keeping all SDK, NuGet and temp data inside this folder.
#   .\build.ps1            -> build
#   .\build.ps1 run        -> build and run
#   .\build.ps1 publish    -> self-contained release build in .\publish
param([ValidateSet('build', 'run', 'publish')][string]$Action = 'build')

$root = $PSScriptRoot
$env:DOTNET_ROOT = "$root\.dotnet"
$env:DOTNET_CLI_HOME = "$root\.dotnet-home"
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
$env:NUGET_PACKAGES = "$root\.nuget\packages"
$env:NUGET_HTTP_CACHE_PATH = "$root\.nuget\http-cache"
$env:NUGET_PLUGINS_CACHE_PATH = "$root\.nuget\plugins-cache"
$env:TEMP = "$root\.tmp"
$env:TMP = "$root\.tmp"
New-Item -ItemType Directory -Force $env:DOTNET_CLI_HOME, $env:TEMP | Out-Null

$dotnet = "$root\.dotnet\dotnet.exe"
$project = "$root\src\LockScreenLive\LockScreenLive.csproj"

switch ($Action) {
    'build'   { & $dotnet build $project -c Debug }
    'run'     { & $dotnet run --project $project -c Debug }
    'publish' { & $dotnet publish $project -c Release -r win-x64 --self-contained -o "$root\publish" }
}
exit $LASTEXITCODE
