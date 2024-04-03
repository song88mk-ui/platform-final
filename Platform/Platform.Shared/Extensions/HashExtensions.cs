using System;
using System.Security.Cryptography;
using System.Text;

namespace Platform.Shared.Extensions
{
    public static class HashExtensions
    {
        public static string ToMD5Hash(this string input, string privateKey)
        {
            var hex = string.Empty;

            using (MD5 md5Hash = MD5.Create())
            {
                var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes($"{input}{privateKey}"));
                hex = BitConverter.ToString(data).Replace("-", string.Empty);
            }

            return hex;
        }
    }
}
