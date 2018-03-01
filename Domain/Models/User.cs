using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Bson.Serialization.Attributes;
using ProtoBuf;

namespace Domain.Models
{
    [ProtoContract]
    public class User
    {
        [BsonId]
        [ProtoMember(1)]
        public Guid Id { get; set; }
        [ProtoMember(2)]
        public string Email { get; set; }
        [ProtoMember(3)]
        public ICollection<TypeValueClaim> Claims { get; set; } = new Collection<TypeValueClaim>();
        /// <summary>
        /// Represents user contact list. Each <see cref="String"/> represents user email.
        /// </summary>
        [ProtoMember(4)]
        public ICollection<string> Contacts { get; set; } = new Collection<string>();
        [ProtoMember(5)]
        public ICollection<GroupUserPrivateKey> PrivateKeys { get; set; } = new Collection<GroupUserPrivateKey>();
    }

    [ProtoContract]
    public class TypeValueClaim
    {
        [ProtoMember(1)]
        public string Type { get; set; }
        [ProtoMember(2)]
        public string Value { get; set; }
    }

    [ProtoContract]
    public class ChatUserViewModel
    {
        [ProtoMember(1)]
        [BsonId]
        public Guid Id { get; set; }
        [ProtoMember(2)]
        public string Email { get; set; }
        [ProtoMember(3)]
        public ICollection<TypeValueClaim> Claims { get; set; } = new Collection<TypeValueClaim>();
    }
}
