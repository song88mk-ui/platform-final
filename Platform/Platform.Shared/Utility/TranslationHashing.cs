using System.Security.Cryptography;
using System.Text;

namespace Platform.Shared.Utility
{
    /// <summary>
    /// Produces a hash that is byte-for-byte identical to the SportsbookDynamic.SOURCE_HASH
    /// computed column, so translations can be looked up by source text (content-keying):
    ///   SOURCE_HASH = CONVERT(varchar(64), HASHBYTES('SHA2_256', SOURCE_TEXT), 2)
    /// SOURCE_TEXT is nvarchar, so the hash is over the UTF-16LE bytes; output is uppercase
    /// hex with no "0x" prefix (SQL Server style 2). See docs/proposals/TranslationContentKeying.md.
    /// </summary>
    public static class TranslationHashing
    {
        public static string SourceHash(string sourceText)
        {
            if (sourceText == null)
                return null;

            using (var sha = SHA256.Create())
            {
                // nvarchar in SQL Server == UTF-16LE; Encoding.Unicode is UTF-16LE.
                var bytes = Encoding.Unicode.GetBytes(sourceText);
                var hash = sha.ComputeHash(bytes);

                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("X2")); // uppercase hex, matches CONVERT(..., 2)

                return sb.ToString();
            }
        }
    }
}
