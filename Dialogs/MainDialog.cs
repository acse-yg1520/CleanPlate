using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using AdaptiveCards;
using System.Linq;
using Newtonsoft.Json.Linq;


namespace CleanPlateBot
{
    public class MainDialog : ComponentDialog
    {
        private readonly List<string> _choices;

        public MainDialog(IHttpClientFactory clientFactory)
            : base(nameof(MainDialog))
        {
            _choices = new List<string> { "Plate Detect", "Upload Bills", "Score Query", "Weekly Report", "Survey" };

            AddDialog(new PlateDetectDialog(clientFactory));
            AddDialog(new UploadBillsDialog(clientFactory));
            AddDialog(new ScoreQueryDialog());
            AddDialog(new WeeklyReportDialog());
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                WelcomeStepAsync,
                TakeOperationStepAsync,
                ConfirmStepAsync,
                FinalStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }
        
        public string name;
        private async Task<DialogTurnResult> WelcomeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {      
            
            name = stepContext.Context.Activity.From.Name;

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"Hi {name}! What operation would you like to perform?"),
                    Choices = ChoiceFactory.ToChoices(_choices),
                    RetryPrompt = MessageFactory.Text("Input invalid! Please select your operation in offering list.")
                }, cancellationToken);
            //  await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Hi {name}! What operation would you like to perform?"), cancellationToken);
            //  var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            // {
            //     // Use LINQ to turn the choices into submit actions
            //     Actions = _choices.Select(choice => new AdaptiveSubmitAction
            //     {
            //         Title = choice,
            //         Data = choice,  // This will be a string
            //     }).ToList<AdaptiveAction>(),
            // };
            // // Prompt
            // return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            // {
            //     Prompt = (Activity)MessageFactory.Attachment(new Attachment
            //     {
            //         ContentType = AdaptiveCard.ContentType,
            //         // Convert the AdaptiveCard to a JObject
            //         Content = JObject.FromObject(card),
            //     }),
            //     Choices = ChoiceFactory.ToChoices(_choices),
            //     // Don't render the choices outside the card
            //     Style = ListStyle.None,
            // },
            //     cancellationToken);
        }

        private async Task<DialogTurnResult> TakeOperationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("Enter TakeOperationStepAsync"), cancellationToken);

            var selectOperation = ((FoundChoice)stepContext.Result).Value;
            string dialogId = string.Empty;

            switch (selectOperation)
            {
                case "Plate Detect":
                    {
                        dialogId = nameof(PlateDetectDialog);
                        break;
                    }
                case "Upload Bills":
                    {
                        dialogId = nameof(UploadBillsDialog);
                        break;
                    }
                case "Score Query":
                    {
                        dialogId = nameof(ScoreQueryDialog);
                        break;
                    }
                case "Weekly Report":
                    {
                        dialogId = nameof(WeeklyReportDialog);
                        break;
                    }

                default:
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("invalid operation"), cancellationToken);
                        return await stepContext.EndDialogAsync(null, cancellationToken);
                    }
            }

            return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // await stepContext.Context.SendActivityAsync(MessageFactory.Text("Do you need other operations? If yes, please type anything to continue. \r\n Thank you for your participation and have a nice day!"), cancellationToken);

            // return await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Do you need other operations?") }, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            if ((bool)stepContext.Result)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
            else
            {
                await  stepContext.Context.SendActivityAsync("Thank you for your participation. Have a nice day!☺");
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        } 
    }
}
