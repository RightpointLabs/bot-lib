using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using RightpointLabs.BotLib.Extensions;

namespace RightpointLabs.BotLib.Dialogs
{
    /// <summary>
    /// Builds and sends an activity to the user prompting them to log into Azure AD.  Azure AD will redirect back to the URL returned by <see cref="GetRedirectUri"/> when complete.
    /// If you will be using multiple resources based on this token and want to make sure you have access+refresh tokens right away (https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/issues/1040), call <see cref="PreAuthForResources"/> from your <see cref="SaveSettings"/> implementation.
    /// See <see cref="AuthHelper"/> for help implementing the thing this posts to.
    /// </summary>
    [Serializable]
    public abstract class LoginDialog : IDialog<string>
    {
        private readonly bool _requireConsent;
        private SimpleAuthenticationResultModel _authResult;
        private bool _promptForKey = true;

        public LoginDialog(bool requireConsent)
        {
            _requireConsent = requireConsent;
        }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.FromResult(0);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var activity = await argument;

            // need to ask the user to authenticate
            var replyToConversation = context.MakeMessage();
            replyToConversation.Recipient = activity.From;
            replyToConversation.Type = "message";

            var signinButton = new CardAction()
            {
                Value = (await GetAuthUrl(context, activity)).AbsoluteUri,
                Type = (activity.ChannelId == "msteams" || activity.ChannelId == "cortana") ? "openUrl" : "signin",
                Title = "Authentication Required"
            };
            var signinCard =
                (activity.ChannelId == "msteams" || activity.ChannelId == "cortana" || activity.ChannelId == "skypeforbusiness") ? 
                new ThumbnailCard("Please login to this bot", null, null, null, new List<CardAction>() { signinButton }).ToAttachment() : 
                new SigninCard("Please login to this bot", new List<CardAction>() { signinButton }).ToAttachment();
            replyToConversation.Attachments = new List<Attachment>() { signinCard };

            _promptForKey = activity.ChannelId != "cortana";

            Log($"LD: Prompting for login from {activity.From.Id}/{activity.From.Name} on {activity.ChannelId}");
            await context.PostAsync(replyToConversation);

            context.Wait<object>(ReceiveTokenAsync);
        }

        private async Task<Uri> GetAuthUrl(IDialogContext context, IMessageActivity activity)
        {
            var authority = ConfigurationManager.AppSettings["Authority"];
            var p = new Dictionary<string, string>();
            p["client_id"] = ConfigurationManager.AppSettings["ClientId"];
            p["redirect_uri"] = GetRedirectUri();
            p["response_mode"] = "form_post";
            p["response_type"] = "code";
            p["scope"] = "openid profile";
            if (_requireConsent)
            {
                p["prompt"] = "consent";
            }
            p["state"] = SecureUrlToken.Encode(new LoginState() { State = activity.ToConversationReference() });

            return new Uri(authority + "/oauth2/authorize?" + string.Join("&", p.Select(i => $"{HttpUtility.UrlEncode(i.Key)}={HttpUtility.UrlEncode(i.Value)}")));
        }

        public async Task ReceiveTokenAsync(IDialogContext context, IAwaitable<object> awaitableArgument)
        {
            var argument = await awaitableArgument;
            var model = argument as AuthenticationResultActivity;

            if (!string.IsNullOrEmpty(model.Error))
            {
                Log($"LD: {model.Error}: {model.ErrorDescription}");
                await context.PostAsync($"{model.Error}: {model.ErrorDescription}");
                context.Done(string.Empty);
                return;
            }

            // Get access token
            var authContext = context.GetAuthenticationContext();
            var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(
                model.Code,
                new Uri(model.RequestUri.GetLeftPart(UriPartial.Path)),
                context.GetClientCredential());

            var uniqueId = authResult?.UserInfo.UniqueId;
            var result = new SimpleAuthenticationResultModel()
            {
                AccessToken = authResult.IdToken,
                Upn = authResult?.UserInfo?.DisplayableId,
                GivenName = authResult?.UserInfo?.GivenName,
                FamilyName = authResult?.UserInfo?.FamilyName,
                UniqueId = uniqueId,
            };

            var lastUniqueId = context.GetLastUniqueId();
            if (uniqueId == model.State.State.User.Id || uniqueId == lastUniqueId)
            {
                model.Done(null);
            }
            else
            {
                var rnd = new Random();
                result.SecurityKey = string.Join("", Enumerable.Range(0, 6).Select(i => rnd.Next(10).ToString()));
                model.Done(result.SecurityKey);
            }

            if (argument is IMessageActivity)
            {
                await context.PostAsync("Cancelled");
                context.Done(string.Empty);
                return;
            }
            else if (null != result)
            {
                if (string.IsNullOrEmpty(result.SecurityKey))
                {
                    Log($"LD: got token, no key needed");
                    await context.PostAsync("Got your token, no security key is required");
                    await SaveSettings(context, result);
                    context.Done(result.AccessToken);
                }
                else
                {
                    Log($"LD: got token, waiting for key");
                    _authResult = result;
                    if(_promptForKey)
                        await context.PostAsync("Please enter your security key");
                    context.Wait(ReceiveSecurityKeyAsync);
                }
                return;
            }

            await context.PostAsync("Got unknown thing: " + argument?.GetType()?.Name);
            context.Wait<object>(ReceiveTokenAsync);
        }

        public async Task ReceiveSecurityKeyAsync(IDialogContext context, IAwaitable<IMessageActivity> awaitableArgument)
        {
            var message = await awaitableArgument;
            var securityKeyRegex = new Regex("[^0-9]");

            if (message.Text.ToLower() == "cancel")
            {
                context.Done<string>(null);
            }
            else if (message.Text.ToLower() == "retry")
            {
                await MessageReceivedAsync(context, awaitableArgument);
            }
            else if (_authResult.SecurityKey == securityKeyRegex.Replace(message.Text, ""))
            {
                Log($"LD: security key matches");
                await context.PostAsync("Security key matches");
                await SaveSettings(context, _authResult);
                context.Done(_authResult.AccessToken);
            }
            else
            {
                await context.PostAsync("Sorry, I didn't understand you.  Enter your security key, or 'cancel' to abort, or 'retry' to get a new authentication link.");
                context.Wait(ReceiveSecurityKeyAsync);
            }
        }

        protected virtual Task SaveSettings(IDialogContext context, SimpleAuthenticationResultModel authResult)
        {
            context.SetLastUniqueId(authResult.UniqueId);
            return Task.FromResult(0);
        }

        protected virtual Task PreAuthForResources(IDialogContext context, SimpleAuthenticationResultModel authResult, params string[] resources)
        {
            return PreAuthForResources(context, authResult.AccessToken, resources);
        }

        protected virtual async Task PreAuthForResources(IDialogContext context, string accessToken, params string[] resources)
        {
            var authContext = context.GetAuthenticationContext();
            foreach (var resource in resources)
            {
                try
                {
                    await authContext.AcquireTokenAsync(resource, context.GetClientCredential(), new UserAssertion(accessToken));
                    Log($"LD: pre-authed access to {resource}");
                }
                catch (Exception ex)
                {
                    Log($"LD: unable to pre-auth access to {resource}: {ex.Message}");
                }
            }
        }

        protected abstract string GetRedirectUri();

        protected virtual void Log(string message)
        {
        }
    }
}