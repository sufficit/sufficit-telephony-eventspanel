using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class QueueAgentInfo : IMonitor
    {
        #region IMPLEMENT INTERFACE MONITOR

        public string Key => Interface;

        public event IMonitor.AsyncEventHandler? OnChanged;

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

        public string? Title { get; set; }
        public int Penalty { get; set; }
        public int CallsTaken { get; set; }
        public double LastCall { get; set; }
        public int Status { get; set; }
        public bool Paused { get; set; }
        public bool InCall { get; set; }
        public string? PausedReason { get; set; }
        public int LastPause { get; set; }
        public int WrapUpTime { get; set; }

        /// <summary>
        /// Data hora da ultima atualização
        /// </summary>
        public DateTime Updated { get; set; }

    }
}
