using Sufficit.Asterisk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public static class CultureExtensions
    {
        public static string ToCulture(this PeerStatus source, CultureInfo? culture = null)
        {
            switch (source)
            {
                case PeerStatus.Unregistered: return "Não registrado";
                case PeerStatus.Lagged: return "Com lentidão";
                case PeerStatus.Reachable: return "Alcançavel";
                case PeerStatus.Unreachable: return "Inalcançável";
                case PeerStatus.Rejected: return "Rejeitado";
                case PeerStatus.Registered: return "Registrado";
                case PeerStatus.Unmonitored: return "Não monitorado";
                case PeerStatus.Ok: return "Tudo em ordem";
                default: return "Sem Informação";
            }
        }
    }
}
