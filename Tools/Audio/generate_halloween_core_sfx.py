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


def render_button() -> np.ndarray:
    duration = 0.12
    t = timebase(duration)
    env = adsr(duration, 0.002, 0.04, 0.18, 0.04)
    tone = 0.62 * sine(1120, t) + 0.24 * sine(1680, t) + 0.14 * triangle(2240, t)
    click = butter_filter(white_noise(len(t), 0.4), "bandpass", (1800, 5000), 2) * exp_decay(duration, 1.0, 0.0008)
    output = tone * env + click * 0.2
    return fade(normalize(soft_clip(output)))


def render_countdown() -> np.ndarray:
    duration = 0.23
    t = timebase(duration)
    env = adsr(duration, 0.003, 0.06, 0.12, 0.06)
    bell = 0.62 * sine(910, t) + 0.24 * sine(1820, t) + 0.12 * sine(2730, t)
    shimmer = butter_filter(white_noise(len(t), 0.28), "bandpass", (2200, 7000), 2) * exp_decay(duration, 1.0, 0.002)
    output = bell * env + shimmer * 0.16
    output = echo(output, [38], [0.22])
    return fade(normalize(soft_clip(output, 1.18)))


def render_whistle() -> np.ndarray:
    duration = 0.43
    t = timebase(duration)
    vibrato = np.sin(2.0 * np.pi * 6.5 * t).astype(np.float32) * 32.0
    lead_freq = np.linspace(1880, 1510, len(t), dtype=np.float32) + vibrato
    env = adsr(duration, 0.02, 0.06, 0.85, 0.1)
    breath = butter_filter(white_noise(len(t), 0.48), "bandpass", (900, 7000), 2) * adsr(duration, 0.01, 0.08, 0.2, 0.12)
    tone = 0.72 * sine(lead_freq, t) + 0.23 * sine(lead_freq * 2.02, t)
    output = tone * env + breath * 0.22
    output = echo(output, [34, 79], [0.12, 0.08])
    return fade(normalize(soft_clip(output, 1.12)))


def render_buzzer() -> np.ndarray:
    duration = 0.88
    t = timebase(duration)
    wobble = 1.0 + 0.032 * np.sin(2.0 * np.pi * 7.0 * t).astype(np.float32)
    body = 0.56 * saw(112 * wobble, t) + 0.28 * sine(224 * wobble, t) + 0.16 * sine(336 * wobble, t)
    env = adsr(duration, 0.01, 0.05, 0.82, 0.18)
    grit = butter_filter(white_noise(len(t), 0.16), "bandpass", (350, 2200), 2) * adsr(duration, 0.01, 0.1, 0.14, 0.18)
    output = body * env + grit
    output = butter_filter(output, "lowpass", 2800, 3)
    return fade(normalize(soft_clip(output, 1.05), 0.88), 0.002, 0.03)


def render_teleport() -> np.ndarray:
    duration = 0.54
    t = timebase(duration)
    reverse_env = np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 2.2
    swirl = butter_filter(white_noise(len(t), 0.42), "bandpass", (220, 3200), 2) * reverse_env
    drop_freq = np.linspace(210, 58, len(t), dtype=np.float32)
    drop = (0.66 * sine(drop_freq, t) + 0.18 * sine(drop_freq * 1.98, t)) * adsr(duration, 0.03, 0.09, 0.38, 0.18)
    flash_center = int(0.22 * SAMPLE_RATE)
    flash = np.zeros_like(t)
    flash_len = int(0.06 * SAMPLE_RATE)
    flash_slice = slice(flash_center, min(len(flash), flash_center + flash_len))
    ft = np.linspace(0.0, 1.0, flash_slice.stop - flash_slice.start, endpoint=False, dtype=np.float32)
    flash[flash_slice] = (0.85 * sine(np.linspace(1800, 980, len(ft), dtype=np.float32), ft) + 0.2 * white_noise(len(ft), 0.6)) * np.hanning(len(ft))
    output = swirl * 0.18 + drop + flash
    output = echo(output, [55, 110], [0.18, 0.11])
    return fade(normalize(soft_clip(output, 1.2)))


def render_swoosh() -> np.ndarray:
    duration = 0.17
    t = timebase(duration)
    noise_layer = butter_filter(white_noise(len(t), 0.7), "bandpass", (600, 9000), 2) * adsr(duration, 0.003, 0.05, 0.08, 0.05)
    tone = signal.chirp(t, f0=620, f1=180, t1=duration, method="quadratic").astype(np.float32)
    output = noise_layer * 0.74 + tone * exp_decay(duration, 1.0, 0.004) * 0.22
    return fade(normalize(soft_clip(output, 1.25), 0.9))


def render_energy() -> np.ndarray:
    duration = 0.49
    t = timebase(duration)
    env = adsr(duration, 0.01, 0.08, 0.3, 0.12)
    base = 0.34 * sine(520, t) + 0.28 * sine(656, t) + 0.24 * sine(780, t) + 0.12 * sine(1040, t)
    pulse = 0.55 + 0.45 * np.sin(2.0 * np.pi * 12.0 * t).astype(np.float32) ** 2
    shimmer = butter_filter(white_noise(len(t), 0.36), "bandpass", (2600, 8000), 2) * np.linspace(0.0, 1.0, len(t), dtype=np.float32) * 0.18
    output = base * env * pulse + shimmer
    output = echo(output, [42, 97], [0.24, 0.14])
    return fade(normalize(soft_clip(output, 1.18)))


