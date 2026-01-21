using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Gibbed.Dunia2.FileFormats;
using Gibbed.IO;
using NDesk.Options;
using Big = Gibbed.Dunia2.FileFormats.Big;
using EntryDecompression = Gibbed.Dunia2.FileFormats.Big.EntryDecompression;

namespace FarCrySDK.Library
{
    public class ArchiveManager
    {
        public event EventHandler<string> LogMessage;
        public event EventHandler<int> ProgressChanged;
        public event EventHandler<bool> OperationCompleted;

        private void OnLog(string message) => LogMessage?.Invoke(this, message);
        private void OnProgress(int percent) => ProgressChanged?.Invoke(this, percent);
        private void OnComplete(bool success) => OperationCompleted?.Invoke(this, success);

        public async Task<bool> UnpackAsync(string fatFilePath, string outputDirectory)
        {
            // Используем Task.Run чтобы не блокировать GUI-поток
            return await Task.Run(() =>
            {
                try
                {
                    OnLog($"[INFO] Начинаю распаковку архива: {Path.GetFileName(fatFilePath)}");
                    OnLog($"[INFO] Выходная директория: {outputDirectory}");

                    // 1. ПОДГОТОВКА ПУТЕЙ (логика из Main)
                    string dataFilePath = Path.ChangeExtension(fatFilePath, ".dat");
                    if (File.Exists(dataFilePath) == false)
                    {
                        OnLog($"[ERROR] Не найден файл данных: {dataFilePath}");
                        OnComplete(false);
                        return false;
                    }

                    // 2. ЧТЕНИЕ .FAT ФАЙЛА (самое важное)
                    OnLog("[INFO] Чтение структуры архива (.fat)...");
                    var bigFile = new BigFile(); // <- Ключевой класс из FileFormats

                    using (var input = File.OpenRead(fatFilePath))
                    {
                        bigFile.Deserialize(input); // <- Вот он, главный вызов!
                    }

                    int totalEntries = bigFile.Entries.Count;
                    OnLog($"[INFO] Найдено записей в архиве: {totalEntries}");

                    if (totalEntries == 0)
                    {
                        OnLog("[WARN] Архив не содержит файлов.");
                        OnComplete(true); // Это не ошибка, просто пустой архив
                        return true;
                    }

                    // 3. ИЗВЛЕЧЕНИЕ ФАЙЛОВ ИЗ .DAT
                    OnLog("[INFO] Извлечение файлов...");
                    using (var dataInput = File.OpenRead(dataFilePath))
                    {
                        for (int i = 0; i < totalEntries; i++)
                        {
                            var entry = bigFile.Entries[i];

                            // ОБНОВЛЕНИЕ ПРОГРЕССА
                            int currentProgress = (i * 100) / totalEntries;
                            OnProgress(currentProgress);

                            // ЛОГИКА ИЗВЛЕЧЕНИЯ ОДНОГО ФАЙЛА
                            // TODO: Здесь может быть сложная логика с именами файлов через ProjectData.
                            // Для начала сделаем просто.
                            string entryName = entry.Name ?? $"__UNKNOWN_{entry.Hash:X8}";
                            OnLog($"[EXTRACT] ({i + 1}/{totalEntries}): {entryName}");

                            string outputPath = Path.Combine(outputDirectory, entryName);
                            string outputDir = Path.GetDirectoryName(outputPath);

                            if (outputDir != null && Directory.Exists(outputDir) == false)
                            {
                                Directory.CreateDirectory(outputDir);
                            }

                            dataInput.Position = entry.Offset; // Перемещаемся к данным файла в .dat
                            // TODO: Здесь может быть проверка на сжатие (entry.IsCompressed)
                            var data = dataInput.ReadBytes(entry.Size); // <- Читаем сырые данные

                            File.WriteAllBytes(outputPath, data); // <- Пишем на диск
                        }
                    }

                    OnProgress(100);
                    OnLog("[INFO] Распаковка успешно завершена!");
                    OnComplete(true);
                    return true;
                }
                catch (Exception ex)
                {
                    OnLog($"[ERROR] Критическая ошибка: {ex.Message}");
                    OnLog($"[ERROR] Подробности: {ex.StackTrace}");
                    OnComplete(false);
                    return false;
                }
            });
        }
    }
}