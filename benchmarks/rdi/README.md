# RDI benchmark

Python scripts to benchmark RDI against PNG and JPEG on multiple image datasets. Results are written as CSV files.

## Procedure

- Convert each input image to a temporary BMP
- Encode with each codec candidate
- Decode to BMP
- Compute metrics
  - PSNR, PSNR_Y, SSIM, MS-SSIM, MSE, MAE
  - LPIPS runs only if `torch` and `lpips` are installed
- Write one CSV row per image per codec

## Output files

- `results/<dataset>.samples.csv`, one row per image per codec
- `results/<dataset>.csv`, summary stats per codec per metric: mean, std, min, max
- Intermediate files are written under `temporaries/`

## Directory layout

```
benchmarks/rdi/
    benchmark.py
    rdi_codecs.py
    rdi_metrics.py
    datasets/
        dataset1/
            img1.png
            img2.jpg
        dataset2/
        ...
    tools/
        rdc[.exe]
        ffmpeg[.exe]  # optional if ffmpeg is already in PATH
    results/
    temporaries/
```

## Requirements

- Python 3.9+
- `rdc` CLI tool available in `tools/`
- `ffmpeg` available in `tools/` or installed system-wide

## Install

Create a virtual environment and install Python dependencies.

### Windows

```bash
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
```

### Linux/macOS

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

## Run

From the directory shown above:

```bash
python benchmark.py .  # Or pass an explicit /path/to/bench
```

## License

Copyright &copy; 2025 Anar Bastanov <br>
See root [README.md](../../README.md#license).
