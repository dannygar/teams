/* 
 *  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
 *  See LICENSE in the source repository root for complete license information. 
 */

using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft_Teams_Graph_RESTAPIs_Connect.Auth;
using Microsoft_Teams_Graph_RESTAPIs_Connect.Models;
using Resources;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft_Teams_Graph_RESTAPIs_Connect.ImportantFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Configuration;
using System.Web;
using Microsoft.Graph;
using Microsoft_Teams_Graph_RESTAPIs_Connect;
using Microsoft_Teams_Graph_RESTAPIs_Connect.Utils;
using Group = Microsoft_Teams_Graph_RESTAPIs_Connect.Models.Group;

namespace GraphAPI.Web.Controllers
{
    public enum ActionCodes
    {
        Cancel = 0,
        Approved,
        ChangeDate,
        ChangeStatus
    }

    public class HomeController : Controller
    {
        public static bool _hasAppId = ServiceHelper.AppId != "Enter AppId of your application";
        private static string _processApprovalUrl = string.Format(Globals.IncomingWebhookUrl, 
            ConfigurationManager.AppSettings["ida:TeamId"],
            ConfigurationManager.AppSettings["ida:TenantId"],
            ConfigurationManager.AppSettings["ida:IncomingWebhookId"]
            );
        private static string _callbackUrl = ConfigurationManager.AppSettings["ida:CallbackUrl"];

        readonly GraphService graphService ;

        public HomeController()
        {
            graphService = new GraphService();

        }

        private async Task<ActionResult> WithExceptionHandling(Func<string, FormOutput> f, [CallerMemberName] string callerName = "")
        {
            return await WithExceptionHandlingAsync(
                async s => f(s),
                callerName);
        }

        private async Task<ActionResult> WithExceptionHandlingAsync(Func<string, Task<FormOutput>> f, [CallerMemberName] string callerName = "")
        {
            try
            {
                if (ConfigurationManager.AppSettings["ida:AppId"] == null
                    || ConfigurationManager.AppSettings["ida:AppSecret"] == null
                    || ConfigurationManager.AppSettings["ida:TenantId"] == null)
                {
                    return RedirectToAction("Index", "Error", new {
                        message = "You need to put your appId, appSecret and tenantId in Web.config.secrets. See CSharp\\README.md for details."
                    });
                }

                // Get an access token.
                string accessToken = await AuthProvider.Instance.GetUserAccessTokenAsync();
                graphService.accessToken = accessToken;
                FormOutput output = await f(accessToken);

                output.Action = callerName.Replace("Form", "Action");

                output.UserUpn = await graphService.GetMyId(accessToken); // todo: cache

                if (output.ShowTeamDropdown)
                    output.Teams = (await graphService.GetMyTeams(accessToken)).ToArray();
                if (output.ShowGroupDropdown)
                    output.Groups = (await graphService.GetMyGroups(accessToken)).ToArray();

                //results.Items = await graphService.GetMyTeams(accessToken, Convert.ToString(Resource.Prop_ID));
                return View("Graph", output);
            }
            catch (Exception e)
            {
                if (e.Message == Resource.Error_AuthChallengeNeeded) return new EmptyResult();
                return RedirectToAction("Index", "Error", new { message = Resource.Error_Message + Request.RawUrl + ": " + e.Message });
            }

        }

        [Authorize]
        public async Task<ActionResult> GetTeamsForm()
        {
            return await WithExceptionHandling(
                token => new FormOutput()
                {
                    ShowTeamDropdown=true
                });
        }

