// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicBot.Dialogs.Shoes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class BasicBot : IBot
    {
        // Supported LUIS Intents
        public const string GreetingIntent = "Greeting";
        public const string CancelIntent = "Cancel";
        public const string HelpIntent = "Help";
        public const string NoneIntent = "None";
        public const string ShoesIntent = "Shoes";

        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public static readonly string LuisConfiguration = "BasicBotLuisApplication";

        public static readonly string QnAMakerKey = "botCustomerKB";

        private readonly IStatePropertyAccessor<GreetingState> _greetingStateAccessor;
        private readonly IStatePropertyAccessor<ShoesState> _shoesStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicBot"/> class.
        /// </summary>
        /// <param name="botServices">Bot services.</param>
        /// <param name="accessors">Bot State Accessors.</param>
        public BasicBot(BotServices services, UserState userState, ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _greetingStateAccessor = _userState.CreateProperty<GreetingState>(nameof(GreetingState));
            _shoesStateAccessor = _userState.CreateProperty<ShoesState>(nameof(ShoesState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

            // Verify LUIS configuration.
            if (!_services.LuisServices.ContainsKey(LuisConfiguration))
            {
                throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            }

            if (!_services.QnAServices.ContainsKey(QnAMakerKey))
            {
                throw new System.ArgumentException(
                    $"Invalid configuration. Please check your '.bot' file for a QnA service named '{QnAMakerKey}'.");
            }

            Dialogs = new DialogSet(_dialogStateAccessor);
            Dialogs.Add(new GreetingDialog(_greetingStateAccessor, loggerFactory));
            Dialogs.Add(new ShoesDialog(_shoesStateAccessor, loggerFactory));
        }

        private DialogSet Dialogs { get; set; }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;

            // Create a dialog context
            var dc = await Dialogs.CreateContextAsync(turnContext);

            if (activity.Type == ActivityTypes.Message)
            {
                // Perform a call to LUIS to retrieve results for the current activity message.

                // QnA//
                await QnAResponse(turnContext, cancellationToken);
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (activity.MembersAdded != null)
                {
                    // Iterate over all new members added to the conversation.
                    foreach (var member in activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message.
                        // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                        if (member.Id != activity.Recipient.Id)
                        {
                            var welcomeCard = CreateAdaptiveCardAttachment();
                            var response = CreateResponse(activity, welcomeCard);
                            await dc.Context.SendActivityAsync(response);
                        }
                    }
                }
            }

            await _conversationState.SaveChangesAsync(turnContext);
            await _userState.SaveChangesAsync(turnContext);
        }

        /// <summary>
        /// QnA Maker response.
        /// </summary>
        /// <param name="turnContext">Activity Text.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>QnA Response , if the response equal null call luis .</returns>
        public async Task QnAResponse(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var responseQnA = await _services.QnAServices[QnAMakerKey].GetAnswersAsync(turnContext);

            if (responseQnA != null && responseQnA.Length > 0)
            {
                await turnContext.SendActivityAsync(responseQnA[0].Answer, cancellationToken: cancellationToken);
            }
            else
            {
                // Luis
                await this.LuisResponse(turnContext, cancellationToken);
            }
        }

        /// <summary>
        /// Luis Intent Function .
        /// </summary>
        /// <param name="turnContext">Activity Text.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Luis Response.</returns>
        public async Task LuisResponse(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var dc = await Dialogs.CreateContextAsync(turnContext);

            var luisResults = await _services.LuisServices[LuisConfiguration].RecognizeAsync(dc.Context, cancellationToken);

            var topScoringIntent = luisResults?.GetTopScoringIntent();

            var topIntent = topScoringIntent.Value.intent;

            // update greeting state with any entities captured
            await UpdateGreetingState(luisResults, dc.Context);
            await UpdateShoesState(luisResults, dc.Context);

            // Handle conversation interrupts first.
            var interrupted = await IsTurnInterruptedAsync(dc, topIntent);
            if (interrupted)
            {
                // Bypass the dialog.
                // Save state before the next turn.
                await _conversationState.SaveChangesAsync(turnContext);
                await _userState.SaveChangesAsync(turnContext);
                return;
            }

            // Continue the current dialog
            var dialogResult = await dc.ContinueDialogAsync();

            // if no one has responded,
            if (!dc.Context.Responded)
            {
                // examine results from active dialog
                switch (dialogResult.Status)
                {
                    case DialogTurnStatus.Empty:
                        switch (topIntent)
                        {
                            case GreetingIntent:
                                await dc.BeginDialogAsync(nameof(GreetingDialog));
                                break;
                            case ShoesIntent:
                                await dc.BeginDialogAsync(nameof(ShoesDialog));
                                break;
                            case NoneIntent:
                            default:
                                // Help or no intent identified, either way, let's provide some help.
                                // to the user
                                await dc.Context.SendActivityAsync("I didn't understand what you just said to me.");
                                break;
                        }

                        break;

                    case DialogTurnStatus.Waiting:
                        // The active dialog is waiting for a response from the user, so do nothing.
                        break;

                    case DialogTurnStatus.Complete:
                        await dc.EndDialogAsync();
                        break;

                    default:
                        await dc.CancelAllDialogsAsync();
                        break;
                }
            }
        }

        // Determine if an interruption has occurred before we dispatch to any active dialog.
        private async Task<bool> IsTurnInterruptedAsync(DialogContext dc, string topIntent)
        {
            // See if there are any conversation interrupts we need to handle.
            if (topIntent.Equals(CancelIntent))
            {
                if (dc.ActiveDialog != null)
                {
                    await dc.CancelAllDialogsAsync();
                    await dc.Context.SendActivityAsync("Ok. I've canceled our last activity.");
                }
                else
                {
                    await dc.Context.SendActivityAsync("I don't have anything to cancel.");
                }

                return true;        // Handled the interrupt.
            }

            if (topIntent.Equals(HelpIntent))
            {
                await dc.Context.SendActivityAsync("Let me try to provide some help.");
                await dc.Context.SendActivityAsync("I understand greetings, being asked for help, or being asked to cancel what I am doing.");
                if (dc.ActiveDialog != null)
                {
                    await dc.RepromptDialogAsync();
                }

                return true;        // Handled the interrupt.
            }

            if (topIntent.Equals(ShoesIntent))
            {
                await dc.CancelAllDialogsAsync();
                await dc.BeginDialogAsync(nameof(ShoesDialog));
                return true;
            }
            if (topIntent.Equals(GreetingIntent))
            {
                await dc.CancelAllDialogsAsync();
                await dc.BeginDialogAsync(nameof(GreetingDialog));
                return true;
            }

            return false;           // Did not handle the interrupt.
        }

        // Create an attachment message response.
        private Activity CreateResponse(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        // Load attachment from file.
        private Attachment CreateAdaptiveCardAttachment()
        {
            var adaptiveCard = File.ReadAllText(@".\Dialogs\Welcome\Resources\welcomeCard.json");
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }

        /// <summary>
        /// Helper function to update greeting state with entities returned by LUIS.
        /// </summary>
        /// <param name="luisResult">LUIS recognizer <see cref="RecognizerResult"/>.</param>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private async Task UpdateGreetingState(RecognizerResult luisResult, ITurnContext turnContext)
        {
            if (luisResult.Entities != null && luisResult.Entities.HasValues)
            {
                // Get latest GreetingState
                var greetingState = await _greetingStateAccessor.GetAsync(turnContext, () => new GreetingState());
                var entities = luisResult.Entities;

                // Supported LUIS Entities
                string[] userNameEntities = { "userName", "userName_patternAny" };
                string[] userLocationEntities = { "userLocation", "userLocation_patternAny" };

                // Update any entities
                // Note: Consider a confirm dialog, instead of just updating.
                foreach (var name in userNameEntities)
                {
                    // Check if we found valid slot values in entities returned from LUIS.
                    if (entities[name] != null)
                    {
                        // Capitalize and set new user name.
                        var newName = (string)entities[name][0];
                        greetingState.Name = char.ToUpper(newName[0]) + newName.Substring(1);
                        break;
                    }
                }

                foreach (var city in userLocationEntities)
                {
                    if (entities[city] != null)
                    {
                        // Capitalize and set new city.
                        var newCity = (string)entities[city][0];
                        greetingState.City = char.ToUpper(newCity[0]) + newCity.Substring(1);
                        break;
                    }
                }

                // Set the new values into state.
                await _greetingStateAccessor.SetAsync(turnContext, greetingState);
            }

        }

        private async Task UpdateShoesState(RecognizerResult luisResult, ITurnContext turnContext)
        {
            if (luisResult.Entities != null && luisResult.Entities.HasValues)
            {
                // Get latest GreetingState
                var shoesState = await _shoesStateAccessor.GetAsync(turnContext, () => new ShoesState());
                var entities = luisResult.Entities;

                shoesState.PriceMax = 0;
                shoesState.PriceMin = 0;

                // Supported LUIS Entities
                string[] productCategoriEntities = { "productCategorie", "productCategorie_patternAny" };
                string[] priceMinEntities = { "priceMin", "priceMin_patternAny" };
                string[] priceMaxEntities = { "priceMax", "priceMax_patternAny" };

                // Update any entities
                // Note: Consider a confirm dialog, instead of just updating.
                foreach (var productCategorie in productCategoriEntities)
                {
                    // Check if we found valid slot values in entities returned from LUIS.
                    if (entities[productCategorie] != null)
                    {
                        // Capitalize and set new user name.
                        var newCategorie = (string)entities[productCategorie][0];
                        shoesState.Categorie = char.ToUpper(newCategorie[0]) + newCategorie.Substring(1);
                        break;
                    }
                }

                foreach (var priceMin in priceMinEntities)
                {
                    if (entities[priceMin] != null)
                    {
                        // Capitalize and set new city.
                        var newPriceMin = (string)entities[priceMin][0];
                        shoesState.PriceMin = Convert.ToDouble(newPriceMin);
                        break;
                    }
                }

                foreach (var priceMax in priceMaxEntities)
                {
                    if (entities[priceMax] != null)
                    {
                        // Capitalize and set new city.
                        var newPriceMax = (string)entities[priceMax][0];
                        shoesState.PriceMax = Convert.ToDouble(newPriceMax);
                        break;
                    }
                }
                // Set the new values into state.
                await _shoesStateAccessor.SetAsync(turnContext, shoesState);
            }
        }
        }
}
