namespace DocumentDb.Pictures.Models
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    public class PictureItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [Required]
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [Required]
        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "dateCreated")]
        public DateTime DateCreated { get; set; }
    }
}