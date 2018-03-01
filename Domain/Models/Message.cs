using System;
using MongoDB.Bson.Serialization.Attributes;
using ProtoBuf;

namespace Domain.Models
{
    [ProtoContract]
    public class Message
    {
        [ProtoMember(1)]
        [BsonId]
        public Guid Id { get; set; }
        [ProtoMember(2)]
        public string SenderEmail { get; set; }
        [ProtoMember(3)]
        public string Content { get; set; }
        [ProtoMember(4)]
        public DateTime Sended { get; set; }
        [ProtoMember(5)]
        public MessageType Type { get; set; }
    }

    public enum MessageType : byte
    {
        Plain = 0,
        File = 1,
        Picture = 2,
        Info = 3
    }
}
