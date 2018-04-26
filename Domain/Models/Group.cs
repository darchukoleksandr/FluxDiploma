using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Models
{
    public class Group
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ICollection<GroupUserPublicKey> UsersPublicKeys { get; set; } = new Collection<GroupUserPublicKey>();
        public string Owner { get; set; }
        public string Picture { get; set; }
        public GroupType Type { get; set; }
        public ICollection<Message> Messages { get; set; } = new Collection<Message>();
    }
    
    public enum GroupType : byte
    {
        Open = 0,
        Closed = 1,
        Secret = 2,
        Personal = 3,
        Channel = 4
    }

    public class GroupUserPublicKey
    {
        public string Email { get; set; }
        public byte[] PublicKey { get; set; }
    }
    
    public class GroupUserPrivateKey
    {
        public Guid GroupId { get; set; }
        public byte[] PrivateKey { get; set; }
    }
    
    public class UserGroupPrivateKeyInfo
    {
        public string Email { get; set; }
        public byte[] PrivateKey { get; set; }
    }
}
