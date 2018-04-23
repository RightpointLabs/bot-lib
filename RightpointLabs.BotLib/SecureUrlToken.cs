using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace RightpointLabs.BotLib
{
    /// <summary>
    /// Allow object instances to serialized to URLs.  Base64 can not be stored in URLs due to special characters.
    /// </summary>
    /// <remarks>
    /// We use Bson and Gzip to make it small enough to fit within the maximum character limit of URLs.
    /// http://stackoverflow.com/a/32999062 suggests HttpServerUtility's UrlTokenEncode and UrlTokenDecode
    /// is not standards-compliant, but they seem to do the job.
    /// </remarks>
    public static class SecureUrlToken
    {
        /// <summary>Encode an item to be stored in a url.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="item">The item instance.</param>
        /// <returns>The encoded token.</returns>
        public static string Encode<T>(T item)
        {
            var rsa = new RSACryptoServiceProvider();
            var key = Config.GetAppSetting("EncryptionKey");
            if (string.IsNullOrEmpty(key))
                throw new Exception("AppSetting 'EncryptionKey' is missing");
            rsa.ImportCspBlob(Convert.FromBase64String(key));

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    using (var bsonWriter = new BsonWriter(gzipStream))
                        JsonSerializer.CreateDefault().Serialize(bsonWriter, item);
                }
                return Base64UrlEncoder.Encode(rsa.Encrypt(memoryStream.ToArray(), true));
            }
        }

        /// <summary>Decode an item from a url token.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="token">The item token.</param>
        /// <returns>The item instance.</returns>
        public static T Decode<T>(string token)
        {
            var rsa = new RSACryptoServiceProvider();
            var key = Config.GetAppSetting("EncryptionKey");
            if (string.IsNullOrEmpty(key))
                throw new Exception("AppSetting 'EncryptionKey' is missing");
            rsa.ImportCspBlob(Convert.FromBase64String(key));
            var data = Base64UrlEncoder.DecodeBytes(token);
            data = rsa.Decrypt(data, true);

            using (var memoryStream = new MemoryStream(data))
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (var bsonReader = new BsonDataReader(gzipStream))
                        return JsonSerializer.CreateDefault().Deserialize<T>(bsonReader);
                }
            }
        }
    }
}
