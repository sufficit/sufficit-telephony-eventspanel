using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class QueueAgentInfo : IMonitor
    {
        #region IMPLEMENT INTERFACE MONITOR

        string IKey.Key => Interface;

        public event Action<IMonitor, object?>? OnChanged;

        #endregion

        public QueueAgentInfo(string iface)
        {
            Interface = iface;
        }

        /// <summary>
        /// Discagem (Dial) = Chave de agentes da fila de espera
        /// </summary>
        public string Interface { get; }
        
        /// <summary>
        /// Tipo de agente ( estático ou dinamico )
        /// </summary>
        public string? Membership { get; set; }

        public string? Name { get; set; }
        public uint Penalty { get; set; }
        public uint CallsTaken { get; set; }
        public double LastCall { get; set; }
        public AsteriskDeviceStatus Status { get; set; }
        public bool Paused { get; set; }
        public bool InCall { get; set; }
        public string? PausedReason { get; set; }
        public int LastPause { get; set; }
        public int WrapUpTime { get; set; }

        /// <summary>
        /// Data hora da ultima atualização
        /// </summary>
        public DateTime Updated { get; set; }

        public void Event(IManagerEvent @event)
        {
            bool ShouldUpdate = false;
            var timestamp = @event.GetTimeStamp();
            if (timestamp > Updated)
            {
                Updated = timestamp;
                if (@event is IQueueMemberEvent statusEvent)
                    Handle(this, statusEvent);
            }

            if (ShouldUpdate && OnChanged != null)
                OnChanged.Invoke(this, null);
        }

        public static void Handle(QueueAgentInfo source, IQueueMemberEvent eventObj)
        {
            source.Name = eventObj.MemberName;
            source.Membership = eventObj.Membership;
            source.Penalty = eventObj.Penalty;
            source.CallsTaken = eventObj.CallsTaken;
            source.LastCall = eventObj.LastCall;
            source.Status = eventObj.Status;
            source.Paused = eventObj.Paused;
            source.InCall = eventObj.InCall;
            source.PausedReason = eventObj.PausedReason;
        }
    }
}
