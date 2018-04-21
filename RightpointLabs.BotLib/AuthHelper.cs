using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace RightpointLabs.BotLib
{
    /// <summary>
    /// Does the work to move the POST from Azure AD back to the <see cref="LoginDialog"/>'s conversation.
    /// </summary>
    public class AuthHelper
    {
        public class AuthorizeArgs
        {
            public string state { get; set; }
            public string code { get; set; }
            public string error { get; set; }
            public string error_description { get; set; }
            public string session_state { get; set; }
        }

        public static Task<string> Process(Uri requestUri, AuthorizeArgs args)
        {
            return Process(requestUri, args.state, args.code, args.error, args.error_description);
        }

        public static async Task<string> Process(Uri requestUri, string state, string code, string error, string error_description)
        {
            var cookie = SecureUrlToken.Decode<LoginState>(state);
            if (!string.IsNullOrEmpty(error))
            {
                await Conversation.ResumeAsync(cookie.State, new AuthenticationResultActivity(cookie.State.GetPostToUserMessage()) { Error = error, ErrorDescription = error_description });
                return "<html><head><script type='text/javascript'>window.close();</script></head><body>An error occurred during authentication.  You can close this browser window</body></html>";
            }

            string securityCode = null;
            await Conversation.ResumeAsync(cookie.State, new AuthenticationResultActivity(cookie.State.GetPostToUserMessage())
            {
                Code = code,
                RequestUri = requestUri,
                State = cookie,
                Done = (x) =>
                {
                    securityCode = x;
                }
            });

            if (string.IsNullOrEmpty(securityCode))
            {
                return "<html><head><script type='text/javascript'>window.close();</script></head><body>You can close this browser window</body></html>";
            }
            else
            {
                return $"<html><head></head><body>Please copy and paste this key into the conversation with the bot: {securityCode}.</body></html>";
            }
        }
    }
}
