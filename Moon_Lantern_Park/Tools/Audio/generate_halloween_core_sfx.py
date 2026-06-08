#!/usr/bin/env python3
"""
万圣节核心音效程序化生成脚本

本脚本使用纯数学方法（正弦波、噪声、滤波器等）程序化生成游戏中所有的核心音效。
不依赖任何外部音频素材，所有声音都是通过叠加基本波形并经过滤波、包络、回声等
处理后合成的。

音效分类：
1. UI音效（前缀20/19）：按钮点击、倒计时
2. 比赛音效（前缀2/9）：哨声、蜂鸣器
3. 玩家动作音效（前缀4-7/11/13/17-18）：传送、挥空、充能、眩晕、超级扣篮、护盾、冲刺
4. 篮球音效（前缀8/10/16/21-23）：篮筐撞击、篮筐弹响、球弹地、球入网、球砸框、球入筐

每种音效由多个声音层叠加而成，典型结构：
- 主体层（body/tone/horn）：定义音效的基本音色
- 冲击层（click/knock/thud）：定义音效的瞬态特征
- 纹理层（noise/grit/wind）：增加声音的质感和细节
- 尾部层（tail/air）：定义音效的衰减特征

处理管线：
1. 合成各声音层
2. 叠加混合
3. 可选的回声效果
4. 归一化到目标峰值
5. 软削波（soft clip）防止失真
6. 淡入淡出消除爆音
7. 导出为16位PCM WAV文件（44100Hz采样率）

依赖：numpy, scipy
"""
import argparse
from pathlib import Path

import numpy as np
from scipy import signal
from scipy.io import wavfile

# 采样率：44100Hz（CD音质标准）
SAMPLE_RATE = 44100

# 随机数生成器（固定种子确保每次生成的音效完全一致）
RNG = np.random.default_rng(20260422)


# ============================================================
# 基础波形和信号处理工具函数
# ============================================================

def timebase(duration: float) -> np.ndarray:
    """
    生成指定时长的时间轴数组

    创建一个从0到duration的等间距时间点序列，采样率由SAMPLE_RATE决定。
    这是所有波形生成函数的基础。

    参数:
        duration: 时长（秒）

    返回:
        float32类型的时间轴数组
    """
    count = max(1, int(SAMPLE_RATE * duration))
    return np.linspace(0.0, duration, count, endpoint=False, dtype=np.float32)


def sine(frequency, t: np.ndarray) -> np.ndarray:
    """
    生成正弦波

    最基础的波形，音色纯净，常用于音效的基频分量。

    参数:
        frequency: 频率（Hz），可以是标量或数组（实现频率调制）
        t: 时间轴数组

    返回:
        float32类型的正弦波数组（范围-1~1）
    """
    return np.sin(2.0 * np.pi * np.asarray(frequency) * t).astype(np.float32)


def triangle(frequency, t: np.ndarray) -> np.ndarray:
    """
    生成三角波

    音色比正弦波更明亮，比锯齿波更柔和，泛音结构介于两者之间。

    参数:
        frequency: 频率（Hz）
        t: 时间轴数组

    返回:
        float32类型的三角波数组
    """
    return signal.sawtooth(2.0 * np.pi * np.asarray(frequency) * t, 0.5).astype(np.float32)


def saw(frequency, t: np.ndarray) -> np.ndarray:
    """
    生成锯齿波

    音色明亮刺耳，含有丰富的奇次和偶次泛音，常用于模拟号角、合成器音色。

    参数:
        frequency: 频率（Hz）
        t: 时间轴数组

    返回:
        float32类型的锯齿波数组
    """
    return signal.sawtooth(2.0 * np.pi * np.asarray(frequency) * t).astype(np.float32)


def white_noise(length: int, amount: float = 1.0) -> np.ndarray:
    """
    生成白噪声

    白噪声包含所有频率的能量，用于模拟风声、摩擦声、冲击声等。

    参数:
        length: 采样点数
        amount: 噪声幅度系数

    返回:
        float32类型的白噪声数组
    """
    return (RNG.standard_normal(length).astype(np.float32)) * amount


