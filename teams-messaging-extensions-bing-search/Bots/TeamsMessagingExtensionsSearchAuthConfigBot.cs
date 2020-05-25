// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamsMessagingExtensionsSearchAuthConfig.Extensions;
using TeamsMessagingExtensionsSearchAuthConfig.Models;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class TeamsMessagingExtensionsSearchAuthConfigBot : TeamsActivityHandler
    {
        readonly string _connectionName;
        readonly string _searchUrl;
        readonly UserState _userState;
        private readonly string _cogServiceApiId;
        private readonly string _cogCustomConfigurationId;
        readonly IStatePropertyAccessor<string> _userConfigProperty;
        private readonly INewsSearchClient _bingWebSearchClient;


        public TeamsMessagingExtensionsSearchAuthConfigBot(IConfiguration configuration, UserState userState, INewsSearchClient webSearchClient )
        {
            _connectionName = configuration["ConnectionName"] ?? throw new NullReferenceException("ConnectionName");
            _searchUrl = configuration["SearchUrl"] ?? throw new NullReferenceException("SearchUrl");
            _userState = userState ?? throw new NullReferenceException(nameof(userState));
            _userConfigProperty = userState.CreateProperty<string>("UserConfiguration");
            _bingWebSearchClient = webSearchClient;
            _cogCustomConfigurationId = configuration["CustomConfigId"] ?? throw new NullReferenceException("CustomConfigId");
            _cogServiceApiId = configuration["BingWebSearchKey"] ?? throw new NullReferenceException("BingWebSearchKey");
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // After the turn is complete, persist any UserState changes.
            await _userState.SaveChangesAsync(turnContext);
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionConfigurationQuerySettingUrlAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            // The user has requested the Messaging Extension Configuration page.  
            var escapedSettings = string.Empty;
            var userConfigSettings = await _userConfigProperty.GetAsync(turnContext, () => string.Empty);

            if (!string.IsNullOrEmpty(userConfigSettings))
            {
                escapedSettings = Uri.EscapeDataString(userConfigSettings);
            }

            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "config",
                    SuggestedActions = new MessagingExtensionSuggestedAction
                    {
                        Actions = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = ActionTypes.OpenUrl,
                                Value = $"{_searchUrl}/searchSettings.html?settings={escapedSettings}",
                            },
                        },
                    },
                },
            };
        }
        
        protected override async Task OnTeamsMessagingExtensionConfigurationSettingAsync(ITurnContext<IInvokeActivity> turnContext, JObject settings, CancellationToken cancellationToken)
        {
            // When the user submits the settings page, this event is fired.
            var state = settings["state"];
            if (state != null)
            {
                var userConfigSettings = state.ToString();
                await _userConfigProperty.SetAsync(turnContext, userConfigSettings, cancellationToken);
            }
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery action, CancellationToken cancellationToken)
        {
            var text = action?.Parameters?[0]?.Value as string ?? string.Empty;
            
            var attachments = new List<MessagingExtensionAttachment>();
            var userConfigSettings = await _userConfigProperty.GetAsync(turnContext, () => string.Empty);
            if (userConfigSettings.ToUpper().Contains("EMAIL"))
            {
                // When the Bot Service Auth flow completes, the action.State will contain a magic code used for verification.
                var magicCode = string.Empty;
                var state = action.State;
                if (!string.IsNullOrEmpty(state))
                {
                    int parsed = 0;
                    if (int.TryParse(state, out parsed))
                    {
                        magicCode = parsed.ToString();
                    }
                }

                var tokenResponse = await (turnContext.Adapter as IUserTokenProvider).GetUserTokenAsync(turnContext, _connectionName, magicCode, cancellationToken: cancellationToken);
                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
                {
                    // There is no token, so the user has not signed in yet.

                    // Retrieve the OAuth Sign in Link to use in the MessagingExtensionResult Suggested Actions
                    var signInLink = await (turnContext.Adapter as IUserTokenProvider).GetOauthSignInLinkAsync(turnContext, _connectionName, cancellationToken);

                    return new MessagingExtensionResponse
                    {
                        ComposeExtension = new MessagingExtensionResult
                        {
                            Type = "auth",
                            SuggestedActions = new MessagingExtensionSuggestedAction
                            {
                                Actions = new List<CardAction>
                                {
                                    new CardAction
                                    {
                                        Type = ActionTypes.OpenUrl,
                                        Value = signInLink,
                                        Title = "Bot Service OAuth",
                                    },
                                },
                            },
                        },
                    };
                }

                var client = new SimpleGraphClient(tokenResponse.Token);

                var messages = await client.SearchMailInboxAsync(text);

                // Here we construct a ThumbnailCard for every attachment, and provide a HeroCard which will be
                // displayed if the selects that item.
                attachments = messages.Select(msg => new MessagingExtensionAttachment
                    {
                        ContentType = HeroCard.ContentType,
                        Content = new HeroCard
                        {
                            Title = msg.From.EmailAddress.Address,
                            Subtitle = msg.Subject,
                            Text = msg.Body.Content,
                        },
                        Preview = new ThumbnailCard
                        {
                            Title = msg.From.EmailAddress.Address,
                            Text = $"{msg.Subject}<br />{msg.BodyPreview}",
                            Images = new List<CardImage>()
                            {
                                new CardImage("https://raw.githubusercontent.com/microsoft/botbuilder-samples/master/docs/media/OutlookLogo.jpg", "Outlook Logo"),
                            },
                        }.ToAttachment()
                    }
                ).ToList();
            }
            else
            {
                var articles = await SearchWeb(text);
                // We take every row of the results and wrap them in cards wrapped in in MessagingExtensionAttachment objects.
                // The Preview is optional, if it includes a Tap, that will trigger the OnTeamsMessagingExtensionSelectItemAsync event back on this bot.
                attachments = articles.Select(package => {
                    var previewCard = new ThumbnailCard { Title = package.Name, Tap = new CardAction { Type = "invoke", Value = package } };
                    if (!string.IsNullOrEmpty(package.ThumbnailUrl))
                    {
                        previewCard.Images = new List<CardImage>() { new CardImage(package.ThumbnailUrl, "Icon") };
                    }

                    var attachment = new MessagingExtensionAttachment
                    {
                        ContentType = HeroCard.ContentType,
                        Content = new HeroCard { Title = package.Description },
                        Preview = previewCard.ToAttachment()
                    };

                    return attachment;
                }).ToList();
            }
            //else
            //{
            //    var packages = await FindPackages(text);
            //    // We take every row of the results and wrap them in cards wrapped in in MessagingExtensionAttachment objects.
            //    // The Preview is optional, if it includes a Tap, that will trigger the OnTeamsMessagingExtensionSelectItemAsync event back on this bot.
            //    attachments = packages.Select(package => {
            //        var previewCard = new ThumbnailCard { Title = package.Item1, Tap = new CardAction { Type = "invoke", Value = package } };
            //        if (!string.IsNullOrEmpty(package.Item5))
            //        {
            //            previewCard.Images = new List<CardImage>() { new CardImage(package.Item5, "Icon") };
            //        }

            //        var attachment = new MessagingExtensionAttachment
            //        {
            //            ContentType = HeroCard.ContentType,
            //            Content = new HeroCard { Title = package.Item1 },
            //            Preview = previewCard.ToAttachment()
            //        };

            //        return attachment;
            //    }).ToList();
            //}

            // The list of MessagingExtensionAttachments must we wrapped in a MessagingExtensionResult wrapped in a MessagingExtensionResponse.
            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = attachments
                }
            };
        }

        protected override Task<MessagingExtensionResponse> OnTeamsMessagingExtensionSelectItemAsync(ITurnContext<IInvokeActivity> turnContext, JObject query, CancellationToken cancellationToken)
        {
            // The Preview card's Tap should have a Value property assigned, this will be returned to the bot in this event. 
            var article = query.ToObject<CustomSearchModel>();

            // We take every row of the results and wrap them in cards wrapped in in MessagingExtensionAttachment objects.
            // The Preview is optional, if it includes a Tap, that will trigger the OnTeamsMessagingExtensionSelectItemAsync event back on this bot.
            var card = new ThumbnailCard
            {
                Title = $"{article.Name}, published on: {article.DatePublished}",
                Subtitle = article.Description,
                Buttons = new List<CardAction>
                    {
                        new CardAction { Type = ActionTypes.OpenUrl, Title = "Go to the Article", Value = article.Url }
                    },
            };

            if (!string.IsNullOrEmpty(article.ThumbnailUrl))
            {
                card.Images = new List<CardImage>() { new CardImage(article.ThumbnailUrl, "Icon") };
            }

            var attachment = new MessagingExtensionAttachment
            {
                ContentType = ThumbnailCard.ContentType,
                Content = card,
            };

            return Task.FromResult(new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = new List<MessagingExtensionAttachment> { attachment }
                }
            });
        }

        protected override Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            // This method is to handle the 'Close' button on the confirmation Task Module after the user signs out.
            return Task.FromResult(new MessagingExtensionActionResponse());
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            if (action.CommandId.ToUpper() == "SIGNOUTCOMMAND")
            {
                await (turnContext.Adapter as IUserTokenProvider).SignOutUserAsync(turnContext, _connectionName, turnContext.Activity.From.Id, cancellationToken);

                return new MessagingExtensionActionResponse
                {
                    Task = new TaskModuleContinueResponse
                    {
                        Value = new TaskModuleTaskInfo
                        {
                            Card = CreateAdaptiveCardAttachment(),
                            Height = 200,
                            Width = 400,
                            Title = "Adaptive Card: Inputs",
                        },
                    },
                };
            }
            return null;
        }

        private static Attachment CreateAdaptiveCardAttachment()
        {
            // combine path for cross platform support
            string[] paths = { ".", "Resources", "adaptiveCard.json" };
            var adaptiveCardJson = File.ReadAllText(Path.Combine(paths));

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }

        // Generate a set of substrings to illustrate the idea of a set of results coming back from a query. 
        private async Task<IEnumerable<(string, string, string, string, string)>> FindPackages(string text)
        {
            var obj = JObject.Parse(await (new HttpClient()).GetStringAsync($"https://azuresearch-usnc.nuget.org/query?q=id:{text}&prerelease=true"));
            return obj["data"].Select(item => (item["id"].ToString(), item["version"].ToString(), item["description"].ToString(), item["projectUrl"]?.ToString(), item["iconUrl"]?.ToString()));
        }



        private async Task<IEnumerable<CustomSearchModel>> SearchWeb(string text)
        {
            var articles = new List<CustomSearchModel>();
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_searchUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("User-Agent", "TeamsMessagingExtensionsSearchApp");
                    client.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _cogServiceApiId);

                    // connect to the REST endpoint            
                    HttpResponseMessage response = client.GetAsync(
                        $"?q={text}&customconfig={_cogCustomConfigurationId}&mkt=en-US").Result;

                    // check to see if we have a successful respond
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        var result = JsonConvert.DeserializeObject<BingCustomSearchResponse>(responseContent);
                        articles.AddRange(SearchResultConverter.ToWebArticleList(result));
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException();
                    }
                    else
                    {
                        articles.Add(new CustomSearchModel { Name = $"Status Code: {response.StatusCode}", Description = response.ReasonPhrase });
                    }
                }
            }
            catch (Exception e)
            {
                articles.Add(new CustomSearchModel {Name = "Exception", Description = e.Message});
            }
            return articles;
        }


    }
}
