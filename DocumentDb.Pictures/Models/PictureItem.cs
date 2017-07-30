namespace DocumentDb.Pictures.Models
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    public class PictureItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "isApproved")]
        public bool Approved { get; set; }

        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }
    }

    //public class PictureItem
    //{
    //    [JsonProperty(PropertyName = "id")]
    //    public string Id { get; set; }

    //    [JsonProperty(PropertyName = "name")]
    //    public string Name { get; set; }

    //    [JsonProperty(PropertyName = "description")]
    //    public string Description { get; set; }

    //    [JsonProperty(PropertyName = "isComplete")]
    //    public bool Completed { get; set; }
    //}
}