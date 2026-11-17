using System;
using System.Linq;
using System.Security.Cryptography;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class AesGcmNoteEncryptionTests
    {
        private static readonly byte[] TestKey =
            Enumerable.Repeat((byte)0x31, 32).ToArray();

        [Fact]
        public void EncryptThenDecrypt_RestoresPlaintext()
        {
            AesGcmNoteEncryption encryption = CreateEncryption();
            byte[] plaintext = "conteúdo da nota"u8.ToArray();

            EncryptedNotePayload payload = encryption.Encrypt(plaintext);
            byte[] decrypted = encryption.Decrypt(payload);

            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void Encrypt_UsesFreshNonceForEachCall()
        {
            AesGcmNoteEncryption encryption = CreateEncryption();
            byte[] plaintext = "nota"u8.ToArray();

            EncryptedNotePayload first = encryption.Encrypt(plaintext);
            EncryptedNotePayload second = encryption.Encrypt(plaintext);

            Assert.Equal(12, first.Nonce.Length);
            Assert.Equal(16, first.Tag.Length);
            Assert.NotEqual(first.Nonce, second.Nonce);
            Assert.Equal(first.KeyId, second.KeyId);
        }

        [Fact]
        public void Decrypt_WhenCiphertextIsTampered_ThrowsCryptographicException()
        {
            AesGcmNoteEncryption encryption = CreateEncryption();
            EncryptedNotePayload payload = encryption.Encrypt(
                "nota"u8.ToArray());

            payload.Ciphertext[0] ^= 0xFF;

            Assert.ThrowsAny<CryptographicException>(
                () => encryption.Decrypt(payload));
        }

        [Fact]
        public void Decrypt_WhenTagIsTampered_ThrowsCryptographicException()
        {
            AesGcmNoteEncryption encryption = CreateEncryption();
            EncryptedNotePayload payload = encryption.Encrypt(
                "nota"u8.ToArray());

            payload.Tag[0] ^= 0xFF;

            Assert.ThrowsAny<CryptographicException>(
                () => encryption.Decrypt(payload));
        }

        [Fact]
        public void Decrypt_WhenKeyDoesNotMatch_ThrowsCryptographicException()
        {
            EncryptedNotePayload payload = CreateEncryption().Encrypt(
                "nota"u8.ToArray());

            byte[] otherKey = Enumerable.Repeat((byte)0x57, 32).ToArray();
            AesGcmNoteEncryption other = new(
                new FixedKeyProvider(otherKey));

            Assert.ThrowsAny<CryptographicException>(
                () => other.Decrypt(payload));
        }

        [Fact]
        public void Decrypt_WhenNonceSizeIsInvalid_ThrowsCryptographicException()
        {
            AesGcmNoteEncryption encryption = CreateEncryption();
            EncryptedNotePayload payload = encryption.Encrypt(
                "nota"u8.ToArray());

            var invalid = new EncryptedNotePayload(
                payload.KeyId,
                new byte[8],
                payload.Tag,
                payload.Ciphertext);

            Assert.ThrowsAny<CryptographicException>(
                () => encryption.Decrypt(invalid));
        }

        [Fact]
        public void Decrypt_WhenTagSizeIsInvalid_ThrowsCryptographicException()
        {
            AesGcmNoteEncryption encryption = CreateEncryption();
            EncryptedNotePayload payload = encryption.Encrypt(
                "nota"u8.ToArray());

            var invalid = new EncryptedNotePayload(
                payload.KeyId,
                payload.Nonce,
                new byte[8],
                payload.Ciphertext);

            Assert.ThrowsAny<CryptographicException>(
                () => encryption.Decrypt(invalid));
        }

        [Fact]
        public void Encrypt_WhenKeyHasInvalidSize_ThrowsCryptographicException()
        {
            var encryption = new AesGcmNoteEncryption(
                new FixedKeyProvider(new byte[16]));

            Assert.ThrowsAny<CryptographicException>(
                () => encryption.Encrypt(Array.Empty<byte>()));
        }

        private static AesGcmNoteEncryption CreateEncryption()
        {
            return new AesGcmNoteEncryption(
                new FixedKeyProvider(TestKey));
        }
    }
}
