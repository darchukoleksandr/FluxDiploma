using Domain.Models;
using MongoDB.Driver;

namespace DAL.MongoDb
{
    public sealed class MongoCollectionsProvider
    {
        static MongoCollectionsProvider()
        {
            //string connectionString = @"mongodb://admin:root@ cluster0-shard-00-00-thlyj.mongodb.net:27017, cluster0-shard-00-01-thlyj.mongodb.net:27017,cluster0-shard-00-02-thlyj.mongodb.net:27017/test?ssl=true&replicaSet=cluster0-thlyj.mongodb.net&authSource=admin";
            string connectionString = @"mongodb+srv://admin:root@cluster0-thlyj.mongodb.net/test";
            var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
//            settings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls12 };

            _client = new MongoClient(settings);
            var mongoDatabase = _client.GetDatabase("test");

            UserCollection = mongoDatabase.GetCollection<User>("Users");
            GroupCollection = mongoDatabase.GetCollection<Group>("Groups");
        }

        private static readonly MongoClient _client;

        public static IMongoCollection<User> UserCollection { get; }

        public static IMongoCollection<Group> GroupCollection { get; }
    }
}
