using System;
using System.Collections.Generic;

namespace Runeheir.Audio
{
    /// <summary>What the music should feel like (picked from the map's theme and what's happening).</summary>
    public enum MusicMood
    {
        Title, Town, Meadow, Forest, Cold, Dungeon, Arena, Boss,

        // Phase 8 themes (added last so the older moods keep their seeds).
        Desert, Volcano, Jungle, Coast,
    }

    /// <summary>
    /// GDD: "Nordic folk × drum-and-bass". Composes a seamless loop per mood from a seed: a modal chord progression,
    /// drones, a folk melody (flute or plucked lyre), arpeggios, bass and percussion — frame drums for the north,
    /// a 172 BPM break for bosses. All synthesized (see <see cref="Synth"/>); nothing is recorded.
    /// </summary>
    public static class MusicComposer
    {
        public const int Rate = 32000;

        private static readonly int[] Aeolian = { 0, 2, 3, 5, 7, 8, 10 };
        private static readonly int[] Dorian = { 0, 2, 3, 5, 7, 9, 10 };
        private static readonly int[] Phrygian = { 0, 1, 3, 5, 7, 8, 10 };
        private static readonly int[] PhrygianDominant = { 0, 1, 4, 5, 7, 8, 10 };
        private static readonly int[] Mixolydian = { 0, 2, 4, 5, 7, 9, 10 };

        private sealed class Plan
        {
            public float Bpm;
            public int Bars;
            public int Root;
            public int[] Mode;
            public int[] Progression;
            public string Lead;     // "flute", "lyre", "harp", "horn", "none"
            public string Arp;      // "kantele", "harp", "none"
            public string Drums;    // "none", "frame", "shaker", "heart", "dnb", "war"
            public bool Drone;
            public bool Bass;
            public float Reverb;
            public float Density;   // melody notes per beat
        }

