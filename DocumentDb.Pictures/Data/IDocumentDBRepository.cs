using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DocumentDb.Pictures.Data
{
    public interface IDocumentDBRepository<T> where T : class
    {
        Task<T> GetItemAsync(string id);

        Task<T> GetItemAsync(string id, string partitionKey);

        Task<Document> GetDocumentAsync(string id, string partitionKey);

        Task<IEnumerable<T>> GetItemsAsync();

        Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate);

        Task<Document> CreateItemAsync(T item);

        Task<Document> CreateItemAsync(T item, RequestOptions options);

        Task<Document> UpdateItemAsync(string id, T item);

        Task<ResourceResponse<Attachment>> CreateAttachmentAsync(string attachmentsLink, object attachment, RequestOptions options);

        Task<ResourceResponse<Attachment>> ReadAttachmentAsync(string attachmentLink, string partitionkey);

        Task<ResourceResponse<Attachment>> ReplaceAttachmentAsync(Attachment attachment, RequestOptions options);

        Task DeleteItemAsync(string id);

        Task DeleteItemAsync(string id, string partitionKey);

        Task<StoredProcedureResponse<dynamic>> ExecuteStoredProcedureAsync(string procedureName, string query, string partitionKey);

        Task InitAsync(string collectionId);
    }
}
