using BasicBot.Dialogs;
using BasicBot.Model;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Demonstrates the following concepts:
    /// - Use a subclass of ComponentDialog to implement a multi-turn conversation
    /// - Use a Waterflow dialog to model multi-turn conversation flow
    /// - Use custom prompts to validate user input
    /// - Store conversation and user state.
    /// </summary>
    public class ShoesDialog : ComponentDialog
    {
        // User state for greeting dialog

        // Dialog IDs
        private const string ProfileDialog = "profileDialog";
        private const string CategoriePrompt = "categoriePrompt";

        /// <summary>
        /// Initializes a new instance of the <see cref="ShoesDialog"/> class.
        /// </summary>
        /// <param name="botServices">Connected services used in processing.</param>
        /// <param name="botState">The <see cref="UserState"/> for storing properties at user-scope.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> that enables logging and tracing.</param>
        public ShoesDialog()
            : base(nameof(ShoesDialog))
        {

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                    ShoesCategoriePromptAsync,
                    ShoesPricePromptAsync,
                    ShoesSuggestionPromptAsync,
                    GoodByeUser,
            };
            AddDialog(new WaterfallDialog(ProfileDialog, waterfallSteps));
            AddDialog(new TextPrompt(CategoriePrompt));
        }

        private async Task<DialogTurnResult> ShoesCategoriePromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dc = stepContext.Context.Activity;
            if (dc.Text.Equals("Sneakers") || dc.Text.Equals("Loafers") || dc.Text.Equals("Boots"))
            {
                return await stepContext.NextAsync();
            }

            PromptOptions opts = CardShoesCategorie();
            return await stepContext.PromptAsync(CategoriePrompt, opts);
        }

        private PromptOptions CardShoesCategorie()
        {
            var card = new HeroCard()
            {
                Buttons = new List<CardAction>()
                   {
                     new CardAction(ActionTypes.ImBack, title: "Sneakers", value: "Sneakers"),
                     new CardAction(ActionTypes.ImBack, title: "Loafers", value: "Loafers"),
                     new CardAction(ActionTypes.ImBack, title: "Boots", value: "Boots"),
                    },
            };
            var opts = new PromptOptions
            {

                Prompt = new Activity
                {
                    Text = $"Great what Kind of shoes?",
                    Type = ActivityTypes.Message,
                    Attachments = new List<Attachment>
                    {
                      card.ToAttachment(),
                    },
                },
            };
            return opts;
        }

        private async Task<DialogTurnResult> ShoesPricePromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dc = stepContext.Context.Activity;
            if (dc.Text.Equals("Under") || dc.Text.Equals("To") || dc.Text.Equals("Over"))
            {
                return await stepContext.NextAsync();
            }
            return await stepContext.PromptAsync(CategoriePrompt, CardServices.CardPrice());
        }


        private async Task<DialogTurnResult> ShoesSuggestionPromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dc = stepContext.Context.Activity;

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Text = $"What do you think of these?",
                    Type = ActivityTypes.Message,
                    Attachments = ShoesSuggestionList(),
                    AttachmentLayout = AttachmentLayoutTypes.Carousel,
                },
            };
            return await stepContext.PromptAsync(CategoriePrompt, opts);
        }

        private async Task<DialogTurnResult> GoodByeUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var context = stepContext.Context;

            // Display their profile information and end dialog.
            await context.SendActivityAsync($"Good by!");
            return await stepContext.EndDialogAsync();
        }

        private List<HeroCard> ShoesSuggestionList1()
        {
            List<HeroCard> shoesList = new List<HeroCard>
            {
                new HeroCard()
                {
                    Title = "adidas",
                    Subtitle = "39.98",
                    Text = "Skechers Men's Go Golf Elite 3 Shoe",
                    Images = new List<CardImage>
                    {
                       new CardImage("https://images-na.ssl-images-amazon.com/images/I/812Y0qDRtsL._AC_SR201,266_.jpg"),
                    },
                    Buttons = new List<CardAction>()
                   {
                       new CardAction(ActionTypes.ImBack, title: "Buy this item", value: "Buy"),
                       new CardAction(ActionTypes.ImBack, title: "See more like this", value: "More"),
                       new CardAction(ActionTypes.ImBack, title: "Ask a question", value: "Question"),
                    },
                },
                new HeroCard()
                {
                    Title = "Skechers",
                    Subtitle = "68.55",
                    Text = "PUMA Men's Ignite Nxt Pro Golf Shoe",
                    Images = new List<CardImage>
                    {
                       new CardImage("https://images-na.ssl-images-amazon.com/images/I/61XnSvWaj7L._AC_SR201,266_.jpg"),
                    },
                    Buttons = new List<CardAction>()
                   {
                      new CardAction(ActionTypes.ImBack, title: "Buy this item", value: "Buy"),
                      new CardAction(ActionTypes.ImBack, title: "See more like this", value: "More"),
                      new CardAction(ActionTypes.ImBack, title: "Ask a question", value: "Question"),
                    },
                },
                new HeroCard()
                {
                    Title = "adidas",
                    Subtitle = "97.41",
                    Text = "ECCO Men's Biom Hybrid 2 Hydromax Golf Shoe",
                    Images = new List<CardImage>
                    {
                       new CardImage("https://images-na.ssl-images-amazon.com/images/I/71XRCCS3igL._AC_SR201,266_.jpg"),
                    },
                    Buttons = new List<CardAction>()
                   {
                       new CardAction(ActionTypes.ImBack, title: "Buy this item", value: "Buy"),
                       new CardAction(ActionTypes.ImBack, title: "See more like this", value: "More"),
                       new CardAction(ActionTypes.ImBack, title: "Ask a question", value: "Question"),
                    },
                },
                new HeroCard()
                {
                    Title = "adidas",
                    Subtitle = "109.15",
                    Text = "Nike Men's Explorer 2 Golf Shoe",
                    Images = new List<CardImage>
                    {
                       new CardImage("http://contososcubademo.azurewebsites.net/assets/tofu.jpg"),
                    },
                    Buttons = new List<CardAction>()
                   {
                       new CardAction(ActionTypes.ImBack, title: "Buy this item", value: "Buy"),
                       new CardAction(ActionTypes.ImBack, title: "See more like this", value: "More"),
                       new CardAction(ActionTypes.ImBack, title: "Ask a question", value: "Question"),
                    },
                },
             };
            return shoesList;
        }

        private List<Attachment> ShoesSuggestionList()
        {
            var products = new List<Product> {
                new Product("adidas ",1020,"https://images-na.ssl-images-amazon.com/images/I/812Y0qDRtsL._AC_SR201,266_.jpg","Sneakers"),
                new Product("PUMA ",120,"https://images-na.ssl-images-amazon.com/images/I/812Y0qDRtsL._AC_SR201,266_.jpg","Sneakers"),
                new Product("NIKE ",590,"https://images-na.ssl-images-amazon.com/images/I/812Y0qDRtsL._AC_SR201,266_.jpg","Sneakers"),
                new Product("adidas22 ",300,"https://images-na.ssl-images-amazon.com/images/I/812Y0qDRtsL._AC_SR201,266_.jpg","Sneakers"),
                new Product("adidas ",560,"https://images-na.ssl-images-amazon.com/images/I/812Y0qDRtsL._AC_SR201,266_.jpg","Sneakers"),
        
            };
            List<Attachment> attachments = new List<Attachment>();
            foreach (var p in products)
            {
                attachments.Add(CardProduct(p).ToAttachment());
            }
            return attachments;
        }

        private HeroCard CardProduct(Product product)
        {
            return new HeroCard()
            {
                Title = product.Name,
                Subtitle = product.Price.ToString(),
                Text = product.Name,
                Images = new List<CardImage>
                    {
                       new CardImage(product.Image),
                    },
                Buttons = new List<CardAction>()
                   {
                       new CardAction(ActionTypes.ImBack, title: "Buy this item", value: "Buy"),
                       new CardAction(ActionTypes.ImBack, title: "See more like this", value: "More"),
                       new CardAction(ActionTypes.ImBack, title: "Ask a question", value: "Question"),
                    },
            };
        }
    }
}
