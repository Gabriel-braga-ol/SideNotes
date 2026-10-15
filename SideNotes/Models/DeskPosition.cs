using System;
using System.Collections.Generic;
using System.Text;

namespace SideNotes.Models
{
    public sealed class DeskPosition
    {
        public string? MonitorDeviceName { get; set; }

        public bool IsOnRight { get; set; } = true;

        public double VerticalPosition { get; set; } = 0.5;

    }
}
