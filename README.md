# Root Delta Codec

`RDC` is a lossy media codec framework designed to significantly reduce file sizes while maintaining high perceptual quality. It uses efficient compression algorithms and a unified approach to support various types of content.

The framework defines a family of formats including:

- `RDI` – Root Delta Image
- `RDA` – Root Delta Audio
- `RDV` – Root Delta Video
- `RDAI` – Root Delta Animated Image

> [!WARNING]
> This project is under active development and not ready for general use.

## Installation & Build Instructions

You can build `RDC` from source or download prebuilt binaries from the latest [GitHub Releases](https://github.com/anar-bastanov/root-delta-codec/releases).

To build `RDC` from source, ensure you have the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.

Run the following commands from the root of the repository to restore dependencies and publish the project:

### Windows

```bash
dotnet publish ./RdcCli/RdcCli.csproj -c Release -r win-x64
```

### Linux

```bash
dotnet publish ./RdcCli/RdcCli.csproj -c Release -r linux-x64
```

### Output

After publishing, the executable will be located at:

- `./bin/Release/net10.0/win-x64/publish/rdc.exe` (Windows)
- `./bin/Release/net10.0/linux-x64/publish/rdc` (Linux)

## Usage

`RDC` is available both as a CLI tool and as a .NET library for programmatic use.

### Command-Line Interface

```bash
rdc encode input.bmp output.rdi
rdc decode input.rdi output.bmp

rdc encode -w -f bmp:rdi input output
rdc decode -w -f :bmp input.rdi output
```

Options:

* `-f`, `--format`: Input and output media formats as `FROM:TO`
* `-m`, `--mode`: Set encoding mode
* `-w`, `--overwrite`: Allow overwriting the output file if it exists
* `-h`, `--help`: Show help and usage information
* `-v`, `--version`: Show tool version

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

> [!TIP]
> You can use tools like `ffmpeg` or other image converters to transform unsupported file types (e.g., `PNG` to `BMP`) before encoding, or back again (`BMP` to `PNG`) after decoding.

## License

Copyright &copy; 2025 Anar Bastanov <br>
Source code is distributed under the [MIT License](LICENSE-MIT.txt). <br>
Specifications and other documentation are licensed under [CC BY 4.0](LICENSE-CC-BY-4.0.txt).
