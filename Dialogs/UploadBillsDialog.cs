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
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json.Linq;

namespace CleanPlateBot
{
    public class UploadBillsDialog : ComponentDialog
    {
        private readonly IHttpClientFactory _clientFactory;

        public UploadBillsDialog(IHttpClientFactory clientFactory)
            : base(nameof(UploadBillsDialog))
        {
            _clientFactory = clientFactory;
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
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter UploadBillStepAsync"));

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
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter ProcessBillStepAsync"));

            var attachemnt = ((IList<Attachment>)stepContext.Result)?.FirstOrDefault();
            if (attachemnt != null)
            {
                var fileName = await DownloadImage(attachemnt,
                    stepContext.Context.Activity.ChannelId, cancellationToken);
                
                // add code for processing image here,
                //stepContext.Values["image"] = fileName;
                var ocr_list = await OCR_api.GetOcr(fileName);
                List<string> dish_list = new List<string>();
                List<string> price_list = new List<string>();
                List<int> idx = new List<int>();
                
                for (int i=0;i<ocr_list.Count; i++)
                    { 
                        if (ocr_list[i][0].ToString() == "*")
                        {
                           idx.Add(i);
                        }  
                    }
                string total_spend = ocr_list[idx[1]+5];
                int step = (idx[1]-1-idx[0])/idx[0];
                for (int j = 1; j < step ; j++)
                    {
                        dish_list.Add(ocr_list[idx[0]+1+4*j]);
                        price_list.Add(ocr_list[idx[0]+2+4*j]);        
                    }

                string  reply = "Dish Name and Price: " + "\n\n";
                for (int i = 0; i < dish_list.Count; i++ )
                {
                    
                    reply = reply + dish_list[i] + "—" + price_list[i] + ";" + "\n\n";
                    
                }
                reply = reply + $"Your spend for this meal is {total_spend}.";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(reply), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("No picture in the attachment"));
            }

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

            var ret = (bool)stepContext.Result;
            if ((bool)stepContext.Result)// true
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You confirmed result."));
            }
            else // false
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You didn't confirm result."));
            }
            return await stepContext.EndDialogAsync();
        }

        private async Task<string> DownloadImage(Attachment attachment, string channel, CancellationToken cancellationToken)
        {
            var client = _clientFactory.CreateClient();
            var fileDownload = JObject.FromObject(attachment.Content).ToObject<FileDownloadInfo>();
            var downloadUrl = fileDownload.DownloadUrl;
            //var downloadUrl = attachment.ContentUrl;
            var response = await client.GetAsync(downloadUrl);

            string fileName = string.Empty;
            if (string.IsNullOrEmpty(attachment.Name) || string.IsNullOrWhiteSpace(attachment.Name))
            {
               fileName = $"{Guid.NewGuid().ToString()}.png";
            }
            else
            {
               fileName = attachment.Name;
            }
            
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            //string filePath = Path.Combine(Path.GetTempPath(),  attachment.Name);
            //var response = await client.GetAsync(downloadUrl);
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
           {
                //add memory stream here
               await response.Content.CopyToAsync(fileStream);
           }

            return filePath;
        }
    }
}
