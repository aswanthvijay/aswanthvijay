using System;

namespace Runeheir.Audio
{
    /// <summary>
    /// Tiny offline synthesizer for Runeheir's procedural audio: oscillators, envelopes, filters, plucked strings
    /// (Karplus–Strong), drums and a small reverb. Everything renders into float buffers (−1..1, mono) without Unity, so
    /// clips can be built on a worker thread and are the same on every machine (fixed seeds).
    /// </summary>
    public static class Synth
    {
        public const float TwoPi = (float)(Math.PI * 2.0);

        public static float Midi(float note)
        {
            return 440f * (float)Math.Pow(2.0, (note - 69f) / 12.0);
        }

        // ---------------------------------------------------------------- oscillators (phase in cycles)
        public static float Sine(double phase)
        {
            return (float)Math.Sin(phase * Math.PI * 2.0);
        }

        public static float Triangle(double phase)
        {
            double p = phase - Math.Floor(phase);
            return (float)(p < 0.5 ? 4.0 * p - 1.0 : 3.0 - 4.0 * p);
        }

        /// <summary>A sawtooth from its first <paramref name="harmonics"/> partials (no aliasing at our sample rates).</summary>
        public static float Saw(double phase, int harmonics)
        {
            double sum = 0;
            for (int h = 1; h <= harmonics; h++)
            {
                sum += Math.Sin(phase * Math.PI * 2.0 * h) / h;
            }

            return (float)(sum * 0.55);
        }

        public static float Square(double phase, int harmonics)
        {
            double sum = 0;
            for (int h = 1; h <= harmonics; h += 2)
            {
                sum += Math.Sin(phase * Math.PI * 2.0 * h) / h;
            }

            return (float)(sum * 0.8);
        }

        // ---------------------------------------------------------------- envelopes
        /// <summary>Attack–decay–sustain–release for a note held <paramref name="hold"/> seconds.</summary>
        public static float Adsr(float t, float attack, float decay, float sustain, float hold, float release)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / Math.Max(1e-5f, attack);
            if (t < attack + decay) return 1f - (1f - sustain) * (t - attack) / Math.Max(1e-5f, decay);
            if (t < hold) return sustain;
            float r = (t - hold) / Math.Max(1e-5f, release);
            return r >= 1f ? 0f : sustain * (1f - r);
        }

        /// <summary>A plain exponential decay with a short attack: drums, plucks, impacts.</summary>
        public static float Perc(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / Math.Max(1e-5f, attack);
            return (float)Math.Exp(-(t - attack) / Math.Max(1e-5f, decay));
        }