        private static Plan PlanFor(MusicMood mood)
        {
            switch (mood)
            {
                case MusicMood.Title:
                    return new Plan { Bpm = 70, Bars = 16, Root = 50, Mode = Dorian, Progression = new[] { 0, 6, 3, 0, 5, 6, 0, 4 }, Lead = "harp", Arp = "none", Drums = "none", Drone = true, Bass = false, Reverb = 0.45f, Density = 0.75f };
                case MusicMood.Town:
                    return new Plan { Bpm = 92, Bars = 16, Root = 55, Mode = Dorian, Progression = new[] { 0, 3, 6, 0, 0, 3, 4, 6 }, Lead = "lyre", Arp = "kantele", Drums = "frame", Drone = true, Bass = true, Reverb = 0.3f, Density = 1.2f };
                case MusicMood.Meadow:
                    return new Plan { Bpm = 100, Bars = 16, Root = 57, Mode = Dorian, Progression = new[] { 0, 6, 3, 4, 0, 6, 5, 6 }, Lead = "flute", Arp = "kantele", Drums = "shaker", Drone = false, Bass = true, Reverb = 0.3f, Density = 1.0f };
                case MusicMood.Forest:
                    return new Plan { Bpm = 88, Bars = 16, Root = 52, Mode = Aeolian, Progression = new[] { 0, 5, 2, 6, 0, 5, 3, 4 }, Lead = "flute", Arp = "harp", Drums = "frame", Drone = true, Bass = true, Reverb = 0.38f, Density = 0.9f };
                case MusicMood.Cold:
                    return new Plan { Bpm = 76, Bars = 16, Root = 49, Mode = Aeolian, Progression = new[] { 0, 0, 5, 5, 3, 3, 6, 4 }, Lead = "harp", Arp = "harp", Drums = "none", Drone = true, Bass = false, Reverb = 0.5f, Density = 0.6f };
                case MusicMood.Dungeon:
                    return new Plan { Bpm = 72, Bars = 16, Root = 45, Mode = Phrygian, Progression = new[] { 0, 1, 0, 6, 0, 1, 5, 6 }, Lead = "harp", Arp = "none", Drums = "heart", Drone = true, Bass = true, Reverb = 0.55f, Density = 0.45f };
                case MusicMood.Desert:
                    return new Plan { Bpm = 94, Bars = 16, Root = 50, Mode = PhrygianDominant, Progression = new[] { 0, 1, 0, 6, 0, 1, 5, 1 }, Lead = "flute", Arp = "kantele", Drums = "frame", Drone = true, Bass = true, Reverb = 0.35f, Density = 1.1f };
                case MusicMood.Volcano:
                    return new Plan { Bpm = 84, Bars = 16, Root = 43, Mode = Phrygian, Progression = new[] { 0, 1, 0, 1, 5, 6, 5, 1 }, Lead = "horn", Arp = "none", Drums = "war", Drone = true, Bass = true, Reverb = 0.45f, Density = 0.5f };
                case MusicMood.Jungle:
                    return new Plan { Bpm = 104, Bars = 16, Root = 55, Mode = Dorian, Progression = new[] { 0, 3, 0, 6, 0, 3, 4, 3 }, Lead = "flute", Arp = "kantele", Drums = "shaker", Drone = false, Bass = true, Reverb = 0.32f, Density = 1.2f };
                case MusicMood.Coast:
                    return new Plan { Bpm = 90, Bars = 16, Root = 57, Mode = Mixolydian, Progression = new[] { 0, 6, 3, 0, 4, 6, 3, 4 }, Lead = "lyre", Arp = "harp", Drums = "shaker", Drone = false, Bass = true, Reverb = 0.4f, Density = 0.9f };
                case MusicMood.Arena:
                    return new Plan { Bpm = 128, Bars = 16, Root = 50, Mode = Aeolian, Progression = new[] { 0, 6, 5, 6 }, Lead = "lyre", Arp = "kantele", Drums = "war", Drone = true, Bass = true, Reverb = 0.25f, Density = 1.5f };
                default:
                    return new Plan { Bpm = 172, Bars = 16, Root = 45, Mode = Phrygian, Progression = new[] { 0, 0, 1, 0, 0, 0, 6, 5 }, Lead = "horn", Arp = "none", Drums = "dnb", Drone = true, Bass = true, Reverb = 0.2f, Density = 0.5f };
            }
        }

