using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using Sufficit.Notification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsMonitorService : AMIHubClient 
    { 
        private readonly ILogger _logger;

        public EventsMonitorService(ILogger<EventsMonitorService> logger, IOptionsMonitor<AMIHubClientOptions> huboptmonitor, ILogger<AMIHubClient> hublogger) : base(huboptmonitor, hublogger)
        {
            _logger = logger;
            Channels = new ChannelInfoCollection();
            Peers = new PeerInfoCollection();
            Queues = new QueueInfoCollection();

            Register();
        }

        protected virtual void Register()
        {
            Register<SuccessfulAuthEvent>(IManagerEventHandler);
            Register<ChallengeSentEvent>(IManagerEventHandler);
            Register<InvalidPasswordEvent>(IManagerEventHandler);
            Register<ChallengeResponseFailedEvent>(IManagerEventHandler);

            Register<PeerStatusEvent>(PeerStatusEventHandler);

            Register<NewChannelEvent>(IManagerEventHandler);
            Register<NewStateEvent>(IManagerEventHandler);
            Register<HangupEvent>(IManagerEventHandler);
            Register<StatusEvent>(IManagerEventHandler);
            Register<PeerEntryEvent>(IManagerEventHandler);

            // events queue and channels
            Register<QueueCallerJoinEvent>(IManagerEventHandler);
            Register<QueueCallerAbandonEvent>(IManagerEventHandler);
            Register<QueueCallerLeaveEvent>(IManagerEventHandler);

            Register<QueueMemberAddedEvent>(IManagerEventHandler);
            Register<QueueMemberPauseEvent>(IManagerEventHandler);
            Register<QueueMemberPenaltyEvent>(IManagerEventHandler);
            Register<QueueMemberRemovedEvent>(IManagerEventHandler);
            Register<QueueMemberRinginuseEvent>(IManagerEventHandler);
            Register<QueueMemberStatusEvent>(IManagerEventHandler);

            Register<QueueParamsEvent>(IManagerEventHandler);
        }

        /// <summary>
        /// On event received from servers
        /// </summary>
        public event Action<IEnumerable<string>, IManagerEventFromAsterisk>? OnEvent;

        /// <summary>
        ///     Channel monitor collection
        /// </summary>
        public ChannelInfoCollection Channels { get; }

        /// <summary>
        ///     Peer monitor collection
        /// </summary>
        public PeerInfoCollection Peers { get; }

        /// <summary>
        ///     Queue monitor collection
        /// </summary>
        public QueueInfoCollection Queues { get; }

        public virtual bool IgnoreLocal { get; internal set; }

        #region EVENTS

        private void PeerStatusEventHandler(string sender, PeerStatusEvent @event)
        {
            var monitor = Peers.Monitor(@event.Peer, false);
            monitor.Event(@event);
        }

        private void IManagerEventHandler(string sender, IManagerEventFromAsterisk @event)
        {
            return;

            _logger.LogTrace("event: {type}, from: {sender}", @event.GetType(), sender);
            var cardKeys = new HashSet<string>();
            if (@event is IChannelEvent eventChannel)
            {
                bool proccess = true;
                if (IgnoreLocal)
                {
                    var channel = new AsteriskChannel(eventChannel.Channel);
                    if (channel.Protocol == AsteriskChannelProtocol.LOCAL)
                        proccess = false;
                }

                if (proccess)
                {
                    var key = Channels.HandleEvent(eventChannel);
                    cardKeys.Add(key);
                }
            }

            if (@event is SecurityEvent securityEvent)
                cardKeys.Add(Peers.HandleEvent(securityEvent));
            else if (@event is IPeerStatus peerStatusEvent)
                cardKeys.Add(Peers.HandleEvent(peerStatusEvent));

            if (@event is IQueueEvent eventQueue)
                cardKeys.Add(Queues.HandleEvent(eventQueue));

            DispatchEvent(cardKeys, @event);
        }

        #endregion

        protected virtual void DispatchEvent(IEnumerable<string> cardKeys, IManagerEventFromAsterisk @event) 
            => OnEvent?.Invoke(cardKeys, @event);
    }
}
