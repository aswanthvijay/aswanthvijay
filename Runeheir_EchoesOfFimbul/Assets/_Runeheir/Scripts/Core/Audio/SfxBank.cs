using System;
using Runeheir.Combat;

namespace Runeheir.Audio
{
    /// <summary>Every procedural sound effect the game plays.</summary>
    public enum Sfx
    {
        SwingLight, SwingHeavy, Hit, HitHeavy, Critical, Miss, Block, BowShot, ArrowHit, ShotFired,
        CastCharge, CastRelease, Fire, Frost, Lightning, Holy, Shadow, Earth, Wind, Poison, Ghost,
        Heal, Buff, Debuff, LevelUp, JobLevelUp, Pickup, Zeny, Equip, UiClick, UiOpen, UiClose, UiError,
        Portal, Death, Revive, RefineSuccess, RefineFail, RefineShatter, MonsterDeath, BossRoar, MvpFanfare, Stagger,
    }

    /// <summary>
    /// Builds each <see cref="Sfx"/> from synthesis recipes (no recorded audio anywhere in the game). Deterministic: the
    /// same effect sounds the same on every machine.
    /// </summary>
    public static class SfxBank
    {
        public const int Rate = 44100;

        public static Sfx ForElement(Element element)
        {
            switch (element)
            {
                case Element.Fire: return Sfx.Fire;
                case Element.Water: return Sfx.Frost;
                case Element.Wind: return Sfx.Lightning;
                case Element.Earth: return Sfx.Earth;
                case Element.Holy: return Sfx.Holy;
                case Element.Shadow:
                case Element.Undead: return Sfx.Shadow;
                case Element.Poison: return Sfx.Poison;
                case Element.Ghost: return Sfx.Ghost;
                default: return Sfx.CastRelease;
            }
        }

