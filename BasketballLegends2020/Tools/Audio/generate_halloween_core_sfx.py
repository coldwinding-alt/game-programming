#!/usr/bin/env python3
import argparse
from pathlib import Path

import numpy as np
from scipy import signal
from scipy.io import wavfile

SAMPLE_RATE = 44100
RNG = np.random.default_rng(20260422)


def timebase(duration: float) -> np.ndarray:
    count = max(1, int(SAMPLE_RATE * duration))
    return np.linspace(0.0, duration, count, endpoint=False, dtype=np.float32)


def sine(frequency, t: np.ndarray) -> np.ndarray:
    return np.sin(2.0 * np.pi * np.asarray(frequency) * t).astype(np.float32)


def triangle(frequency, t: np.ndarray) -> np.ndarray:
    return signal.sawtooth(2.0 * np.pi * np.asarray(frequency) * t, 0.5).astype(np.float32)


def saw(frequency, t: np.ndarray) -> np.ndarray:
    return signal.sawtooth(2.0 * np.pi * np.asarray(frequency) * t).astype(np.float32)


def white_noise(length: int, amount: float = 1.0) -> np.ndarray:
    return (RNG.standard_normal(length).astype(np.float32)) * amount


def adsr(duration: float, attack: float, decay: float, sustain_level: float, release: float) -> np.ndarray:
    t = timebase(duration)
    env = np.ones_like(t)
    attack = max(attack, 1e-4)
    decay = max(decay, 1e-4)
    release = max(release, 1e-4)
    sustain_end = max(0.0, duration - release)
    for i, ts in enumerate(t):
        if ts < attack:
            env[i] = ts / attack
        elif ts < attack + decay:
            phase = (ts - attack) / decay
            env[i] = 1.0 + (sustain_level - 1.0) * phase
        elif ts < sustain_end:
            env[i] = sustain_level
        else:
            phase = (ts - sustain_end) / release
            env[i] = sustain_level * max(0.0, 1.0 - phase)
    return env


def exp_decay(duration: float, start: float = 1.0, end: float = 0.001) -> np.ndarray:
    count = max(1, int(SAMPLE_RATE * duration))
    return np.geomspace(start, end, count).astype(np.float32)


def fade(signal_data: np.ndarray, fade_in_s: float = 0.002, fade_out_s: float = 0.01) -> np.ndarray:
    output = signal_data.copy()
    fade_in = min(len(output), max(1, int(SAMPLE_RATE * fade_in_s)))
    fade_out = min(len(output), max(1, int(SAMPLE_RATE * fade_out_s)))
    output[:fade_in] *= np.linspace(0.0, 1.0, fade_in, dtype=np.float32)
    output[-fade_out:] *= np.linspace(1.0, 0.0, fade_out, dtype=np.float32)
    return output


def butter_filter(signal_data: np.ndarray, mode: str, cutoff, order: int = 4) -> np.ndarray:
    nyquist = SAMPLE_RATE * 0.5
    if isinstance(cutoff, tuple):
        normalized = [c / nyquist for c in cutoff]
    else:
        normalized = cutoff / nyquist
    b, a = signal.butter(order, normalized, btype=mode)
    return signal.filtfilt(b, a, signal_data).astype(np.float32)


def echo(signal_data: np.ndarray, taps_ms, gains) -> np.ndarray:
    output = signal_data.astype(np.float32).copy()
    for tap_ms, gain in zip(taps_ms, gains):
        delay = int(SAMPLE_RATE * tap_ms / 1000.0)
        if delay <= 0 or delay >= len(output):
            continue
        delayed = np.zeros_like(output)
        delayed[delay:] = signal_data[:-delay] * gain
        output += delayed
    return output


def normalize(signal_data: np.ndarray, peak: float = 0.92) -> np.ndarray:
    max_value = float(np.max(np.abs(signal_data)))
    if max_value < 1e-6:
        return signal_data.astype(np.float32)
    return (signal_data / max_value * peak).astype(np.float32)


def soft_clip(signal_data: np.ndarray, drive: float = 1.25) -> np.ndarray:
    return np.tanh(signal_data * drive).astype(np.float32)


def band_noise(duration: float, low_hz: float, high_hz: float, amount: float = 1.0, order: int = 2) -> np.ndarray:
    t = timebase(duration)
    return butter_filter(white_noise(len(t), amount), "bandpass", (low_hz, high_hz), order)


