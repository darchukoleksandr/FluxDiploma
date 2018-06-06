using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Domain.Models;
using Domain.Repository;

namespace DAL.MongoDb.Repository
{
    public class GroupRepository : IGroupRepository
    {
        private readonly IMongoCollection<Group> _mongoCollection;

        public GroupRepository()
        {
            _mongoCollection = MongoCollectionsProvider.GroupCollection;
        }

        public async Task Create(Group group)
        {
            await _mongoCollection.InsertOneAsync(group);
        }

        public async Task<Group> GetById(Guid id)
        {
            var filter = Builders<Group>.Filter.Eq(group => group.Id, id);
            var result = await _mongoCollection.Find(filter).FirstOrDefaultAsync();
            return result;
        }

        public async Task<Group> GetByIdIncludeMessages(Guid id)
        {
            var groupFilter = Builders<Group>.Filter.Eq(group => group.Id, id);
            var groupProjection = Builders<Group>.Projection.Exclude(group => group.Messages);
            var result = await _mongoCollection.Find(groupFilter)
                .Project<Group>(groupProjection).FirstOrDefaultAsync();

            var messages = await _mongoCollection.Find(groupFilter)
                .Project(group => group.Messages).Limit(25).FirstOrDefaultAsync();
            result.Messages = messages;

            return result;
        }
        
        public async Task<Group> GetByIdExcludeMessages(Guid id)
        {
            var groupFilter = Builders<Group>.Filter.Eq(group => group.Id, id);
            var groupProjection = Builders<Group>.Projection.Exclude(group => group.Messages);
            var result = await _mongoCollection.Find(groupFilter).Project<Group>(groupProjection).FirstOrDefaultAsync();

            return result;
        }

        public async Task<Message> GetMessageById(Guid groupId, Guid messsageId)
        {
            var result = await _mongoCollection.Find(group => group.Id == groupId)
                .Project(group => group.Messages.First(message => message.Id == messsageId))
                .FirstOrDefaultAsync();
            return result;
        }

        public async Task<ICollection<Message>> GetMessages(Guid groupId, int skip = 0, int amount = 25)
        {
            var filter = Builders<Group>.Filter.Eq(group => group.Id, groupId);
            var projection = Builders<Group>.Projection.Include(group => group.Messages);
            var result = await _mongoCollection.Find(filter)
                .Project<Message>(projection).Skip(skip * amount)
                .Limit(amount).ToListAsync();
            return result;
        }

        public async Task InsertMessage(Guid groupId, Message message)
        {
            message.Id = Guid.NewGuid();

            var filterDefinition = Builders<Group>.Filter.Eq(group => group.Id, groupId);
            var updateDefinition = Builders<Group>.Update.Push(group => group.Messages, message); // AddToSet?
            await _mongoCollection.UpdateOneAsync(filterDefinition, updateDefinition);
        }

        public async Task LeaveGroup(Guid groupId, string userEmail)
        {
            var groupFilter = Builders<Group>.Filter.Eq(group => group.Id, groupId);
            var keyFilter = Builders<GroupUserPublicKey>.Filter.Eq(key => key.Email, userEmail);
            var updateDefinition = Builders<Group>.Update.PullFilter(group => group.UsersPublicKeys, keyFilter);
            await _mongoCollection.UpdateOneAsync(groupFilter, updateDefinition);
        }

        public async Task<IEnumerable<string>> GetReceipents(Guid id)
        {
            var filter = Builders<Group>.Filter.Eq(chat => chat.Id, id);
            var receipents = await _mongoCollection.Find(filter)
                .Project(group => group.UsersPublicKeys).ToListAsync();
            var result = receipents.SelectMany(users => users);
            return result.Select(user => user.Email);
        }

        public async Task<IEnumerable<Group>> Search(string groupName)
        {
            var filter1 = Builders<Group>.Filter.Eq(chat => chat.Type, GroupType.Open);
//            var filter2 = Builders<Group>.Filter.Eq(chat => chat.Type, GroupType.Channel);
            var filter3 = Builders<Group>.Filter.Regex(chat => chat.Name, $"/{groupName}/");
//            var filter = filter1 & filter2 & filter3;
            var filter = filter1 & filter3;

            var projection = Builders<Group>.Projection
                .Exclude(group => group.Messages)
                .Exclude(group => group.UsersPublicKeys);
            return await _mongoCollection.Find(filter).Project<Group>(projection).Limit(10).ToListAsync();
        }

        public async Task AddNewParcipantPublicKey(Guid groupId, GroupUserPublicKey publicKey)
        {
            var filterDefinition = Builders<Group>.Filter.Eq(group => group.Id, groupId);
            var updateDefinition = Builders<Group>.Update.Push(group => group.UsersPublicKeys, publicKey);
            await _mongoCollection.UpdateOneAsync(filterDefinition, updateDefinition);
        }

        #region unused

        public Task DeleteById(Guid id)
        {
            throw new NotImplementedException();
        }
        
        public Task Update(Group item)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
