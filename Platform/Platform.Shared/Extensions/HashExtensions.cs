using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Platform.Shared.Extensions
{
    public static class HashExtensions
    {
        public static string ToMd5Hash(this string input, string privateKey)
        {
            string hex;
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input + privateKey));
                hex = BitConverter.ToString(data).Replace("-", string.Empty);
            }
            return hex;
        }

    }
}
