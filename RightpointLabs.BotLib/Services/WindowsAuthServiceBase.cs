using System.Net;
using System.Threading.Tasks;

namespace RightpointLabs.BotLib.Services
{
    public abstract class WindowsAuthServiceBase : SimpleServiceBase
    {
        private readonly string _username;
        private readonly string _password;

        protected WindowsAuthServiceBase(string username, string password)
        {
            _username = username;
            _password = password;
        }

        protected override Task<ICredentials> GetCredentials()
        {
            return Task.FromResult<ICredentials>(new NetworkCredential(_username, _password));
        }
    }
}