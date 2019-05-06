using System.Collections.Generic;

namespace BitTorrentEdu
{
    public interface IPeerEventDataFactory
    {
        PeerEventDataWrapper TryParsePeerEventDataFromByteArray(byte[] byteContent);
        PeerEventDataWrapper TryParsePeerEventDataFromEnumerable(IEnumerable<byte> byteContent);
    }
}