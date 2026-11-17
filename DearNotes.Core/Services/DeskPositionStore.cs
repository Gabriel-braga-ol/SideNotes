using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using DearNotes.Models;

namespace DearNotes.Services
{
    // Assume o caminho do arquivo, a leitura, a gravação e a validação da posição carregada.
    public sealed class DeskPositionStore : IDeskPositionStore
    {
        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            WriteIndented = true
        };

        private readonly string filePath;

        public DeskPositionStore()
            : this(AppPaths.DeskPositionFilePath)
        {
        }

        public DeskPositionStore(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            this.filePath = Path.GetFullPath(filePath);
        }

        public DeskPosition Load()
        {
            try
            {
                if (!File.Exists(filePath)) return new DeskPosition();

                string json = File.ReadAllText(filePath);

                DeskPosition? position =
                    JsonSerializer.Deserialize<DeskPosition>(json);

                if (position is null ||
                    !double.IsFinite(position.VerticalPosition))
                {
                    return new DeskPosition();
                }

                position.VerticalPosition = Math.Clamp(
                    position.VerticalPosition,
                    0,
                    1);

                return position;
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is JsonException)
            {
                Debug.WriteLine(
                    $"Não foi possível carregar a posição do Desk: {ex.Message}");

                return new DeskPosition();
            }
        }

        public void Save(DeskPosition position)
        {
            try
            {
                string folder = Path.GetDirectoryName(filePath)
                    ?? throw new IOException(
                        "O caminho do arquivo não contém uma pasta.");

                Directory.CreateDirectory(folder);

                string json = JsonSerializer.Serialize(
                    position,
                    serializerOptions);

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                Debug.WriteLine(
                    $"Não foi possível salvar a posição do Desk: {ex.Message}");
            }
        }
    }
}
