// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient().AddControllers().AddNewtonsoftJson();

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<UserProfileDialog>();

            //services.AddSingleton<PointQueryDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogBot<UserProfileDialog>>();

            //services.AddTransient<IBot, DialogBot<PointQueryDialog>>();

              // Register Cosmos DB Client
            services.AddSingleton<CosmosDBClient>();
        
            // Use partitioned CosmosDB for storage, instead of in-memory storage.
        //      services.AddSingleton<IStorage>(
        //      new CosmosDbPartitionedStorage(
        //         new CosmosDbPartitionedStorageOptions
        //    {
        //     CosmosDbEndpoint ="https://f0d58c44-0ee0-4-231-b9ee.documents.azure.com:443/",
        //     AuthKey ="stYGaSXtlvr5mMQGB3oh4So4EEFypXfEdV2XUPlS0zpj5BZ14KSX7aSdAC7s9Q9mHPrF5WJ1NV45XZBM4vKOzA==",
        //     DatabaseId = "ChatBotDB",
        //     ContainerId ="botStorage",
        //     CompatibilityMode = false,
        //    }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
