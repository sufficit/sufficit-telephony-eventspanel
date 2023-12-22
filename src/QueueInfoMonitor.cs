using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class QueueInfoMonitor : EventsPanelMonitor<QueueInfo>
    {
        /// <summary>
        /// Events count
        /// </summary>
        public uint Count { get; internal set; }

        public QueueInfoMonitor(string key) : base(new QueueInfo(key)) { }

        #region IMPLEMENT ABSTRACT MONITOR CONTENT

        public override void Event(object @event)
        {
            Count++;
            switch (@event)
            {
                case QueueMemberStatusEvent newEvent:       Handle(Content, newEvent); break;
                case QueueParamsEvent newEvent:             Handle(Content, newEvent); break;
                case AbstractQueueMemberEvent newEvent:             Handle(Content, newEvent); break;
                case QueueCallerJoinEvent newEvent:         Handle(Content, newEvent); break;
                case QueueCallerLeaveEvent newEvent:        Handle(Content, newEvent); break;
                case QueueCallerAbandonEvent newEvent:      Handle(Content, newEvent); break;
                default: break;
            }

            // keeps that to trigger on changed
            base.Event(@event);
        }

        #endregion

        static void Handle(QueueInfo source, AbstractQueueMemberEvent eventObj)
        {
            // finding agent
            QueueAgentInfo? status = source.Agents.FirstOrDefault(s => ((IKey)s).Key == eventObj.Interface);
            if (status == null)
            {
                //creating new agent
                status = new QueueAgentInfo(eventObj.Interface);
                source.Agents.Add(status);
            }

            status.Event(eventObj);
        }

        static void Handle(QueueInfo source, QueueMemberStatusEvent eventObj)
        {
            QueueAgentInfo? status = source.Agents.FirstOrDefault(s => ((IKey)s).Key == eventObj.Interface);
            if (status == null)
            {
                status = new QueueAgentInfo(eventObj.Interface);
                source.Agents.Add(status);
            }

            status.Event(eventObj);
        }

        static void Handle(QueueInfo source, QueueParamsEvent eventObj)
        {
            if (eventObj.DateReceived > source.Updated)
            {
                source.Updated = eventObj.DateReceived;

                source.Strategy = eventObj.Strategy;
                source.Max = eventObj.Max;
                source.Calls = eventObj.Calls;
                source.HoldTime = eventObj.Holdtime;
                source.Completed = eventObj.Completed;
                source.Abandoned = eventObj.Abandoned;
                source.ServiceLevel = eventObj.ServiceLevel;
                source.Weight = eventObj.Weight;                
            }
        }

        static void Handle(QueueInfo source, QueueCallerJoinEvent eventObj)
        {
            if (eventObj.DateReceived > source.Updated)
            {
                source.Updated = eventObj.DateReceived;
                source.Calls = eventObj.Count;
            }
        }

        static void Handle(QueueInfo source, QueueCallerLeaveEvent eventObj)
        {
            if (eventObj.DateReceived > source.Updated)
            {
                source.Updated = eventObj.DateReceived;
                source.Calls = eventObj.Count;
            }
        }

        static void Handle(QueueInfo source, QueueCallerAbandonEvent eventObj)
        {
            if (eventObj.DateReceived > source.Updated)
            {
                source.Updated = eventObj.DateReceived;
            }
        }
    }
}
