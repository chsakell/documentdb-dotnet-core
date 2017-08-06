namespace DocumentDb.Pictures.Models
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using System;

    public class PictureItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "dateCreated")]
        public DateTime DateCreated { get; set; }
    }
}