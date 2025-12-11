using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;

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

        /// <summary>
        /// Registers all Asterisk Manager Interface (AMI) event handlers for monitoring telephony operations.
        /// Each registered event type triggers the appropriate handler to update the monitoring collections (Channels, Peers, Queues).
        /// </summary>
        protected virtual void Register()
        {
            // ============================================================================
            // 🔐 AUTHENTICATION & SECURITY EVENTS
            // ============================================================================
            
            // ✅ Authentication successful - AMI connection authenticated
            Register<SuccessfulAuthEvent>(IManagerEventHandler);
      
            // 🔐 Challenge sent - Challenge-response authentication initiated
            Register<ChallengeSentEvent>(IManagerEventHandler);
            
            // ❌ Invalid password - Login attempt with incorrect credentials
            Register<InvalidPasswordEvent>(IManagerEventHandler);
            
            // ❌ Challenge response failed - Failed to respond to authentication challenge
            Register<ChallengeResponseFailedEvent>(IManagerEventHandler);

            // ============================================================================
            // 📞 PEER EVENTS (Extensions/Devices)
            // ============================================================================
            
            // 📊 Peer status changed - Extension availability updated (Available, Unavailable, Busy, Lagged, etc.)
            Register<PeerStatusEvent>(PeerStatusEventHandler);

            // ============================================================================
            // 🔔 CHANNEL EVENTS (Active Calls)
            // ============================================================================
            
            // 🆕 New channel created - Call initiated (inbound or outbound)
            Register<NewChannelEvent>(IManagerEventHandler);
    
            // 🔄 Channel state changed - Call state transition (Ringing → Up → Busy, etc.)
            Register<NewStateEvent>(IManagerEventHandler);
       
            // 📴 Call hangup - Channel terminated (normal or abnormal)
            Register<HangupEvent>(IManagerEventHandler);
            
            // 📊 Channel status - Response to AMI 'Status' command
            Register<StatusEvent>(IManagerEventHandler);

            // 📋 Peer entry - Response to 'SIPpeers' or 'IAXpeers' listing command
            Register<PeerEntryEvent>(IManagerEventHandler);

            // ⏸️ Music on hold started - Channel placed on hold
            Register<MusicOnHoldStartEvent>(IManagerEventHandler);
  
            // ▶️ Music on hold stopped - Channel taken off hold
            Register<MusicOnHoldStopEvent>(IManagerEventHandler);

            // ============================================================================
            // 🎯 QUEUE EVENTS - CALLERS (Customers in Queue)
            // ============================================================================
   
            // ➕ Customer joined queue - Call placed in waiting queue
            Register<QueueCallerJoinEvent>(IManagerEventHandler);
          
            // 🚪 Customer abandoned queue - Caller hung up before being answered
            Register<QueueCallerAbandonEvent>(IManagerEventHandler);
    
            // ➖ Customer left queue - Caller answered, removed by timeout, or transferred
            Register<QueueCallerLeaveEvent>(IManagerEventHandler);

            // ============================================================================
            // 👥 QUEUE EVENTS - MEMBERS (Agents/Attendants)
            // ============================================================================
         
            // ➕ Agent added to queue - Extension joined as queue member
            Register<QueueMemberAddedEvent>(IManagerEventHandler);
  
            // ⏸️ Agent paused/unpaused - Member temporarily unavailable (coffee break, meeting, etc.)
            Register<QueueMemberPauseEvent>(IManagerEventHandler);
         
            // ⚖️ Agent penalty changed - Member priority adjusted (lower penalty = higher priority)
            Register<QueueMemberPenaltyEvent>(IManagerEventHandler);

            // ➖ Agent removed from queue - Member unassigned from queue
            Register<QueueMemberRemovedEvent>(IManagerEventHandler);
            
            // 🔔 'Ringinuse' setting changed - Defines if busy members can receive new calls
            Register<QueueMemberRinginuseEvent>(IManagerEventHandler);
  
            // 📊 Agent status changed - Member availability updated (Available, Busy, Unavailable, In Call)
            Register<QueueMemberStatusEvent>(IManagerEventHandler);

            // ============================================================================
            // ⚙️ QUEUE EVENTS - CONFIGURATION
            // ============================================================================
          
            // 🎛️ Queue parameters updated - Configuration changed (max wait time, strategy: rrmemory/leastrecent/etc., announce frequency)
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

            var cardKeys = new string[] { @event.GetEventKey() };
            DispatchEvent(cardKeys, @event);
        }

        private void IManagerEventHandler(string sender, IManagerEventFromAsterisk @event)
        {
            //return;

            _logger.LogTrace("event: {type}, from: {sender}", @event.GetType(), sender);
            var cardKeys = new HashSet<string>();
            if (@event is IChannelEvent eventChannel)
            {
                bool process = true;
                if (IgnoreLocal)
                {
                    var channel = Sufficit.Asterisk.Utils.AsteriskChannelGenerate(eventChannel.Channel);
                    if (channel.Protocol == AsteriskChannelProtocol.LOCAL)
                        process = false;
                }

                if (process)                
                    cardKeys.Add(eventChannel.GetEventKey());                
            }

            if (@event is SecurityEvent securityEvent)
                cardKeys.Add(securityEvent.GetEventKey());
            else if (@event is IPeerStatus peerStatusEvent)
                cardKeys.Add(peerStatusEvent.GetEventKey());

            if (@event is IQueueEvent eventQueue)
                cardKeys.Add(eventQueue.GetEventKey());

            DispatchEvent(cardKeys, @event);
        }

        #endregion

        protected virtual void DispatchEvent(IEnumerable<string> cardKeys, IManagerEventFromAsterisk @event) 
            => OnEvent?.Invoke(cardKeys, @event);
    }
}
