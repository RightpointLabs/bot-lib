using Microsoft.Bot.Builder.Dialogs;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace RightpointLabs.BotLib.Extensions
{
    public static class ContextExtensions
    {
#if DEBUG_TOKEN_CACHE
        static ContextExtensions()
        {
            LoggerCallbackHandler.LogCallback = (level, message, pii) =>
            {
                System.Diagnostics.Trace.WriteLine($"LogCallback: {level} - {pii} - {message}");
            };
        }
#endif

        private static readonly string LastUniqueId = nameof(LastUniqueId);
        
        public static string GetLastUniqueId(this IDialogContext context)
        {
            return context.UserData.TryGetValue(LastUniqueId, out string value) ? value : null;
        }

        public static void SetLastUniqueId(this IDialogContext context, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                context.UserData.RemoveValue(LastUniqueId);
            }
            else
            {
                context.UserData.SetValue(LastUniqueId, value);
            }
        }

        public static AuthenticationContext GetAuthenticationContext(this IDialogContext context)
        {
            var ctx = new AuthenticationContext(ConfigurationManager.AppSettings["Authority"], new UserTokenCache(context.UserData));
            ctx.ExtendedLifeTimeEnabled = true;
            return ctx;
        }

        public static ClientCredential GetClientCredential(this IDialogContext context)
        {
            return new ClientCredential(
                ConfigurationManager.AppSettings["ClientId"],
                ConfigurationManager.AppSettings["ClientSecret"]);
        }
    }
}