        public static float[] Compose(MusicMood mood, int seed)
        {
            var plan = PlanFor(mood);
            var random = new Random(seed * 7919 + (int)mood);
            float beat = 60f / plan.Bpm;
            float bar = beat * 4f;
            int total = (int)(plan.Bars * bar * Rate);
            var mix = new float[total];

            int ChordRoot(int index)
            {
                int degree = plan.Progression[index % plan.Progression.Length];
                return plan.Root + plan.Mode[degree % 7] + 12 * (degree / 7);
            }

            int ScaleNote(int degree, int octaveBase)
            {
                int oct = (int)Math.Floor(degree / 7.0);
                int d = ((degree % 7) + 7) % 7;
                return octaveBase + plan.Mode[d] + 12 * oct;
            }

            void Place(float[] part, float atSeconds, float gain)
            {
                int start = (int)(atSeconds * Rate);
                for (int i = 0; i < part.Length; i++)
                {
                    mix[(start + i) % total] += part[i] * gain; // wraps: the loop has no seam
                }
            }

            // ---- drone: the chord root and fifth held under every two bars
            if (plan.Drone)
            {
                for (int b = 0; b < plan.Bars; b += 2)
                {
                    int root = ChordRoot(b) - 12;
                    var d1 = Synth.Drone(Synth.Midi(root), bar * 2f + 0.5f, Rate, mood == MusicMood.Boss ? 10 : 6);
                    var d2 = Synth.Drone(Synth.Midi(root + 7), bar * 2f + 0.5f, Rate, 5);
                    Synth.LowPass(d1, mood == MusicMood.Boss ? 1800f : 900f, Rate);
                    Synth.LowPass(d2, 800f, Rate);
                    Place(d1, b * bar, 0.18f);
                    Place(d2, b * bar, 0.1f);
                }
            }

            // ---- bass on the downbeats (and the "and" of three for movement)
            if (plan.Bass)
            {
                for (int b = 0; b < plan.Bars; b++)
                {
                    int root = ChordRoot(b) - 24;
                    float[] hits = mood == MusicMood.Boss ? new[] { 0f, 1.5f, 2.75f } : new[] { 0f, 2.5f };
                    foreach (float h in hits)
                    {
                        Place(Synth.Bass(Synth.Midi(root), beat * (mood == MusicMood.Boss ? 0.7f : 1.4f), Rate), b * bar + h * beat, 0.32f);
                    }
                }
            }

            // ---- arpeggio: plucked chord tones in eighths
            if (plan.Arp != "none")
            {
                int[] shape = { 0, 2, 4, 7, 4, 2, 0, 4 };
                for (int b = 0; b < plan.Bars; b++)
                {
                    int chordDegree = plan.Progression[b % plan.Progression.Length];
                    for (int e = 0; e < 8; e++)
                    {
                        if (plan.Arp == "harp" && e % 2 == 1)
                        {
                            continue;
                        }

                        int note = ScaleNote(chordDegree + shape[e], plan.Root);
                        var pluck = Synth.Pluck(Synth.Midi(note), plan.Arp == "harp" ? 2.2f : 1.2f, Rate, random, plan.Arp == "harp" ? 0.45f : 0.65f,
                            plan.Arp == "harp" ? 0.9975f : 0.995f);
                        Place(pluck, b * bar + e * beat * 0.5f, 0.11f);
                    }
                }
            }

            // ---- melody: an AABA folk phrase over the progression
            if (plan.Lead != "none")
            {
                var phraseA = MakePhrase(random, plan, 4);
                var phraseB = MakePhrase(random, plan, 4);
                var form = new[] { phraseA, phraseA, phraseB, phraseA };
                float t0 = 0f;
                foreach (var phrase in form)
                {
                    foreach (var (degree, beats, at) in phrase)
                    {
                        float start = t0 + at * beat;
                        int note = ScaleNote(degree, plan.Root + 12);
                        float length = beats * beat;
                        float[] voice;
                        switch (plan.Lead)
                        {
                            case "flute": voice = Synth.Flute(Synth.Midi(note), length * 0.95f, Rate, random); break;
                            case "horn": voice = Synth.Horn(Synth.Midi(note - 12), length * 0.9f, Rate); break;
                            case "harp": voice = Synth.Pluck(Synth.Midi(note), Math.Max(1.5f, length * 1.5f), Rate, random, 0.5f, 0.998f); break;
                            default: voice = Synth.Pluck(Synth.Midi(note), Math.Max(1f, length * 1.2f), Rate, random, 0.75f, 0.996f); break;
                        }

                        Place(voice, start, plan.Lead == "horn" ? 0.2f : plan.Lead == "flute" ? 0.22f : 0.2f);
                    }

                    t0 += 4 * bar;
                    if (t0 >= plan.Bars * bar)
                    {
                        break;
                    }
                }
            }

            // ---- drums
            Drums(plan, random, beat, bar, Place);

            // ---- room, then fold the reverb tail back onto the start so the loop stays seamless
            var wet = Synth.Reverb(mix, Rate, plan.Reverb, 0.84f);
            var looped = new float[total];
            for (int i = 0; i < wet.Length; i++)
            {
                looped[i % total] += wet[i];
            }

            Synth.Normalize(looped, 0.8f);
            return looped;
        }

