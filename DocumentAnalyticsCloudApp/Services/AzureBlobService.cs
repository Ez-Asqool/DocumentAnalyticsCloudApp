using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace DocumentAnalyticsCloudApp.Services
{

    /*
     * AzureBlobService is a service responsible for handling file storage operations in Azure Blob Storage.
     * AzureBlobService provides UploadFileAsync() function  to upload files, generate secure download URLs using SAS tokens throw GenerateSasUrl() function,
     * and delete files from the azure blob container by using DeleteFileAsync() function.
     * 
     * -- Developed By: Ez-Aldeen Asqool (E.A Developer) --
     */
    public class AzureBlobService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobService(IConfiguration config)
        {
            var connStr = config["AzureBlobStorage:ConnectionString"];
            var containerName = config["AzureBlobStorage:ContainerName"];
            _containerClient = new BlobContainerClient(connStr, containerName);
            _containerClient.CreateIfNotExists();
        }

        //This function responsible for uploading new file 
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);

            var headers = new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = "application/octet-stream",
                ContentDisposition = $"attachment; filename=\"{fileName}\""
            };

            await blobClient.UploadAsync(fileStream, overwrite: true);
            await blobClient.SetHttpHeadersAsync(headers); 

            return GenerateSasUrl(blobClient, fileName); 
        }

        private string GenerateSasUrl(BlobClient blobClient, string originalFileName)
        {
            if (!blobClient.CanGenerateSasUri)
                throw new InvalidOperationException();

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMonths(6)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            sasBuilder.ContentDisposition = $"attachment; filename={originalFileName}";

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }

        public async Task DeleteFileAsync(string blobFileName)
        {
            var blob = _containerClient.GetBlobClient(blobFileName);
            await blob.DeleteIfExistsAsync();
        }
    }
}
