using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace CleanPlateBot
{
    public class UploadBillsDialog : ComponentDialog
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _msAppId;
        private readonly string _msAppPassword;
        private readonly string _ocrSubscriptionKey;
        private readonly string _ocrEndpoint;

        public UploadBillsDialog(IHttpClientFactory clientFactory, IConfiguration configuration)
            : base(nameof(UploadBillsDialog))
        {
            _clientFactory = clientFactory;
            _msAppId = configuration["MicrosoftAppId"];
            _msAppPassword = configuration["MicrosoftAppPassword"];
            _ocrSubscriptionKey = configuration["OcrSubscriptionKey"];
            _ocrEndpoint = configuration["OcrEndpoint"];

            // AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt), Helper.ImagePromptValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                UploadBillStepAsync,
                ProcessBillStepAsync,
                ConfirmResultStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> UploadBillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.PromptAsync(
                nameof(AttachmentPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please upload your bill picture!"),
                    RetryPrompt = MessageFactory.Text("The attachment must be a jpeg/png image file."),
                }, cancellationToken);

            //return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> ProcessBillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var attachment = ((IList<Attachment>)stepContext.Result)?.FirstOrDefault();
            if (attachment == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("No picture in the attachment"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var credentialToken = await new MicrosoftAppCredentials(_msAppId, _msAppPassword).GetTokenAsync();
            var image = await Helper.DownloadImage(
                                        _clientFactory.CreateClient(),
                                        credentialToken,
                                        attachment,
                                        stepContext.Context.Activity.ChannelId,
                                        cancellationToken);

            // add code for processing bill image here,
            await ProcessBillImage(stepContext, image, cancellationToken);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("Is this result ok?"),
                   RetryPrompt = MessageFactory.Text("Input invalid. is this result ok?")
               }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter ConfirmResultStepAsync"));
            string dialogId = string.Empty;
            var ret = (bool)stepContext.Result;
            if ((bool)stepContext.Result)// true
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Great! You confirmed the result."));
            }
            else // false
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("If the result is not correct, try to upload a clearer bill picture."));
                dialogId = nameof(UploadBillsDialog);
                return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
            }
            return await stepContext.EndDialogAsync();
        }

        private async Task ProcessBillImage(WaterfallStepContext stepContext, string imagePath, CancellationToken cancellationToken)
        {
            //stepContext.Values["image"] = fileName;
            var ocr_list = await Helper.GetOcr(imagePath, _ocrEndpoint, _ocrSubscriptionKey);

            List<string> dish_list = new List<string>();
            List<string> price_list = new List<string>();
            List<int> idx = new List<int>();

            for (int i = 0; i < ocr_list.Count; i++)
            {
                if (ocr_list[i][0].ToString() == "*")
                {
                    idx.Add(i);
                }
            }

            if (idx.Count == 0){
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please make sure you send a proper image."), cancellationToken);
                await stepContext.EndDialogAsync();
                return ;
            }

            string total_spend = ocr_list[idx[1] + 5];
            int step = (idx[1] - 1 - idx[0]) / idx[0];
            for (int j = 1; j < step; j++)
            {
                dish_list.Add(ocr_list[idx[0] + 1 + 4 * j]);
                price_list.Add(ocr_list[idx[0] + 2 + 4 * j]);
            }

            string reply = "Dish Name and Price: " + "\n\n";
            for (int i = 0; i < dish_list.Count; i++)
            {
                reply = reply + dish_list[i] + "—" + price_list[i] + ";" + "\n\n";
            }
            reply = reply + $"Your spend for this meal is {total_spend}.";

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
        }
    }
}
