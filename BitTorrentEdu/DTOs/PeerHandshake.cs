using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentEdu.DTOs
{
    public class PeerHandshake
    {
        public int Length { get; }
        public string ProtocolIdentifier { get; }
        public byte[] Reserved { get; }
        public byte[] InfoHash { get; }
        public string PeerId { get; }

        public PeerHandshake(string protocolIdentifier, byte[] infoHash, string peerId)
        {
            if (protocolIdentifier.Length > 255)
                throw new ArgumentException("Protocol identifier length must be 255 or less");

            if (infoHash.Length != Constants.InfoHashLength)
                throw new ArgumentException($"Info hash must be {Constants.InfoHashLength} bytes long");

            if(peerId.Length != Constants.PeerIdLength)
                throw new ArgumentException($"Info hash must be {Constants.PeerIdLength} characters");

            Length = protocolIdentifier.Length;
            ProtocolIdentifier = protocolIdentifier;
            Reserved = Enumerable.Repeat<byte>(0, Constants.HandshakeReservedBytes).ToArray();
            InfoHash = infoHash;
            PeerId = peerId;
        }

        public PeerHandshake(byte[] handshakeContent)
        {

            Length = handshakeContent.Take(1).Single();
            var totalBytes = 1 + Length + Constants.HandshakeReservedBytes + Constants.InfoHashLength + Constants.PeerIdLength;
            if (handshakeContent.Length < totalBytes)
                throw new ArgumentException($"Handshake content must be {totalBytes} bytes long");

            int offset = 1;
            var protocolBytes = handshakeContent.Skip(offset).Take(Length);
            ProtocolIdentifier = Encoding.GetEncoding(28591).GetString(protocolBytes.ToArray());

            offset += Length;
            Reserved = handshakeContent.Skip(offset).Take(Constants.HandshakeReservedBytes).ToArray();

            offset += Constants.HandshakeReservedBytes;
            InfoHash = handshakeContent.Skip(offset).Take(Constants.InfoHashLength).ToArray();

            offset += Constants.InfoHashLength;
            var peerIdBytes = handshakeContent.Skip(offset).Take(Constants.PeerIdLength);
            PeerId = Encoding.GetEncoding(28591).GetString(peerIdBytes.ToArray());
        }

        public byte[] ToHandshakeBytes()
        {
            //According to specifications, handshake consists of:
            //1 Byte indicating protocol identifier length
            //length bytes indicating protocol
            //8 reserved bytes that should be all zeros as of the time of the writting of this program
            //20 byte info hash
            //20 byte peer id

            var lengthByte = (byte) ProtocolIdentifier.Length;
            var protocolBytes = Encoding.GetEncoding(28591).GetBytes(ProtocolIdentifier);
            var peerIdBytes = Encoding.GetEncoding(28591).GetBytes(PeerId);

            var byteContent = new List<byte>();
            byteContent.Add(lengthByte);
            byteContent.AddRange(protocolBytes);
            byteContent.AddRange(Reserved);
            byteContent.AddRange(InfoHash);
            byteContent.AddRange(peerIdBytes);

            return byteContent.ToArray();
        }
    }
}
