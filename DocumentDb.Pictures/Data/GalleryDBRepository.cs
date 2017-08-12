using DocumentDb.Pictures.Models;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentDb.Pictures.Data
{
    public class GalleryDBRepository : DocumentDBRepositoryBase<GalleryDBRepository>, IDocumentDBRepository<GalleryDBRepository>
    {
        public GalleryDBRepository(IConfiguration configuration)
        {
            Endpoint = configuration["DocumentDBEndpoint"];
            Key = configuration["DocumentDBKey"];
            DatabaseId = "Gallery";
        }

        public override async Task InitAsync(string collectionId)
        {
            if (client == null)
                client = new DocumentClient(new Uri(Endpoint), Key);

            if (CollectionId != collectionId)
            {
                CollectionId = collectionId;
                collection = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
            }
        }
    }
}
