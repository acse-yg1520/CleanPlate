using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace Microsoft.BotBuilderSamples
{
    public class CosmosDBClient
    {
        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;


        public async Task GetStartedAsync(string EndpointUri, string PrimaryKey, string databaseId, string containerId, string partitionKey )
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });
            await this.CreateDatabaseAsync(databaseId);
            await this.CreateContainerAsync(containerId, partitionKey);
            //await this.AddItemsToContainerAsync(userId, score);

        }

        // <CreateDatabaseAsync>
        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync(string databaseId)
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }
        // </CreateDatabaseAsync>


        // <CreateContainerAsync>
        /// <summary>
        /// Create the container if it does not exist. 
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync(string containerId, string partitionKey)
        {
            // Create a new container
            this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, partitionKey, 400);
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }
        // </CreateContainerAsync>


        // public async Task<bool> CheckUniquenessAsync(string EndpointUri, string PrimaryKey, string imageHash)
        // {
        //     this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });
        //     this.container = this.cosmosClient.GetContainer("ChatBotDB", "Storage");
        //     //Console.WriteLine(imageHash);
        //     var sqlQueryText = $"SELECT c.id FROM c WHERE c.imageHash = '{imageHash}'";

        //     Console.WriteLine("Running query: {0}\n", sqlQueryText);

        //     QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
        //     FeedIterator<userScore> queryResultSetIterator = this.container.GetItemQueryIterator<userScore>(queryDefinition);

        //     while (queryResultSetIterator.HasMoreResults)
        //     {
        //         FeedResponse<userScore> currentResultSet = await queryResultSetIterator.ReadNextAsync();
        //         Console.WriteLine(currentResultSet.Count);
        //         if (currentResultSet.Count == 0)
        //         {
        //             return true;

        //         }
        //         else
        //         {
        //             return false;
        //         }
        //     }
        //     return false;
            
        // }
    
        public async Task AddItemsToContainerAsync(string EndpointUri, string PrimaryKey, string userId, int score)
        {
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });
            this.container = this.cosmosClient.GetContainer("ChatBotDB", "Storage");
            userScore userScore = new userScore
            {
                Id = userId,
                score = score,

            };


            ItemResponse<userScore> AddResponse = await this.container.UpsertItemAsync<userScore>(userScore, new PartitionKey(userScore.score));

            
        }
        // </AddItemsToContainerAsync>
    
        public async Task<List<userScore>> QueryItemsAsync(string userId, string EndpointUri, string PrimaryKey)
        {
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });
            this.container = this.cosmosClient.GetContainer("ChatBotDB", "Storage");

            var sqlQueryText = $"SELECT * FROM c WHERE c.id = '{userId}' ORDER BY c._ts DESC";

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