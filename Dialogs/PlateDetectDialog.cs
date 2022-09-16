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

namespace CleanPlateBot
{
    public class PlateDetectDialog : ComponentDialog
    {
        private readonly IHttpClientFactory _clientFactory;

        public PlateDetectDialog(IHttpClientFactory clientFactory)
            : base(nameof(PlateDetectDialog))
        {
            _clientFactory = clientFactory;
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

        public string type;
        private async Task<DialogTurnResult> PlateDetectStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter PlateDetectStepAsync"));
            
            var attachment = ((IList<Attachment>)stepContext.Result)?.FirstOrDefault();
            if ((IList<Attachment>)stepContext.Result == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Attachment is empty"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var predictions = await DetectionResult(
                 attachment,
                stepContext.Context.Activity.ChannelId, cancellationToken);
            type = predictions;
            if (predictions == "Clean")
            {   

                //score += 5;
                await  stepContext.Context.SendActivityAsync("Congratulations! You have finished all the food. Thank you for your contribution to reduce food waste.");
                //await _cosmosDBClient.AddItemsToContainerAsync(EndpointUri, PrimaryKey, Name, score);
                return await stepContext.PromptAsync(nameof(ConfirmPrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("Are you happy with this result?"),
                   RetryPrompt = MessageFactory.Text("Input invalid. is this result ok?")
               }, cancellationToken);
             
            }
            else
            {
                            //await stepContext.Context.SendActivityAsync(MessageFactory.Text($"your image name is {fileName}"), cancellationToken);

               return await stepContext.PromptAsync(nameof(ConfirmPrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("Sorry! It seems that you haven't finished all the food. \n\n To better improve the service, could you answer two follow up questions?"),
                   RetryPrompt = MessageFactory.Text("Input invalid.")
               }, cancellationToken);
                        
            } 

            //return await stepContext.NextAsync(null,cancellationToken);
        }

        private async Task<string> DetectionResult(Attachment attachment, string channel, CancellationToken cancellationToken)
        {
            var client = _clientFactory.CreateClient();
            var downloadUrl = string.Empty;

            if (channel == Channels.Msteams)
            {
                //Teams Channel
                var fileDownload = JObject.FromObject(attachment.Content).ToObject<FileDownloadInfo>();
                downloadUrl = fileDownload.DownloadUrl;

            }
            else if (channel == Channels.Emulator)
            {
                //Emulator Channel
                downloadUrl = attachment.ContentUrl;
            }
            

            string filePath = Path.Combine(Path.GetTempPath(),  attachment.Name);
            var response = await client.GetAsync(downloadUrl);
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
           {
                //add memory stream here
               await response.Content.CopyToAsync(fileStream);
            }
            var predictions = await predictionApi.GetImagePredictionsAsync(filePath);
            return predictions;
        }

        private async Task<DialogTurnResult> ConfirmResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter ConfirmResultStepAsync"));
            string dialogId = string.Empty;
            var ret = (bool)stepContext.Result;
            if (type.Equals("Clean"))
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
