using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Azure.Cosmos;


namespace Microsoft.BotBuilderSamples
{
    public class PointAwardDialog : ComponentDialog
    {
        private static readonly string EndpointUri = "https://f0d58c44-0ee0-4-231-b9ee.documents.azure.com:443/";

        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "stYGaSXtlvr5mMQGB3oh4So4EEFypXfEdV2XUPlS0zpj5BZ14KSX7aSdAC7s9Q9mHPrF5WJ1NV45XZBM4vKOzA==";
               
        private static readonly string databaseId = "ChatBotDB";
        private static readonly string containerId = "Storage";
        private static readonly string partitionKey ="/score";
        private const string UserInfo = "value-userInfo";

        private readonly CosmosDBClient _cosmosDBClient;
        private readonly UserState _userState;
        public PointAwardDialog(UserState userState, CosmosDBClient cosmosDBClient) : base(nameof(PointAwardDialog))
        {
            //Configuration = configuration;
            _cosmosDBClient = cosmosDBClient;
            _userState = userState;

            var waterfallSteps = new WaterfallStep[]
            {
                PictureStepAsync,
                DetectionStepAsync,
                //ConfirmStepAsync,
               // SummaryStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new PointQueryDialog(_userState, _cosmosDBClient));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt), PicturePromptValidatorAsync));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> PictureStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

         // We can send messages to the user at any point in the WaterfallStep.
           // await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);

            if (stepContext.Context.Activity.ChannelId == Channels.Msteams)
            {
                // This attachment prompt example is not designed to work for Teams attachments, so skip it in this case
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Skipping attachment prompt in Teams channel..."), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text($"Thanks {stepContext.Result}. Please upload a picture of your plate."),
                    RetryPrompt = MessageFactory.Text("The attachment must be a jpeg/png image file."),
                };

                return await stepContext.PromptAsync(nameof(AttachmentPrompt), promptOptions, cancellationToken);
            }
        }


        private async Task<DialogTurnResult> DetectionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
        
            stepContext.Values["picture"] = ((IList<Attachment>)stepContext.Result)?.FirstOrDefault();
          
            // Get the current profile object from user state.
            //var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            // userProfile.Name = (string)stepContext.Values["name"];
            // userProfile.Picture = (Attachment)stepContext.Values["picture"];

            var score = 0;
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            List<userScore> userScores = await _cosmosDBClient.QueryItemsAsync(userProfile.Name, EndpointUri, PrimaryKey);
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
                    await  stepContext.Context.SendActivityAsync("Congratulations! You have finished all the food.");
                
                    //await _cosmosDBClient.AddItemsToContainerAsync(EndpointUri, PrimaryKey, userProfile.Name, score );
                    // var promptOptions = new PromptOptions 
                    // { 
                    //     Prompt = MessageFactory.Text("Do you want to detect more plates or query for points?"),
                    //     Choices = ChoiceFactory.ToChoices(new List<string> { "Detect plates", "Points Query" }),
                    // };
                    

                }
                else
                {
                   await  stepContext.Context.SendActivityAsync("Sorry! It seems that you haven't finished all the food.");
                //    var promptOptions = new PromptOptions 
                //     { 
                //         Prompt = MessageFactory.Text("Do you want to detect more plates or query for points?"),
                //         Choices = ChoiceFactory.ToChoices(new List<string> { "Detect plates", "Points Query" }),
                //     };
                }
            }

            return await stepContext.EndDialogAsync(cancellationToken);
            // return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions 
            //         { 
            //             Prompt = MessageFactory.Text("Do you want to detect more plates or query for points?"),
            //             Choices = ChoiceFactory.ToChoices(new List<string> { "Detect plates", "Points Query" }),
            //         }, cancellationToken);
        }

         private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = ((FoundChoice)stepContext.Result).Value;
            if (choice == "Detect plates")
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, cancellationToken);
            }
            else if (choice == "Points Query")
            {
                return await stepContext.ReplaceDialogAsync(nameof(PointQueryDialog), cancellationToken);
            }
            else
            {
                var userInfo = (UserProfile)stepContext.Result;
                var accessor = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
                await accessor.SetAsync(stepContext.Context, userInfo, cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Ok. Thank you for your participation."));
                return await stepContext.EndDialogAsync(cancellationToken);
            }
        }



        private static async Task<bool> PicturePromptValidatorAsync(PromptValidatorContext<IList<Attachment>> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                var attachments = promptContext.Recognized.Value;
                var validImages = new List<Attachment>();

                foreach (var attachment in attachments)
                {
                    if (attachment.ContentType == "image/jpeg" || attachment.ContentType == "image/png")
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
                await promptContext.Context.SendActivityAsync("No attachments received. Proceeding without a profile picture...");

                // We can return true from a validator function even if Recognized.Succeeded is false.
                return true;
            }
        }

    }
}