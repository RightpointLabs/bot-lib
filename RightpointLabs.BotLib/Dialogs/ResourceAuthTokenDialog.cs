using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using RightpointLabs.BotLib.Extensions;

namespace RightpointLabs.BotLib.Dialogs
{
    [Serializable]
    public abstract class ResourceAuthTokenDialog : IDialog<string>
    {
        private readonly string _resource;
        private readonly bool _ignoreCache;
        private readonly bool _requireConsent;

        public ResourceAuthTokenDialog(string resource, bool ignoreCache, bool requireConsent)
        {
            _resource = resource;
            _ignoreCache = ignoreCache;
            _requireConsent = requireConsent;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            string accessToken = null;
            if (!_ignoreCache && !_requireConsent)
            {
                var lastUniqueId = context.GetLastUniqueId();
                if (!string.IsNullOrEmpty(lastUniqueId))
                {
                    var authenticationContext = context.GetAuthenticationContext();
                    try
                    {
                        Log($"RATD: Making silent token call for {lastUniqueId} @ {_resource}");
                        var authResult = await authenticationContext.AcquireTokenSilentAsync(_resource,
                            context.GetClientCredential(),
                            new UserIdentifier(lastUniqueId, UserIdentifierType.UniqueId));
                        accessToken = authResult?.AccessToken;
                    }
                    catch (Exception ex)
                    {
                        Log($"RATD: Ignoring silent token call error: {ex.Message}");
                    }
                }
                if (!string.IsNullOrEmpty(accessToken))
                {
                    Log($"RATD: using {accessToken}");
                    context.Done(accessToken);
                    return;
                }
            }

            Log($"RATD: prompting for token");
            await context.Forward(CreateAppAuthTokenDialog(_ignoreCache, _requireConsent), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
        }

        public async Task RecieveAppAuthTokenAsync(IDialogContext context, IAwaitable<string> awaitableArgument)
        {
            var appAccessToken = await awaitableArgument;

            var idaClientId = Config.GetAppSetting("ClientId");
            var idaClientSecret = Config.GetAppSetting("ClientSecret");

            var clientCredential = new ClientCredential(idaClientId, idaClientSecret);
            var userAssertion = new UserAssertion(appAccessToken);
            var authenticationContext = new AuthenticationContext(Config.GetAppSetting("Authority"), new UserTokenCache(context.UserData));

            // try silent again - we may have pre-loaded it as we completed auth - no harm in trying
            var lastUniqueId = context.GetLastUniqueId();
            if (!string.IsNullOrEmpty(lastUniqueId))
            {
                try
                {
                    Log($"RATD: Making silent token call for {lastUniqueId} @ {_resource}");
                    var authResult = await authenticationContext.AcquireTokenSilentAsync(_resource,
                        context.GetClientCredential(),
                        new UserIdentifier(lastUniqueId, UserIdentifierType.UniqueId));
                    var accessToken = authResult?.AccessToken;
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Done(accessToken);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log($"RATD: Ignoring silent token call error: {ex.Message}");
                }
            }

            var sw = Stopwatch.StartNew();
            try
            {
                Log($"RATD: redeeming for token");
                var newToken = await authenticationContext.AcquireTokenAsync(_resource, clientCredential, userAssertion);
                TokenRequestComplete(sw.Elapsed, null);

                context.Done(newToken.AccessToken);
            }
            catch (Exception ex)
            {
                TokenRequestComplete(sw.Elapsed, ex);
                if (ex.Message.Contains("AADSTS65001"))
                {
                    // consent required
                    Log($"RATD: need consent");
                    await context.PostAsync("Looks like we haven't asked your consent for this, doing that now....");
                    await context.Forward(CreateAppAuthTokenDialog(true, true), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
                    return;
                }
                if (ex.Message.Contains("AADSTS50013"))
                {
                    // invalid app access token
                    Log($"RATD: token expired");
                    await context.PostAsync("Looks like your application token is expired - need a new one....");
                    await context.Forward(CreateAppAuthTokenDialog(true, false), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
                    return;
                }
                throw;
            }
        }

        public string CacheKey => $"AuthToken_{this.GetType().Name}_{_resource}";

        protected abstract AppAuthTokenDialog CreateAppAuthTokenDialog(bool ignoreCache, bool requireConsent);

        protected virtual void Log(string message)
        {
        }

        protected virtual void TokenRequestComplete(TimeSpan duration, Exception ex)
        {
        }
    }
}