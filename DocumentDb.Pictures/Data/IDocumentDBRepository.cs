using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DocumentDb.Pictures.Data
{
    public interface IDocumentDBRepository<DatabaseDB>
    {
        Task<T> GetItemAsync<T>(string id) where T : class;

        Task<T> GetItemAsync<T>(string id, string partitionKey) where T : class;

        Task<Document> GetDocumentAsync(string id, string partitionKey);

        Task<IEnumerable<T>> GetItemsAsync<T>() where T : class;

        Task<IEnumerable<T>> GetItemsAsync<T>(Expression<Func<T, bool>> predicate) where T : class;

        Task<Document> CreateItemAsync<T>(T item) where T : class;

        Task<Document> CreateItemAsync<T>(T item, RequestOptions options) where T : class;

        Task<Document> UpdateItemAsync<T>(string id, T item) where T : class;

        Task<ResourceResponse<Attachment>> CreateAttachmentAsync(string attachmentsLink, object attachment, RequestOptions options);

        Task<ResourceResponse<Attachment>> ReadAttachmentAsync(string attachmentLink, string partitionkey);

        Task<ResourceResponse<Attachment>> ReplaceAttachmentAsync(Attachment attachment, RequestOptions options);

        Task DeleteItemAsync(string id);

        Task DeleteItemAsync(string id, string partitionKey);

        Task<StoredProcedureResponse<dynamic>> ExecuteStoredProcedureAsync(string procedureName, string query, string partitionKey);

        Task InitAsync(string collectionId);
    }
}