def resonance(freqs, duration: float, weights=None, decay_end: float = 0.001) -> np.ndarray:
    t = timebase(duration)
    if weights is None:
        weights = [1.0] * len(freqs)
    output = np.zeros_like(t)
    for freq, weight in zip(freqs, weights):
        output += sine(freq, t) * weight
    output *= exp_decay(duration, 1.0, decay_end)
    return output.astype(np.float32)


def pitch_drop(duration: float, start_hz: float, end_hz: float, weight: float = 1.0) -> np.ndarray:
    t = timebase(duration)
    return (sine(np.linspace(start_hz, end_hz, len(t), dtype=np.float32), t) * weight).astype(np.float32)


def render_button() -> np.ndarray:
    duration = 0.11
    click = band_noise(duration, 1800, 9000, 0.7, 2) * exp_decay(duration, 1.0, 0.0004) * 0.32
    tick = resonance([920, 1380, 2080], duration, [0.34, 0.18, 0.08], 0.0009) * 0.42
    body = pitch_drop(duration, 220, 110, 0.16) * adsr(duration, 0.002, 0.035, 0.06, 0.03)
    output = click + tick + body
    return fade(normalize(soft_clip(output, 1.08), 0.8), 0.001, 0.018)


def render_countdown() -> np.ndarray:
    duration = 0.24
    bell = resonance([540, 803, 1270, 1730], duration, [0.45, 0.22, 0.12, 0.06], 0.0007) * 0.7
    hammer = band_noise(duration, 1200, 7800, 0.6, 2) * exp_decay(duration, 1.0, 0.0003) * 0.15
    air = band_noise(duration, 3000, 11000, 0.22, 2) * adsr(duration, 0.001, 0.03, 0.02, 0.05) * 0.08
    output = bell + hammer + air
    output = echo(output, [42, 88], [0.12, 0.07])
    return fade(normalize(soft_clip(output, 1.05), 0.86), 0.001, 0.03)


def render_whistle() -> np.ndarray:
    duration = 0.39
    t = timebase(duration)
    vibrato = np.sin(2.0 * np.pi * 5.8 * t).astype(np.float32) * 48.0
    lead_freq = np.linspace(2360, 1980, len(t), dtype=np.float32) + vibrato
    env = adsr(duration, 0.012, 0.04, 0.72, 0.08)
    tone = (0.58 * sine(lead_freq, t) + 0.18 * sine(lead_freq * 2.03, t)) * env
    wind = band_noise(duration, 1800, 8500, 0.42, 2) * adsr(duration, 0.01, 0.05, 0.22, 0.08) * 0.3
    grit = band_noise(duration, 4200, 11000, 0.18, 2) * exp_decay(duration, 1.0, 0.002) * 0.08
    output = tone + wind + grit
    output = butter_filter(output, "bandpass", (800, 9000), 2)
    return fade(normalize(soft_clip(output, 1.04), 0.86), 0.001, 0.025)


def render_buzzer() -> np.ndarray:
    duration = 0.82
    t = timebase(duration)
    wobble = 1.0 + 0.024 * np.sin(2.0 * np.pi * 6.4 * t).astype(np.float32)
    horn = (0.5 * saw(118 * wobble, t) + 0.25 * triangle(236 * wobble, t) + 0.12 * sine(354 * wobble, t)) * adsr(duration, 0.01, 0.06, 0.8, 0.14)
    dirt = band_noise(duration, 260, 1800, 0.34, 2) * adsr(duration, 0.008, 0.1, 0.18, 0.14) * 0.18
    output = butter_filter(horn + dirt, "lowpass", 2200, 3)
    return fade(normalize(soft_clip(output, 1.06), 0.9), 0.002, 0.04)


def render_teleport() -> np.ndarray:
    duration = 0.56
    t = timebase(duration)
    reverse_air = band_noise(duration, 220, 4200, 0.56, 2) * (np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 2.3) * 0.18
    hollow = pitch_drop(duration, 165, 48, 0.56) * adsr(duration, 0.02, 0.06, 0.26, 0.16)
    arc = band_noise(duration, 2600, 11500, 0.26, 2) * (0.2 + 0.8 * np.sin(2.0 * np.pi * 24.0 * t).astype(np.float32) ** 2) * adsr(duration, 0.02, 0.06, 0.1, 0.18) * 0.12
    flash = np.zeros_like(t)
    start = int(0.22 * SAMPLE_RATE)
    flash_len = int(0.08 * SAMPLE_RATE)
    end = min(len(flash), start + flash_len)
    flash_t = np.linspace(0.0, 1.0, end - start, endpoint=False, dtype=np.float32)
    flash[start:end] = (
        resonance([1480, 2380, 3320], len(flash_t) / SAMPLE_RATE, [0.25, 0.16, 0.1], 0.001)[: len(flash_t)]
        + band_noise(len(flash_t) / SAMPLE_RATE, 3200, 11500, 0.35, 2)[: len(flash_t)] * 0.08
    ) * np.hanning(len(flash_t))
    output = reverse_air + hollow + arc + flash
    output = echo(output, [46, 98], [0.14, 0.09])
    return fade(normalize(soft_clip(output, 1.08), 0.9), 0.001, 0.035)


