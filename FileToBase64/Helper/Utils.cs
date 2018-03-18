using System;
using System.IO;
using System.Security.Cryptography;

namespace FileToBase64.Helper
{
    /// <summary>
    /// Utilities class.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Calculate the MD5 hash of a file.
        /// </summary>
        /// <param name="fileFullPath">Full path to the file.</param>
        /// <returns>The MD5 hash of the file.</returns>
        public static string CalculateMd5Hash(string fileFullPath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileFullPath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
