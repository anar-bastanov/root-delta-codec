import math
import numpy as np
from collections import OrderedDict
from skimage.metrics import structural_similarity as ssim


try:
    import torch
    import lpips as lpips_lib
    _lpips_model = lpips_lib.LPIPS(net='alex')
    _lpips_model.eval()
except Exception:
    _lpips_model = None


def metric_psnr(orig_img, recon_img):
    orig = np.asarray(orig_img, dtype=np.float32)
    recon = np.asarray(recon_img, dtype=np.float32)

    if orig.shape != recon.shape:
        raise ValueError(f"Shape mismatch: {orig.shape} vs {recon.shape}")

    mse = np.mean((orig - recon) ** 2)
    return float("inf") if mse <= 0.0 else 10.0 * math.log10((255.0 ** 2) / mse)


def metric_psnr_y(orig_img, recon_img):
    orig = np.asarray(orig_img, dtype=np.float32)
    recon = np.asarray(recon_img, dtype=np.float32)

    if orig.shape != recon.shape:
        raise ValueError(f"Shape mismatch: {orig.shape} vs {recon.shape}")

    if orig.ndim != 3 or orig.shape[2] != 3:
        raise ValueError(f"Expected RGB image for PSNR_Y, got shape {orig.shape}")

    w_r, w_g, w_b = 0.299, 0.587, 0.114
    orig_y = w_r * orig[..., 0] + w_g * orig[..., 1] + w_b * orig[..., 2]
    recon_y = w_r * recon[..., 0] + w_g * recon[..., 1] + w_b * recon[..., 2]

    mse = np.mean((orig_y - recon_y) ** 2)
    return float("inf") if mse <= 0.0 else 10.0 * math.log10((255.0 ** 2) / mse)


def metric_ssim(orig_img, recon_img):
    orig = np.asarray(orig_img, dtype=np.float32)
    recon = np.asarray(recon_img, dtype=np.float32)

    if orig.shape != recon.shape:
        raise ValueError(f"Shape mismatch: {orig.shape} vs {recon.shape}")

    return ssim(orig, recon, channel_axis=-1, data_range=255.0)


def _downsample_2x(img):
    h, w = img.shape[:2]
    h2 = h // 2
    w2 = w // 2
    img = img[: 2 * h2, : 2 * w2, :]
    img = img.reshape(h2, 2, w2, 2, -1).mean(axis=(1, 3))
    return img


def metric_ms_ssim(orig_img, recon_img, levels=4):
    orig = np.asarray(orig_img, dtype=np.float32)
    recon = np.asarray(recon_img, dtype=np.float32)

    if orig.shape != recon.shape:
        raise ValueError(f"Shape mismatch: {orig.shape} vs {recon.shape}")

    scores = []
    cur_orig = orig.copy()
    cur_recon = recon.copy()

    for level in range(levels):
        scores.append(
            ssim(
                cur_orig,
                cur_recon,
                channel_axis=-1,
                data_range=255.0,
                gaussian_weights=True,
                use_sample_covariance=False,
            )
        )

        if level < levels - 1:
            cur_orig = _downsample_2x(cur_orig)
            cur_recon = _downsample_2x(cur_recon)

    return float(np.mean(scores))


def metric_lpips(orig_img, recon_img):
    if _lpips_model is None:
        raise RuntimeError("LPIPS model not available. Install torch and lpips.")

    orig = np.asarray(orig_img, dtype=np.float32) / 255.0
    recon = np.asarray(recon_img, dtype=np.float32) / 255.0

    if orig.shape != recon.shape:
        raise ValueError(f"Shape mismatch: {orig.shape} vs {recon.shape}")

    if orig.ndim != 3 or orig.shape[2] != 3:
        raise ValueError(f"Expected RGB image for LPIPS, got shape {orig.shape}")

    t1 = torch.from_numpy(orig).permute(2, 0, 1).unsqueeze(0)
    t2 = torch.from_numpy(recon).permute(2, 0, 1).unsqueeze(0)
    t1 = t1 * 2.0 - 1.0
    t2 = t2 * 2.0 - 1.0

    with torch.no_grad():
        return float(_lpips_model(t1, t2).item())


def metric_mse(orig_img, recon_img):
    orig = np.asarray(orig_img, dtype=np.float32)
    recon = np.asarray(recon_img, dtype=np.float32)

    if orig.shape != recon.shape:
        raise ValueError(f"Shape mismatch: {orig.shape} vs {recon.shape}")

    return float(np.mean((orig - recon) ** 2))


def metric_mae(orig_img, recon_img):
    orig = np.asarray(orig_img, dtype=np.float32)
    recon = np.asarray(recon_img, dtype=np.float32)

    if orig.shape != recon.shape:
        raise ValueError(f"Shape mismatch: {orig.shape} vs {recon.shape}")

    return float(np.mean(np.abs(orig - recon)))


METRICS = OrderedDict(
    [
        ("psnr", metric_psnr),
        ("psnr_y", metric_psnr_y),
        ("ssim", metric_ssim),
        ("ms_ssim", metric_ms_ssim),
        ("mse", metric_mse),
        ("mae", metric_mae),
    ]
)

if _lpips_model is not None:
    METRICS["lpips"] = metric_lpips
