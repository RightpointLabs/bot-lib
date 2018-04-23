using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace RightpointLabs.BotLib
{
    public static class Config
    {
        public static string GetAppSetting(string key)
        {
            var value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(value))
                return value;

            value = Environment.GetEnvironmentVariable("APPSETTING_" + key, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(value))
                return value;

            value = ConfigurationManager.AppSettings[key];

            return value;
        }
    }
}
