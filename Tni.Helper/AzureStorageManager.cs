using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Tni.Helper.Entities;
using Tni.Helper.Extensions;

namespace Tni.Helper
{
    /// <summary>
    /// Sets of commands of more ease of use of Azure Blob Storage.
    /// </summary>
    public class AzureStorageManager
    {
        /// <summary>
        /// Initializes with the provided credentials
        /// </summary>
        /// <param name="info"></param>
        public AzureStorageManager(string connectionString)
        {
            ConnectionString = connectionString;
        }

        #region Properties
        /// <summary>
        /// Info informations to connect to Azure Blob Storage.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// An work container can be specified so that all uploads, downloads, list
        /// can refer all the time to that container without the need to be specified all the time.
        /// </summary>
        public string WorkContainer { get; set; }
        #endregion

        #region Public methods
        /// <summary>
        /// Gets a list of obtained resource files from a specific/work container.
        /// </summary>
        /// <param name="container">Container to be analyzed (Optional if WorkContainer is specified)</param>
        /// <returns></returns>
        public async Task<List<string>> GetList(string container = default)
        {
            container = container ?? WorkContainer;
            if (string.IsNullOrWhiteSpace(container))
                throw new Exception("No container was provided");

            var blobContainer = new BlobContainerClient(ConnectionString, container);

            var exists = await blobContainer.ExistsAsync();

            if (exists == default || !exists.Value)
                return default;

            var response = new List<string>();
            var resultSegment = blobContainer.GetBlobs();
            return resultSegment.Select(s => s.Name).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container">Container to be analyzed (Optional if WorkContainer is specified)</param>
        /// <returns></returns>
        public async Task<AzureBlobStorageStat> GetSize(string container = default)
        {
            container = container ?? WorkContainer;
            if (string.IsNullOrWhiteSpace(container))
                throw new Exception("No container was provided");

            var blobContainer = new BlobContainerClient(ConnectionString, container);

            var exists = await blobContainer.ExistsAsync();
            if (exists == default || !exists.Value)
                return default;

            var resultSegment = blobContainer.GetBlobs();
            return new AzureBlobStorageStat()
            {
                Size = resultSegment.Where(w => w.Properties.ContentLength.HasValue)
                .Select(s => s.Properties.ContentLength.Value).Sum(),
                Files = resultSegment.Count(),

                First = resultSegment.Min(m => m.Properties.CreatedOn.Value).DateTime,
                Last = resultSegment.Max(m => m.Properties.CreatedOn.Value).DateTime
            };
        }

        /// <summary>
        /// Uploads a resource file into azure blob storage.
        /// If an work container is established, then container parameter becomes optional.
        /// </summary>
        /// <param name="file">Resource file to be uploaded</param>
        /// <param name="overwrite">Indicator if should overwrite</param>
        /// <param name="container">Container where to be uploaded (Optional if WorkContainer is settled)</param>
        /// <param name="createContainerIfNotExists">Create the specified container if not existing. (Optional)</param>
        /// <exception cref="Exception"></exception>
        public async Task UploadFile(FileInfo file, bool overwrite = false, string container = default, bool createContainerIfNotExists = false)
        {
            file.Refresh();
            if (!file.Exists)
                throw new Exception($"File [{file.FullName}] was not found");

            container = container ?? WorkContainer;
            if (string.IsNullOrWhiteSpace(container))
                throw new Exception("No container was provided");

            var blobContainer = new BlobContainerClient(ConnectionString, container);
            if (!await blobContainer.ExistsAsync())
            {
                if (createContainerIfNotExists)
                {
                    await blobContainer.CreateAsync();
                    await blobContainer.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
                }
                else
                    throw new Exception($"Container [{container}] does not exist.");
            }

            var blob = blobContainer.GetBlobClient(file.Name);
            if (!overwrite && await blob.ExistsAsync())
                throw new InvalidOperationException($"Cannot upload file [{file.Name}], already exists.");

            await blob.UploadAsync(file.FullName, overwrite);
        }

        /// <summary>
        /// Downloads a resource file from azure blob storage.
        /// If an work container is established, then container parameter becomes optional.
        /// </summary>
        /// <param name="file">Resource file to be downloaded.</param>
        /// <param name="container">Container from where to be downloaded (Optional if WorkContainer is settled)</param>
        /// <returns>Returns a FileInfo object which points to the downloaded file.</returns>
        /// <exception cref="Exception"></exception>
        public async Task<FileInfo> DownloadFile(FileInfo file, string container = default)
        {
            container = container ?? WorkContainer;
            if (string.IsNullOrWhiteSpace(container))
                throw new Exception("No container was provided");

            var blobContainer = new BlobContainerClient(ConnectionString, container);
            var exists = await blobContainer.ExistsAsync();
            if (!exists || !exists.Value)
                throw new Exception($"Container [{container}] does not exist.");

            var blob = blobContainer.GetBlobClient(file.Name);
            BlobDownloadInfo download = await blob.DownloadAsync();
            using (var downloadFileStream = File.OpenWrite(file.FullName))
            {
                await download.Content.CopyToAsync(downloadFileStream);
                downloadFileStream.Close();
            }

            return file;
        } 
        #endregion
    }
}