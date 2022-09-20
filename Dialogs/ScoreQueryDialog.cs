using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;

namespace CleanPlateBot
{
    public class ScoreQueryDialog : ComponentDialog
    {

        private readonly string _CosmosEndpoint;
        private readonly string _PrimaryKey;
        private readonly CosmosDBClient _cosmosDBClient;
        
        public ScoreQueryDialog(IConfiguration configuration, CosmosDBClient cosmosDBClient)
            : base(nameof(ScoreQueryDialog))
        {
            
            _CosmosEndpoint = configuration["CosmosEndpoint"];
            _PrimaryKey = configuration["PrimaryKey"];
            _cosmosDBClient = cosmosDBClient;

            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ScoreQueryStepAsync,
                ConfirmResultStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ScoreQueryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            var score = 0;
            var name = stepContext.Context.Activity.From.Name;
            var id = stepContext.Context.Activity.From.Id;
            List<userScore> userScores = await _cosmosDBClient.QueryItemsAsync(id, name, _CosmosEndpoint, _PrimaryKey);
            if (userScores.Count == 0)
            {
                    //await stepContext.Context.SendActivityAsync(MessageFactory.Text(Name), cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your point is 0. Why don't you join in the 'Clean Plate Campaign' from today? Get your reward for the clean plate!"), cancellationToken);
            }
            else
            {
                score = userScores[0].score;
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You get {score} points."), cancellationToken);
            }  

            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Is this result ok?")
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var ret = (bool)stepContext.Result;
            if ((bool)stepContext.Result)// true
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Great!"));
            }
            else // false
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry to hear that."));
            }
            return await stepContext.EndDialogAsync();
        }
    }
}