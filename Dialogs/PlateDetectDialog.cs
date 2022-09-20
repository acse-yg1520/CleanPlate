using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Connector.Authentication;

namespace CleanPlateBot
{
    public class PlateDetectDialog : ComponentDialog
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _msAppId;
        private readonly string _msAppPassword;
        private readonly string _predictionEndpoint;
        private readonly string _predictionKey;
        private readonly string _predictionProjetId;
        private readonly string _predictionPublishedName;
        private readonly string _CosmosEndpoint;
        private readonly string _PrimaryKey;
        private readonly CosmosDBClient _cosmosDBClient;
        


        public PlateDetectDialog(IHttpClientFactory clientFactory, IConfiguration configuration, CosmosDBClient cosmosDBClient)
            : base(nameof(PlateDetectDialog))
        {
            _clientFactory = clientFactory;
           _cosmosDBClient = cosmosDBClient;
            _msAppId = configuration["MicrosoftAppId"];
            _msAppPassword = configuration["MicrosoftAppPassword"];

            _predictionEndpoint = configuration["PredictionEndpoint"];
            _predictionKey = configuration["PredictionKey"];
            _predictionProjetId = configuration["PredictionProjetId"];
            _predictionPublishedName = configuration["PredictionPublishedName"];
            _CosmosEndpoint = configuration["CosmosEndpoint"];
            _PrimaryKey = configuration["PrimaryKey"];

            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt), Helper.ImagePromptValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new FollowUpDialog());

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                UploadImageStepAsync,
                PlateDetectStepAsync,
                ConfirmResultStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> UploadImageStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter UploadImageStepAsync"));

            return await stepContext.PromptAsync(
                nameof(AttachmentPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please upload your plate picture."),
                    RetryPrompt = MessageFactory.Text("The attachment is not an image file. Please upload an image file again!"),
                }, cancellationToken);

            //return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PlateDetectStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter PlateDetectStepAsync"));

            var attachment = ((IList<Attachment>)stepContext.Result)?.FirstOrDefault();
            if (attachment == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("No picture in the attachment"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            
            var name = stepContext.Context.Activity.From.Name;
            var id = stepContext.Context.Activity.From.Id;
            var predictions = await DetectionResult(stepContext, attachment, cancellationToken);
            stepContext.Values["PredictionResult"] = predictions;

            // Store the score if clean plate
            var score = 0;
            List<userScore> userScores = await _cosmosDBClient.QueryItemsAsync(id, name, _CosmosEndpoint, _PrimaryKey);
            if (userScores.Count == 0)
                {
                    score = 0;
                }
            else
                {
                    score = userScores[0].score;
                }

            if (predictions.Equals("Clean"))
            {
                score += 5;
                await stepContext.Context.SendActivityAsync("Congratulations! You have finished all the food. Thank you for your contribution to reduce food waste.");
                await _cosmosDBClient.AddItemsToContainerAsync( _CosmosEndpoint, _PrimaryKey, id, name, score);

                return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Are you happy with this result?"),
                        RetryPrompt = MessageFactory.Text("Input invalid. is this result ok?")
                    }, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Sorry! It seems that you haven't finished all the food. \n\n To better improve the service, could you answer two follow up questions?"),
                    RetryPrompt = MessageFactory.Text("Input invalid.")
                }, cancellationToken);
            }
            //return await stepContext.NextAsync(null,cancellationToken);
        }

        private async Task<string> DetectionResult(WaterfallStepContext stepContext, Attachment attachment, CancellationToken cancellationToken)
        {
            var credentialToken = await new MicrosoftAppCredentials(_msAppId, _msAppPassword).GetTokenAsync();
            var image = await Helper.DownloadImage(
                                    _clientFactory.CreateClient(),
                                    credentialToken,
                                    attachment,
                                    stepContext.Context.Activity.ChannelId,
                                    cancellationToken);

            return await Helper.GetImagePredictionsAsync(image, _predictionKey, _predictionEndpoint, _predictionProjetId, _predictionPublishedName);
        }

        private async Task<DialogTurnResult> ConfirmResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string dialogId = string.Empty;
            var ret = (bool)stepContext.Result;

            var predictionResult = (string)stepContext.Values["PredictionResult"];
            if (predictionResult == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("PredictionResult is null"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            if (predictionResult == "Clean")
            {
                if ((bool)stepContext.Result)// true
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Glad to hear that.☺"));
                }
                else // false
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry to hear that.☹"));
                }
            }
            else
            {
                if ((bool)stepContext.Result)// true
                {
                    dialogId = nameof(FollowUpDialog);
                    return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
                }
                else // false
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry to hear that."));
                }

            }

            return await stepContext.EndDialogAsync();
        }
    }
}
