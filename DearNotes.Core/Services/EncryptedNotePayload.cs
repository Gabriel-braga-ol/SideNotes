using System;

namespace DearNotes.Services
{
    public sealed class EncryptedNotePayload
    {
        public EncryptedNotePayload(
            string keyId,
            byte[] nonce,
            byte[] tag,
            byte[] ciphertext)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
            ArgumentNullException.ThrowIfNull(nonce);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(ciphertext);

            KeyId = keyId;
            Nonce = nonce;
            Tag = tag;
            Ciphertext = ciphertext;
        }

        public string KeyId { get; }

        public byte[] Nonce { get; }

        public byte[] Tag { get; }

        public byte[] Ciphertext { get; }
    }
}
