import subprocess
import sys
import os
import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Callable


def run_cmd(cmd):
    proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    if proc.returncode != 0:
        sys.stderr.write(f"[ERROR] Command failed: {' '.join(cmd)}\n")
        sys.stderr.write(proc.stderr.decode(errors="ignore") + "\n")
        raise RuntimeError(f"Command failed: {' '.join(cmd)}")

    return proc


def resolve_tool(tools_path: Path, name: str) -> str:
    if os.name == "nt":
        candidates = [tools_path / f"{name}.exe", tools_path / name]
    else:
        candidates = [tools_path / name, tools_path / f"{name}.exe"]

    for p in candidates:
        if p.exists():
            return str(p)

    found = shutil.which(name)
    if found:
        return found

    if os.name == "nt":
        found = shutil.which(f"{name}.exe")
        if found:
            return found

    raise FileNotFoundError(f"Tool '{name}' not found in {tools_path} and not in PATH")


def ensure_dir(path: Path):
    path.mkdir(parents=True, exist_ok=True)


def rel_to_safe(rel: Path) -> str:
    return "_".join(rel.with_suffix("").parts)


def file_bpp(path: Path, width: int, height: int) -> float:
    size_bytes = path.stat().st_size
    return (size_bytes * 8.0) / (width * height)


@dataclass
class Candidate:
    name: str
    extension: str
    encode_fn: Callable[[Path, Path, Path], None]
    decode_fn: Callable[[Path, Path, Path], None]


def encode_png(tools_path: Path, input_path: Path, output_path: Path):
    ensure_dir(output_path.parent)
    run_cmd(
        [
            resolve_tool(tools_path, "ffmpeg"),
            "-y",
            "-i",
            str(input_path),
            "-compression_level",
            "9",
            str(output_path),
        ]
    )


def decode_png(tools_path: Path, input_path: Path, output_path: Path):
    ensure_dir(output_path.parent)
    run_cmd(
        [
            resolve_tool(tools_path, "ffmpeg"),
            "-y",
            "-i",
            str(input_path),
            str(output_path)
        ]
    )


def make_png_candidate(name: str):
    return Candidate(name=name, extension="png", encode_fn=encode_png, decode_fn=decode_png)


def encode_jpeg(tools_path: Path, input_path: Path, output_path: Path, quality: int):
    qscale = max(2, min(31, int(31 - quality * 0.29)))
    ensure_dir(output_path.parent)
    run_cmd(
        [
            resolve_tool(tools_path, "ffmpeg"),
            "-y",
            "-i",
            str(input_path),
            "-qscale:v",
            str(qscale),
            str(output_path),
        ]
    )


def decode_jpeg(tools_path: Path, input_path: Path, output_path: Path):
    ensure_dir(output_path.parent)
    run_cmd(
        [
            resolve_tool(tools_path, "ffmpeg"),
            "-y",
            "-i",
            str(input_path),
            str(output_path)
        ]
    )


def make_jpeg_candidate(name: str, quality: int):
    def enc(tools_path: Path, inp: Path, out: Path):
        encode_jpeg(tools_path, inp, out, quality)

    return Candidate(name=name, extension="jpg", encode_fn=enc, decode_fn=decode_jpeg)


def encode_rdi(tools_path: Path, input_path: Path, output_path: Path, mode: int):
    ensure_dir(output_path.parent)
    run_cmd(
        [
            resolve_tool(tools_path, "rdc"),
            "encode",
            "--mode",
            str(mode),
            "--overwrite",
            str(input_path),
            str(output_path),
        ]
    )


def decode_rdi(tools_path: Path, input_path: Path, output_path: Path):
    ensure_dir(output_path.parent)
    run_cmd(
        [
            resolve_tool(tools_path, "rdc"),
            "decode",
            "--overwrite",
            str(input_path),
            str(output_path),
        ]
    )


def make_rdi_candidate(name: str, mode: int):
    def enc(tools_path: Path, inp: Path, out: Path):
        encode_rdi(tools_path, inp, out, mode)

    return Candidate(name=name, extension="rdi", encode_fn=enc, decode_fn=decode_rdi)


def find_images(dataset_dir: Path):
    exts = {".bmp", ".png", ".jpg", ".jpeg", ".tif", ".tiff"}

    for p in dataset_dir.rglob("*"):
        if p.is_file() and p.suffix.lower() in exts:
            yield p
