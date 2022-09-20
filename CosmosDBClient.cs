using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace CleanPlateBot
{
    public class CosmosDBClient
    {
        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The container we will create.
        private Container container;


        public async Task AddItemsToContainerAsync(string EndpointUri, string PrimaryKey, string id, string userName, int score)
        {
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });
            this.container = this.cosmosClient.GetContainer("Clean-Plate-Info", "Score-Info");
            userScore userScore = new userScore
            {
                Id = id,
                name = userName,
                score = score,

            };


            ItemResponse<userScore> AddResponse = await this.container.UpsertItemAsync<userScore>(userScore, new PartitionKey(userScore.score));

            
        }
        // </AddItemsToContainerAsync>
    
        public async Task<List<userScore>> QueryItemsAsync(string id, string userName, string EndpointUri, string PrimaryKey)
        {
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });
            this.container = this.cosmosClient.GetContainer("Clean-Plate-Info", "Score-Info");

            var sqlQueryText = $"SELECT * FROM c WHERE c.id = '{id}' ORDER BY c._ts DESC";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<userScore> queryResultSetIterator = this.container.GetItemQueryIterator<userScore>(queryDefinition);

            List<userScore> userScores = new List<userScore>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<userScore> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (userScore userScore in currentResultSet)
                {
                    userScores.Add(userScore);
             
                }
            }
            return userScores;
        }

    


    }
} 