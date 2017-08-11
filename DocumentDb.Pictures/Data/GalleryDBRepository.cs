using DocumentDb.Pictures.Models;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentDb.Pictures.Data
{
    public class GalleryDBRepository : DocumentDBRepositoryBase<GalleryDBRepository>, IDocumentDBRepository<GalleryDBRepository>
    {
        public GalleryDBRepository()
        {
            Endpoint = "https://localhost:8081";
            Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            DatabaseId = "Gallery";
        }

        public override async Task InitAsync(string collectionId)
        {
            if (client == null)
                client = new DocumentClient(new Uri(Endpoint), Key);

            CollectionId = collectionId;
            collection = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
        }
    }
}
