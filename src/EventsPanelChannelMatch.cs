using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelChannelMatch
    {
        public EventsPanelChannelMatch(string s)
        {
            if(string.IsNullOrWhiteSpace(s))
                throw new ArgumentNullException(nameof(s));

            var normalized = s.Trim().ToLowerInvariant();
            if (normalized.StartsWith('^'))
            {
                Key = normalized.Substring(1);
                Kind = EventsPanelChannelMatchKind.STARTSWITH;
            } else if (normalized.StartsWith('*'))
            {
                Key = normalized.Substring(1);
                Kind = EventsPanelChannelMatchKind.CONTAINS;
            } else
            {
                Key = normalized;
                Kind = EventsPanelChannelMatchKind.EXACTMATCH;
            }
        }

        public string Key { get; }

        public EventsPanelChannelMatchKind Kind { get; }

        public override string ToString()
        {
            switch (Kind)
            {
                case EventsPanelChannelMatchKind.STARTSWITH: return $"^{ Key }";
                case EventsPanelChannelMatchKind.CONTAINS: return $"*{ Key }";
                default: return Key;
            }
        }

        public static implicit operator string (EventsPanelChannelMatch m)
            => m.ToString();

        public static implicit operator EventsPanelChannelMatch(string m)
            => new EventsPanelChannelMatch(m);

        /// <summary>
        /// Compare if channel match with a card
        /// </summary>
        /// <param name="match">Full Channel string to compare</param>
        /// <returns></returns>
        public bool IsMatch(string match)
        {
            switch (Kind)
            {
                case EventsPanelChannelMatchKind.STARTSWITH: 
                    return match.StartsWith(Key);

                case EventsPanelChannelMatchKind.CONTAINS:
                    return match.Contains(Key);

                default: return match.Equals(Key);
            }
        }
    }
}
