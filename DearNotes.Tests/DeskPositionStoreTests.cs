using System.IO;
using DearNotes.Models;
using DearNotes.Services;

namespace DearNotes.Tests
{
    public sealed class DeskPositionStoreTests
    {
        [Fact]
        public void Load_WhenFileDoesNotExist_ReturnsDefaults()
        {
            using var directory = new TemporaryDirectory();
            var store = new DeskPositionStore(
                directory.Combine("desk_position.json"));

            DeskPosition position = store.Load();

            Assert.Null(position.MonitorDeviceName);
            Assert.True(position.IsOnRight);
            Assert.Equal(0.5, position.VerticalPosition);
        }

        [Fact]
        public void Save_ThenLoad_RestoresPosition()
        {
            using var directory = new TemporaryDirectory();
            var store = new DeskPositionStore(
                directory.Combine("desk_position.json"));

            store.Save(new DeskPosition
            {
                MonitorDeviceName = "\\\\.\\DISPLAY2",
                IsOnRight = false,
                VerticalPosition = 0.75
            });

            DeskPosition position = store.Load();

            Assert.Equal("\\\\.\\DISPLAY2", position.MonitorDeviceName);
            Assert.False(position.IsOnRight);
            Assert.Equal(0.75, position.VerticalPosition);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0.25, 0.25)]
        [InlineData(2, 1)]
        public void Load_ClampsVerticalPosition(
            double stored,
            double expected)
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("desk_position.json");
            string storedJson = stored.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            File.WriteAllText(
                filePath,
                $$"""{"MonitorDeviceName":null,"IsOnRight":true,"VerticalPosition":{{storedJson}}}""");

            var store = new DeskPositionStore(filePath);

            Assert.Equal(expected, store.Load().VerticalPosition);
        }

        [Fact]
        public void Load_WhenJsonIsInvalid_ReturnsDefaults()
        {
            using var directory = new TemporaryDirectory();
            string filePath = directory.Combine("desk_position.json");
            File.WriteAllText(filePath, "não é json");

            var store = new DeskPositionStore(filePath);

            DeskPosition position = store.Load();

            Assert.Null(position.MonitorDeviceName);
            Assert.True(position.IsOnRight);
            Assert.Equal(0.5, position.VerticalPosition);
        }
    }
}
