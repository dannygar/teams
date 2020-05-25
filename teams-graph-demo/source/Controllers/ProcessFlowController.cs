using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Graph;
using Microsoft_Teams_Graph_RESTAPIs_Connect.Auth;
using Microsoft_Teams_Graph_RESTAPIs_Connect.ImportantFiles;
using Microsoft_Teams_Graph_RESTAPIs_Connect.Utils;

namespace Microsoft_Teams_Graph_RESTAPIs_Connect.Controllers
{
    public class ProcessFlowController : ApiController
    {
        readonly GraphService _graphService;
        readonly string _teamId;
        readonly string _channelId;

        public ProcessFlowController()
        {
            _graphService = new GraphService();
            _teamId = ConfigurationManager.AppSettings["ida:TeamId"];
            _channelId = ConfigurationManager.AppSettings["ida:ChannelId"];
        }

        // POST api/<controller>?id=<id>&st=<secure token>
        [System.Web.Http.HttpPost]
        public async Task<HttpResponseMessage> ApprovalResponse(int id, string enc, string token)
        {
            string status = String.Empty;
            var newUrl = this.Url.Link("Default", new {Controller = "Home", Action = "Index"});

            //var accessToken = Encryption.Decrypt(HttpUtility.UrlDecode(st), Globals.PublicKey, Globals.PrivateKey);
            var decrPart = Encryption.Decrypt(HttpUtility.UrlDecode(enc), Globals.PublicKey, Globals.PrivateKey);
            var accessToken = $"{decrPart}{token}";


            switch (id)
            {
                case 1:
                    status = "Approved";
                    break;
                case 2:
                    status = "change due date";
                    break;
                case 3:
                    status = "change status";
                    break;
                default:
                    status = "cancelled";
                    break;
            }

            await PostMessageAction(status, accessToken);
            return Request.CreateResponse(HttpStatusCode.OK, new { Success = true, Status = status, RedirectUrl = newUrl });
        }


        public async Task PostMessageAction(string message, string accessToken)
        {
            var chatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = message
                },
            };

            // Assign an access token.
            _graphService.accessToken = accessToken;

            await _graphService.PostMessage(accessToken,
                _teamId, _channelId, chatMessage);
        }


    }
}