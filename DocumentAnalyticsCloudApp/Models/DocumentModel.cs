using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DocumentAnalyticsCloudApp.Models
{
    public class DocumentModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string ClassificationPath { get; set; } = "Unclassified";
        public double ClassificationTime { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
