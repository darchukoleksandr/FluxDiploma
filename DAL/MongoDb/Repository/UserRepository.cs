using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Domain.Models;
using Domain.Repository;

namespace DAL.MongoDb.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _mongoCollection;

        public UserRepository()
        {
            _mongoCollection = MongoCollectionsProvider.UserCollection;
        }

        public async Task Create(User user)
        {
            await _mongoCollection.InsertOneAsync(user);
        }

        public async Task<User> GetByEmail(string email)
        {
            var filter = Builders<User>.Filter.Eq(user => user.Email, email);
            var cursor = await _mongoCollection.FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<User> GetById(Guid id)
        {
            var filter = Builders<User>.Filter.Eq(user => user.Id, id);
            var cursor = await _mongoCollection.FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<ChatUserViewModel> GetByEmailForContacts(string email)
        {
            var filter = Builders<User>.Filter.Eq(user => user.Email, email);
            var projection = Builders<User>.Projection.Expression<ChatUserViewModel>(chatUser => 
                new ChatUserViewModel
                {
                    Id = chatUser.Id,
                    Email = chatUser.Email,
                    Claims = chatUser.Claims
                });
            return await _mongoCollection.Find(filter).Project(projection).FirstOrDefaultAsync();
        }

        public async Task AddPrivateKey(string userEmail, byte[] privateKey, Guid chatRoomId)
        {
            var filter = Builders<User>.Filter.Eq(user => user.Email, userEmail);
            var update = Builders<User>.Update.Push(user => user.PrivateKeys, new GroupUserPrivateKey
            {
                GroupId = chatRoomId, 
                PrivateKey = privateKey
            });
            await _mongoCollection.UpdateOneAsync(filter, update);
        }
        
        public async void AddContact(string userEmail, string contactEmail)
        {
            var filter = Builders<User>.Filter.Eq(user => user.Email, userEmail);
            var update = Builders<User>.Update.Push(user => user.Contacts, contactEmail);
            await _mongoCollection.UpdateOneAsync(filter, update);
        }

        public async Task<IEnumerable<User>> Search(string email)
        {
            var filter = Builders<User>.Filter.Regex(user => user.Email, $"/{email}/");
            var projection = Builders<User>.Projection
                .Exclude(group => group.Contacts)
                .Exclude(group => group.PrivateKeys);
            return await _mongoCollection.Find(filter).Project<User>(projection).Limit(10).ToListAsync();
        }

        public async Task RemoveContact(string userEmail, string contactEmail)
        {
            var filter = Builders<User>.Filter.Eq(user => user.Email, userEmail);
            var update = Builders<User>.Update.Pull(user => user.Contacts, contactEmail);
            await _mongoCollection.UpdateOneAsync(filter, update);
        }

        #region unused

        public Task DeleteById(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task Update(User user)
        {
            var filter = new BsonDocument("Email", user.Email);
            await _mongoCollection.ReplaceOneAsync(filter, user);
        }

        #endregion
    }
}