def render_swoosh() -> np.ndarray:
    duration = 0.16
    rush = band_noise(duration, 500, 9000, 0.8, 2) * adsr(duration, 0.002, 0.04, 0.05, 0.04) * 0.76
    whisper = band_noise(duration, 2200, 12000, 0.24, 2) * exp_decay(duration, 1.0, 0.002) * 0.12
    body = pitch_drop(duration, 190, 90, 0.12) * exp_decay(duration, 1.0, 0.01)
    output = rush + whisper + body
    return fade(normalize(soft_clip(output, 1.1), 0.86), 0.001, 0.02)


def render_energy() -> np.ndarray:
    duration = 0.46
    t = timebase(duration)
    reverse_shimmer = band_noise(duration, 2600, 11500, 0.34, 2) * (np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 2) * 0.18
    bell = resonance([670, 1010, 1520], duration, [0.22, 0.12, 0.06], 0.0012) * adsr(duration, 0.01, 0.06, 0.08, 0.1)
    pulse = band_noise(duration, 900, 5000, 0.24, 2) * (0.3 + 0.7 * np.sin(2.0 * np.pi * 14.0 * t).astype(np.float32) ** 2) * 0.08
    output = reverse_shimmer + bell + pulse
    output = echo(output, [38, 84], [0.16, 0.09])
    return fade(normalize(soft_clip(output, 1.04), 0.84), 0.001, 0.03)


def render_stunned() -> np.ndarray:
    duration = 0.38
    t = timebase(duration)
    wobble = 1.0 + 0.08 * np.sin(2.0 * np.pi * 7.0 * t).astype(np.float32)
    ring = (0.34 * sine(410 * wobble, t) + 0.21 * sine(610 * wobble, t) + 0.08 * sine(910 * wobble, t)) * adsr(duration, 0.004, 0.06, 0.14, 0.1)
    crack = band_noise(duration, 1400, 7600, 0.42, 2) * exp_decay(duration, 1.0, 0.00035) * 0.12
    tail = band_noise(duration, 2400, 9200, 0.18, 2) * adsr(duration, 0.008, 0.05, 0.04, 0.08) * 0.08
    output = ring + crack + tail
    output = echo(output, [32], [0.12])
    return fade(normalize(soft_clip(output, 1.06), 0.86), 0.001, 0.03)


