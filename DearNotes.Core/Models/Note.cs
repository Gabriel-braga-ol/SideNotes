using System;
using System.ComponentModel;

namespace DearNotes.Models
{
    public class Note : INotifyPropertyChanged
    {
        private string title = string.Empty;
        private string content = string.Empty;
        private string color = "#B8E6D0";
        private bool isPinnedToDesk;
        private DateTimeOffset updatedAt;

        public Note()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTimeOffset.UtcNow;
            updatedAt = CreatedAt;
        }

        // Usado ao carregar o arquivo para preservar identidade e datas gravadas.
        public Note(
            Guid id,
            string title,
            string content,
            string color,
            bool isPinnedToDesk,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            Id = id;
            this.title = title;
            this.content = content;
            this.color = color;
            this.isPinnedToDesk = isPinnedToDesk;
            CreatedAt = createdAt;
            this.updatedAt = updatedAt;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid Id { get; }

        public DateTimeOffset CreatedAt { get; }

        public DateTimeOffset UpdatedAt => updatedAt;

        public string Color
        {
            get => color;
            set
            {
                if (color == value) return;

                color = value;
                Touch();
                Notify(nameof(Color));
            }
        }

        public bool IsPinnedToDesk
        {
            get => isPinnedToDesk;
            set
            {
                if (isPinnedToDesk == value) return;

                isPinnedToDesk = value;
                Touch();
                Notify(nameof(IsPinnedToDesk));
            }
        }

        public string Title
        {
            get => title;
            set
            {
                if (title == value) return;

                title = value;
                Touch();
                Notify(nameof(Title));
            }
        }

        public string Content
        {
            get => content;
            set
            {
                if (content == value) return;

                content = value;
                Touch();
                Notify(nameof(Content));
            }
        }

        // Cópia imutável para gravação em segundo plano.
        public Note CreateSnapshot()
        {
            return new Note(
                Id,
                title,
                content,
                color,
                isPinnedToDesk,
                CreatedAt,
                updatedAt);
        }

        private void Touch()
        {
            updatedAt = DateTimeOffset.UtcNow;
        }

        private void Notify(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
