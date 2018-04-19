using Microsoft.Bot.Builder.Dialogs;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace RightpointLabs.BotLib
{
    public class UserTokenCache : TokenCache
    {
        private IBotDataBag _userData;

        private const string KEY = nameof(UserTokenCache);

        public UserTokenCache(IBotDataBag userData)
        {
            _userData = userData;
            BeforeAccess = args =>
            {
                if (userData.TryGetValue(KEY, out byte[] data))
                {
                    this.Deserialize(data);
                    this.HasStateChanged = false;
                }
            };
            BeforeWrite = BeforeAccess;
            AfterAccess = args =>
            {
#if DEBUG_TOKEN_CACHE
                var data = this.Serialize();
                using (var s = new System.IO.MemoryStream(data))
                {
                    var reader = new System.IO.BinaryReader(s);
                    reader.ReadInt32();
                    var count = reader.ReadInt32();
                    System.Diagnostics.Trace.WriteLine($"AfterAccess - {count} items in user token cache");
                    for (var i = 0; i < count; i++)
                    {
                        System.Diagnostics.Trace.WriteLine($"  {reader.ReadString()} - {reader.ReadString()}");
                    }
                }
#endif
                if (this.HasStateChanged)
                {
#if DEBUG_TOKEN_CACHE
                    System.Diagnostics.Trace.WriteLine($"AfterAccess - saving user token cache");
#endif
                    userData.SetValue(KEY, this.Serialize());
                }
            };
        }
    }
}