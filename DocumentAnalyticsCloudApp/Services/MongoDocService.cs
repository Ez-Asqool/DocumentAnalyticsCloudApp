using DocumentAnalyticsCloudApp.Models;
using MongoDB.Driver;

namespace DocumentAnalyticsCloudApp.Services
{
    /*
     * MongoDocService is responsible for handling all operations related to documents stored in MongoDB Atlas
     * It manages CRUD operations (Create, Read, Update, Delete), and Search operation.
     * 
     * -- Developed By: Ez-Aldeen Asqool (E.A Developer) --
     */
    public class MongoDocService
    {
        private readonly IMongoCollection<DocumentModel> _documents;

        public MongoDocService(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDB:ConnectionString"];
            var databaseName = configuration["MongoDB:DatabaseName"];
            var collectionName = configuration["MongoDB:CollectionName"];

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _documents = database.GetCollection<DocumentModel>(collectionName);
        }

        public async Task AddDocumentAsync(DocumentModel doc)
        {
            await _documents.InsertOneAsync(doc);
        }

        public async Task<List<DocumentModel>> GetAllDocumentsByUserAsync(string userId)
        {
            var filter = Builders<DocumentModel>.Filter.Eq(d => d.UserId, userId);
            return await _documents.Find(filter).ToListAsync();
        }

        public async Task<List<DocumentModel>> SearchDocumentsAsync(string keyword, string userId)
        {
            var filter = Builders<DocumentModel>.Filter.And(
                Builders<DocumentModel>.Filter.Text(keyword),
                Builders<DocumentModel>.Filter.Eq(d => d.UserId, userId)
            );

            return await _documents.Find(filter).ToListAsync();
        }


        public async Task<DocumentModel?> GetDocumentByIdAsync(string id)
        {
            var filter = Builders<DocumentModel>.Filter.Eq(d => d.Id, id);
            return await _documents.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpdateDocumentAsync(DocumentModel updated)
        {
            var filter = Builders<DocumentModel>.Filter.Eq(d => d.Id, updated.Id);
            await _documents.ReplaceOneAsync(filter, updated);
        }

        public async Task DeleteDocumentAsync(string id)
        {
            var filter = Builders<DocumentModel>.Filter.Eq(d => d.Id, id);
            await _documents.DeleteOneAsync(filter);
        }

    }
}
