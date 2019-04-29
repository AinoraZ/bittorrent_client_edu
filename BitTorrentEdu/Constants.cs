using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu
{
    public class Constants
    {
        public static readonly int PieceHashLength = 20;
        public static readonly int InfoHashLength = 20;
        public static readonly int PeerIdLength = 20; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters
        public static readonly int MinPortNumber = 6881; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters
        public static readonly int MaxPortNumber = 6889; //According to specification https://wiki.theory.org/index.php/BitTorrentSpecification#Tracker_Request_Parameters
    }
}
