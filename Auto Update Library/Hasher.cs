using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Auto_updater
{
    internal enum HashType
    {
        MD5,
        SHA1,
        SHA512
    }
    internal class Hasher
    {
        internal static string HashFile(string filePath, HashType algorithm)
        {
            try
            {
                switch (algorithm)
                {
                    case HashType.MD5:
                        return MakeHashString(MD5.Create().ComputeHash(new FileStream(filePath, FileMode.Open)));
                        break;
                    case HashType.SHA1:
                        return MakeHashString(SHA1.Create().ComputeHash(new FileStream(filePath, FileMode.Open)));
                    case HashType.SHA512:
                        return MakeHashString(SHA512.Create().ComputeHash(new FileStream(filePath, FileMode.Open)));
                    default:
                        return "";
                }
            }
            catch (Exception e)
            {
                return "";
            }
        }

        private static string MakeHashString(byte[] hash)
        {
            StringBuilder s = new StringBuilder(hash.Length * 2);

            foreach (byte b in hash)
                s.Append(b.ToString("X2").ToLower());

            return s.ToString();
        }
    }
}