def render_stunned() -> np.ndarray:
    duration = 0.42
    t = timebase(duration)
    wobble = 1.0 + 0.065 * np.sin(2.0 * np.pi * 8.0 * t).astype(np.float32)
    tone = 0.54 * sine(430 * wobble, t) + 0.33 * sine(660 * wobble * 0.98, t)
    ring = 0.22 * triangle(np.linspace(720, 320, len(t), dtype=np.float32), t)
    env = adsr(duration, 0.005, 0.08, 0.28, 0.12)
    clang = butter_filter(white_noise(len(t), 0.28), "bandpass", (1200, 4800), 2) * exp_decay(duration, 1.0, 0.0005) * 0.12
    output = (tone + ring) * env + clang
    output = echo(output, [36], [0.18])
    return fade(normalize(soft_clip(output, 1.18)))


def render_mega_start() -> np.ndarray:
    duration = 0.7
    t = timebase(duration)
    rise = signal.chirp(t, f0=72, f1=250, t1=0.34, method="quadratic").astype(np.float32)
    rise *= np.clip(np.linspace(0.0, 1.0, len(t), dtype=np.float32) * 1.9, 0.0, 1.0)
    sub = 0.58 * rise * adsr(duration, 0.02, 0.18, 0.44, 0.22)
    fire = butter_filter(white_noise(len(t), 0.7), "bandpass", (900, 7000), 2) * np.clip(np.linspace(0.1, 1.0, len(t), dtype=np.float32), 0.0, 1.0) * 0.18
    crackle = np.zeros_like(t)
    for center_ms in (120, 170, 220, 280):
        center = int(center_ms / 1000.0 * SAMPLE_RATE)
        width = int(0.012 * SAMPLE_RATE)
        left = max(0, center - width // 2)
        right = min(len(crackle), left + width)
        crackle[left:right] += white_noise(right - left, 0.9) * np.hanning(right - left) * 0.2
    impact_center = int(0.34 * SAMPLE_RATE)
    impact = np.zeros_like(t)
    impact_len = int(0.12 * SAMPLE_RATE)
    left = max(0, impact_center - impact_len // 6)
    right = min(len(impact), left + impact_len)
    impact_t = np.linspace(0.0, 1.0, right - left, endpoint=False, dtype=np.float32)
    impact[left:right] = (0.7 * sine(np.linspace(180, 60, len(impact_t), dtype=np.float32), impact_t) + 0.35 * butter_filter(white_noise(len(impact_t), 0.8), "lowpass", 1200, 2)) * np.hanning(len(impact_t))
    output = sub + fire + crackle + impact
    output = echo(output, [68, 132], [0.2, 0.1])
    return fade(normalize(soft_clip(output, 1.16), 0.94), 0.003, 0.04)


def render_shield() -> np.ndarray:
    duration = 0.58
    t = timebase(duration)
    prefall = butter_filter(white_noise(len(t), 0.3), "bandpass", (700, 5000), 2) * (np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 3) * 0.12
    slam = (0.55 * sine(np.linspace(170, 58, len(t), dtype=np.float32), t) + 0.28 * sine(np.linspace(520, 160, len(t), dtype=np.float32), t)) * adsr(duration, 0.01, 0.1, 0.22, 0.16)
    plate = (0.28 * sine(430, t) + 0.16 * sine(690, t) + 0.1 * sine(980, t)) * exp_decay(duration, 1.0, 0.0015)
    arc = butter_filter(white_noise(len(t), 0.48), "bandpass", (1800, 7800), 2) * (0.35 + 0.65 * np.sin(2.0 * np.pi * 28.0 * t).astype(np.float32) ** 2) * adsr(duration, 0.02, 0.1, 0.15, 0.18)
    output = prefall + slam + plate * 0.22 + arc * 0.2
    output = echo(output, [48, 96], [0.2, 0.12])
    return fade(normalize(soft_clip(output, 1.14)))


def render_dash() -> np.ndarray:
    duration = 0.22
    t = timebase(duration)
    rush = butter_filter(white_noise(len(t), 0.8), "bandpass", (500, 9000), 2) * adsr(duration, 0.004, 0.05, 0.08, 0.06)
    low = 0.22 * sine(np.linspace(240, 90, len(t), dtype=np.float32), t) * exp_decay(duration, 1.0, 0.01)
    output = rush * 0.82 + low
    return fade(normalize(soft_clip(output, 1.22), 0.88))


def render_super_dash() -> np.ndarray:
    duration = 0.48
    t = timebase(duration)
    rush = butter_filter(white_noise(len(t), 0.78), "bandpass", (420, 9000), 2) * adsr(duration, 0.004, 0.08, 0.18, 0.1)
    engine = 0.36 * sine(np.linspace(260, 85, len(t), dtype=np.float32), t) * adsr(duration, 0.01, 0.1, 0.24, 0.14)
    streak = 0.17 * signal.chirp(t, f0=1300, f1=430, t1=duration, method="quadratic").astype(np.float32) * exp_decay(duration, 1.0, 0.01)
    output = rush * 0.74 + engine + streak
    output = echo(output, [34, 78], [0.18, 0.12])
    return fade(normalize(soft_clip(output, 1.18), 0.92))


def render_bsteel() -> np.ndarray:
    duration = 0.23
    t = timebase(duration)
    thud = 0.42 * sine(np.linspace(210, 76, len(t), dtype=np.float32), t) * adsr(duration, 0.002, 0.05, 0.08, 0.05)
    ring = (0.34 * sine(480, t) + 0.2 * sine(960, t) + 0.08 * sine(1440, t)) * exp_decay(duration, 1.0, 0.001)
    grit = butter_filter(white_noise(len(t), 0.35), "bandpass", (1700, 6000), 2) * exp_decay(duration, 1.0, 0.0009) * 0.2
    output = thud + ring + grit
    return fade(normalize(soft_clip(output, 1.14), 0.9))


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
