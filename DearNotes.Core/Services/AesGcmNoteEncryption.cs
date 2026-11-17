using System;
using System.Security.Cryptography;

namespace DearNotes.Services
{
    public sealed class AesGcmNoteEncryption : INoteEncryption
    {
        private const int KeySize = 32;

        private const int NonceSize = 12;

        private const int TagSize = 16;

        private const int KeyIdSize = 8;

        private readonly IKeyProvider keyProvider;

        public AesGcmNoteEncryption(IKeyProvider keyProvider)
        {
            this.keyProvider = keyProvider;
        }

        public EncryptedNotePayload Encrypt(byte[] plaintext)
        {
            ArgumentNullException.ThrowIfNull(plaintext);

            byte[] key = GetKey();

            try
            {
                byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
                byte[] tag = new byte[TagSize];
                byte[] ciphertext = new byte[plaintext.Length];

                using var aes = new AesGcm(key, TagSize);
                aes.Encrypt(nonce, plaintext, ciphertext, tag);

                return new EncryptedNotePayload(
                    GetKeyId(key),
                    nonce,
                    tag,
                    ciphertext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        public byte[] Decrypt(EncryptedNotePayload payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            ValidatePayload(payload);

            byte[] key = GetKey();

            try
            {
                if (!string.Equals(
                        payload.KeyId,
                        GetKeyId(key),
                        StringComparison.Ordinal))
                {
                    throw new CryptographicException(
                        "A chave de criptografia não corresponde ao arquivo.");
                }

                byte[] plaintext = new byte[payload.Ciphertext.Length];

                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(
                    payload.Nonce,
                    payload.Ciphertext,
                    payload.Tag,
                    plaintext);

                return plaintext;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        private static void ValidatePayload(EncryptedNotePayload payload)
        {
            if (payload.Nonce.Length != NonceSize ||
                payload.Tag.Length != TagSize)
            {
                throw new CryptographicException(
                    "O envelope de criptografia está inválido.");
            }
        }

        private byte[] GetKey()
        {
            byte[] key = keyProvider.GetKey();

            if (key.Length == KeySize) return key;

            CryptographicOperations.ZeroMemory(key);

            throw new CryptographicException(
                "A chave de criptografia possui tamanho inválido.");
        }

        private static string GetKeyId(byte[] key)
        {
            byte[] hash = SHA256.HashData(key);

            return Convert.ToHexString(
                hash.AsSpan(0, KeyIdSize));
        }
    }
}