def adsr(duration: float, attack: float, decay: float, sustain_level: float, release: float) -> np.ndarray:
    """
    生成ADSR包络线

    ADSR（Attack-Decay-Sustain-Release）是音效设计中最常用的包络模型：
    - Attack（起音）：从0上升到1.0的时间
    - Decay（衰减）：从1.0下降到sustain_level的时间
    - Sustain（持续）：维持在sustain_level的水平
    - Release（释放）：从sustain_level下降到0的时间

    包络线控制音效的音量随时间的变化，是塑造音效"形态"的关键。

    参数:
        duration: 总时长（秒）
        attack: 起音时间（秒）
        decay: 衰减时间（秒）
        sustain_level: 持续电平（0~1）
        release: 释放时间（秒）

    返回:
        float32类型的包络数组（范围0~1）
    """
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
    """
    生成指数衰减包络

    比线性衰减更自然，模拟自然界中声音能量的衰减规律。
    常用于冲击音、敲击音的衰减尾部。

    参数:
        duration: 时长（秒）
        start: 起始值
        end: 终止值（不能为0，使用对数间距）

    返回:
        float32类型的指数衰减数组
    """
    count = max(1, int(SAMPLE_RATE * duration))
    return np.geomspace(start, end, count).astype(np.float32)


def fade(signal_data: np.ndarray, fade_in_s: float = 0.002, fade_out_s: float = 0.01) -> np.ndarray:
    """
    对信号施加淡入淡出

    淡入：信号开头从0线性上升到满幅，消除起始爆音。
    淡出：信号末尾从满幅线性下降到0，消除结束爆音。

    参数:
        signal_data: 输入信号
        fade_in_s: 淡入时长（秒）
        fade_out_s: 淡出时长（秒）

    返回:
        处理后的信号
    """
    output = signal_data.copy()
    fade_in = min(len(output), max(1, int(SAMPLE_RATE * fade_in_s)))
    fade_out = min(len(output), max(1, int(SAMPLE_RATE * fade_out_s)))
    output[:fade_in] *= np.linspace(0.0, 1.0, fade_in, dtype=np.float32)
    output[-fade_out:] *= np.linspace(1.0, 0.0, fade_out, dtype=np.float32)
    return output


def butter_filter(signal_data: np.ndarray, mode: str, cutoff, order: int = 4) -> np.ndarray:
    """
    应用巴特沃斯滤波器

    巴特沃斯滤波器具有平坦的通带响应，是最常用的音频滤波器。
    支持低通、高通、带通三种模式。

    参数:
        signal_data: 输入信号
        mode: 滤波模式（"lowpass"/"highpass"/"bandpass"）
        cutoff: 截止频率（Hz），带通模式为(low, high)元组
        order: 滤波器阶数（越高越陡峭）

    返回:
        滤波后的信号
    """
    nyquist = SAMPLE_RATE * 0.5
    if isinstance(cutoff, tuple):
        normalized = [c / nyquist for c in cutoff]
    else:
        normalized = cutoff / nyquist
    b, a = signal.butter(order, normalized, btype=mode)
    return signal.filtfilt(b, a, signal_data).astype(np.float32)


def echo(signal_data: np.ndarray, taps_ms, gains) -> np.ndarray:
    """
    添加回声效果

    将延迟信号以指定的增益叠加到原始信号上，
    模拟声音在空间中的反射。常用于增加音效的空间感和厚度。

    参数:
        signal_data: 输入信号
        taps_ms: 各回声的延迟时间列表（毫秒）
        gains: 各回声的增益列表（0~1）

    返回:
        添加回声后的信号
    """
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
    """
    将信号归一化到目标峰值电平

    确保信号的最大绝对值等于peak参数，避免音量过低或削波。

    参数:
        signal_data: 输入信号
        peak: 目标峰值电平（0~1，0.92留有少量余量防止DAC失真）

    返回:
        归一化后的信号
    """
    max_value = float(np.max(np.abs(signal_data)))
    if max_value < 1e-6:
        return signal_data.astype(np.float32)
    return (signal_data / max_value * peak).astype(np.float32)


