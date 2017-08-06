namespace DocumentDb.Pictures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using System.Collections.ObjectModel;
    using System.IO;
    using DocumentDb.Pictures.Models;

    public static class DocumentDBRepository<T> where T : class
    {
        private static readonly string Endpoint = "https://localhost:8081";
        private static readonly string Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private static readonly string DatabaseId = "Gallery";
        private static readonly string CollectionId = "Pictures";
        private static DocumentClient client;
        private static DocumentCollection collection;

        public static async Task<T> GetItemAsync(string id)
        {
            try
            {
                Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<T> GetItemAsync(string id, string partitionKey)
        {
            try
            {
                if (client == null)
                {
                    client = new DocumentClient(new Uri(Endpoint), Key);
                }

                Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync()
        {
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync(T item)
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        }

        public static async Task<Document> CreateItemAsync(T item, RequestOptions options)
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item, options);
        }

        public static async Task<Document> UpdateItemAsync(string id, T item)
        {
            return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
        }

        public static async Task DeleteItemAsync(string id)
        {
            await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id));
        }

        public static async Task DeleteItemAsync(string id, string partitionKey)
        {
            await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
        }

        public static async Task<ResourceResponse<Attachment>> CreateAttachmentAsync(string attachmentsLink, object attachment, RequestOptions options)
        {
            return await client.CreateAttachmentAsync(attachmentsLink, attachment, options);
        }

        public static async Task<ResourceResponse<Attachment>> ReplaceAttachmentAsync(Attachment attachment, RequestOptions options)
        {
            return await client.ReplaceAttachmentAsync(attachment, options);
        }

        public static async Task<ResourceResponse<Attachment>> ReadAttachmentAsync(string attachmentLink, string partitionkey)
        {
            return await client.ReadAttachmentAsync(attachmentLink, new RequestOptions() { PartitionKey = new PartitionKey(partitionkey) });
        }

        public static async Task<StoredProcedureResponse<dynamic>> ExecuteStoredProcedureAsync(string procedureName, string query, string partitionKey)
        {
            StoredProcedure storedProcedure = client.CreateStoredProcedureQuery(collection.StoredProceduresLink)
                                    .Where(sp => sp.Id == procedureName)
                                    .AsEnumerable()
                                    .FirstOrDefault();

            return await client.ExecuteStoredProcedureAsync<dynamic>(storedProcedure.SelfLink, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) }, query);
        }

        public static void Initialize()
        {
            client = new DocumentClient(new Uri(Endpoint), Key);
            CreateDatabaseIfNotExistsAsync().Wait();
            collection = CreateCollectionIfNotExistsAsync("category").Result;
            CreateTriggerIfNotExistsAsync().Wait();
            CreateStoredProcedureIfNotExistsAsync().Wait();
            InitGalleryAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
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

        private static async Task<ResourceResponse<DocumentCollection>> CreateCollectionIfNotExistsAsync(string partitionkey = null)
        {
            try
            {
                return await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    if (string.IsNullOrEmpty(partitionkey))
                    {
                        return await client.CreateDocumentCollectionAsync(
                            UriFactory.CreateDatabaseUri(DatabaseId),
                            new DocumentCollection { Id = CollectionId },
                            new RequestOptions { OfferThroughput = 1000 });
                    }
                    else
                    {
                        return await client.CreateDocumentCollectionAsync(
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

        private static async Task CreateTriggerIfNotExistsAsync()
        {
            string triggersLink = collection.TriggersLink;
            const string TriggerName = "createDate";

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
                    Body = File.ReadAllText(Path.Combine(Config.ContentRootPath, @"Data\Triggers\createDate.js")),
                    TriggerOperation = TriggerOperation.Create,
                    TriggerType = TriggerType.Pre
                };

                await client.CreateTriggerAsync(triggersLink, trigger);
            }
        }

        private static async Task CreateStoredProcedureIfNotExistsAsync()
        {
            string storedProceduresLink = collection.StoredProceduresLink;
            const string StoredProcedureName = "bulkDelete";

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
                    Body = File.ReadAllText(Path.Combine(Config.ContentRootPath, @"Data\StoredProcedures\bulkDelete.js"))
                };
                storedProcedure = await client.CreateStoredProcedureAsync(storedProceduresLink,
            storedProcedure);
            }
        }

        private static async Task InitGalleryAsync()
        {
            var items = await DocumentDBRepository<PictureItem>.GetItemsAsync();
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
                        Document document = await DocumentDBRepository<PictureItem>.CreateItemAsync(item, options);

                        new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);
                        var attachment = new Attachment { ContentType = contentType, Id = "wallpaper", MediaLink = string.Empty };
                        var input = new byte[File.OpenRead(filePath).Length];
                        File.OpenRead(filePath).Read(input, 0, input.Length);
                        attachment.SetPropertyValue("file", input);
                        ResourceResponse<Attachment> createdAttachment = await DocumentDBRepository<PictureItem>.CreateAttachmentAsync(document.AttachmentsLink, attachment, new RequestOptions() { PartitionKey = new PartitionKey(item.Category) });
                    }
                }
            }
        }
    }
}