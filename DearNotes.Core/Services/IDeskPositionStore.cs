using DearNotes.Models;

namespace DearNotes.Services
{
    public interface IDeskPositionStore
    {
        DeskPosition Load();

        void Save(DeskPosition position);
    }
}
