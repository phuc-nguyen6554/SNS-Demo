using Amazon;
using Amazon.SimpleEmail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleEmail.Model;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.Extensions.Primitives;

namespace M1.SNS.Controllers
{
    [Route("emails")]
    //[ApiController]
    public class EmailController : ControllerBase
    {
        private readonly AmazonSimpleEmailServiceClient _client;
        public EmailController()
        {
            var accessKey = "AKIASGSTPJQO4277AXVA";
            var secretKey = "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";
            _client = new AmazonSimpleEmailServiceClient(accessKey, secretKey, RegionEndpoint.APNortheast1);
        }

        [HttpGet("send")]
        public async Task<IActionResult> SendMail()
        {
            var html = GetHtml();

            try
            {
                await _client.SendEmailAsync(new Amazon.SimpleEmail.Model.SendEmailRequest
                {
                    ConfigurationSetName = "SES",
                    Source = "phuc.nguyen@siliconstack.com.au",
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { "phuc.nguyen@siliconstack.com.au" }
                    },
                    Message = new Message
                    {
                        Subject = new Content("Hello this is testing"),
                        Body = new Body
                        {
                            Html = new Content
                            {
                                Charset = "UTF-8",
                                Data = html
                            }
                        }
                    }
                });
            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Ok(ex.Message);
            }

            return Ok();
        }

        [HttpPost("sns")]
        public async Task<IActionResult> ReceiveSNS()
        {
           var headers = Request.Headers;

            if (headers.TryGetValue("x-amz-sns-message-type", out StringValues value) && value == "SubscriptionConfirmation")
            {
                await SNSSubscriptionConfirm(Request.Body);
            }else if(value == "Notification")
            {
                await SNSNotification(Request.Body);
            }

            return Ok();
        }

        private async Task SNSSubscriptionConfirm(Stream bodyStream)
        {
            var jObject = await ParseBodyStream(bodyStream);

            if(jObject != null && jObject["SubscribeURL"] != null)
            {
                var url = jObject["SubscribeURL"].ToString();

                HttpClient client = new HttpClient();
                var response = await client.GetAsync(new Uri(url));

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Some thing went wrong during confirmation");
            }
        }

        private async Task SNSNotification(Stream bodyStream)
        {
            var json = await ParseBodyStream(bodyStream);

            if (json == null)
                throw new Exception("Bug roi");

            var eventType = json["eventType"].ToString().ToLower();

            JToken data = null;

            if (eventType == "click")
                data = json[eventType.ToString()];
        }

        private async Task<JObject> ParseBodyStream(Stream bodyStream)
        {
            var reader = new StreamReader(bodyStream);
            var json = await reader.ReadToEndAsync();

            return JObject.Parse(json);
        }

        private string GetHtml()
        {
            var html = System.IO.File.ReadAllText(@"C:\Workspace\NetCore\M1\m1.sns\index.html");

            return html;
        }
    }
}
