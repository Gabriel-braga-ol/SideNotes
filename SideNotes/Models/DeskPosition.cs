using System;
using System.Collections.Generic;
using System.Text;

namespace SideNotes.Models
{
    public sealed class DeskPosition
    {
        public bool IsOnRight { get; set; } = true;
        public double VerticalPosition { get; set; } = 0.5;
    }
}
