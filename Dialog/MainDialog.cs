// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Security.Cryptography;
using System.Text;
//using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
// using Microsoft.Extensions.Logging;
// using Newtonsoft.Json.Linq;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Bot.Builder.Azure;
// using Microsoft.Azure.Cosmos;





namespace Microsoft.BotBuilderSamples
{
    public class UserProfileDialog : ComponentDialog
    {

        // The Azure Cosmos DB endpoint for running this sample.
        
        private static readonly string EndpointUri = "https://f0d58c44-0ee0-4-231-b9ee.documents.azure.com:443/";

        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "stYGaSXtlvr5mMQGB3oh4So4EEFypXfEdV2XUPlS0zpj5BZ14KSX7aSdAC7s9Q9mHPrF5WJ1NV45XZBM4vKOzA==";
               
        //private static readonly string databaseId = "ChatBotDB";
        //private static readonly string containerId = "Storage";
        //private static readonly string partitionKey ="/score";
 
        private readonly CosmosDBClient _cosmosDBClient;
        private const string UserInfo = "value-userInfo";
        private readonly UserState _userState;
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;


        public UserProfileDialog(UserState userState, CosmosDBClient cosmosDBClient)
            : base(nameof(UserProfileDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            _cosmosDBClient = cosmosDBClient;
            _userState = userState;

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                NameStepAsync,
                WelcomeStepAsync,
                ActStepAsync,
                DetectionStepAsync,
                FinalStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            //AddDialog(new PointAwardDialog(userState, _cosmosDBClient));
            //AddDialog(new PointQueryDialog(userState, _cosmosDBClient));
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt), PicturePromptValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private const string FLAG = "MY_FLAG";
        public int turn = 0;
        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           stepContext.Values[UserInfo] = new UserProfile();
           var messageText = stepContext.Options?.ToString() ?? "welcome-message";
        
            if (messageText.Equals(FLAG))
            {
                
                return await stepContext.NextAsync(null,cancellationToken);
            }
            else
            {
                turn = 0;
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the name for points reward and query.") }, cancellationToken);
            }
        }


        public string Name = "";
        public string Name2 = "";
        private async Task<DialogTurnResult> WelcomeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Set the user's name to what they entered in response to the name prompt.
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            var messageText = stepContext.Options?.ToString() ?? "welcome-message";
            if (turn == 0)
            {
                 
                Name = (string)stepContext.Result;
                
            }
            else if (turn>0)
            {
                
                Name = Name2; 
            
            }
            
            Name2 = Name ;
            turn += 1;

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What operation would you like to perform?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Plate Detection", "Points Query" }),
                }, cancellationToken);
        }  
       
        public string type = "True";
        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Operation"] = ((FoundChoice)stepContext.Result).Value;
            string operation = (string)stepContext.Values["Operation"];
            
            if ("Plate Detection".Equals(operation))
            {
               var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please upload a picture of your plate."),
                    RetryPrompt = MessageFactory.Text("Please make sure to upload a jpeg/png image file."),
                };
                type = "True";
                return await stepContext.PromptAsync(nameof(AttachmentPrompt), promptOptions, cancellationToken);
                 
            }
            else if ("Points Query".Equals(operation))
            {    
                 var score = 0;
                 List<userScore> userScores = await _cosmosDBClient.QueryItemsAsync(Name, EndpointUri, PrimaryKey);
                 if (userScores.Count == 0)
                 {
                    //await stepContext.Context.SendActivityAsync(MessageFactory.Text(Name), cancellationToken);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your point is 0."), cancellationToken);
                 }
                 else
                 {
                     score = userScores[0].score;
                     await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You get {score} points."), cancellationToken);
                 }               
                   
                type = "False";
                //return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
                return await stepContext.NextAsync(-1, cancellationToken);
            }
            else
            {
                type = "False";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("The selected option not found."), cancellationToken);
                return await stepContext.NextAsync(-1, cancellationToken);
            }
        }  
        
        public string imageHash;
        private async Task<DialogTurnResult> DetectionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if (type == "False")
            {
                return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Do you need other operations?") }, cancellationToken);
            }
            else
            {
                var score = 0;
                List<userScore> userScores = await _cosmosDBClient.QueryItemsAsync(Name, EndpointUri, PrimaryKey);
                if (userScores.Count == 0)
                {
                    score = 0;
                }
                else
                {
                    score = userScores[0].score;
                }
           
                List<Attachment> attachments = (List<Attachment>)stepContext.Result;
                string replyText = string.Empty;
                
                foreach (var file in attachments)
                {
                 // Determine where the file is hosted.
                    var remoteFileUrl = file.ContentUrl;

                // Save the attachment to the system temp directory.
                    var localFileName = Path.Combine(Path.GetTempPath(), file.Name);

                // Download the actual attachment
                    using (var webClient = new WebClient())
                    {
                        webClient.DownloadFile(remoteFileUrl, localFileName);
                    }
              

                    var predictions = await predictionApi.GetImagePredictionsAsync(file.Name);
                    if (predictions == "Clean")
                    {   

                        score += 5;
                        await  stepContext.Context.SendActivityAsync("Congratulations! You have finished all the food. You will get 5 points.");
                        await _cosmosDBClient.AddItemsToContainerAsync(EndpointUri, PrimaryKey, Name, score);
             
                    }
                    else
                    {
                        await  stepContext.Context.SendActivityAsync("Sorry! It seems that you haven't finished all the food.");
                        
                    }
    
                        //return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Do you need other operations?") }, cancellationToken);
                }

            }

                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Do you need other operations?") }, cancellationToken);

        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            if ((bool)stepContext.Result)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, FLAG, cancellationToken);
            }
            else
            {
                await  stepContext.Context.SendActivityAsync("Thank you for your participation. Have a nice day!");
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }    

        private async Task<bool> PicturePromptValidatorAsync(PromptValidatorContext<IList<Attachment>> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {   
                
                var attachments = promptContext.Recognized.Value;
                var validImages = new List<Attachment>();
                foreach (var attachment in attachments)
                {   
                   
                   
                    if ((attachment.ContentType == "image/jpeg" || attachment.ContentType == "image/png"))
                    {  
                        validImages.Add(attachment);
                    }
                }

                promptContext.Recognized.Value = validImages;
                //await promptContext.Context.SendActivityAsync($"{validImages.Name}");
                // If none of the attachments are valid images, the retry prompt should be sent.
                return validImages.Any();
            }
            else
            {
                await promptContext.Context.SendActivityAsync("No valid attachments received.");

                // We can return true from a validator function even if Recognized.Succeeded is false.
                return true;
            }
        }


        private async Task<bool> CheckUniqueAsync(PromptValidatorContext<IList<Attachment>> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {   
                
                var attachments = promptContext.Recognized.Value;
                var validImages = new List<Attachment>();
                foreach (var attachment in attachments)
                {   
                   
                   
                    if ((attachment.ContentType == "image/jpeg" || attachment.ContentType == "image/png"))
                    {  
                        validImages.Add(attachment);
                    }
                }

                promptContext.Recognized.Value = validImages;
                //await promptContext.Context.SendActivityAsync($"{validImages.Name}");
                // If none of the attachments are valid images, the retry prompt should be sent.
                return validImages.Any();
            }
            else
            {
                await promptContext.Context.SendActivityAsync("No valid attachments received.");

                // We can return true from a validator function even if Recognized.Succeeded is false.
                return true;
            }
        }

        
    }
}