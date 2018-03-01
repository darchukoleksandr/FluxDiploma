using ProtoBuf;

namespace Domain.Models
{
    [ProtoContract]
    public class PgpKeyPair
    {
        [ProtoMember(1)]
        public byte[] PublicKey { get; set; }
        [ProtoMember(2)]
        public byte[] PrivateKey { get; set; }
        [ProtoMember(3)]
        public string Owner { get; set; }
    }
}
