using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace CleanPlateBot
{
    public class MainDialog : ComponentDialog
    {
        private readonly List<string> _choices;
        

        public MainDialog(IHttpClientFactory clientFactory)
            : base(nameof(MainDialog))
        {

            _choices = new List<string> { "Plate Detect", "Upload Bills", "Score Query" };

            AddDialog(new PlateDetectDialog(clientFactory));
            AddDialog(new UploadBillsDialog(clientFactory));
            AddDialog(new ScoreQueryDialog());
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                WelcomeStepAsync,
                TakeOperationStepAsync,
                FinalStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> WelcomeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // stepContext.Context.Activity.MembersAdded
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("Enter WelcomeStepAsync"), cancellationToken);

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Welcome! What operation would you like to perform?"),
                    Choices = ChoiceFactory.ToChoices(_choices),
                    RetryPrompt = MessageFactory.Text("Input invalid! Please select your operation in offering list.")
                }, cancellationToken);
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
                default:
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("invalid operation"), cancellationToken);
                        return await stepContext.EndDialogAsync(null, cancellationToken);
                    }
            }

            return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Do you need other operations? If yes, please type anything to continue./r/n Thank you for your participation. Have a nice day!"), cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
