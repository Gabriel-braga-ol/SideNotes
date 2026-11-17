using System;

namespace DearNotes.Services
{
    public interface INoteEncryption
    {
        EncryptedNotePayload Encrypt(byte[] plaintext);

        byte[] Decrypt(EncryptedNotePayload payload);
    }
}
