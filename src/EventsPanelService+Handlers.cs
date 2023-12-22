using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public partial class EventsPanelService
    {
        public static string GetKeyFromEvent(object @event)
        {
            switch (@event)
            {
                case NewStateEvent @new: 
                    {
                        var index = @new.Channel.LastIndexOf('-');
                        return @new.Channel.Substring(0, index);
                    }
                case NewChannelEvent @new:
                    {
                        var index = @new.Channel.LastIndexOf('-');
                        return @new.Channel.Substring(0, index);
                    }
                case HangupEvent @new:
                    {
                        var index = @new.Channel.LastIndexOf('-');
                        return @new.Channel.Substring(0, index);
                    }
                case IChannelEvent @new: return @new.Channel;
                default: return "invalid";
            }
        }        
    }
}
