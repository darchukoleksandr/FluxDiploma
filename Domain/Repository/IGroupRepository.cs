using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repository
{
    using System;

    public interface IGroupRepository : IRepository<Group, Guid>
    {
        Task<Group> GetByIdIncludeMessages(Guid id);
        Task<Message> GetMessageById(Guid groupId, Guid messsageId);
        Task<ICollection<Message>> GetMessages(Guid groupId, int skip = 0, int amount = 25);
        Task InsertMessage(Guid groupId, Message message);
        Task LeaveGroup(Guid groupId, string userEmail);
        Task<IEnumerable<string>> GetReceipents(Guid id);
    }
}
