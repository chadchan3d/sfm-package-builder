# SFM Package Builder 1.0.4 Source Build

This repository contains the uncompiled source for SFM Package Builder 1.0.4.

## Requirements

- Windows 10 or Windows 11, x64.
- .NET SDK 10.0.400 or a compatible .NET 10 SDK.
- PowerShell 7 or Windows PowerShell.
- Internet access for the first `dotnet restore`, unless the required NuGet packages and .NET runtime packs are already available in the local NuGet cache.

The application targets:

- Core library: `net10.0`
- WinForms application: `net10.0-windows`
- Release runtime identifier: `win-x64`
- Release deployment mode: self-contained

No external 7-Zip installation is required. ZIP creation is implemented with .NET and PowerShell is used only to assemble the release archive.

## Dependencies

NuGet dependencies are declared in the test project files:

- `tests/SfmPackageBuilder.Core.Tests/SfmPackageBuilder.Core.Tests.csproj`
- `tests/SfmPackageBuilder.IntegrationTests/SfmPackageBuilder.IntegrationTests.csproj`
- `tests/SfmPackageBuilder.WinForms.Tests/SfmPackageBuilder.WinForms.Tests.csproj`

The test projects use MSTest:

- `Microsoft.NET.Test.Sdk` 17.14.1
- `MSTest.TestAdapter` 3.10.2
- `MSTest.TestFramework` 3.10.2

The application projects do not reference third-party NuGet packages directly.

## Verify Source Version

From the repository root:

```powershell
Select-String -Path .\src\SfmPackageBuilder.WinForms\SfmPackageBuilder.WinForms.csproj -Pattern '<Version>1.0.4</Version>'
Select-String -Path .\src\SfmPackageBuilder.Core\SfmPackageBuilder.Core.csproj -Pattern '<Version>1.0.4</Version>'
Select-String -Path .\src\SfmPackageBuilder.WinForms\app.manifest -Pattern 'version="1.0.4.0"'
Select-String -Path .\outputs\release-assets\README.txt -Pattern 'SFM PACKAGE BUILDER 1.0.4'
```

## Build And Test

These commands use an isolated artifacts directory so local `bin`/`obj` state is not required.

```powershell
dotnet restore .\SfmPackageBuilder.sln -p:NuGetAudit=false --artifacts-path .\work\artifacts-test-1.0.4
dotnet test .\SfmPackageBuilder.sln -c Release --no-restore --artifacts-path .\work\artifacts-test-1.0.4
dotnet build .\SfmPackageBuilder.sln -c Release --no-restore --artifacts-path .\work\artifacts-test-1.0.4
```

Expected test projects:

- `SfmPackageBuilder.Core.Tests`
- `SfmPackageBuilder.IntegrationTests`
- `SfmPackageBuilder.WinForms.Tests`

Some integration tests may be skipped when external Source Filmmaker fixture paths are not available on the build machine.

To run the optional installed-SFM acceptance fixture, set this environment variable to a local `SourceFilmmaker\game` directory before running tests:

```powershell
$env:SFM_PACKAGE_BUILDER_ACCEPTANCE_SFM_GAME_ROOT = "D:\SteamLibrary\steamapps\common\SourceFilmmaker\game"
```

## Publish Windows x64

From the repository root:

```powershell
dotnet restore .\src\SfmPackageBuilder.WinForms\SfmPackageBuilder.WinForms.csproj -r win-x64 -p:NuGetAudit=false --artifacts-path .\work\artifacts-publish-1.0.4

dotnet publish .\src\SfmPackageBuilder.WinForms\SfmPackageBuilder.WinForms.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  --no-restore `
  --artifacts-path .\work\artifacts-publish-1.0.4 `
  -o .\outputs\publish-1.0.4 `
  -p:DebugType=None `
  -p:DebugSymbols=false
```

The published application files will be in:

```text
outputs\publish-1.0.4
```

## Create The Windows x64 Distribution ZIP

From the repository root, after publishing:

```powershell
$distributionRoot = ".\outputs\distribution-1.0.4"
$appFolder = Join-Path $distributionRoot "SFM Package Builder"
$zipPath = Join-Path $distributionRoot "SFM-Package-Builder-v1.0.4-win-x64.zip"

New-Item -ItemType Directory -Path $appFolder -Force | Out-Null
Copy-Item -Path ".\outputs\publish-1.0.4\*" -Destination $appFolder -Recurse -Force
Copy-Item -Path ".\outputs\release-assets\licenses" -Destination (Join-Path $appFolder "licenses") -Recurse -Force
Copy-Item -Path ".\outputs\release-assets\README.txt" -Destination (Join-Path $distributionRoot "README.txt") -Force

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive `
  -Path (Join-Path $distributionRoot "README.txt"), $appFolder `
  -DestinationPath $zipPath `
  -CompressionLevel Optimal

Get-FileHash -Algorithm SHA256 $zipPath |
  ForEach-Object { "$($_.Hash)  $(Split-Path $_.Path -Leaf)" } |
  Set-Content -Path (Join-Path $distributionRoot "SFM-Package-Builder-v1.0.4-win-x64.sha256.txt") -Encoding ASCII
```

The resulting distributable is:

```text
outputs\distribution-1.0.4\SFM-Package-Builder-v1.0.4-win-x64.zip
```

The ZIP layout should contain:

```text
README.txt
SFM Package Builder\SfmPackageBuilder.exe
SFM Package Builder\licenses\...
```

## Reproducibility Notes

- The build is not code-signed. Windows may show an Unknown Publisher warning.
- Exact ZIP bytes may differ between machines because PowerShell ZIP creation records file metadata such as timestamps and uses implementation-defined compression details.
- .NET self-contained publish output can vary if a different .NET 10 SDK/runtime-pack patch version is used. Use .NET SDK 10.0.400 to match the release environment used for the 1.0.4 source package verification.
- No manual source-code edits are required to build or publish.
