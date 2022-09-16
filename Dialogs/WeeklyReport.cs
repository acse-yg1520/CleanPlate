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
    public class WeeklyReportDialog : ComponentDialog
    {

        public WeeklyReportDialog()
            : base(nameof(WeeklyReportDialog))
        {
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                WeeklyReportStepAsync,
                ConfirmResultStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> WeeklyReportStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Uri ur = new Uri("https://aka.ms/cp");
            var name = stepContext.Context.Activity.From.Name;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{name}, please check your weekly report: " + ur), cancellationToken);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Are you happy with this report?")
                }, cancellationToken);
        }
        private async Task<DialogTurnResult> ConfirmResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // need to remove after developing stage
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("enter ConfirmResultStepAsync"));

            var ret = (bool)stepContext.Result;
            if ((bool)stepContext.Result)// true
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Glad to hear that.☺"));
            }
            else // false
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry to hear that.☹"));
            }
            return await stepContext.EndDialogAsync();
        }


    }
}