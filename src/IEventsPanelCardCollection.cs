﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json.Serialization;

namespace Sufficit.Telephony.EventsPanel
{

    public interface IEventsPanelCardCollection : ICollection<EventsPanelCard>, IEventsPanelCardsAreaOptions
    {
        IEnumerable<EventsPanelCard> this[string key] { get; }

        IList<EventsPanelCard> ToList();

        IList<T> ToList<T>();

        event Action<EventsPanelCard?, NotifyCollectionChangedAction>? OnChanged;

        IEnumerable<EventsPanelTrunkCard> Trunks { get; }

        IEnumerable<EventsPanelPeerCard> Peers { get; }

        IEnumerable<EventsPanelQueueCard> Queues { get; }

        [JsonIgnore]
        Func<EventsPanelCardInfo, Task<string>>? CardAvatarHandler { get; set; }
    }
}
