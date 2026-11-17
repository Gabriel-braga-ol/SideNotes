using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DearNotes.Services
{
    public sealed class DpapiKeyProvider : IKeyProvider
    {
        private const int KeySize = 32;

        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes("DearNotes.NotesKey.v1");

        private readonly object syncRoot = new();

        private readonly string keyFilePath;

        private byte[]? cachedKey;

        public DpapiKeyProvider(string keyFilePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyFilePath);

            this.keyFilePath = Path.GetFullPath(keyFilePath);
        }

        public byte[] GetKey()
        {
            lock (syncRoot)
            {
                cachedKey ??= LoadOrCreateKey();

                return (byte[])cachedKey.Clone();
            }
        }

        private byte[] LoadOrCreateKey()
        {
            if (!File.Exists(keyFilePath))
            {
                CreateKeyFile();
            }

            byte[] protectedKey = File.ReadAllBytes(keyFilePath);

            byte[] key = ProtectedData.Unprotect(
                protectedKey,
                Entropy,
                DataProtectionScope.CurrentUser);

            if (key.Length == KeySize) return key;

            CryptographicOperations.ZeroMemory(key);

            throw new CryptographicException(
                "A chave de criptografia das notas possui tamanho inválido.");
        }

        private void CreateKeyFile()
        {
            string folder = Path.GetDirectoryName(keyFilePath)
                ?? throw new IOException(
                    "O caminho da chave não contém uma pasta.");

            Directory.CreateDirectory(folder);

            byte[] key = RandomNumberGenerator.GetBytes(KeySize);

            try
            {
                byte[] protectedKey = ProtectedData.Protect(
                    key,
                    Entropy,
                    DataProtectionScope.CurrentUser);

                string temporaryPath = keyFilePath + ".tmp";

                File.WriteAllBytes(temporaryPath, protectedKey);

                if (File.Exists(keyFilePath))
                {
                    File.Replace(temporaryPath, keyFilePath, null);
                }
                else
                {
                    File.Move(temporaryPath, keyFilePath);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }
}
