using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class QueueInfo : IKey
    {
        #region IMPLEMENT INTERFACE KEY

        public string Key { get; }

        #endregion

        public QueueInfo(string key)
        {
            Key = key;
            Agents = new GenericCollection<QueueAgentInfo>();
        }

        #region PROPRIEDADES PUBLICAS

        public GenericCollection<QueueAgentInfo> Agents { get; }
        public string? Strategy { get; set; }
        public int Max { get; set; }
        public int Calls { get; set; }
        public int HoldTime { get; set; }
        public int Completed { get; set; }
        public int Abandoned { get; set; }
        public int ServiceLevel { get; set; }
        public float ServiceLevelPerf { get; set; }
        public int Weight { get; set; }
        public int TalkTime { get; set; }

        public DateTime Updated { get; internal set; }


        #endregion

    }
}
