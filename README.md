# Root Delta Codec

**`RDC`** is a lossy media codec framework designed to significantly reduce file sizes while maintaining high perceptual quality. It uses efficient compression algorithms and a unified approach to support various types of content.

The framework defines an extensible family of formats including:

- **`RDI`** – Root Delta Image
- **`RDA`** – Root Delta Audio
- **`RDV`** – Root Delta Video
- **`RDAI`** – Root Delta Animated Image

## Status

> [!WARNING]
> *This project is under active development and not ready for general use.*

## Installation & Build Instructions

You can build RDC from source or download prebuilt binaries from the latest [GitHub Releases](https://github.com/anar-bastanov/root-delta-codec/releases).

To build RDC from source, ensure you have the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed.

Run the following commands from the root of the repository to restore dependencies and publish the project:

### Windows

```bash
dotnet restore
dotnet publish ./RootDeltaCodec.sln -c Release -r win-x64 --self-contained true /p:PublishAot=true /p:PublishTrimmed=true /p:TrimMode=link
```

### Linux

```bash
dotnet restore
dotnet publish ./RootDeltaCodec.sln -c Release -r linux-x64 --self-contained true /p:PublishAot=true /p:PublishTrimmed=true /p:TrimMode=link
```

### Output

After publishing, the executable will be located at:

- `./bin/Release/net9.0/win-x64/publish/rdc.exe` (Windows)
- `./bin/Release/net9.0/linux-x64/publish/rdc` (Linux)

## Usage

RDC is available both as a CLI tool and as a .NET library for programmatic use.

### Command-Line Interface

```bash
rdc encode input.bmp output.rdi
rdc decode input.rdi output.bmp

rdc encode -w -f bmp:rdi input output
rdc decode -w -f :bmp input.rdi output
```

Options:

* `-f`, `--format`: Input and output media formats as FROM:TO
* `-m`, `--mode`: Set encoding mode
* `-w`, `--overwrite`: Allow overwriting the output file if it exists
* `-h`, `--help`: Show help and usage information

### .NET Library

The core encoding/decoding logic lives in `RdcEngine`, while the CLI front-end uses `RdcCli`.

```cs
using RdcEngine.Image;

{
    using var inputBmp = File.OpenRead("input.bmp");
    using var outputRdi = File.Create("output.rdi");
    ImageCodec.EncodeBmp(inputBmp, outputRdi);
}

{
    using var inputRdi = File.OpenRead("input.rdi");
    using var outputBmp = File.Create("output.bmp");
    ImageCodec.DecodeBmp(inputRdi, outputBmp);
}
```

## Supported Formats

| From            | To                   | Status    |
| :-------------- | :------------------- | :-------- |
| `BMP`           | `RDI`                | Supported |
| `RDI`           | `BMP`                | Supported |
| `PNG`, `JPEG`   | `RDI`                | Not Yet   |
| `RDI`           | `PNG`, `JPEG`        | Not Yet   |
| Video/Audio/GIF | `RDV`, `RDA`, `RDAI` | Planned   |

## License

Copyright © 2025 Anar Bastanov  
Distributed under the [MIT License](http://www.opensource.org/licenses/mit-license.php).
