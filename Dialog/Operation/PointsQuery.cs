using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Azure.Cosmos;


namespace Microsoft.BotBuilderSamples
{
    public class PointQueryDialog : ComponentDialog
    {
        //protected readonly IConfiguration Configuration;
        private static readonly string EndpointUri = "https://f0d58c44-0ee0-4-231-b9ee.documents.azure.com:443/";

        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "stYGaSXtlvr5mMQGB3oh4So4EEFypXfEdV2XUPlS0zpj5BZ14KSX7aSdAC7s9Q9mHPrF5WJ1NV45XZBM4vKOzA==";
               
        private static readonly string databaseId = "ChatBotDB";
        private static readonly string containerId = "Storage";
        private static readonly string partitionKey ="/score";
        private const string UserInfo = "value-userInfo";
        private readonly CosmosDBClient _cosmosDBClient;
        private readonly UserState _userState;

        public PointQueryDialog(UserState userState, CosmosDBClient cosmosDBClient) : base(nameof(PointQueryDialog))
        {
            //Configuration = configuration;
            _cosmosDBClient = cosmosDBClient;
            _userState = userState;

            var waterfallSteps = new WaterfallStep[]
            {
                ShowPointsStepAsync,
                ConfirmStepAsync,
                SummaryStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new PointAwardDialog(_userState, _cosmosDBClient));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowPointsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
           var score = 0;
           var userProfile = (UserProfile)stepContext.Values[UserInfo];

            List<userScore> userScores = await _cosmosDBClient.QueryItemsAsync(userProfile.Name, EndpointUri, PrimaryKey);
            if (userScores.Count == 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You point is 0."), cancellationToken);
            }
            else
            {
                score = userScores[0].score;
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You point is {score}."), cancellationToken);
            }
            
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to detect your plate?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
 
            if ((bool)stepContext.Result)
            {
                return await stepContext.ReplaceDialogAsync(nameof(PointAwardDialog), cancellationToken);
            }
            else
            {
                var userInfo = (UserProfile)stepContext.Result;
                var accessor = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
                await accessor.SetAsync(stepContext.Context, userInfo, cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Ok. Thank you for your participation."));
                return await stepContext.EndDialogAsync(cancellationToken);
            }
        }
    }
}
    
