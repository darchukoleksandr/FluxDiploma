using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Bson.Serialization.Attributes;
using ProtoBuf;

namespace Domain.Models
{
    [ProtoContract]
    public class Group
    {
        [BsonId]
        [ProtoMember(1)]
        public Guid Id { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public ICollection<GroupUserPublicKey> UsersPublicKeys { get; set; } = new Collection<GroupUserPublicKey>();
        [ProtoMember(4)]
        public string Owner { get; set; }
        [ProtoMember(5)]
        public GroupType Type { get; set; }
        [ProtoMember(6)]
        public ICollection<Message> Messages { get; set; } = new Collection<Message>();
    }
    
    public enum GroupType : byte
    {
        Open = 0,
        Closed = 1,
        Secret = 2,
    }

    [ProtoContract]
    public class GroupUserPublicKey
    {
        [ProtoMember(1)]
        public string Email { get; set; }
        [ProtoMember(2)]
        public byte[] PublicKey { get; set; }
    }
    
    [ProtoContract]
    public class GroupUserPrivateKey
    {
        [ProtoMember(1)]
        public Guid GroupId { get; set; }
        [ProtoMember(2)]
        public byte[] PrivateKey { get; set; }
    }
    
    [ProtoContract]
    public class UserGroupPrivateKeyInfo
    {
        [ProtoMember(1)]
        public string Email { get; set; }
        [ProtoMember(2)]
        public byte[] PrivateKey { get; set; }
    }
}