def soft_clip(signal_data: np.ndarray, drive: float = 1.25) -> np.ndarray:
    """
    软削波处理

    使用tanh函数对信号进行软削波，当信号接近满幅时自动压缩，
    避免硬削波产生的刺耳失真。drive参数控制压缩的激进程度。

    参数:
        signal_data: 输入信号
        drive: 驱动增益（>1时产生更多谐波，增加音色的"温暖感"）

    返回:
        软削波处理后的信号
    """
    return np.tanh(signal_data * drive).astype(np.float32)


def band_noise(duration: float, low_hz: float, high_hz: float, amount: float = 1.0, order: int = 2) -> np.ndarray:
    """
    生成带通滤波的噪声

    先生成白噪声，然后通过带通滤波器只保留指定频率范围内的能量。
    不同频段的噪声模拟不同的物理现象：
    - 低频（200~1000Hz）：沉重的冲击、爆炸
    - 中频（1000~5000Hz）：摩擦、敲击
    - 高频（5000~12000Hz）：气流、嘶嘶声

    参数:
        duration: 时长（秒）
        low_hz: 带通下限频率
        high_hz: 带通上限频率
        amount: 噪声幅度
        order: 滤波器阶数

    返回:
        float32类型的带通噪声
    """
    t = timebase(duration)
    return butter_filter(white_noise(len(t), amount), "bandpass", (low_hz, high_hz), order)


def resonance(freqs, duration: float, weights=None, decay_end: float = 0.001) -> np.ndarray:
    """
    生成谐振音色

    将多个正弦波按指定权重叠加后施加指数衰减，模拟物体的共振特性。
    不同物体（金属、木头、空气）有不同的谐振频率组合：
    - 金属：高频谐振（800~3000Hz），衰减较慢
    - 木头：中频谐振（300~1200Hz），衰减较快
    - 空气：宽带谐振，衰减很快

    参数:
        freqs: 谐振频率列表（Hz）
        duration: 时长（秒）
        weights: 各频率的权重（默认全部为1.0）
        decay_end: 衰减终值

    返回:
        float32类型的谐振信号
    """
    t = timebase(duration)
    if weights is None:
        weights = [1.0] * len(freqs)
    output = np.zeros_like(t)
    for freq, weight in zip(freqs, weights):
        output += sine(freq, t) * weight
    output *= exp_decay(duration, 1.0, decay_end)
    return output.astype(np.float32)


def pitch_drop(duration: float, start_hz: float, end_hz: float, weight: float = 1.0) -> np.ndarray:
    """
    生成频率下降的正弦波（滑音效果）

    频率从start_hz线性下降到end_hz，模拟物体下落、碰撞后的
    频率衰减。常用于模拟球体撞击后的"咚"声。

    参数:
        duration: 时长（秒）
        start_hz: 起始频率
        end_hz: 终止频率
        weight: 幅度权重

    返回:
        float32类型的滑音信号
    """
    t = timebase(duration)
    return (sine(np.linspace(start_hz, end_hz, len(t), dtype=np.float32), t) * weight).astype(np.float32)


# ============================================================
# 各种音效的渲染函数
# ============================================================

def render_button() -> np.ndarray:
    """
    渲染按钮点击音效

    短促的UI按钮点击声（0.11秒），由三层组成：
    - click：高频噪声冲击（1800~9000Hz），快速衰减，模拟手指触感
    - tick：中高频谐振（920/1380/2080Hz），模拟按钮机构的机械声
    - body：低频滑音（220→110Hz），赋予按钮"重量感"

    返回:
        按钮音效的PCM信号
    """
    duration = 0.11
    click = band_noise(duration, 1800, 9000, 0.7, 2) * exp_decay(duration, 1.0, 0.0004) * 0.32
    tick = resonance([920, 1380, 2080], duration, [0.34, 0.18, 0.08], 0.0009) * 0.42
    body = pitch_drop(duration, 220, 110, 0.16) * adsr(duration, 0.002, 0.035, 0.06, 0.03)
    output = click + tick + body
    return fade(normalize(soft_clip(output, 1.08), 0.8), 0.001, 0.018)