        private static List<(int degree, float beats, float at)> MakePhrase(Random random, Plan plan, int bars)
        {
            var notes = new List<(int, float, float)>();
            float[][] rhythms =
            {
                new[] { 1f, 1f, 1f, 1f }, new[] { 1.5f, 0.5f, 1f, 1f }, new[] { 2f, 1f, 1f }, new[] { 1f, 0.5f, 0.5f, 2f },
                new[] { 0.5f, 0.5f, 1f, 2f }, new[] { 3f, 1f }, new[] { 4f },
            };
            int degree = random.Next(0, 3) * 2;
            float at = 0f;
            for (int b = 0; b < bars; b++)
            {
                var rhythm = plan.Density < 0.7f ? rhythms[5 + random.Next(2)] : rhythms[random.Next(plan.Density > 1.1f ? 5 : 4)];
                foreach (float r in rhythm)
                {
                    // Mostly steps, now and then a leap; phrases end on the root.
                    int move = random.NextDouble() < 0.75 ? (random.Next(2) == 0 ? -1 : 1) : (random.Next(2) == 0 ? -3 : 3);
                    degree = Math.Max(-2, Math.Min(9, degree + move));
                    bool last = b == bars - 1 && Math.Abs(at + r - bars * 4f) < 0.01f;
                    notes.Add((last ? 0 : degree, r, at));
                    at += r;
                }
            }

            return notes;
        }

        private static void Drums(Plan plan, Random random, float beat, float bar, Action<float[], float, float> place)
        {
            if (plan.Drums == "none")
            {
                return;
            }

            var frame = Synth.FrameDrum(MusicComposer.Rate, random);
            var frameHigh = Synth.FrameDrum(MusicComposer.Rate, random, 1.6f);
            var kick = Synth.Kick(MusicComposer.Rate);
            var snare = Synth.Snare(MusicComposer.Rate, random);
            var hat = Synth.Hat(MusicComposer.Rate, random, 0.03f);
            var shaker = Synth.Hat(MusicComposer.Rate, random, 0.06f);
            Synth.LowPass(shaker, 7000f, MusicComposer.Rate);
            for (int b = 0; b < plan.Bars; b++)
            {
                float t = b * bar;
                switch (plan.Drums)
                {
                    case "frame":
                        place(frame, t, 0.5f);
                        place(frameHigh, t + 1.5f * beat, 0.25f);
                        place(frame, t + 2f * beat, 0.4f);
                        place(frameHigh, t + 3.5f * beat, 0.2f);
                        for (int e = 0; e < 8; e++) place(shaker, t + e * beat * 0.5f, e % 2 == 0 ? 0.08f : 0.05f);
                        break;
                    case "shaker":
                        place(frame, t, 0.35f);
                        place(frame, t + 2.5f * beat, 0.25f);
                        for (int e = 0; e < 8; e++) place(shaker, t + e * beat * 0.5f, e % 2 == 0 ? 0.08f : 0.05f);
                        break;
                    case "heart":
                        place(kick, t, 0.35f);
                        place(kick, t + 0.35f * beat, 0.25f);
                        if (b % 2 == 1) place(frame, t + 2f * beat, 0.2f);
                        break;
                    case "war":
                        for (int q = 0; q < 4; q++) place(frame, t + q * beat, q % 2 == 0 ? 0.5f : 0.35f);
                        place(snare, t + 1f * beat, 0.25f);
                        place(snare, t + 3f * beat, 0.25f);
                        for (int e = 0; e < 8; e++) place(hat, t + e * beat * 0.5f, 0.06f);
                        break;
                    case "dnb":
                        // The two-step: kick on 1 and the "and" of 3, snare on 2 and 4, sixteenth hats, ghost snares.
                        place(kick, t, 0.6f);
                        place(kick, t + 2.5f * beat, 0.55f);
                        place(snare, t + 1f * beat, 0.45f);
                        place(snare, t + 3f * beat, 0.45f);
                        place(snare, t + 3.75f * beat, 0.12f);
                        for (int s = 0; s < 16; s++) place(hat, t + s * beat * 0.25f, s % 4 == 2 ? 0.09f : 0.05f);
                        if (b % 4 == 3)
                        {
                            for (int s = 12; s < 16; s++) place(snare, t + s * beat * 0.25f, 0.2f); // fill
                        }

                        place(frame, t + 0.5f * beat, 0.25f); // the north under the break
                        break;
                }
            }
        }
    }
}