        public static float[] Build(Sfx sfx)
        {
            var random = new Random(1000 + (int)sfx);
            float[] s;
            switch (sfx)
            {
                case Sfx.SwingLight: s = Whoosh(random, 0.18f, 900f, 2600f, 0.6f); break;
                case Sfx.SwingHeavy: s = Whoosh(random, 0.32f, 350f, 1300f, 0.9f); break;
                case Sfx.Hit: s = Impact(random, 0.18f, 140f, 0.55f); break;
                case Sfx.HitHeavy: s = Impact(random, 0.32f, 90f, 0.8f); break;
                case Sfx.Critical: s = Mix(Impact(random, 0.3f, 110f, 0.9f), Synth.Bell(1320f, 0.5f, Rate, 1.2f), 0.35f); break;
                case Sfx.Miss: s = Whoosh(random, 0.2f, 1400f, 3200f, 0.35f); break;
                case Sfx.Block: s = Mix(Synth.Bell(620f, 0.35f, Rate, 0.6f), Impact(random, 0.12f, 220f, 0.5f), 0.7f); break;
                case Sfx.BowShot: s = Mix(Twang(random, 220f, 0.35f), Whoosh(random, 0.25f, 2200f, 900f, 0.4f), 0.7f); break;
                case Sfx.ArrowHit: s = Impact(random, 0.15f, 260f, 0.45f); break;
                case Sfx.ShotFired: s = Mix(Crack(random, 0.25f), Impact(random, 0.2f, 70f, 0.6f), 0.8f); break;
                case Sfx.CastCharge: s = Shimmer(random, 0.6f, 300f, 900f); break;
                case Sfx.CastRelease: s = Mix(Shimmer(random, 0.35f, 900f, 500f), Whoosh(random, 0.3f, 700f, 2000f, 0.5f), 0.8f); break;
                case Sfx.Fire: s = Roar(random, 0.6f, 500f); break;
                case Sfx.Frost: s = Mix(Glass(random, 0.7f), Whoosh(random, 0.4f, 3000f, 6000f, 0.3f), 0.7f); break;
                case Sfx.Lightning: s = Mix(Crack(random, 0.5f), Rumble(random, 0.8f), 0.6f); break;
                case Sfx.Holy: s = Chord(new[] { 76f, 80f, 83f, 88f }, 0.9f, 0.04f); break;
                case Sfx.Shadow: s = Swell(random, 0.7f, 110f); break;
                case Sfx.Earth: s = Mix(Rumble(random, 0.7f), Impact(random, 0.4f, 60f, 1f), 0.9f); break;
                case Sfx.Wind: s = Whoosh(random, 0.6f, 500f, 3500f, 0.7f); break;
                case Sfx.Poison: s = Bubbles(random, 0.6f); break;
                case Sfx.Ghost: s = Mix(Swell(random, 0.6f, 330f), Shimmer(random, 0.6f, 1200f, 600f), 0.6f); break;
                case Sfx.Heal: s = Chord(new[] { 72f, 76f, 79f, 84f }, 0.8f, 0.07f); break;
                case Sfx.Buff: s = Mix(Chord(new[] { 67f, 71f, 74f, 79f }, 0.7f, 0.05f), Shimmer(random, 0.6f, 600f, 1800f), 0.4f); break;
                case Sfx.Debuff: s = Chord(new[] { 64f, 61f, 58f }, 0.6f, 0.08f); break;
                case Sfx.LevelUp: s = Fanfare(random, new[] { 67f, 71f, 74f, 79f, 83f }, 0.11f, 1.6f); break;
                case Sfx.JobLevelUp: s = Fanfare(random, new[] { 72f, 76f, 79f }, 0.09f, 1.0f); break;
                case Sfx.Pickup: s = Mix(Synth.Bell(1568f, 0.25f, Rate, 0.4f), Click(random, 0.03f), 0.5f); break;
                case Sfx.Zeny: s = Coins(random); break;
                case Sfx.Equip: s = Mix(Click(random, 0.05f), Synth.Bell(880f, 0.18f, Rate, 0.3f), 0.4f); break;
                case Sfx.UiClick: s = Click(random, 0.025f); break;
                case Sfx.UiOpen: s = Mix(Whoosh(random, 0.12f, 900f, 2400f, 0.3f), Synth.Bell(1046f, 0.15f, Rate, 0.2f), 0.4f); break;
                case Sfx.UiClose: s = Whoosh(random, 0.1f, 2200f, 800f, 0.3f); break;
                case Sfx.UiError: s = Chord(new[] { 52f, 53f }, 0.25f, 0f); break;
                case Sfx.Portal: s = Mix(Shimmer(random, 1.1f, 200f, 1600f), Whoosh(random, 1.0f, 300f, 2400f, 0.5f), 0.8f); break;
                case Sfx.Death: s = Descend(random, 1.2f); break;
                case Sfx.Revive: s = Chord(new[] { 60f, 67f, 72f, 76f, 79f, 84f }, 1.4f, 0.09f); break;
                case Sfx.RefineSuccess: s = Mix(Anvil(random), Chord(new[] { 79f, 83f, 86f }, 0.8f, 0.06f), 0.5f); break;
                case Sfx.RefineFail: s = Mix(Anvil(random), Impact(random, 0.4f, 60f, 0.7f), 0.8f); break;
                case Sfx.RefineShatter: s = Mix(Glass(random, 0.9f), Crack(random, 0.4f), 0.9f); break;
                case Sfx.MonsterDeath: s = Mix(Impact(random, 0.25f, 80f, 0.6f), Whoosh(random, 0.4f, 1600f, 400f, 0.3f), 0.6f); break;
                case Sfx.BossRoar: s = Roar(random, 1.4f, 160f); break;
                case Sfx.MvpFanfare: s = Mix(Synth.Horn(Synth.Midi(50f), 1.8f, Rate), Fanfare(random, new[] { 62f, 66f, 69f, 74f }, 0.16f, 1.8f), 0.6f); break;
                case Sfx.Stagger: s = Mix(Impact(random, 0.3f, 120f, 0.8f), Synth.Bell(440f, 0.4f, Rate, 0.4f), 0.4f); break;
                default: s = Click(random, 0.03f); break;
            }

            Synth.Fade(s, Rate, 0.003f);
            Synth.Normalize(s, 0.85f);
            return s;
        }

