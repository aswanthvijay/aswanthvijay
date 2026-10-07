using System;
using System.Collections.Generic;
using System.Text;

namespace Runeheir.Social
{
    public enum ChatChannel
    {
        /// <summary>Everyone on the same map.</summary>
        Local = 0,

        /// <summary>Everyone on the server.</summary>
        Shout = 1,

        Party = 2,
        Guild = 3,
        Whisper = 4,

        /// <summary>Server announcements.</summary>
        System = 5,
    }

    /// <summary>What the chat box turned a line into.</summary>
    public readonly struct ChatInput
    {
        public ChatInput(ChatChannel channel, string target, string text, bool isCommand)
        {
            Channel = channel;
            Target = target;
            Text = text;
            IsCommand = isCommand;
        }

        public ChatChannel Channel { get; }

        /// <summary>Whisper target (character name).</summary>
        public string Target { get; }

        public string Text { get; }

        /// <summary>An @ command (GM and helper commands) rather than a message.</summary>
        public bool IsCommand { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Text);
    }

    /// <summary>
    /// Chat channels (Ragnarok style): plain text goes to the map, <c>%text</c> or <c>/p text</c> to the party,
    /// <c>$text</c> or <c>/g text</c> to the guild, <c>/sh text</c> to everyone, <c>/w Name text</c> (or
    /// <c>/w "Two Words" text</c>) whispers and <c>/r text</c> answers the last whisper.
    /// </summary>
    public static class ChatRules
    {
        public const int MaxLength = 120;

        /// <summary>Messages allowed per <see cref="FloodWindowSeconds"/> before the server mutes for a moment.</summary>
        public const int FloodLimit = 6;

        public const double FloodWindowSeconds = 4.0;

        /// <summary>Shouts reach the whole server, so they're rarer.</summary>
        public const double ShoutCooldownSeconds = 10.0;

        /// <summary>
        /// Reads a chat line. <paramref name="replyTarget"/> is who /r answers. Unknown slash commands come back as
        /// commands so the caller can say so.
        /// </summary>
        public static ChatInput Parse(string raw, string replyTarget = null)
        {
            string line = (raw ?? string.Empty).Trim();
            if (line.Length == 0)
            {
                return new ChatInput(ChatChannel.Local, null, string.Empty, false);
            }

            if (line[0] == '@')
            {
                return new ChatInput(ChatChannel.Local, null, line, true);
            }

            if (line[0] == '%')
            {
                return new ChatInput(ChatChannel.Party, null, Sanitize(line.Substring(1)), false);
            }

            if (line[0] == '$')
            {
                return new ChatInput(ChatChannel.Guild, null, Sanitize(line.Substring(1)), false);
            }

            if (line[0] != '/')
            {
                return new ChatInput(ChatChannel.Local, null, Sanitize(line), false);
            }

            int space = line.IndexOf(' ');
            string verb = (space < 0 ? line : line.Substring(0, space)).ToLowerInvariant();
            string rest = space < 0 ? string.Empty : line.Substring(space + 1).Trim();
            switch (verb)
            {
                case "/s":
                case "/say":
                    return new ChatInput(ChatChannel.Local, null, Sanitize(rest), false);
                case "/p":
                case "/party":
                    return new ChatInput(ChatChannel.Party, null, Sanitize(rest), false);
                case "/g":
                case "/guild":
                    return new ChatInput(ChatChannel.Guild, null, Sanitize(rest), false);
                case "/sh":
                case "/shout":
                case "/y":
                    return new ChatInput(ChatChannel.Shout, null, Sanitize(rest), false);
                case "/r":
                case "/reply":
                    return new ChatInput(ChatChannel.Whisper, replyTarget, Sanitize(rest), false);
                case "/w":
                case "/whisper":
                case "/t":
                case "/tell":
                    SplitTarget(rest, out string target, out string text);
                    return new ChatInput(ChatChannel.Whisper, target, Sanitize(text), false);
                default:
                    return new ChatInput(ChatChannel.Local, null, line, true);
            }
        }

        /// <summary>
        /// Makes a message safe to show: no control characters or rich-text tags (&lt; and &gt; become ‹ ›), single
        /// spaces, at most <paramref name="maxLength"/> characters.
        /// </summary>
        public static string Sanitize(string raw, int maxLength = MaxLength)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(Math.Min(raw.Length, maxLength));
            bool lastSpace = true;
            foreach (char c in raw)
            {
                char ch = c;
                if (char.IsControl(ch) || char.IsWhiteSpace(ch))
                {
                    ch = ' ';
                }
                else if (ch == '<')
                {
                    ch = '‹';
                }
                else if (ch == '>')
                {
                    ch = '›';
                }

                if (ch == ' ')
                {
                    if (lastSpace)
                    {
                        continue;
                    }

                    lastSpace = true;
                }
                else
                {
                    lastSpace = false;
                }

                builder.Append(ch);
                if (builder.Length >= maxLength)
                {
                    break;
                }
            }

            return builder.ToString().Trim();
        }

        /// <summary>The line as the chat window shows it.</summary>
        public static string Format(ChatChannel channel, string sender, string text, bool outgoingWhisper = false)
        {
            switch (channel)
            {
                case ChatChannel.Shout:
                    return $"[Shout] {sender}: {text}";
                case ChatChannel.Party:
                    return $"[Party] {sender}: {text}";
                case ChatChannel.Guild:
                    return $"[Guild] {sender}: {text}";
                case ChatChannel.Whisper:
                    return outgoingWhisper ? $"(To {sender}) {text}" : $"(From {sender}) {text}";
                case ChatChannel.System:
                    return text;
                default:
                    return $"{sender}: {text}";
            }
        }

        private static void SplitTarget(string rest, out string target, out string text)
        {
            target = null;
            text = string.Empty;
            if (string.IsNullOrEmpty(rest))
            {
                return;
            }

            if (rest[0] == '"')
            {
                int close = rest.IndexOf('"', 1);
                if (close > 1)
                {
                    target = rest.Substring(1, close - 1).Trim();
                    text = rest.Substring(close + 1).Trim();
                    return;
                }
            }

            int space = rest.IndexOf(' ');
            target = space < 0 ? rest : rest.Substring(0, space);
            text = space < 0 ? string.Empty : rest.Substring(space + 1).Trim();
        }
    }

    /// <summary>Server-side flood guard for one sender: <see cref="ChatRules.FloodLimit"/> lines per window, rarer shouts.</summary>
    public sealed class ChatFloodGate
    {
        private readonly Queue<double> _recent = new Queue<double>();
        private double _lastShout = double.NegativeInfinity;

        /// <summary>True when a message on <paramref name="channel"/> at <paramref name="now"/> (seconds) may go out.</summary>
        public bool Allow(ChatChannel channel, double now, out string error)
        {
            while (_recent.Count > 0 && now - _recent.Peek() > ChatRules.FloodWindowSeconds)
            {
                _recent.Dequeue();
            }

            if (_recent.Count >= ChatRules.FloodLimit)
            {
                error = "You're talking too fast.";
                return false;
            }

            if (channel == ChatChannel.Shout && now - _lastShout < ChatRules.ShoutCooldownSeconds)
            {
                error = $"You can shout again in {Math.Ceiling(ChatRules.ShoutCooldownSeconds - (now - _lastShout)):0} s.";
                return false;
            }

            _recent.Enqueue(now);
            if (channel == ChatChannel.Shout)
            {
                _lastShout = now;
            }

            error = null;
            return true;
        }
    }
}
