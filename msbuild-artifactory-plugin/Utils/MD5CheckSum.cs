﻿using System;
using System.IO;
using System.Security.Cryptography;

namespace JFrog.Artifactory.Utils
{
    /// <summary>
    /// generate a SHA1 to a given file
    /// </summary>
    public class MD5CheckSum
    {

        /// <summary>
        /// generate the SHA1 for a file
        /// </summary>
        /// <param name="path">the path for the file and its name</param>
        /// <returns>SHA1 as string</returns>
        public static string GenerateMD5(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var md5 = MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(fs)).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// generate the MD5 for a file
        /// </summary>
        /// <param name="bytes">the bytes for the file</param>
        /// <returns>MD5 as string</returns>
        public static string GenerateMD5(byte[] bytes)
        {
            if (bytes == null) return string.Empty;
            using (var md5 = MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "").ToLower();
            }
        }
    }
}
