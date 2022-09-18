using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class AMIHubClientOptions
    {
        public const string SECTIONNAME = "Sufficit:Telephony:AMIHubClient";

        [JsonPropertyName("endpoint")]
        public Uri EndPoint { get; set; } = default!;

        public Exception? Validate()
        {
            if(EndPoint != null)
            {
                return null;
            } 
            else { return new Exception("null or empty endpoint"); }
        }

        public AMIHubClientOptions ValidateAndThrow()
        {
            var ex = Validate();
            if(ex != null) throw ex;

            return this;
        }
    }
}
