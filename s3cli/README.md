# Amazon S3 Command Line - .NET 8 Port

This is a .NET 8 conversion of [cdc1979/Amazon-S3-Command-Line](https://github.com/cdc1979/Amazon-S3-Command-Line).

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

## Build

```bash
cd s3cli
dotnet build
dotnet run -- -A LIST -Ac YOUR_KEY -S YOUR_SECRET -B your-bucket
```

## Publish as a single self-contained executable

### Windows
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

### macOS
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

Output will be in `bin/Release/net8.0/<rid>/publish/`.

## Usage

```
s3cli -A <action> [options]

Actions: PUTFILE, PUTFOLDER, GET, DELETE, DELETEBUCKET, CREATEBUCKET, LIST, USECONFIG, BUCKETSIZE

Options:
  -Ac  Your Amazon Access Key
  -S   Your Amazon Secret Key
  -B   Bucket name
  -F   Local file or folder path
  -Fi  Remote object name
  -St  Storage class: STANDARD, STANDARDIA, REDUCEDREDUNDANCY, GLACIER
  -C   Config file path (JSON)
  -D   Date-stamp the uploaded filename
  -P   Proxy address
  -Pr  Proxy port
  -Z   Zip before uploading
  -Zi  Zip speed: SLOW, DEFAULT, FAST
  -Pe  Persist credentials for future use
```

## Migration Notes (from .NET 4.7.2)

| Area | Original | .NET 8 Port |
|---|---|---|
| Target framework | `net472` | `net8.0` |
| AWS SDK | `AWSSDK` 2.3.x (monolithic) | `AWSSDK.S3` 3.7.x (modular) |
| AWS API style | Synchronous | Async (.GetAwaiter().GetResult() wrappers) |
| Credential storage (Windows) | Windows Registry `HKCU\s3cli` | Windows Registry (unchanged via `Microsoft.Win32.Registry` NuGet) |
| Credential storage (Linux/macOS) | Not supported | `~/.s3cli_credentials.json` (chmod 600) |
| Embedded DLL trick | AppDomain.AssemblyResolve | Not needed — use `PublishSingleFile` instead |
| Project format | Old-style `.csproj` + `packages.config` | SDK-style `.csproj` with `<PackageReference>` |
| Installer | WiX `.wixproj` | Removed — use `dotnet publish` |
| `System.Web` | Referenced | Removed (not available on .NET Core) |

## Cross-platform

The tool now works on Windows, Linux, and macOS.
On non-Windows platforms, credentials saved with `-Pe` are stored in `~/.s3cli_credentials.json`.
