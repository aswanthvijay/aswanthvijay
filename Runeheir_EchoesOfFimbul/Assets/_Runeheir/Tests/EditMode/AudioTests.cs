using System;
using System.Diagnostics;
using NUnit.Framework;
using Runeheir.Audio;

namespace Runeheir.Tests
{
    /// <summary>Phase 8 audio: every procedural sound and track builds, sounds like something, and is the same every time.</summary>
    public sealed class AudioTests
    {
        private static void AssertSane(float[] samples, string what, float peak)
        {
            Assert.IsNotEmpty(samples, what);
            float max = 0f;
            double energy = 0;
            foreach (float s in samples)
            {
                Assert.IsFalse(float.IsNaN(s) || float.IsInfinity(s), $"{what}: bad sample");
                max = Math.Max(max, Math.Abs(s));
                energy += s * s;
            }

            Assert.AreEqual(peak, max, 0.02f, $"{what} is normalized");
            Assert.Greater(Math.Sqrt(energy / samples.Length), 0.005, $"{what} isn't silent");
        }

        [Test]
        public void EverySoundEffect_Builds()
        {
            foreach (Sfx sfx in Enum.GetValues(typeof(Sfx)))
            {
                var samples = SfxBank.Build(sfx);
                AssertSane(samples, sfx.ToString(), 0.85f);
                Assert.Less(samples.Length, SfxBank.Rate * 3, $"{sfx} is a short effect");
            }

            CollectionAssert.AreEqual(SfxBank.Build(Sfx.Fire), SfxBank.Build(Sfx.Fire), "deterministic");
        }

        [Test]
        public void EveryMood_ComposesALoop()
        {
            var clock = Stopwatch.StartNew();
            foreach (MusicMood mood in Enum.GetValues(typeof(MusicMood)))
            {
                var track = MusicComposer.Compose(mood, 17);
                AssertSane(track, mood.ToString(), 0.8f);
                float seconds = (float)track.Length / MusicComposer.Rate;
                Assert.That(seconds, Is.InRange(15f, 80f), $"{mood} loop length");
            }

            Assert.Less(clock.Elapsed.TotalSeconds, 60, "composing every mood stays quick enough to do at startup");
            CollectionAssert.AreEqual(MusicComposer.Compose(MusicMood.Town, 3), MusicComposer.Compose(MusicMood.Town, 3), "deterministic");
        }

        [Test]
        public void Elements_HaveTheirOwnSounds()
        {
            Assert.AreEqual(Sfx.Fire, SfxBank.ForElement(Combat.Element.Fire));
            Assert.AreEqual(Sfx.Frost, SfxBank.ForElement(Combat.Element.Water));
            Assert.AreEqual(Sfx.Holy, SfxBank.ForElement(Combat.Element.Holy));
            Assert.AreEqual(Sfx.CastRelease, SfxBank.ForElement(Combat.Element.Neutral));
        }
    }
}
