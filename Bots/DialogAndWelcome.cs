// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.IO;
using Newtonsoft.Json;

namespace CleanPlateBot
{
    public class DialogAndWelcomeBot<T> : DialogBot<T> where T : Dialog
    {
        public DialogAndWelcomeBot(ConversationState conversationState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, dialog, logger)
        {
        }

        protected override async Task OnMembersAddedAsync(
            IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeCard = Cards.CreateAdaptiveCardAttachment();
                    var reply = MessageFactory.Attachment(welcomeCard);
                    // var reply = MessageFactory.Text($"Welcome to use Clean Plate Bot, {member.Name}!\n\n This bot provides points award and query function for clean plate.\n\n" +
                    //     "You can also upload your bills to get a report for your spending and calory intake every week.\n\n" +
                    //     "Please type anything to get started.");
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            }
        }

    }
}