def render_countdown() -> np.ndarray:
    """
    渲染倒计时音效

    比赛开始前的倒计时提示音（0.24秒），由三层组成：
    - bell：金属谐振（540/803/1270/1730Hz），清脆的钟声质感
    - hammer：高频噪声冲击（1200~7800Hz），模拟敲击瞬间
    - air：高频空气噪声（3000~11000Hz），增加空气感
    加上短回声（42ms和88ms）增加空间感。

    返回:
        倒计时音效的PCM信号
    """
    duration = 0.24
    bell = resonance([540, 803, 1270, 1730], duration, [0.45, 0.22, 0.12, 0.06], 0.0007) * 0.7
    hammer = band_noise(duration, 1200, 7800, 0.6, 2) * exp_decay(duration, 1.0, 0.0003) * 0.15
    air = band_noise(duration, 3000, 11000, 0.22, 2) * adsr(duration, 0.001, 0.03, 0.02, 0.05) * 0.08
    output = bell + hammer + air
    output = echo(output, [42, 88], [0.12, 0.07])
    return fade(normalize(soft_clip(output, 1.05), 0.86), 0.001, 0.03)


def render_whistle() -> np.ndarray:
    """
    渲染哨声音效

    裁判哨声（0.39秒），模拟真实哨子的发声原理：
    - tone：带有颤音（5.8Hz）的主音（2360→1980Hz下行），加二次泛音
    - wind：中高频气流噪声（1800~8500Hz），模拟吹气声
    - grit：高频摩擦噪声（4200~11000Hz），增加哨子的粗糙感
    最后通过800~9000Hz带通滤波器塑造整体音色。

    返回:
        哨声音效的PCM信号
    """
    duration = 0.39
    t = timebase(duration)
    # 颤音调制：5.8Hz的正弦波，深度48Hz
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
    """
    渲染蜂鸣器音效

    比赛结束或犯规时的蜂鸣声（0.82秒），模拟体育场馆的电子蜂鸣器：
    - horn：锯齿波+三角波+正弦波叠加（118Hz基频），带6.4Hz颤音
    - dirt：低频噪声（260~1800Hz），增加浑厚感
    最后通过2200Hz低通滤波器使音色更浑厚。

    返回:
        蜂鸣器音效的PCM信号
    """
    duration = 0.82
    t = timebase(duration)
    wobble = 1.0 + 0.024 * np.sin(2.0 * np.pi * 6.4 * t).astype(np.float32)
    horn = (0.5 * saw(118 * wobble, t) + 0.25 * triangle(236 * wobble, t) + 0.12 * sine(354 * wobble, t)) * adsr(duration, 0.01, 0.06, 0.8, 0.14)
    dirt = band_noise(duration, 260, 1800, 0.34, 2) * adsr(duration, 0.008, 0.1, 0.18, 0.14) * 0.18
    output = butter_filter(horn + dirt, "lowpass", 2200, 3)
    return fade(normalize(soft_clip(output, 1.06), 0.9), 0.002, 0.04)


def render_teleport() -> np.ndarray:
    """
    渲染传送音效

    角色瞬移时的音效（0.56秒），营造"从一个空间消失并出现在另一个空间"的感觉：
    - reverse_air：反向渐强的空气噪声（220~4200Hz），模拟能量聚集
    - hollow：低频空洞滑音（165→48Hz），营造虚无感
    - arc：带调制的中高频噪声（2600~11500Hz），模拟能量电弧
    - flash：在0.22秒处的短促闪光音（谐振+高频噪声），模拟瞬间穿越
    加上回声增加空间感。

    返回:
        传送音效的PCM信号
    """
    duration = 0.56
    t = timebase(duration)
    reverse_air = band_noise(duration, 220, 4200, 0.56, 2) * (np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 2.3) * 0.18
    hollow = pitch_drop(duration, 165, 48, 0.56) * adsr(duration, 0.02, 0.06, 0.26, 0.16)
    arc = band_noise(duration, 2600, 11500, 0.26, 2) * (0.2 + 0.8 * np.sin(2.0 * np.pi * 24.0 * t).astype(np.float32) ** 2) * adsr(duration, 0.02, 0.06, 0.1, 0.18) * 0.12
    # 闪光音效：在0.22秒处爆发
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
    """
    渲染挥空音效

    角色挥拳/挥棒但未命中时的快速划空声（0.16秒）：
    - rush：宽带噪声冲击（500~9000Hz），快速起音
    - whisper：高频空气噪声（2200~12000Hz），模拟空气被划开
    - body：低频滑音（190→90Hz），赋予动作重量感

    返回:
        挥空音效的PCM信号
    """
    duration = 0.16
    rush = band_noise(duration, 500, 9000, 0.8, 2) * adsr(duration, 0.002, 0.04, 0.05, 0.04) * 0.76
    whisper = band_noise(duration, 2200, 12000, 0.24, 2) * exp_decay(duration, 1.0, 0.002) * 0.12
    body = pitch_drop(duration, 190, 90, 0.12) * exp_decay(duration, 1.0, 0.01)
    output = rush + whisper + body
    return fade(normalize(soft_clip(output, 1.1), 0.86), 0.001, 0.02)


