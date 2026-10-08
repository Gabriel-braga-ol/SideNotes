using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace SideNotes.Models
{
    public class Note : INotifyPropertyChanged
    {
        private string title = string.Empty;
        private string content = string.Empty;
        public string Color { get; set; } = "#B8E6D0";
        public event PropertyChangedEventHandler? PropertyChanged;


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

        public string Content
        {
            get => content;
            set
            {
                if (content == value)
                {
                    return;
                }
                
                content = value;
                
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(Content)));
            }
        }
    }
}
