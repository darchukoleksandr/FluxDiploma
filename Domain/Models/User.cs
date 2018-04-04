using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Models
{
    public class User
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Email { get; set; }
        public ICollection<TypeValueClaim> Claims { get; set; } = new Collection<TypeValueClaim>();
        /// <summary>
        /// Represents user contact list. Each <see cref="String"/> represents user email.
        /// </summary>
        public ICollection<string> Contacts { get; set; } = new Collection<string>();
        public ICollection<GroupUserPrivateKey> PrivateKeys { get; set; } = new Collection<GroupUserPrivateKey>();
    }

    public class TypeValueClaim
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class ChatUserViewModel
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Email { get; set; }
        public ICollection<TypeValueClaim> Claims { get; set; } = new Collection<TypeValueClaim>();
    }
}