def render_energy() -> np.ndarray:
    """
    渲染充能音效

    角色蓄力或获得能量时的上升音效（0.46秒）：
    - reverse_shimmer：渐强的高频闪烁（2600~11500Hz），模拟能量聚集
    - bell：清脆的谐振（670/1010/1520Hz），赋予能量"晶体"质感
    - pulse：带调制的中频脉冲（900~5000Hz），14Hz节奏脉动
    加上回声增加空间感。

    返回:
        充能音效的PCM信号
    """
    duration = 0.46
    t = timebase(duration)
    reverse_shimmer = band_noise(duration, 2600, 11500, 0.34, 2) * (np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 2) * 0.18
    bell = resonance([670, 1010, 1520], duration, [0.22, 0.12, 0.06], 0.0012) * adsr(duration, 0.01, 0.06, 0.08, 0.1)
    pulse = band_noise(duration, 900, 5000, 0.24, 2) * (0.3 + 0.7 * np.sin(2.0 * np.pi * 14.0 * t).astype(np.float32) ** 2) * 0.08
    output = reverse_shimmer + bell + pulse
    output = echo(output, [38, 84], [0.16, 0.09])
    return fade(normalize(soft_clip(output, 1.04), 0.84), 0.001, 0.03)


def render_stunned() -> np.ndarray:
    """
    渲染眩晕音效

    角色被击晕时的音效（0.38秒），模拟头部受击后的耳鸣感：
    - ring：带8Hz颤音的中频谐振（410/610/910Hz），模拟耳鸣
    - crack：高频冲击噪声（1400~7600Hz），模拟受击瞬间
    - tail：高频空气噪声尾部（2400~9200Hz），渐弱消散
    加上单次回声增加"回响"感。

    返回:
        眩晕音效的PCM信号
    """
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
    """
    渲染超级扣篮蓄力音效

    超级扣篮蓄力阶段的音效（0.68秒），充满能量感和爆发力：
    - rise_air：渐强的空气噪声（220~7000Hz），模拟能量聚集
    - sub：超低频滑音（94→42Hz），营造"地动山摇"的感觉
    - ember：渐强的中高频火花噪声（1500~9000Hz），模拟火星飞溅
    - crackle：在120/170/220/280ms处的四次短促火花声，模拟电弧
    - impact：在0.31秒处的重击音（160→58Hz滑音+低通噪声），模拟起跳踏地
    加上回声增加冲击力。

    返回:
        超级扣篮蓄力音效的PCM信号
    """
    duration = 0.68
    t = timebase(duration)
    rise_air = band_noise(duration, 220, 7000, 0.56, 2) * (np.linspace(0.0, 1.0, len(t), dtype=np.float32) ** 1.8) * 0.18
    sub = pitch_drop(duration, 94, 42, 0.58) * adsr(duration, 0.015, 0.09, 0.18, 0.18)
    ember = band_noise(duration, 1500, 9000, 0.54, 2) * np.clip(np.linspace(0.18, 1.0, len(t), dtype=np.float32), 0.0, 1.0) * 0.14
    # 四次短促火花声
    crackle = np.zeros_like(t)
    for center_ms in (120, 170, 220, 280):
        center = int(center_ms / 1000.0 * SAMPLE_RATE)
        width = int(0.012 * SAMPLE_RATE)
        left = max(0, center - width // 2)
        right = min(len(crackle), left + width)
        crackle[left:right] += band_noise((right - left) / SAMPLE_RATE, 2500, 11500, 0.8, 2)[: right - left] * np.hanning(right - left) * 0.18
    # 起跳踏地冲击音
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
    """
    渲染护盾音效

    护盾激活时的音效（0.6秒），模拟能量护盾展开的声音：
    - prefall：渐强的空气噪声（500~4200Hz），模拟护盾展开前的能量波动
    - slam：双频低频冲击（150→46Hz + 420→130Hz），模拟护盾瞬间成型
    - plate：金属谐振（380/690/1110Hz），赋予护盾"金属板"质感
    - arc：带调制的高频电弧噪声（2000~9800Hz），26Hz脉动，模拟能量流动
    加上回声增加空间感。

    返回:
        护盾音效的PCM信号
    """
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
    """
    渲染普通冲刺音效

    角色快速冲刺时的短促划空声（0.18秒）：
    - rush：宽带噪声冲击（480~9200Hz），快速起音快速衰减
    - step：低频滑音（180→80Hz），模拟脚步蹬地

    返回:
        冲刺音效的PCM信号
    """
    duration = 0.18
    rush = band_noise(duration, 480, 9200, 0.86, 2) * adsr(duration, 0.002, 0.035, 0.04, 0.035) * 0.74
    step = pitch_drop(duration, 180, 80, 0.12) * exp_decay(duration, 1.0, 0.008)
    output = rush + step
    return fade(normalize(soft_clip(output, 1.04), 0.86), 0.001, 0.02)


def render_super_dash() -> np.ndarray:
    """
    渲染超级冲刺音效

    比普通冲刺更强力的冲刺音效（0.44秒），用于持球高速突破：
    - rush：宽带噪声冲击（420~9200Hz），持续时间更长
    - engine：低频引擎声（230→74Hz），模拟高速运动的"轰鸣"
    - streak：高频划痕噪声（2500~12000Hz），增加速度感
    加上回声增加冲击力。

    返回:
        超级冲刺音效的PCM信号
    """
    duration = 0.44
    rush = band_noise(duration, 420, 9200, 0.88, 2) * adsr(duration, 0.003, 0.07, 0.12, 0.08) * 0.72
    engine = pitch_drop(duration, 230, 74, 0.28) * adsr(duration, 0.006, 0.08, 0.14, 0.12)
    streak = band_noise(duration, 2500, 12000, 0.26, 2) * exp_decay(duration, 1.0, 0.006) * 0.1
    output = rush + engine + streak
    output = echo(output, [28, 62], [0.12, 0.08])
    return fade(normalize(soft_clip(output, 1.04), 0.9), 0.001, 0.03)


def render_bsteel() -> np.ndarray:
    """
    渲染篮筐钢铁撞击音效

    球撞击篮筐金属框时的音效（0.21秒）：
    - thud：低频冲击（190→70Hz），模拟球体碰撞的"咚"声
    - knock：中高频敲击噪声（900~4200Hz），模拟金属碰撞的瞬态
    - edge：金属谐振（430/810/1290Hz），赋予"钢铁"质感

    返回:
        篮筐撞击音效的PCM信号
    """
    duration = 0.21
    thud = pitch_drop(duration, 190, 70, 0.4) * adsr(duration, 0.002, 0.04, 0.05, 0.04)
    knock = band_noise(duration, 900, 4200, 0.62, 2) * exp_decay(duration, 1.0, 0.00035) * 0.16
    edge = resonance([430, 810, 1290], duration, [0.16, 0.08, 0.04], 0.0013) * 0.24
    output = thud + knock + edge
    return fade(normalize(soft_clip(output, 1.02), 0.86), 0.001, 0.02)


def render_bring() -> np.ndarray:
    """
    渲染篮筐弹响音效

    球在篮筐上弹跳时的清脆金属声（0.33秒）：
    - ping：高频金属谐振（880/1310/1970/2640Hz），清脆的"叮"声
    - hammer：高频敲击噪声（1600~9200Hz），模拟碰撞瞬间
    - body：低频滑音（230→92Hz），赋予球体重量感
    加上回声增加金属共鸣。

    返回:
        篮筐弹响音效的PCM信号
    """
    duration = 0.33
    ping = resonance([880, 1310, 1970, 2640], duration, [0.34, 0.2, 0.11, 0.05], 0.0008) * 0.68
    hammer = band_noise(duration, 1600, 9200, 0.48, 2) * exp_decay(duration, 1.0, 0.00028) * 0.11
    body = pitch_drop(duration, 230, 92, 0.1) * adsr(duration, 0.002, 0.03, 0.02, 0.06)
    output = echo(ping + hammer + body, [31, 67], [0.12, 0.07])
    return fade(normalize(soft_clip(output, 1.03), 0.84), 0.001, 0.03)


def render_bbounce() -> np.ndarray:
    """
    渲染球弹地音效

    球在地板上弹跳时的音效（0.19秒）：
    - thump：低频冲击（170→54Hz），模拟球体撞击地面的"砰"声
    - skin：中频噪声（650~3200Hz），模拟球皮与地面的摩擦
    - air：高频空气噪声（1800~9200Hz），模拟空气被挤压

    返回:
        球弹地音效的PCM信号
    """
    duration = 0.19
    thump = pitch_drop(duration, 170, 54, 0.58) * adsr(duration, 0.001, 0.025, 0.03, 0.04)
    skin = band_noise(duration, 650, 3200, 0.6, 2) * exp_decay(duration, 1.0, 0.00045) * 0.16
    air = band_noise(duration, 1800, 9200, 0.22, 2) * adsr(duration, 0.001, 0.018, 0.01, 0.03) * 0.05
    output = thump + skin + air
    return fade(normalize(soft_clip(output, 1.02), 0.84), 0.001, 0.02)


def render_bnet() -> np.ndarray:
    """
    渲染球入网音效

    球穿过篮网时的"唰"声（0.16秒）：
    - swish：宽带噪声划空（900~8600Hz），快速起音快速衰减，模拟网绳划过
    - rope：低频绳索声（240~1800Hz），模拟网绳的振动
    - tail：高频尾部噪声（4200~12000Hz），模拟网绳末端的抖动
    加上短回声增加空间感。

    返回:
        球入网音效的PCM信号
    """
    duration = 0.16
    swish = band_noise(duration, 900, 8600, 0.76, 2) * adsr(duration, 0.001, 0.035, 0.02, 0.035) * 0.42
    rope = band_noise(duration, 240, 1800, 0.3, 2) * exp_decay(duration, 1.0, 0.003) * 0.06
    tail = band_noise(duration, 4200, 12000, 0.16, 2) * adsr(duration, 0.001, 0.02, 0.01, 0.025) * 0.04
    output = echo(swish + rope + tail, [18], [0.08])
    return fade(normalize(soft_clip(output, 1.01), 0.8), 0.001, 0.018)


def render_bbrick() -> np.ndarray:
    """
    渲染球砸框音效

    球砸中篮筐边缘（打铁）时的音效（0.24秒）：
    - knock：低频冲击（210→68Hz），模拟球体猛烈撞击金属框
    - crack：高频碎裂噪声（1200~6200Hz），模拟撞击的尖锐瞬态
    - edge：金属谐振（320/570/910Hz），赋予"打铁"的金属感
    - grit：高频砂砾噪声（2600~9800Hz），增加粗糙质感

    返回:
        球砸框音效的PCM信号
    """
    duration = 0.24
    knock = pitch_drop(duration, 210, 68, 0.46) * adsr(duration, 0.001, 0.035, 0.03, 0.05)
    crack = band_noise(duration, 1200, 6200, 0.68, 2) * exp_decay(duration, 1.0, 0.0003) * 0.18
    edge = resonance([320, 570, 910], duration, [0.18, 0.09, 0.05], 0.0012) * 0.18
    grit = band_noise(duration, 2600, 9800, 0.18, 2) * adsr(duration, 0.001, 0.03, 0.01, 0.035) * 0.05
    output = knock + crack + edge + grit
    return fade(normalize(soft_clip(output, 1.04), 0.86), 0.001, 0.025)


def render_bbasket() -> np.ndarray:
    """
    渲染球入筐音效

    球干净利落地穿过篮筐时的音效（0.27秒），是最令人满意的得分音效：
    - hoop：篮筐谐振（540/810/1220Hz），清脆的金属"叮"声
    - drop：低频下降音（140→52Hz），模拟球下落
    - net：中频网绳噪声（850~7200Hz），模拟球穿过网绳
    - pop：高频爆破噪声（2100~9800Hz），模拟球从网底弹出
    加上回声增加空间感和满足感。

    返回:
        球入筐音效的PCM信号
    """
    duration = 0.27
    hoop = resonance([540, 810, 1220], duration, [0.22, 0.11, 0.05], 0.0011) * 0.24
    drop = pitch_drop(duration, 140, 52, 0.4) * adsr(duration, 0.001, 0.03, 0.03, 0.05)
    net = band_noise(duration, 850, 7200, 0.52, 2) * adsr(duration, 0.001, 0.03, 0.02, 0.04) * 0.18
    pop = band_noise(duration, 2100, 9800, 0.22, 2) * exp_decay(duration, 1.0, 0.0008) * 0.06
    output = echo(hoop + drop + net + pop, [24, 52], [0.09, 0.05])
    return fade(normalize(soft_clip(output, 1.03), 0.84), 0.001, 0.025)


# ============================================================
# 音效注册表和导出
# ============================================================

# 音效名称到渲染函数的映射表
# 命名规则：序号_类别_名称
#   序号：用于排序
#   类别：UI=界面, M=比赛, P=玩家, B=篮球
RENDERS = {
    "20_ButtonSnd": render_button,       # UI按钮点击
    "19_M_Countdown": render_countdown,  # 比赛倒计时
    "2_M_Whistle": render_whistle,       # 裁判哨声
    "9_M_Buzzer": render_buzzer,         # 比赛蜂鸣器
    "4_P_Teleport": render_teleport,     # 玩家传送
    "5_P_Swoosh": render_swoosh,         # 玩家挥空
    "6_P_Energy": render_energy,         # 玩家充能
    "7_P_Stunned": render_stunned,       # 玩家眩晕
    "11_P_MegaStart": render_mega_start, # 超级扣篮蓄力
    "13_P_Shield": render_shield,        # 玩家护盾
    "17_P_Dash": render_dash,            # 玩家普通冲刺
    "18_P_SuperDash": render_super_dash, # 玩家超级冲刺
    "8_B_Steel": render_bsteel,          # 篮筐钢铁撞击
    "10_B_Ring": render_bring,           # 篮筐弹响
    "16_B_Bounce": render_bbounce,       # 篮球弹地
    "21_B_NET": render_bnet,             # 篮球入网
    "22_B_Brick": render_bbrick,         # 篮球砸框
    "23_B_Basket": render_bbasket,        # 篮球入筐
}


def write_wav(path: Path, signal_data: np.ndarray) -> None:
    """
    将浮点信号写入16位PCM WAV文件

    将-1~1范围的浮点信号转换为-32767~32767的16位整数，
    以44100Hz采样率写入WAV文件。

    参数:
        path: 输出文件路径
        signal_data: float32类型的信号数组（范围-1~1）
    """
    pcm = np.clip(signal_data, -1.0, 1.0)
    wavfile.write(path, SAMPLE_RATE, (pcm * 32767.0).astype(np.int16))


def main() -> None:
    """
    主函数：解析命令行参数并生成所有音效

    支持的命令行参数：
    - --out：输出目录（默认 Assets/mlp/Resources/mlp/Sound）

    遍历RENDERS注册表中的所有音效，逐一渲染并导出为WAV文件。
    """
    parser = argparse.ArgumentParser(description="生成万圣节核心音效集。")
    parser.add_argument(
        "--out",
        default="Assets/mlp/Resources/mlp/Sound",
        help="输出目录路径。",
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
