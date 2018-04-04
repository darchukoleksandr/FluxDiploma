namespace Domain.Models
{
    public class PgpKeyPair
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
        public string Owner { get; set; }
    }
}
