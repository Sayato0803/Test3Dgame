using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBattle
{
    public enum SfxId
    {
        Pop, Yelp, Thud, Whoosh, CameraWhoosh, Punch, Squish, Boing, SlideWhistle, Ding,
        CardHover, CardFlip, CardDeal, Deny, UiHover, UiClick, Chime, Fanfare, Tick, Splash, Poof, Magic, Slash, Zap
    }

    /// <summary>
    /// Hiệu ứng âm thanh cartoon tự tổng hợp bằng code (không cần file âm thanh, không lo bản quyền).
    /// Mỗi âm sinh 1 lần khi dùng lần đầu rồi cache. Phát qua pool AudioSource 2D; âm lượng tổng = AudioListener.volume (Cài đặt).
    /// </summary>
    public static class Sfx
    {
        private const int Rate = 44100;
        private static readonly Dictionary<SfxId, AudioClip> Clips = new();
        private static readonly List<AudioSource> Pool = new();
        private static GameObject _host;
        private static int _next;
        private static readonly System.Random Rng = new(1234);

        public static void Play(SfxId id, float volume = 1f, float pitchJitter = 0.06f, float delay = 0f)
        {
            if (!Application.isPlaying) return;
            AudioSource src = NextSource();
            src.clip = Get(id);
            src.volume = volume * BaseVolume(id);
            src.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            if (delay > 0f) src.PlayDelayed(delay);
            else src.Play();
        }

        private static float BaseVolume(SfxId id) => id switch
        {
            SfxId.CardHover or SfxId.UiHover or SfxId.Tick => 0.35f,
            SfxId.CameraWhoosh => 0.4f,
            SfxId.Squish or SfxId.Punch or SfxId.Boing => 0.9f,
            _ => 0.7f
        };

        private static AudioSource NextSource()
        {
            if (_host == null)
            {
                _host = new GameObject("Sfx");
                UnityEngine.Object.DontDestroyOnLoad(_host);
                Pool.Clear();
                for (int i = 0; i < 12; i++)
                {
                    var s = _host.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    s.spatialBlend = 0f;
                    Pool.Add(s);
                }
            }
            // Ưu tiên nguồn đang rảnh, hết thì cắt nguồn cũ nhất (xoay vòng).
            for (int i = 0; i < Pool.Count; i++)
            {
                AudioSource s = Pool[(_next + i) % Pool.Count];
                if (!s.isPlaying) { _next = (_next + i + 1) % Pool.Count; return s; }
            }
            AudioSource oldest = Pool[_next];
            _next = (_next + 1) % Pool.Count;
            return oldest;
        }

        private static AudioClip Get(SfxId id)
        {
            if (Clips.TryGetValue(id, out AudioClip c) && c != null) return c;
            float[] data = Synth(id);
            c = AudioClip.Create(id.ToString(), data.Length, 1, Rate, false);
            c.SetData(data, 0);
            Clips[id] = c;
            return c;
        }

        // ---------------- Tổng hợp ----------------

        private static float[] Synth(SfxId id) => id switch
        {
            // Pop: sine tụt cao độ nhanh.
            SfxId.Pop => Tone(0.11f, t => Mathf.Lerp(950f, 280f, Mathf.Sqrt(t / 0.11f)), Decay(25f)),
            // Yelp khi bị nhấc lên: vút lên + rung giọng.
            SfxId.Yelp => Tone(0.16f, t => Mathf.Lerp(480f, 1250f, t / 0.16f) + 60f * Mathf.Sin(t * 2 * Mathf.PI * 32f), Bell(0.16f)),
            SfxId.Thud => Mix(Tone(0.18f, t => Mathf.Lerp(130f, 45f, t / 0.18f), Decay(18f)), Noise(0.05f, 0.15f, Decay(60f)), 0.5f),
            SfxId.Whoosh => Noise(0.32f, 0.25f, Bell(0.32f), sweep: true),
            SfxId.CameraWhoosh => Noise(0.9f, 0.12f, Bell(0.9f), sweep: true),
            SfxId.Punch => Mix(Noise(0.07f, 0.6f, Decay(45f)), Tone(0.16f, t => Mathf.Lerp(190f, 55f, t / 0.16f), Decay(16f)), 0.9f),
            // Squish: tiếng "bẹp" ướt - noise trầm + sweep xuống, biên độ rung lắc.
            SfxId.Squish => Mix(
                Noise(0.38f, 0.08f, t => Decay(9f)(t) * (0.6f + 0.4f * Mathf.Sin(t * 2 * Mathf.PI * 26f))),
                Tone(0.38f, t => Mathf.Lerp(260f, 70f, Mathf.Pow(t / 0.38f, 0.5f)) + 25f * Mathf.Sin(t * 2 * Mathf.PI * 19f), Decay(7f)), 0.8f),
            // Boing: lò xo - cao độ dao động tắt dần.
            SfxId.Boing => Tone(0.6f, t => 210f + 230f * (1f - Mathf.Exp(-7f * t)) + 70f * Mathf.Sin(t * 2 * Mathf.PI * 13f) * Mathf.Exp(-3f * t), Decay(4.5f)),
            // Huýt sáo bay xa (King bị đánh văng).
            SfxId.SlideWhistle => Tone(1.1f, t => Mathf.Lerp(420f, 1700f, t / 1.1f) + 25f * Mathf.Sin(t * 2 * Mathf.PI * 7f), Bell(1.1f)),
            // Magic: chuỗi nốt lấp lánh đi lên + rung (tan rã do phép).
            SfxId.Magic => Mix(Notes(new[] { 880f, 1109f, 1319f, 1760f, 2217f, 2637f }, 0.07f, 0.6f),
                Tone(1.0f, t => 600f + 900f * t + 40f * Mathf.Sin(t * 2 * Mathf.PI * 18f), t => 0.35f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t))), 0.5f),
            // Slash: vút kiếm sắc + tiếng "keng" kim loại.
            SfxId.Slash => Mix(Noise(0.18f, 0.7f, Bell(0.18f), sweep: true), Tone(0.35f, _ => 2400f, Decay(14f)), 0.25f),
            // Zap: tia phép bắn ra - sine vút xuống có rung.
            SfxId.Zap => Tone(0.25f, t => Mathf.Lerp(1800f, 500f, t / 0.25f) + 120f * Mathf.Sin(t * 2 * Mathf.PI * 45f), Decay(9f)),
            // Poof: tiếng "bụp" khói mềm - noise trầm phồng lên rồi tắt + nốt trầm ngắn.
            SfxId.Poof => Mix(Noise(0.32f, 0.12f, t => Mathf.Clamp01(t * 40f) * Mathf.Exp(-9f * t)),
                Tone(0.18f, t => Mathf.Lerp(220f, 120f, t / 0.18f), Decay(18f)), 0.5f),
            // Splash: noise trầm bùng lên rồi lục bục bọt nước.
            SfxId.Splash => Mix(
                Noise(0.7f, 0.18f, t => Mathf.Exp(-5f * t) * Mathf.Clamp01(t * 60f)),
                Tone(0.5f, t => 300f + 500f * Mathf.Abs(Mathf.Sin(t * 2 * Mathf.PI * 9f)), t => 0.25f * Mathf.Exp(-6f * t)), 0.6f),
            SfxId.Ding => Mix(Tone(0.8f, _ => 1760f, Decay(6f)), Tone(0.8f, _ => 2637f, Decay(9f)), 0.5f),
            SfxId.CardHover => Tone(0.04f, _ => 1300f, Decay(80f)),
            SfxId.CardFlip => Mix(Noise(0.06f, 0.9f, Decay(70f)), Tone(0.08f, t => Mathf.Lerp(700f, 1100f, t / 0.08f), Decay(40f)), 0.6f),
            SfxId.CardDeal => Noise(0.14f, 0.5f, Bell(0.14f), sweep: true),
            SfxId.Deny => Square(0.22f, t => t < 0.1f ? 150f : 120f, t => (t < 0.09f || t > 0.12f) ? Decay(8f)(t) : 0f),
            SfxId.UiHover => Tone(0.035f, _ => 1500f, Decay(90f)),
            SfxId.UiClick => Tone(0.08f, t => Mathf.Lerp(620f, 980f, t / 0.08f), Decay(35f)),
            SfxId.Tick => Tone(0.03f, _ => 2200f, Decay(120f)),
            SfxId.Chime => Notes(new[] { 784f, 1047f }, 0.13f, 0.5f),
            SfxId.Fanfare => Notes(new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.12f, 0.7f),
            _ => new float[1]
        };

        private static Func<float, float> Decay(float k) => t => Mathf.Exp(-k * t);
        private static Func<float, float> Bell(float len) => t => Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / len));

        /// <summary>Sine có cao độ thay đổi theo thời gian (tích phân pha) + 1 hoạ âm nhẹ cho "tròn" tiếng.</summary>
        private static float[] Tone(float len, Func<float, float> freq, Func<float, float> env)
        {
            int n = (int)(len * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                phase += freq(t) / Rate;
                float s = Mathf.Sin((float)(phase * 2 * Math.PI)) + 0.25f * Mathf.Sin((float)(phase * 4 * Math.PI));
                d[i] = s * 0.6f * env(t) * Fade(i, n);
            }
            return d;
        }

        private static float[] Square(float len, Func<float, float> freq, Func<float, float> env)
        {
            int n = (int)(len * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                phase += freq(t) / Rate;
                float s = (phase % 1.0) < 0.5 ? 1f : -1f;
                d[i] = s * 0.3f * env(t) * Fade(i, n);
            }
            return d;
        }

        /// <summary>Noise qua bộ lọc 1 cực. cutoff 0..1 (nhỏ = trầm). sweep: cutoff tăng rồi giảm (tiếng vút).</summary>
        private static float[] Noise(float len, float cutoff, Func<float, float> env, bool sweep = false)
        {
            int n = (int)(len * Rate);
            var d = new float[n];
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float a = sweep ? Mathf.Lerp(cutoff * 0.2f, cutoff, Mathf.Sin(Mathf.PI * t / len)) : cutoff;
                float x = (float)(Rng.NextDouble() * 2.0 - 1.0);
                y += a * (x - y);
                d[i] = y * 0.9f / Mathf.Sqrt(Mathf.Max(a, 0.02f)) * env(t) * Fade(i, n);
            }
            return d;
        }

        /// <summary>Chuỗi nốt sine (arpeggio), nốt cuối ngân dài.</summary>
        private static float[] Notes(float[] freqs, float step, float tail)
        {
            float len = step * (freqs.Length - 1) + tail;
            int n = (int)(len * Rate);
            var d = new float[n];
            for (int k = 0; k < freqs.Length; k++)
            {
                int start = (int)(k * step * Rate);
                float noteLen = k == freqs.Length - 1 ? tail : step * 1.6f;
                int count = Mathf.Min((int)(noteLen * Rate), n - start);
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)Rate;
                    float s = Mathf.Sin(2 * Mathf.PI * freqs[k] * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * freqs[k] * t);
                    d[start + i] += s * 0.35f * Mathf.Exp(-5f * t) * Mathf.Clamp01(t * 200f);
                }
            }
            return d;
        }

        private static float[] Mix(float[] a, float[] b, float bGain)
        {
            var d = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < d.Length; i++)
                d[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] * bGain : 0f);
            return d;
        }

        /// <summary>Fade 3ms đầu/cuối để không bị tiếng "lụp bụp".</summary>
        private static float Fade(int i, int n)
        {
            const int f = 132;
            return Mathf.Clamp01(i / (float)f) * Mathf.Clamp01((n - 1 - i) / (float)f);
        }
    }
}
