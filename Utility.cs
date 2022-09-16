using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanPlateBot
{
    public class userScore
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Name { get; set; }
        public int score { get; set; }

        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}