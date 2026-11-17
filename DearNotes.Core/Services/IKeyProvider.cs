namespace DearNotes.Services
{
    // Retorna uma cópia da chave; o chamador pode zerá-la após o uso.
    public interface IKeyProvider
    {
        byte[] GetKey();
    }
}
