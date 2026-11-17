using System;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class FixedKeyProvider : IKeyProvider
    {
        private readonly byte[] key;

        public FixedKeyProvider(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);

            this.key = (byte[])key.Clone();
        }

        public byte[] GetKey()
        {
            return (byte[])key.Clone();
        }
    }
}