def render_mega_start() -> np.ndarray:
    duration = 0.68
    t = timebase(duration)
    rise_air = band_noise(duration, 220, 7000, 0.56, 2) * (np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 1.8) * 0.18
    sub = pitch_drop(duration, 94, 42, 0.58) * adsr(duration, 0.015, 0.09, 0.18, 0.18)
    ember = band_noise(duration, 1500, 9000, 0.54, 2) * np.clip(np.linspace(0.18, 1.0, len(t), dtype=np.float32), 0.0, 1.0) * 0.14
    crackle = np.zeros_like(t)
    for center_ms in (120, 170, 220, 280):
        center = int(center_ms / 1000.0 * SAMPLE_RATE)
        width = int(0.012 * SAMPLE_RATE)
        left = max(0, center - width // 2)
        right = min(len(crackle), left + width)
        crackle[left:right] += band_noise((right - left) / SAMPLE_RATE, 2500, 11500, 0.8, 2)[: right - left] * np.hanning(right - left) * 0.18
    impact_center = int(0.31 * SAMPLE_RATE)
    impact = np.zeros_like(t)
    impact_len = int(0.12 * SAMPLE_RATE)
    left = max(0, impact_center - impact_len // 6)
    right = min(len(impact), left + impact_len)
    impact_t = np.linspace(0.0, 1.0, right - left, endpoint=False, dtype=np.float32)
    impact[left:right] = (
        pitch_drop(len(impact_t) / SAMPLE_RATE, 160, 58, 0.7)[: len(impact_t)]
        + butter_filter(white_noise(len(impact_t), 0.7), "lowpass", 1400, 2) * 0.22
    ) * np.hanning(len(impact_t))
    output = rise_air + sub + ember + crackle + impact
    output = echo(output, [58, 118], [0.16, 0.1])
    return fade(normalize(soft_clip(output, 1.08), 0.92), 0.001, 0.04)


def render_shield() -> np.ndarray:
    duration = 0.6
    t = timebase(duration)
    prefall = band_noise(duration, 500, 4200, 0.38, 2) * (np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 2.6) * 0.1
    slam = (pitch_drop(duration, 150, 46, 0.62) + pitch_drop(duration, 420, 130, 0.18)) * adsr(duration, 0.008, 0.08, 0.18, 0.18)
    plate = resonance([380, 690, 1110], duration, [0.18, 0.1, 0.05], 0.0012) * 0.26
    arc = band_noise(duration, 2000, 9800, 0.36, 2) * (0.24 + 0.76 * np.sin(2.0 * np.pi * 26.0 * t).astype(np.float32) ** 2) * adsr(duration, 0.01, 0.06, 0.08, 0.16) * 0.16
    output = prefall + slam + plate + arc
    output = echo(output, [44, 92], [0.16, 0.08])
    return fade(normalize(soft_clip(output, 1.06), 0.9), 0.001, 0.04)


def render_dash() -> np.ndarray:
    duration = 0.18
    rush = band_noise(duration, 480, 9200, 0.86, 2) * adsr(duration, 0.002, 0.035, 0.04, 0.035) * 0.74
    step = pitch_drop(duration, 180, 80, 0.12) * exp_decay(duration, 1.0, 0.008)
    output = rush + step
    return fade(normalize(soft_clip(output, 1.04), 0.86), 0.001, 0.02)


def render_super_dash() -> np.ndarray:
    duration = 0.44
    rush = band_noise(duration, 420, 9200, 0.88, 2) * adsr(duration, 0.003, 0.07, 0.12, 0.08) * 0.72
    engine = pitch_drop(duration, 230, 74, 0.28) * adsr(duration, 0.006, 0.08, 0.14, 0.12)
    streak = band_noise(duration, 2500, 12000, 0.26, 2) * exp_decay(duration, 1.0, 0.006) * 0.1
    output = rush + engine + streak
    output = echo(output, [28, 62], [0.12, 0.08])
    return fade(normalize(soft_clip(output, 1.04), 0.9), 0.001, 0.03)


def render_bsteel() -> np.ndarray:
    duration = 0.21
    thud = pitch_drop(duration, 190, 70, 0.4) * adsr(duration, 0.002, 0.04, 0.05, 0.04)
    knock = band_noise(duration, 900, 4200, 0.62, 2) * exp_decay(duration, 1.0, 0.00035) * 0.16
    edge = resonance([430, 810, 1290], duration, [0.16, 0.08, 0.04], 0.0013) * 0.24
    output = thud + knock + edge
    return fade(normalize(soft_clip(output, 1.02), 0.86), 0.001, 0.02)


RENDERS = {
    "20_ButtonSnd": render_button,
    "19_M_Countdown": render_countdown,
    "2_M_Whistle": render_whistle,
    "9_M_Buzzer": render_buzzer,
    "4_P_Teleport": render_teleport,
    "5_P_Swoosh": render_swoosh,
    "6_P_Energy": render_energy,
    "7_P_Stunned": render_stunned,
    "11_P_MegaStart": render_mega_start,
    "13_P_Shield": render_shield,
    "17_P_Dash": render_dash,
    "18_P_SuperDash": render_super_dash,
    "8_B_Steel": render_bsteel,
}


def write_wav(path: Path, signal_data: np.ndarray) -> None:
    pcm = np.clip(signal_data, -1.0, 1.0)
    wavfile.write(path, SAMPLE_RATE, (pcm * 32767.0).astype(np.int16))


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate the custom Halloween core SFX set.")
    parser.add_argument(
        "--out",
        default="Assets/BasketballLegends2020/Resources/BL2020/Sound",
        help="Output directory for generated wav files.",
    )
    args = parser.parse_args()

    out_dir = Path(args.out)
    out_dir.mkdir(parents=True, exist_ok=True)

    for key, renderer in RENDERS.items():
        output = renderer()
        write_wav(out_dir / f"{key}.wav", output)
        print(f"generated {key}.wav ({len(output) / SAMPLE_RATE:.2f}s)")


if __name__ == "__main__":
    main()
