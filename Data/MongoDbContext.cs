using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SecureNetBackend.Models;

namespace SecureNetBackend.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("MongoDb");
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase("SecureNetDB");
        }

        public IMongoCollection<PCInfo> PCs => _database.GetCollection<PCInfo>("PCs");
        public IMongoCollection<BlacklistItem> Blacklist => _database.GetCollection<BlacklistItem>("Blacklist");
    }
}
