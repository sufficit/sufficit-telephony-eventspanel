using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ChannelInfoMonitor : ChannelInfo
    {
        public ChannelInfoMonitor(string id) : base(id) { }

        public DateTime Start { get; set; }

        public DateTime Update { get; set; }


        public event EventHandler<AsteriskChannelState>? Changed;

        protected bool UpdateReceived(DateTime dateTime)
        {
            if (dateTime > DateTime.MinValue)
            {
                if (Start == DateTime.MinValue || dateTime < Start)
                {
                    Start = dateTime;
                    return true;
                }
            }

            return false;
        }

        public async Task Event(IChannelEvent @event) 
        {
            switch (@event)
            {
                case StatusEvent channelEvent: await Event(channelEvent); break;
                case AMINewChannelEvent channelEvent: await Event(channelEvent); break;
                case AMINewStateEvent channelEvent: await Event(channelEvent); break;
                case AMIHangupEvent channelEvent: await Event(channelEvent); break;
            }
        }

        public async Task Event(StatusEvent @event)
        {
            bool updated = UpdateReceived(@event.DateReceived);

            if (State != @event.ChannelState)
            {
                State = @event.ChannelState;
                updated = true;
            }

            if (updated) Changed?.Invoke(this, State);
            await Task.CompletedTask;
        }

        public async Task Event(AMINewChannelEvent @event)
        {
            bool updated = UpdateReceived(@event.DateReceived);

            if (State != @event.ChannelState)
            {
                State = @event.ChannelState;
                updated = true;
            }

            if (updated) Changed?.Invoke(this, State);
            await Task.CompletedTask;
        }

        public async Task Event(AMINewStateEvent @event)
        {
            bool updated = UpdateReceived(@event.DateReceived);

            if (State != @event.ChannelState)
            {
                State = @event.ChannelState;
                updated = true;
            }

            if (updated) Changed?.Invoke(this, State);
            await Task.CompletedTask;
        }

        public async Task Event(AMIHangupEvent @event)
        {
            bool updated = UpdateReceived(@event.DateReceived);

            if (Hangup == null)
            {
                Hangup = new Hangup();
                Hangup.Code = @event.Cause;
                Hangup.Description = @event.CauseTxt;
                Hangup.Timestamp = @event.DateReceived;
                updated = true;
            }

            if (updated) Changed?.Invoke(this, State); 
            await Task.CompletedTask;
        }        
    }
}
