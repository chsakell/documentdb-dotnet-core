using DocumentDb.Pictures.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentDb.Pictures.Data
{
    public class DocumentDBInitializer
    {
        private static string Endpoint = string.Empty;
        private static string Key = string.Empty;
        private static DocumentClient client;

        public static void Initialize(IConfiguration configuration)
        {
            Endpoint = configuration["DocumentDBEndpoint"];
            Key = configuration["DocumentDBKey"];

            client = new DocumentClient(new Uri(Endpoint), Key);
            CreateDatabaseIfNotExistsAsync("Gallery").Wait();
            CreateCollectionIfNotExistsAsync("Gallery", "Pictures", "category").Wait();
            CreateTriggerIfNotExistsAsync("Gallery", "Pictures", "createDate", @"Data\Triggers\createDate.js").Wait();
            CreateStoredProcedureIfNotExistsAsync("Gallery", "Pictures", "bulkDelete", @"Data\StoredProcedures\bulkDelete.js").Wait();
            InitGalleryAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync(string DatabaseId)
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync(string DatabaseId, string CollectionId, string partitionkey = null)
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    if (string.IsNullOrEmpty(partitionkey))
                    {
                        await client.CreateDocumentCollectionAsync(
                            UriFactory.CreateDatabaseUri(DatabaseId),
                            new DocumentCollection { Id = CollectionId },
                            new RequestOptions { OfferThroughput = 1000 });
                    }
                    else
                    {
                        await client.CreateDocumentCollectionAsync(
                            UriFactory.CreateDatabaseUri(DatabaseId),
                            new DocumentCollection
                            {
                                Id = CollectionId,
                                PartitionKey = new PartitionKeyDefinition
                                {
                                    Paths = new Collection<string> { "/" + partitionkey }
                                }
                            },
                            new RequestOptions { OfferThroughput = 1000 });
                    }

                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateTriggerIfNotExistsAsync(string databaseId, string collectionId, string triggerName, string triggerPath)
        {
            DocumentCollection collection = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId));

            string triggersLink = collection.TriggersLink;
            string TriggerName = triggerName;

            Trigger trigger = client.CreateTriggerQuery(triggersLink)
                                    .Where(sp => sp.Id == TriggerName)
                                    .AsEnumerable()
                                    .FirstOrDefault();

            if (trigger == null)
            {
                // Register a pre-trigger
                trigger = new Trigger
                {
                    Id = TriggerName,
                    Body = File.ReadAllText(Path.Combine(Config.ContentRootPath, triggerPath)),
                    TriggerOperation = TriggerOperation.Create,
                    TriggerType = TriggerType.Pre
                };

                await client.CreateTriggerAsync(triggersLink, trigger);
            }
        }

        private static async Task CreateStoredProcedureIfNotExistsAsync(string databaseId, string collectionId, string procedureName, string procedurePath)
        {
            DocumentCollection collection = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId));

            string storedProceduresLink = collection.StoredProceduresLink;
            string StoredProcedureName = procedureName;

            StoredProcedure storedProcedure = client.CreateStoredProcedureQuery(storedProceduresLink)
                                    .Where(sp => sp.Id == StoredProcedureName)
                                    .AsEnumerable()
                                    .FirstOrDefault();

            if (storedProcedure == null)
            {
                // Register a stored procedure
                storedProcedure = new StoredProcedure
                {
                    Id = StoredProcedureName,
                    Body = File.ReadAllText(Path.Combine(Config.ContentRootPath, procedurePath))
                };
                storedProcedure = await client.CreateStoredProcedureAsync(storedProceduresLink,
            storedProcedure);
            }
        }

        private static async Task InitGalleryAsync()
        {
            GalleryDBRepository galleryRepository = new GalleryDBRepository();
            await galleryRepository.InitAsync("Pictures");

            var items = await galleryRepository.GetItemsAsync();
            if (items.Count() == 0)
            {
                foreach (var directory in Directory.GetDirectories(Path.Combine(Config.ContentRootPath, @"wwwroot\images\gallery")))
                {
                    foreach (var filePath in Directory.GetFiles(directory))
                    {
                        string category = Path.GetFileName(Path.GetDirectoryName(filePath));
                        string title = Path.GetFileNameWithoutExtension(filePath);
                        string fileName = Path.GetFileName(filePath);
                        string contentType;

                        PictureItem item = new PictureItem()
                        {
                            Category = category,
                            Title = title
                        };

                        RequestOptions options = new RequestOptions { PreTriggerInclude = new List<string> { "createDate" } };
                        Document document = await galleryRepository.CreateItemAsync(item, options);

                        new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);
                        var attachment = new Attachment { ContentType = contentType, Id = "wallpaper", MediaLink = string.Empty };
                        var input = new byte[File.OpenRead(filePath).Length];
                        File.OpenRead(filePath).Read(input, 0, input.Length);
                        attachment.SetPropertyValue("file", input);
                        ResourceResponse<Attachment> createdAttachment = await galleryRepository.CreateAttachmentAsync(document.AttachmentsLink, attachment, new RequestOptions() { PartitionKey = new PartitionKey(item.Category) });
                    }
                }
            }
        }
    }
}
