using System;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repository
{
    public interface IUserRepository : IRepository<User, Guid>
    {
        Task<User> GetByEmail(string email);
        Task<ChatUserViewModel> GetByEmailForContacts(string email);
        Task AddPrivateKey(string userEmail,byte[] privateKey, Guid chatRoomId);
        Task RemoveContact(string userEmail, string contactEmail);
        void AddContact(string userEmail, string contactEmail);
    }
}
