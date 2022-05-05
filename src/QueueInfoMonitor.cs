using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class QueueInfoMonitor : EventsPanelMonitor<QueueInfo>
    {
        public QueueInfoMonitor(string key) : base(new QueueInfo(key)) { }

        #region IMPLEMENT ABSTRACT MONITOR CONTENT

        public override void Event(object @event)
        {
            var content = GetContent();
            switch (@event)
            {
                case QueueMemberStatusEvent newEvent:       Handle(content, newEvent); break;
                case QueueParamsEvent newEvent:             Handle(content, newEvent); break;
                case QueueMemberEvent newEvent:             Handle(content, newEvent); break;
                case QueueCallerJoinEvent newEvent:         Handle(content, newEvent); break;
                case QueueCallerLeaveEvent newEvent:        Handle(content, newEvent); break;
                case QueueCallerAbandonEvent newEvent:      Handle(content, newEvent); break;
                default: break;
            }

            // keeps that to trigger on changed
            base.Event(@event);
        }

        #endregion

        public static void Handle(QueueInfo source, QueueMemberEvent eventObj)
        {
            QueueAgentInfo? status = source.Agents.FirstOrDefault(s => s.Interface == eventObj.Location);
            if (status == null)
            {
                status = new QueueAgentInfo(eventObj.Location);
                source.Agents.Add(status);
            }

            if (eventObj.DateReceived > status.Updated)
            {
                status.Updated = eventObj.DateReceived;
                status.Title = eventObj.Name;
                status.Membership = eventObj.Membership;
                status.Penalty = eventObj.Penalty;
                status.CallsTaken = eventObj.CallsTaken;
                status.LastCall = eventObj.LastCall;
                status.Status = eventObj.Status;
                status.Paused = eventObj.Paused;
                status.InCall = eventObj.InCall;
                status.PausedReason = eventObj.PausedReason;

                //status.LastPause = eventObj.LastPause;
                //status.WrapUpTime = eventObj.WrapUpTime;
            }
        }

        public static void Handle(QueueInfo source, QueueMemberStatusEvent eventObj)
        {
            QueueAgentInfo? status = source.Agents.FirstOrDefault(s => s.Interface == eventObj.Interface);
            if (status == null)
            {
                status = new QueueAgentInfo(eventObj.Interface);
                source.Agents.Add(status);
            }

            if (eventObj.DateReceived > source.Updated)
            {
                status.Updated = eventObj.DateReceived;
                status.Title = eventObj.MemberName;
                status.Membership = eventObj.Membership;
                status.Penalty = eventObj.Penalty;
                status.CallsTaken = eventObj.CallsTaken;
                status.LastCall = eventObj.LastCall;
                status.Status = eventObj.Status;
                status.Paused = eventObj.Paused;
                status.InCall = eventObj.InCall;
                status.PausedReason = eventObj.PausedReason;
                //status.LastPause = eventObj.LastPause;
                //status.WrapUpTime = eventObj.WrapUpTime;
            }
        }

        public static void Handle(QueueInfo source, QueueParamsEvent eventObj)
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
                //ServiceLevelPerf = eventObj.ServiceLevelPerf;
                source.Weight = eventObj.Weight;
                //TalkTime = eventObj.TalkTime;
            }
        }

        public static void Handle(QueueInfo source, QueueCallerJoinEvent eventObj)
        {
            if (eventObj.DateReceived > source.Updated)
            {
                source.Updated = eventObj.DateReceived;
                source.Calls = eventObj.Count;
            }
        }

        public static void Handle(QueueInfo source, QueueCallerLeaveEvent eventObj)
        {
            if (eventObj.DateReceived > source.Updated)
            {
                source.Updated = eventObj.DateReceived;
                source.Calls = eventObj.Count;
            }
        }

        public static void Handle(QueueInfo source, QueueCallerAbandonEvent eventObj)
        {
            if (eventObj.DateReceived > source.Updated)
            {
                source.Updated = eventObj.DateReceived;
            }
        }
    }
}
