using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace SideNotes
{
    public class Note : INotifyPropertyChanged
    {
        private string title = string.Empty;

        public string Title
        {
            get => title;
            set
            {
                if (title == value)
                {
                    return;
                }

                title = value;

                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public string Content { get; set; } = string.Empty;

        public string Color { get; set; } = "#B8E6D0";

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
