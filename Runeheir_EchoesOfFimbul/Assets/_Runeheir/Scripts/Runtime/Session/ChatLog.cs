using System;
using System.Collections.Generic;

namespace Runeheir.Session
{
    public enum ChatKind
    {
        Normal = 0,
        System = 1,
        Notice = 2,
        Error = 3,
        Loot = 4,
        Gm = 5,

        // Phase 6 chat channels.
        Party = 6,
        Guild = 7,
        Whisper = 8,
        Shout = 9,
    }

    public readonly struct ChatLine
    {
        public ChatLine(string text, ChatKind kind)
        {
            Text = text;
            Kind = kind;
        }

        public string Text { get; }

        public ChatKind Kind { get; }
    }

    /// <summary>Global message log shown in the chat window (system messages, loot, @command output).</summary>
    public static class ChatLog
    {
        public const int Capacity = 120;

        private static readonly List<ChatLine> Buffer = new List<ChatLine>();

        public static event Action<ChatLine> LineAdded;

        public static IReadOnlyList<ChatLine> Lines => Buffer;

        public static void Add(string text, ChatKind kind = ChatKind.Normal)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var line = new ChatLine(text, kind);
            Buffer.Add(line);
            if (Buffer.Count > Capacity)
            {
                Buffer.RemoveAt(0);
            }

            LineAdded?.Invoke(line);
        }

        public static void System(string text)
        {
            Add(text, ChatKind.System);
        }

        public static void Notice(string text)
        {
            Add(text, ChatKind.Notice);
        }

        public static void Error(string text)
        {
            Add(text, ChatKind.Error);
        }

        public static void Loot(string text)
        {
            Add(text, ChatKind.Loot);
        }

        public static void Gm(string text)
        {
            Add(text, ChatKind.Gm);
        }

        public static void Clear()
        {
            Buffer.Clear();
        }
    }
}
