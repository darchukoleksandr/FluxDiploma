using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Models
{
    public class Message
    {
        [BsonId]
        public Guid Id { get; set; }
        public string SenderEmail { get; set; }
        public string Content { get; set; }
        public DateTime Sended { get; set; }
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
