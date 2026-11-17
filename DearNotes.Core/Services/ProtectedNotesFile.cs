namespace DearNotes.Services
{
    // Envelope gravado em notes.json; o documento de notas fica cifrado.
    internal sealed class ProtectedNotesFile
    {
        public const string ProtectionName = "dpapi-aes-gcm";

        public const int CurrentFormatVersion = 1;

        public string? Protection { get; set; }

        public int FormatVersion { get; set; }

        public string? KeyId { get; set; }

        public string? Nonce { get; set; }

        public string? Tag { get; set; }

        public string? Ciphertext { get; set; }
    }
}
