using System.Collections.Generic;

namespace Domain.Models
{
    public class ConnectionData
    {
        public User User { get; set; }
        public ICollection<Group> Groups { get; set; }
        public ICollection<ChatUserViewModel> Contacts { get; set; }
    }
}