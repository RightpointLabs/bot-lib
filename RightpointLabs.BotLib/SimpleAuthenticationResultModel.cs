using System;

namespace RightpointLabs.BotLib
{
    [Serializable]
    public class SimpleAuthenticationResultModel
    {
        public string AccessToken { get; set; }
        public string SecurityKey { get; set; }
        public string Upn { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string UniqueId { get; set; }
    }
}