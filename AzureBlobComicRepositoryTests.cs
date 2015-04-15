
namespace DAL.Tests
{
    using DAL;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Auth;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;

    [TestClass()]
    public class AzureBlobComicRepositoryTests
    {
        [TestMethod()]
        public void AzureUploadingTest()
        {
            string strAccount = "testaccount";
            string containerName = "imgsvr";
            string strKey = "VSMnyOXembmbHGOqfbNaKj600it+xIarQXGVdaun6JCs+cs/bc6jDKooMzOKcEOJqxdU81j8UoLPg==";
            StorageCredentials credential1 = new StorageCredentials(strAccount, Convert.FromBase64String(strKey));
            CloudStorageAccount csa_storageAccount1 = new CloudStorageAccount(credential1, "core.windows.net", true);
            CloudBlobClient client = csa_storageAccount1.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(containerName);
            container.CreateIfNotExists();
            BlobContainerPermissions containerPermissions = new BlobContainerPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Off;
            DateTime expiryTime = DateTime.UtcNow.AddYears(1);

            // Add stored access policies
            containerPermissions.SharedAccessPolicies.Add("rwl", new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = expiryTime,
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List
            });
            containerPermissions.SharedAccessPolicies.Add("rl", new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = expiryTime,
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List
            });
            containerPermissions.SharedAccessPolicies.Add("r", new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = expiryTime,
                Permissions = SharedAccessBlobPermissions.Read
            });
            container.SetPermissions(containerPermissions);
            string rwlSAS = container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), "rwl");
            Trace.WriteLine(rwlSAS);
            string rlSAS = container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), "rl");
            Trace.WriteLine(rlSAS);
            string rSAS = container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), "r");
            Trace.WriteLine(rSAS);

            //Test uploading
            StorageCredentials credential = new StorageCredentials(rwlSAS);
            string blobUriTemplate = "https://{0}.blob.core.windows.net/{1}/{2}";
            foreach (var file in Directory.EnumerateFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TestFiles")))
            {
                CloudBlockBlob blob = new CloudBlockBlob(new Uri(string.Format(blobUriTemplate, strAccount, containerName, Path.GetFileName(file))), credential);
                using (var stream = File.OpenRead(file))
                {
                    string blockId = Convert.ToBase64String(Encoding.Unicode.GetBytes(Path.GetFileName(file)));
                    blob.Properties.ContentType = "image/jpeg";
                    blob.Properties.CacheControl = "public, max-age=2592000"; // one month
                    blob.PutBlock(blockId, stream, null);
                    blob.PutBlockList(new string[] { blockId });
                    Console.WriteLine("Done with {0}", Path.GetFileName(file));
                }
                Assert.AreEqual(blob.DownloadBlockList(BlockListingFilter.Committed).Count(), 1);
            }
        }
    }
}