        // ---------------------------------------------------------------- recipes
        private static float[] Whoosh(Random random, float seconds, float from, float to, float loudness)
        {
            var s = Synth.Noise((int)(seconds * Rate), random);
            Synth.BandSweep(s, from, to, 2.2f, Rate);
            for (int i = 0; i < s.Length; i++)
            {
                float t = (float)i / s.Length;
                s[i] *= loudness * (float)Math.Sin(Math.PI * t);
            }

            return s;
        }

        private static float[] Impact(Random random, float seconds, float thump, float weight)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                phase += thump * (1f + 1.5f * (float)Math.Exp(-t * 40f)) / Rate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                s[i] = Synth.Sine(phase) * Synth.Perc(t, 0.001f, seconds * 0.35f) * weight + noise * Synth.Perc(t, 0.0005f, 0.015f) * 0.6f;
            }

            Synth.LowPass(s, 3500f, Rate);
            return s;
        }

        private static float[] Twang(Random random, float frequency, float seconds)
        {
            var s = Synth.Pluck(frequency, seconds, Rate, random, 0.85f, 0.985f);
            Synth.LowPass(s, 2500f, Rate);
            return s;
        }

        private static float[] Crack(Random random, float seconds)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float flicker = random.NextDouble() < 0.02 ? 1.6f : 1f;
                s[i] = noise * Synth.Perc(t, 0.0005f, 0.06f) * flicker;
            }

            Synth.HighPass(s, 900f, Rate);
            return s;
        }

        private static float[] Rumble(Random random, float seconds)
        {
            var s = Synth.Noise((int)(seconds * Rate), random);
            Synth.LowPass(s, 120f, Rate);
            Synth.LowPass(s, 160f, Rate);
            for (int i = 0; i < s.Length; i++)
            {
                s[i] *= Synth.Perc((float)i / Rate, 0.02f, seconds * 0.4f) * 6f;
            }

            return s;
        }

        private static float[] Roar(Random random, float seconds, float body)
        {
            var s = Synth.Noise((int)(seconds * Rate), random);
            Synth.BandSweep(s, body * 2f, body * 0.6f, 1.2f, Rate);
            int n = s.Length;
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                phase += body * 0.5f / Rate;
                float crackle = random.NextDouble() < 0.004 ? (float)(random.NextDouble() * 2.0 - 1.0) : 0f;
                s[i] = (s[i] * 1.4f + Synth.Saw(phase, 6) * 0.3f + crackle) * Synth.Adsr(t, 0.05f, 0.2f, 0.7f, seconds * 0.6f, seconds * 0.4f);
            }

            Synth.Drive(s, 2f);
            return s;
        }

        private static float[] Glass(Random random, float seconds)
        {
            var s = new float[(int)(seconds * Rate)];
            for (int k = 0; k < 6; k++)
            {
                float f = 2200f + (float)random.NextDouble() * 3800f;
                var bell = Synth.Bell(f, seconds * (0.4f + 0.6f * (float)random.NextDouble()), Rate, 0.5f);
                Synth.MixInto(s, bell, (int)(random.NextDouble() * 0.06 * Rate), 0.35f);
            }

            return s;
        }

        private static float[] Shimmer(Random random, float seconds, float from, float to)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            double[] phases = new double[5];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float f = from * (float)Math.Pow(to / from, t / seconds);
                float sum = 0f;
                for (int k = 0; k < phases.Length; k++)
                {
                    phases[k] += f * (1f + k * 0.5f + 0.003f * k) / Rate;
                    sum += Synth.Sine(phases[k]) / (k + 1);
                }

                s[i] = sum * (float)Math.Sin(Math.PI * t / seconds) * (0.7f + 0.3f * (float)Math.Sin(Synth.TwoPi * 11f * t));
            }

            return s;
        }

        private static float[] Swell(Random random, float seconds, float frequency)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                phase += frequency * (1f - 0.3f * t / seconds) / Rate;
                float env = (float)Math.Pow(t / seconds, 1.5) * Synth.Perc(seconds - t, 0.0f, 0.08f);
                s[i] = (Synth.Saw(phase, 5) * 0.6f + (float)(random.NextDouble() * 2.0 - 1.0) * 0.2f) * env;
            }

            Synth.LowPass(s, 900f, Rate);
            return s;
        }

        private static float[] Bubbles(Random random, float seconds)
        {
            var s = new float[(int)(seconds * Rate)];
            for (int k = 0; k < 9; k++)
            {
                int start = (int)(random.NextDouble() * (seconds - 0.12) * Rate);
                float f = 300f + (float)random.NextDouble() * 500f;
                int n = (int)(0.08f * Rate);
                var pop = new float[n];
                double phase = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    phase += f * (1f + t * 12f) / Rate;
                    pop[i] = Synth.Sine(phase) * Synth.Perc(t, 0.002f, 0.025f);
                }

                Synth.MixInto(s, pop, start, 0.6f);
            }

            return s;
        }

        private static float[] Click(Random random, float seconds)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                s[i] = (Synth.Sine(1800.0 * t) * 0.6f + (float)(random.NextDouble() * 2.0 - 1.0) * 0.3f) * Synth.Perc(t, 0.0005f, seconds * 0.3f);
            }

            return s;
        }

        private static float[] Coins(Random random)
        {
            var s = new float[(int)(0.6f * Rate)];
            for (int k = 0; k < 5; k++)
            {
                var c = Synth.Bell(2600f + (float)random.NextDouble() * 1500f, 0.25f, Rate, 1.3f);
                Synth.MixInto(s, c, (int)((0.03 + k * 0.07 + random.NextDouble() * 0.03) * Rate), 0.5f);
            }

            return s;
        }

        private static float[] Anvil(Random random)
        {
            return Mix(Synth.Bell(1180f, 0.9f, Rate, 1.4f), Impact(random, 0.2f, 180f, 0.6f), 0.7f);
        }

        private static float[] Chord(float[] notes, float seconds, float strum)
        {
            var s = new float[(int)((seconds + strum * notes.Length) * Rate)];
            for (int k = 0; k < notes.Length; k++)
            {
                var b = Synth.Bell(Synth.Midi(notes[k]), seconds, Rate, 0.7f);
                Synth.MixInto(s, b, (int)(k * strum * Rate), 0.5f);
            }

            return s;
        }

        private static float[] Fanfare(Random random, float[] notes, float step, float seconds)
        {
            var s = new float[(int)(seconds * Rate)];
            for (int k = 0; k < notes.Length; k++)
            {
                var horn = Synth.Horn(Synth.Midi(notes[k]), Math.Max(0.25f, seconds - k * step), Rate);
                Synth.MixInto(s, horn, (int)(k * step * Rate), 0.35f);
                var bell = Synth.Bell(Synth.Midi(notes[k] + 12f), 0.6f, Rate, 0.6f);
                Synth.MixInto(s, bell, (int)(k * step * Rate), 0.25f);
            }

            return s;
        }

        private static float[] Descend(Random random, float seconds)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                phase += 220f * (float)Math.Pow(0.4, t / seconds) / Rate;
                s[i] = (Synth.Saw(phase, 6) * 0.5f + Synth.Sine(phase * 0.5) * 0.5f) * Synth.Adsr(t, 0.02f, 0.3f, 0.6f, seconds * 0.6f, seconds * 0.4f);
            }

            Synth.LowPass(s, 1200f, Rate);
            return s;
        }

        private static float[] Mix(float[] a, float[] b, float gainB)
        {
            var s = new float[Math.Max(a.Length, b.Length)];
            Synth.MixInto(s, a, 0, 1f);
            Synth.MixInto(s, b, 0, gainB);
            return s;
        }
    }
}
