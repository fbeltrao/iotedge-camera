using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;

namespace CameraModule.Models
{
    public static class IOUtils
    {
        public static bool TryRemoveFolder(string directory)
        {
            try
            {
                foreach (var f in Directory.GetFiles(directory))
                {
                    File.Delete(f);
                }

                Directory.Delete(directory);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error clearing folder {directory}");
            }

            return false;
        }

        internal static async Task<string> UploadFileAsync(string folder, string localFilePath, CameraConfiguration configuration)
        {
            if (CloudStorageAccount.TryParse($"DefaultEndpointsProtocol=https;AccountName={configuration.StorageAccount};AccountKey={configuration.StorageKey};EndpointSuffix=core.windows.net", out var cloudStorageAccount))
            {
                var blobClient = cloudStorageAccount.CreateCloudBlobClient();

                var containerName = configuration.ModuleId;
                if (string.IsNullOrEmpty(containerName))
                    containerName = "camera";
                Logger.Log($"Using container {containerName}");
                var containerReference = blobClient.GetContainerReference(containerName);
                
                if (await containerReference.CreateIfNotExistsAsync())
                {
                    Logger.Log($"Container {containerName} created");
                }

                var filename = Path.GetFileName(localFilePath);

                var blobName = $"{configuration.DeviceId}/{folder}/{filename}";
                var appendBlob = containerReference.GetAppendBlobReference(blobName);

                await appendBlob.UploadFromFileAsync(localFilePath);
                Logger.Log($"File {localFilePath} copied to blob {blobName}");

                return appendBlob.Uri.ToString();
            }
            else
            {
                Logger.Log("Failed to create cloud storage from connection string");
                return string.Empty;
            }
        }
    }
}