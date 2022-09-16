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
    public class ScoreQueryDialog : ComponentDialog
    {

        public ScoreQueryDialog()
            : base(nameof(ScoreQueryDialog))
        {
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
            // need to remove after developing stage
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter ScoreQueryStepAsync"));
            
            // **********************************
            // add your code here for Score Query
            // **********************************

            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Is this result ok?")
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // need to remove after developing stage
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter ConfirmResultStepAsync"));

            var ret = (bool)stepContext.Result;
            if ((bool)stepContext.Result)// true
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("you confirmed result"));
            }
            else // false
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("you didn't confirm result"));
            }
            return await stepContext.EndDialogAsync();
        }
    }
}