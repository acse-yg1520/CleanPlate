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

namespace CleanPlateBot
{
    public class FollowUpDialog : ComponentDialog
    {

        public FollowUpDialog()
            : base(nameof(FollowUpDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                FollowUpStep1Async,
                FollowUpStep2Async,
                EndStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> FollowUpStep1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
          
          return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions 
                  { Prompt = MessageFactory.Text("If you didn't finish dishes because of the taste, please kindly type in those dishes name, seperating by comma. \n\n Otherwise, please type no.") },
                   cancellationToken);
        }

        private async Task<DialogTurnResult> FollowUpStep2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
         
          return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions 
                  { Prompt = MessageFactory.Text("If you didn't finish dishes because of the portions, please kindly type in those dishes name, seperating by comma. \n\n Otherwise, please type no.") },
                   cancellationToken);
        }

        private async Task<DialogTurnResult> EndStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
          await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you for your cooperation."), cancellationToken);
          return await stepContext.EndDialogAsync();
        }
    }
}