        // ---------------------------------------------------------------- filters (in place)
        public static void LowPass(float[] buffer, float cutoff, int rate)
        {
            float a = (float)Math.Exp(-TwoPi * cutoff / rate);
            float y = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                y = buffer[i] * (1f - a) + y * a;
                buffer[i] = y;
            }
        }

        public static void HighPass(float[] buffer, float cutoff, int rate)
        {
            float a = (float)Math.Exp(-TwoPi * cutoff / rate);
            float low = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                low = buffer[i] * (1f - a) + low * a;
                buffer[i] -= low;
            }
        }

        /// <summary>A resonant band-pass (state-variable filter) whose centre can sweep from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static void BandSweep(float[] buffer, float from, float to, float q, int rate)
        {
            float low = 0f, band = 0f;
            float damp = 1f / Math.Max(0.5f, q);
            for (int i = 0; i < buffer.Length; i++)
            {
                float t = (float)i / Math.Max(1, buffer.Length - 1);
                float f = from * (float)Math.Pow(to / from, t);
                float k = 2f * (float)Math.Sin(Math.PI * Math.Min(f, rate * 0.24f) / rate);
                low += k * band;
                float high = buffer[i] - low - damp * band;
                band += k * high;
                buffer[i] = band;
            }
        }

        /// <summary>Soft saturation: warmth on bass and horns.</summary>
        public static void Drive(float[] buffer, float amount)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (float)Math.Tanh(buffer[i] * amount) / (float)Math.Tanh(amount);
            }
        }

        // ---------------------------------------------------------------- mixing
        public static void MixInto(float[] target, float[] source, int offset, float gain)
        {
            for (int i = 0; i < source.Length; i++)
            {
                int j = i + offset;
                if (j >= 0 && j < target.Length)
                {
                    target[j] += source[i] * gain;
                }
            }
        }

        /// <summary>Scales the buffer so its loudest sample sits at <paramref name="peak"/>.</summary>
        public static void Normalize(float[] buffer, float peak)
        {
            float max = 1e-6f;
            foreach (float s in buffer)
            {
                max = Math.Max(max, Math.Abs(s));
            }

            float g = peak / max;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= g;
            }
        }

        /// <summary>Short fades at both ends so clips never click.</summary>
        public static void Fade(float[] buffer, int rate, float seconds = 0.005f)
        {
            int n = Math.Min(buffer.Length / 2, (int)(rate * seconds));
            for (int i = 0; i < n; i++)
            {
                float g = (float)i / n;
                buffer[i] *= g;
                buffer[buffer.Length - 1 - i] *= g;
            }
        }

        // ---------------------------------------------------------------- instruments
        /// <summary>
        /// Karplus–Strong plucked string (lyre, kantele, harp): a noise burst in a delay line, softened each pass.
        /// <paramref name="brightness"/> 0..1 sets the pluck's attack; <paramref name="decay"/> near 1 rings longer.
        /// </summary>
        public static float[] Pluck(float frequency, float seconds, int rate, Random random, float brightness = 0.6f, float decay = 0.996f)
        {
            int n = (int)(seconds * rate);
            var output = new float[n];
            int period = Math.Max(2, (int)(rate / Math.Max(20f, frequency)));
            var line = new float[period];
            for (int i = 0; i < period; i++)
            {
                line[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }

            // Soften the excitation for darker strings.
            for (int pass = 0; pass < (int)((1f - brightness) * 6f); pass++)
            {
                for (int i = 1; i < period; i++)
                {
                    line[i] = (line[i] + line[i - 1]) * 0.5f;
                }
            }

            int idx = 0;
            float previous = 0f;
            for (int i = 0; i < n; i++)
            {
                float current = line[idx];
                float next = (current + previous) * 0.5f * decay;
                previous = current;
                line[idx] = next;
                output[i] = current;
                idx = (idx + 1) % period;
            }

            Fade(output, rate, 0.002f);
            return output;
        }

        /// <summary>A breathy flute: sine with a little second harmonic, vibrato and filtered breath noise.</summary>
        public static float[] Flute(float frequency, float seconds, int rate, Random random, float vibrato = 0.006f)
        {
            int n = (int)(seconds * rate);
            var output = new float[n];
            double phase = 0;
            float breath = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float vib = 1f + vibrato * (float)Math.Sin(TwoPi * 5.2f * t) * Math.Min(1f, t * 2f);
                phase += frequency * vib / rate;
                float env = Adsr(t, 0.06f, 0.1f, 0.85f, seconds - 0.12f, 0.12f);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                breath = breath * 0.9f + noise * 0.1f;
                output[i] = env * (Sine(phase) * 0.8f + Sine(phase * 2) * 0.12f + breath * 0.18f);
            }

            return output;
        }

        /// <summary>A bowed drone (the nyckelharpa's sympathetic hum): a soft sawtooth with slow swell and tremolo.</summary>
        public static float[] Drone(float frequency, float seconds, int rate, int harmonics = 8, float tremolo = 0.08f)
        {
            int n = (int)(seconds * rate);
            var output = new float[n];
            double phase = 0, phase2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                phase += frequency / rate;
                phase2 += frequency * 1.003f / rate; // a second bow, slightly detuned
                float env = Adsr(t, 1.2f, 0.5f, 0.85f, seconds - 1.5f, 1.5f);
                float trem = 1f - tremolo * (0.5f + 0.5f * (float)Math.Sin(TwoPi * 0.31f * t));
                output[i] = env * trem * (Saw(phase, harmonics) + Saw(phase2, harmonics)) * 0.5f;
            }

            return output;
        }

        /// <summary>A war horn: slow-attack brassy tone, low-passed and driven.</summary>
        public static float[] Horn(float frequency, float seconds, int rate)
        {
            int n = (int)(seconds * rate);
            var output = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float bend = 1f - 0.03f * (float)Math.Exp(-t * 6f);
                phase += frequency * bend / rate;
                float env = Adsr(t, 0.18f, 0.2f, 0.8f, seconds - 0.3f, 0.3f);
                output[i] = env * Saw(phase, 10);
            }

            LowPass(output, 1400f, rate);
            Drive(output, 1.8f);
            return output;
        }

        /// <summary>A sub bass: a sine with a hint of its octave.</summary>
        public static float[] Bass(float frequency, float seconds, int rate)
        {
            int n = (int)(seconds * rate);
            var output = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                phase += frequency / rate;
                float env = Adsr(t, 0.01f, 0.15f, 0.7f, seconds - 0.05f, 0.05f);
                output[i] = env * (Sine(phase) + 0.25f * Sine(phase * 2));
            }

            return output;
        }

        /// <summary>A bell: inharmonic partials with long decay (holy magic, coins, level-ups).</summary>
        public static float[] Bell(float frequency, float seconds, int rate, float brightness = 1f)
        {
            float[] ratios = { 1f, 2.0f, 2.76f, 5.4f, 8.93f };
            float[] gains = { 1f, 0.6f, 0.45f * brightness, 0.25f * brightness, 0.12f * brightness };
            int n = (int)(seconds * rate);
            var output = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float s = 0f;
                for (int k = 0; k < ratios.Length; k++)
                {
                    s += gains[k] * (float)Math.Sin(TwoPi * frequency * ratios[k] * t) * (float)Math.Exp(-t * (1.6f + k * 1.4f) / seconds);
                }

                output[i] = s * Perc(t, 0.002f, seconds);
            }

            return output;
        }

        // ---------------------------------------------------------------- drums
        public static float[] Kick(int rate, float punch = 1f)
        {
            int n = (int)(0.45f * rate);
            var output = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float f = 45f + 110f * punch * (float)Math.Exp(-t * 30f);
                phase += f / rate;
                output[i] = Sine(phase) * Perc(t, 0.001f, 0.18f);
            }

            Drive(output, 1.5f);
            return output;
        }

        public static float[] Snare(int rate, Random random)
        {
            int n = (int)(0.25f * rate);
            var output = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                phase += 185f / rate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                output[i] = noise * Perc(t, 0.001f, 0.07f) * 0.8f + Sine(phase) * Perc(t, 0.001f, 0.05f) * 0.5f;
            }

            HighPass(output, 400f, rate);
            return output;
        }

        public static float[] Hat(int rate, Random random, float length = 0.05f)
        {
            int n = (int)(Math.Max(0.02f, length) * 2.5f * rate);
            var output = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                output[i] = (float)(random.NextDouble() * 2.0 - 1.0) * Perc(t, 0.0005f, length);
            }

            HighPass(output, 6000f, rate);
            return output;
        }

        /// <summary>A Norse frame drum: a low membrane thump with a skin slap.</summary>
        public static float[] FrameDrum(int rate, Random random, float pitch = 1f)
        {
            int n = (int)(0.6f * rate);
            var output = new float[n];
            double phase = 0;
            float slap = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                phase += (70f + 50f * (float)Math.Exp(-t * 18f)) * pitch / rate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                slap = slap * 0.6f + noise * 0.4f;
                output[i] = Sine(phase) * Perc(t, 0.002f, 0.22f) + slap * Perc(t, 0.001f, 0.02f) * 0.5f;
            }

            return output;
        }

        // ---------------------------------------------------------------- space
        /// <summary>A small Schroeder reverb (four combs, two all-passes) mixed under the dry signal.</summary>
        public static float[] Reverb(float[] dry, int rate, float wet = 0.3f, float room = 0.82f)
        {
            int[] combs = { 1116, 1188, 1277, 1356 };
            int[] passes = { 556, 441 };
            float scale = rate / 44100f;
            int tail = (int)(rate * 1.5f);
            var output = new float[dry.Length + tail];
            var wetBuf = new float[output.Length];
            foreach (int c in combs)
            {
                int len = Math.Max(8, (int)(c * scale));
                var line = new float[len];
                int idx = 0;
                float filter = 0f;
                for (int i = 0; i < output.Length; i++)
                {
                    float x = i < dry.Length ? dry[i] : 0f;
                    float y = line[idx];
                    filter = y * 0.8f + filter * 0.2f;
                    line[idx] = x + filter * room;
                    idx = (idx + 1) % len;
                    wetBuf[i] += y * 0.25f;
                }
            }

            foreach (int p in passes)
            {
                int len = Math.Max(8, (int)(p * scale));
                var line = new float[len];
                int idx = 0;
                for (int i = 0; i < wetBuf.Length; i++)
                {
                    float buffered = line[idx];
                    float x = wetBuf[i];
                    line[idx] = x + buffered * 0.5f;
                    wetBuf[i] = buffered - x;
                    idx = (idx + 1) % len;
                }
            }

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = (i < dry.Length ? dry[i] : 0f) * (1f - wet * 0.5f) + wetBuf[i] * wet;
            }

            return output;
        }

        public static float[] Noise(int samples, Random random)
        {
            var output = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                output[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }

            return output;
        }
    }
}
