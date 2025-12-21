#!/usr/bin/env python3

import argparse
import csv
import math
import sys
import numpy as np
from collections import defaultdict
from pathlib import Path
from PIL import Image

from rdi_metrics import METRICS
from rdi_codecs import (
    ensure_dir,
    file_bpp,
    find_images,
    make_jpeg_candidate,
    make_png_candidate,
    make_rdi_candidate,
    rel_to_safe,
)


def process_dataset(dataset_dir: Path, tools_root: Path, temporaries_root: Path, results_root: Path, candidates: list):
    dataset_name = dataset_dir.name
    dataset_temp = temporaries_root / dataset_name
    ensure_dir(dataset_temp)
    ensure_dir(results_root)

    samples_csv = results_root / f"{dataset_name}.samples.csv"
    summary_csv = results_root / f"{dataset_name}.csv"
    images = sorted(find_images(dataset_dir))

    print(f"\n=== Processing dataset: {dataset_name} ===", file=sys.stderr)

    if not images:
        print(f"[WARN] No images found in dataset {dataset_name}", file=sys.stderr)
        return

    metrics_per_codec = defaultdict(list)
    sample_fieldnames = ["dataset", "image", "codec", "encoded_bytes", "bpp"] + list(METRICS.keys())

    with samples_csv.open("w", newline="") as f_samples:
        sample_writer = csv.DictWriter(f_samples, fieldnames=sample_fieldnames)
        sample_writer.writeheader()

        for img_path in images:
            rel = img_path.relative_to(dataset_dir)
            safe_base = rel_to_safe(rel)
            orig_img = Image.open(img_path).convert("RGB")
            w, h = orig_img.size
            src_bmp = dataset_temp / "_src_bmp" / f"{safe_base}.bmp"

            ensure_dir(src_bmp.parent)
            orig_img.save(src_bmp)

            for cand in candidates:
                enc_path = dataset_temp / cand.name / "enc" / f"{safe_base}.{cand.extension}"
                dec_path = dataset_temp / cand.name / "dec" / f"{safe_base}.bmp"

                try:
                    cand.encode_fn(tools_root, src_bmp, enc_path)
                    cand.decode_fn(tools_root, enc_path, dec_path)
                except Exception as e:
                    sys.stderr.write(f"[ERROR] {dataset_name}/{rel}: candidate {cand.name} failed: {e}\n")
                    continue

                try:
                    recon_img = Image.open(dec_path).convert("RGB")
                except Exception as e:
                    sys.stderr.write(f"[ERROR] {dataset_name}/{rel}: candidate {cand.name} decode load failed: {e}\n")
                    continue

                row = {
                    "dataset": dataset_name,
                    "image": str(rel),
                    "codec": cand.name,
                }

                encoded_bytes = enc_path.stat().st_size
                bpp = file_bpp(enc_path, w, h)
                row["encoded_bytes"] = encoded_bytes
                row["bpp"] = bpp

                metrics_for_this = {}
                for mname, mfn in METRICS.items():
                    try:
                        val = mfn(orig_img, recon_img)
                    except Exception as e:
                        sys.stderr.write(
                            f"[ERROR] metric {mname} failed on {dataset_name}/{rel} for {cand.name}: {e}\n"
                        )
                        val = float("nan")
                    row[mname] = val
                    metrics_for_this[mname] = val

                sample_writer.writerow(row)
                metrics_per_codec[cand.name].append({"bpp": bpp, **metrics_for_this})

    print(f"[OK] Wrote samples for dataset {dataset_name} to {samples_csv}", file=sys.stderr)

    metric_names = ["bpp"] + list(METRICS.keys())
    summary_fieldnames = ["dataset", "codec", "metric", "mean", "std", "min", "max"]

    with summary_csv.open("w", newline="") as f_summary:
        summary_writer = csv.DictWriter(f_summary, fieldnames=summary_fieldnames)
        summary_writer.writeheader()

        arrays_per_codec = {}

        for cand in candidates:
            name = cand.name
            values = metrics_per_codec.get(name, [])
            if not values:
                continue

            arrs = {}
            arrs["bpp"] = np.array([v["bpp"] for v in values], dtype=np.float64)
            for m in METRICS.keys():
                arrs[m] = np.array([v[m] for v in values], dtype=np.float64)

            arrays_per_codec[name] = arrs

        def stats(arr):
            arr = arr[~np.isnan(arr)]
            if arr.size == 0:
                return (math.nan, math.nan, math.nan, math.nan)
            return (float(np.mean(arr)), float(np.std(arr)), float(np.min(arr)), float(np.max(arr)))

        for metric_name in metric_names:
            summary_writer.writerow({})

            for cand in candidates:
                name = cand.name
                if name not in arrays_per_codec:
                    continue

                arr = arrays_per_codec[name][metric_name]
                mean_v, std_v, min_v, max_v = stats(arr)
                summary_writer.writerow(
                    {
                        "dataset": dataset_name,
                        "codec": name,
                        "metric": metric_name,
                        "mean": round(mean_v, 2),
                        "std": round(std_v, 2),
                        "min": round(min_v, 2),
                        "max": round(max_v, 2),
                    }
                )

    print(f"[OK] Wrote summary  for dataset {dataset_name} to {summary_csv}", file=sys.stderr)


def main():
    parser = argparse.ArgumentParser(description="Benchmark RDI vs PNG/JPEG on multiple datasets.")
    parser.add_argument(
        "root",
        type=str,
        help="Root directory containing datasets/, temporaries/, results/, tools/.",
    )
    args = parser.parse_args()

    root = Path(args.root).resolve()
    datasets_root = root / "datasets"
    tools_root = root / "tools"
    temporaries_root = root / "temporaries"
    results_root = root / "results"

    if not datasets_root.is_dir():
        sys.stderr.write(f"[ERROR] datasets/ directory not found under {root}\n")
        sys.exit(1)

    if not tools_root.is_dir():
        sys.stderr.write(f"[ERROR] tools/ directory not found under {root}\n")
        sys.exit(1)

    ensure_dir(temporaries_root)
    ensure_dir(results_root)

    dataset_dirs = sorted([p for p in datasets_root.iterdir() if p.is_dir()])
    if not dataset_dirs:
        sys.stderr.write(f"[ERROR] No dataset subdirectories found under {datasets_root}\n")
        sys.exit(1)

    candidates = [
        make_png_candidate("png"),
        make_jpeg_candidate("jpeg_q90", quality=90),
        make_jpeg_candidate("jpeg_q70", quality=70),
        make_rdi_candidate("rdi_m8", mode=8),
        make_rdi_candidate("rdi_m9", mode=9),
    ]

    for ds_dir in dataset_dirs:
        process_dataset(ds_dir, tools_root, temporaries_root, results_root, candidates)


if __name__ == "__main__":
    main()