        [Authorize]
        public async Task<ActionResult> GetTeamsAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    var teams = (await graphService.GetMyTeams(token)).ToArray();
                    return new FormOutput()
                    {
                        Teams = teams,
                        ShowTeamOutput = true
                    };
                }
                );
        }

        [Authorize]
        public async Task<ActionResult> GetChannelsForm()
        {
            return await WithExceptionHandling(
                token => new FormOutput()
                {
                    ShowTeamDropdown = true,
                    ButtonLabel="Get channels",
                });
        }

        [Authorize]
        public async Task<ActionResult> GetChannelsAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    var channels = (await graphService.GetChannels(token, data.SelectedTeam)).ToArray();
                    return new FormOutput()
                    {
                        Channels = channels,
                        ShowChannelOutput = true
                    };
                }
                );
        }

        [Authorize]
        public async Task<ActionResult> GetAppsForm()
        {
            return await WithExceptionHandling(
                token =>
                {
                    return new FormOutput()
                    {
                        ShowTeamDropdown = true,
                        ButtonLabel = "Get Apps",
                    };
                }
                );
        }


        [Authorize]
        public async Task<ActionResult> GetMessagesForm()
        {
            return await WithExceptionHandling(
                token => new FormOutput()
                {
                    ShowTeamDropdown = true,
                    ShowChannelDropdown = true,
                    ButtonLabel = "Get Messages",
                });
        }


        [Authorize]
        public async Task<ActionResult> GetMessagesAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    var messages = (await graphService.GetChannelMessages(token, data.SelectedTeam, data.SelectedChannel)).ToArray();
                    return new FormOutput()
                    {
                        Messages = messages,
                        ShowMessagesOutput = true
                    };
                }
                );
        }


        [Authorize]
        public async Task<ActionResult> GetAppsAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    var apps = (await graphService.GetApps(token, data.SelectedTeam)).ToArray();
                    return new FormOutput()
                    {
                        Apps = apps,
                        ShowAppOutput = true
                    };
                }
            );
        }






        [Authorize]
        public async Task<ActionResult> PostChannelsForm()
        {
            return await WithExceptionHandling(
                token => new FormOutput()
                {
                    ShowTeamDropdown = true,
                    ShowNameInput = true,
                    ShowDescriptionInput = true,
                    ButtonLabel = "Create channel",
                });
        }

        [Authorize]
        public async Task<ActionResult> PostChannelsAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    await graphService.CreateChannel(token,
                        data.SelectedTeam, data.NameInput, data.DescriptionInput);
                    var channels = (await graphService.GetChannels(token, data.SelectedTeam)).ToArray();
                    return new FormOutput()
                    {
                        Channels = channels,
                        ShowChannelOutput = true
                    };
                }
                );
        }

        [Authorize]
        public async Task<ActionResult> PostMessageForm()
        {
            return await WithExceptionHandling(
                token => new FormOutput()
                {
                    ShowTeamDropdown = true,
                    ShowChannelDropdown = true,
                    ShowMessageBodyInput = true,
                    ButtonLabel = "Post Message",
                });
        }


        [Authorize]
        public async Task<ActionResult> PostCardForm()
        {
            return await WithExceptionHandling(
                token => new FormOutput()
                {
                    ShowTeamDropdown = true,
                    ShowChannelDropdown = true,
                    ShowMessageBodyInput = false,
                    ButtonLabel = "Post Card",
                });
        }


        [Authorize]
        public async Task<ActionResult> StartFlowForm()
        {
            return await WithExceptionHandling(
                token => new FormOutput()
                {
                    ShowTeamDropdown = false,
                    ShowChannelDropdown = false,
                    ShowMessageBodyInput = true,
                    ButtonLabel = "Start Flow",
                });
        }


        [Authorize]
        public async Task<ActionResult> PostMessageAction(FormOutput data)
        {
            // get Graph client
            //var graphClient = new GraphServiceClient(AuthProvider.Instance.GetAuthProvider());
            var attachmentId = Guid.NewGuid().ToString();
            var ext = $".ms/w/s!AhluAiKOrw4r47kC7aDFv-zO5D9qlA?e=w7Mboo";

            var chatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = $"Here's the latest market analysis. <attachment id=\"{attachmentId}\"></attachment>"
                },
                Attachments = new List<ChatMessageAttachment>()
                {
                    new ChatMessageAttachment
                    {
                        Id = attachmentId,
                        ContentType = "reference",
                        ContentUrl = $"https://msteamsdemos.sharepoint.com/:w:/s/myteamdemo/EYuLzcH6jNxOuKVgXD-PvYcBU3g4kG3pAKw8RwClrP3k-Q?e=GHcPEj",
                        Name = $"https://msteamsdemos.sharepoint.com/:w:/s/myteamdemo/EYuLzcH6jNxOuKVgXD-PvYcBU3g4kG3pAKw8RwClrP3k-Q?e=GHcPEj"
                    }
                }
            };

            //await graphClient.Teams[$"{data.SelectedTeam}"].Channels[$"{data.SelectedChannel}"].Messages
            //    .Request()
            //    .AddAsync(chatMessage);


            return await WithExceptionHandlingAsync(
                async token =>
                {
                    await graphService.PostMessage(token,
                        data.SelectedTeam, data.SelectedChannel, chatMessage);
                    return new FormOutput()
                    {
                        SuccessMessage = "Sent",
                    };
                }
                );
        }


        [Authorize]
        public async Task<ActionResult> PostCardAction(FormOutput data)
        {
            // Create a Post Card
            var attachmentId = Guid.NewGuid().ToString();
            var chatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = $"<attachment id=\"{attachmentId}\"></attachment>"
                },
                Attachments = new List<ChatMessageAttachment>()
                {
                    new ChatMessageAttachment
                    {
                        Id = attachmentId,
                        ContentType = "application/vnd.microsoft.card.thumbnail",
                        Content = "{\r\n  \"title\": \"This is an example of posting a voting card\",\r\n  \"subtitle\": \"<h3>Your meeting with Microsoft Team</h3>\",\r\n  \"text\": \"This meeting is about Ansys Minerva Teams Integration <br>\\r\\n<a href=\\\"https://www.ansys.com/about-ansys\\\">About Ansys Minerva</a>. <br>\\r\\nDo you like how this meeting is going?\",\r\n  \"buttons\": [\r\n    {\r\n      \"type\": \"openUrl\",\r\n      \"title\": \"Yes\",\r\n      \"text\": \"Yes\",\r\n      \"displayText\": \"Yes, very much!\",\r\n      \"value\": \"https://teams.microsoft.com/l/entity/com.microsoft.teamspace.tab.wiki/tab::69d88c81-2e06-4907-b8b8-fa3cf121a6f2?context=%7B%22subEntityId%22%3A%22%7B%5C%22pageId%5C%22%3A2%2C%5C%22sectionId%5C%22%3A3%2C%5C%22origin%5C%22%3A2%7D%22%2C%22channelId%22%3A%2219%3Aaf78edd4b8584bfcb68d47c5f81d447b%40thread.tacv2%22%7D&tenantId=8385af7f-4e3e-42b9-a17d-0a43eb16aefd\"\r\n    },\r\n    {\r\n      \"type\": \"messageBack\",\r\n      \"title\": \"No\",\r\n      \"text\": \"No\",\r\n      \"displayText\": \"No, it's very boring!\",\r\n      \"value\": \"No\"\r\n    }  ]\r\n}",
                        ContentUrl = null,
                        Name = null,
                        ThumbnailUrl = null
                    }
                }
            };

            return await WithExceptionHandlingAsync(
                async token =>
                {
                    await graphService.PostMessage(token,
                        data.SelectedTeam, data.SelectedChannel, chatMessage);
                    return new FormOutput()
                    {
                        SuccessMessage = "Sent",
                    };
                }
            );
        }



        [Authorize]
        public async Task<ActionResult> StartFlowAction(FormOutput data)
        {
            // Create a Post Card
            var message = string.IsNullOrEmpty(data.MessageBodyInput)
                ? "Larry Bryant created a new task"
                : data.MessageBodyInput;
            
            // Obtain and then encrypt the JWT access token
            var accessToken = await AuthProvider.Instance.GetUserAccessTokenAsync();
            //var encAccessToken = HttpUtility.UrlEncode(Encryption.Encrypt(accessToken, Globals.PublicKey, Globals.PrivateKey));
            // Encrypt only the first 4 characters
            var encPart = HttpUtility.UrlEncode(Encryption.Encrypt(accessToken.Substring(0, Globals.ENC_LENGTH), Globals.PublicKey, Globals.PrivateKey));
            // Mix the encrypted portion with the rest of the token
            accessToken = accessToken.Substring(Globals.ENC_LENGTH);

            var messageCard = new MessageCard
            {
                Type = "MessageCard",
                Context = new Uri("http://schema.org/extensions"),
                ThemeColor = "0076D7",
                Summary = message,
                Sections = new List<Section>
                {
                    new Section
                    {
                        ActivityTitle =
                            $"![TestImage](https://47a92947.ngrok.io/Content/Images/default.png){message}",
                        ActivitySubtitle = "On Project Tango",
                        ActivityImage = new Uri("https://teamsnodesample.azurewebsites.net/static/img/image5.png"),
                        Facts = new List<Fact>
                        {
                            new Fact {Name = "Assigned to", Value = "Unassigned"},
                            new Fact
                            {
                                Name = "Due date", Value = "Tue May 26 2020 17:07:18 GMT-0700 (Pacific Daylight Time)"
                            },
                            new Fact {Name = "Status", Value = "Not started"},
                        },
                        Markdown = true
                    }
                },
                PotentialAction = new List<PotentialAction>
                {
                    new PotentialAction
                    {
                        Type = "ActionCard",
                        Name = "Approve",
                        Inputs = new List<Input>
                        {
                            new Input
                            {
                                Type = "TextInput",
                                Id = "comment",
                                IsMultiline = false,
                                Title = "Add a comment here for this task"
                            }
                        },
                        Actions = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = "HttpPOST",
                                Name = "Submit",
                                Target = new Uri($"{_callbackUrl}?id={(int) ActionCodes.Approved}&enc={encPart}&token={accessToken}")
                            }
                        }
                    },
                    new PotentialAction
                    {
                        Type = "ActionCard",
                        Name = "Cancel",
                        Inputs = new List<Input>
                        {
                            new Input
                            {
                                Type = "TextInput",
                                Id = "comment",
                                IsMultiline = false,
                                Title = "Add a comment here for this task"
                            }
                        },
                        Actions = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = "HttpPOST",
                                Name = "Cancel",
                                Target = new Uri($"{_callbackUrl}?id={(int) ActionCodes.Cancel}&enc={encPart}&token={accessToken}")
                            }
                        }
                    },
                    new PotentialAction
                    {
                        Type = "ActionCard",
                        Name = "Set due date",
                        Inputs = new List<Input>
                        {
                            new Input
                            {
                                Type = "DateInput",
                                Id = "dueDate",
                                Title = "Enter a due date for this task"
                            }
                        },
                        Actions = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = "HttpPOST",
                                Name = "Save",
                                Target = new Uri($"{_callbackUrl}?id={(int) ActionCodes.ChangeDate}&enc={encPart}&token={accessToken}")
                            }
                        }
                    },
                    new PotentialAction
                    {
                        Type = "ActionCard",
                        Name = "Change status",
                        Inputs = new List<Input>
                        {
                            new Input
                            {
                                Type = "MultichoiceInput",
                                Id = "list",
                                IsMultiline = false,
                                Title = "Select a status",
                                Choices = new List<Choice>
                                {
                                    new Choice {Display = "In Progress", Value = 1},
                                    new Choice {Display = "Active", Value = 2},
                                    new Choice {Display = "Closed", Value = 3},
                                }
                            }
                        },
                        Actions = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = "HttpPOST",
                                Name = "Save",
                                Target = new Uri($"{_callbackUrl}?id={(int) ActionCodes.ChangeStatus}&enc={encPart}&token={accessToken}")
                            }
                        }
                    },
                }
            };

            return await WithExceptionHandlingAsync(
                async token =>
                {
                    await graphService.PostMessage(_processApprovalUrl, messageCard, token);
                    return new FormOutput()
                    {
                        SuccessMessage = "Sent",
                    };
                }
            );
        }


        [Authorize]
        public async Task<ActionResult> PostGroupForm()
        {
            return await WithExceptionHandling(
                token =>
                {
                    return new FormOutput()
                    {
                        ShowDescriptionInput = true,
                        ShowDisplayNameInput = true,
                        ShowMailNicknameInput = true,
                        ButtonLabel = "Create team",
                    };
                }
                );
        }

        [Authorize]
        public async Task<ActionResult> PostGroupAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    Group group = await graphService.CreateNewTeamAndGroup(token, data.DisplayNameInput, data.MailNicknameInput, data.DescriptionInput);
                    var teams = (await graphService.GetMyTeams(token)).ToArray();
                    return new FormOutput()
                    {
                        Teams = teams,
                        ShowTeamOutput = true
                    };
                }
                );
        }


        [Authorize]
        public async Task<ActionResult> Index()
        {
            return await WithExceptionHandling(
                token =>
                {
                    return new FormOutput()
                    {
                    };
                }
                );
        }



        [Authorize]
        public async Task<ActionResult> AddTeamToGroupForm()
        {
            return await WithExceptionHandling(
                token => new FormOutput()
                {
                    ShowGroupDropdown = true,
                    ButtonLabel = "Create team",
                });
        }

        [Authorize]
        public async Task<ActionResult> AddTeamToGroupAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    await graphService.AddTeamToGroup(data.SelectedGroup, token);
                    var teams = (await graphService.GetMyTeams(token)).ToArray();
                    return new FormOutput()
                    {
                        Teams = teams,
                        ShowTeamOutput = true
                    };
                }
                );
        }

        [Authorize]
        public async Task<ActionResult> GetAddTeamToGroupLoad()
        {
            await GetMyId();
            ViewBag.GetAddTeamToGroupLoad = "Enable";
            return View("Graph");
        }

        [Authorize]
        public async Task<ActionResult> GetTeamLoadUpdate()
        {
            await GetMyId();
            ViewBag.GetTeamLoadUpdate = "Enable";
            return View("Graph");
        }


        // [Authorize]
        public async Task<ActionResult> GetMemberLoad()
        {
            await GetMyId();
            ViewBag.GetMemberLoad = "Enable";
            return View("Graph");
        }
        
        /// <summary>
        /// Get the current user's id from their profile.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<ActionResult> GetMyId()
        {
            try
            {
                // Get an access token.
                string accessToken = await AuthProvider.Instance.GetUserAccessTokenAsync();

                // Get the current user's id.
                ViewBag.UserId = await graphService.GetMyId(accessToken);
                return View("Graph");
            }
            catch (Exception e)
            {
                if (e.Message == Resource.Error_AuthChallengeNeeded) return new EmptyResult();
                return RedirectToAction("Index", "Error", new { message = Resource.Error_Message + Request.RawUrl + ": " + e.Message });
            }
        }

        [Authorize]
        public async Task<ActionResult> UpdateTeamForm()
        {
            return await WithExceptionHandling(
                token =>
                {
                    return new FormOutput()
                    {
                        ShowTeamDropdown = true,
                        ButtonLabel = "Change guest settings",
                    };
                }
                );
        }

        [Authorize]
        public async Task<ActionResult> UpdateTeamAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    await graphService.UpdateTeam(data.SelectedTeam, token);
                    return new FormOutput()
                    {
                        SuccessMessage = "Done",
                    };
                }
                );
        }

        [Authorize]
        public async Task<ActionResult> AddMemberForm()
        {
            return await WithExceptionHandling(
                token =>
                {
                    return new FormOutput()
                    {
                        ShowTeamDropdown = true,
                        ShowUpnInput = true,
                        ButtonLabel = "Add member",
                    };
                }
                );
        }

        [Authorize]
        public async Task<ActionResult> AddMemberAction(FormOutput data)
        {
            return await WithExceptionHandlingAsync(
                async token =>
                {
                    await graphService.AddMember(data.SelectedTeam, data.UpnInput, isOwner: false);
                    return new FormOutput()
                    {
                        SuccessMessage = "Done",
                    };
                }
                );
        }




        public ActionResult About()
        {
            return View();
        }
    }
}