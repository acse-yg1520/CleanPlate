using Newtonsoft.Json;


namespace CleanPlateBot
{
    public class userScore
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string name { get; set; }
        public int score { get; set; }

        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}