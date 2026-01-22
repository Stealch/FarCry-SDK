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
using EntryDecompression = Gibbed.Dunia2.FileFormats.Big.EntryDecompression;
using Gibbed.ProjectData;
using BigFile = Gibbed.Dunia2.FileFormats.BigFile;
using System.Threading;

namespace FarCry_SDK
{
    public class ArchiveManager
    {
        public event EventHandler<string> LogMessage;
        public event EventHandler<int> ProgressChanged;
        public event EventHandler<bool> OperationCompleted;

        private void OnLog(string message) => LogMessage?.Invoke(this, message);
        private void OnProgress(int percent) => ProgressChanged?.Invoke(this, percent);
        private void OnComplete(bool success) => OperationCompleted?.Invoke(this, success);

        // Основной публичный асинхронный метод
        public async Task<bool> UnpackAsync(string fatFilePath, string outputDirectory, string filterPattern = null, bool isVerbose = false)
        {
            return await Task.Run(() =>
            {
                return Unpack(fatFilePath, outputDirectory, filterPattern, isVerbose);
            });
        }

        // Основная логика распаковки (адаптирована из Gibbed.Dunia2.Unpack.Program)
        private bool Unpack(string fatFilePath, string outputDirectory, string filterPattern, bool verbose)
        {
            try
            {
                OnLog($"[INFO] Загрузка архива: {Path.GetFileName(fatFilePath)}");

                // Проверка существования файлов
                string dataFilePath;
                if (Path.GetExtension(fatFilePath) == ".dat")
                {
                    dataFilePath = fatFilePath;
                    fatFilePath = Path.ChangeExtension(fatFilePath, ".fat");
                    if (!File.Exists(fatFilePath))
                    {
                        OnLog($"[ERROR] Не найден .fat файл: {fatFilePath}");
                        return false;
                    }
                }
                else
                {
                    dataFilePath = Path.ChangeExtension(fatFilePath, ".dat");
                }

                if (!File.Exists(dataFilePath))
                {
                    OnLog($"[ERROR] Не найден .dat файл: {dataFilePath}");
                    return false;
                }

                Regex regex = null;
                if (!string.IsNullOrEmpty(filterPattern))
                {
                    regex = new Regex(filterPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }

                // Загрузка проекта и списков имён
                if (verbose) OnLog("[INFO] Загрузка проекта...");
                var manager = Manager.Load();
                if (manager.ActiveProject == null)
                {
                    OnLog("[WARN] Не загружен активный проект.");
                }

                // Чтение FAT
                if (verbose) OnLog("[INFO] Чтение FAT...");
                BigFile bigFile;
                using (FileStream fileStream = File.OpenRead(fatFilePath))
                {
                    bigFile = new BigFile();
                    bigFile.Deserialize(fileStream);
                }

                var hashes = manager.LoadListsFileNames(bigFile.Version);
                var subFatHashes = manager.LoadListsSubFatNames(bigFile.Version);

                // Создание выходной директории
                Directory.CreateDirectory(outputDirectory);

                // Извлечение файлов
                using (var dataStream = File.OpenRead(dataFilePath))
                {
                    int totalEntries = bigFile.Entries.Count;
                    OnLog($"[INFO] Найдено записей: {totalEntries}");

                    // Подсчёт обработанных файлов для прогресса (потокобезопасно)
                    int processedCount = 0;
                    object progressLock = new object();

                    // ПАРАЛЛЕЛЬНАЯ ОБРАБОТКА
                    Parallel.ForEach(bigFile.Entries, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount // Используем все ядра
                    },
                    (entry) =>
                    {
                    // Получение имени файла (потокобезопасно - только чтение)
                    string fileName = hashes[entry.NameHash];
                        if (string.IsNullOrEmpty(fileName))
                        {
                            fileName = $"__UNKNOWN_{entry.NameHash:X8}";
                        }

                   // Фильтрация по регулярному выражению
                   if (regex != null && !regex.IsMatch(fileName))
                        {
                   // Увеличиваем счётчик, даже если файл пропускаем, для корректного прогресса
                    System.Threading.Interlocked.Increment(ref processedCount);
                        return; // Пропускаем этот файл
                    }

                        string outputPath = Path.Combine(outputDirectory, fileName);
                        string outputDir = Path.GetDirectoryName(outputPath);

                   // Создание директории (потокобезопасно)
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                        {
                            Directory.CreateDirectory(outputDir);
                        }

                    // Логирование (с синхронизацией)
                    if (verbose)
                        {
                            lock (progressLock)
                            {
                                OnLog($"[EXTRACT] {fileName}");
                            }
                        }

                    // ВАЖНО: Каждый поток открывает свой FileStream для чтения данных
                        using (var threadDataStream = File.OpenRead(dataFilePath))
                        using (var outputFileStream = File.Create(outputPath))
                        {
                            threadDataStream.Position = entry.Offset;
                            EntryDecompression.Decompress(entry, threadDataStream, outputFileStream);
                        }

                    // Обновление прогресса (потокобезопасно)
                    int newCount = System.Threading.Interlocked.Increment(ref processedCount);
                        int progress = (newCount * 100) / totalEntries;
                        OnProgress(progress);
                    }); // Конец Parallel.ForEach

                    OnProgress(100);
                }

                OnLog("[INFO] Распаковка успешно завершена!");
                return true;
            }
            catch (Exception ex)
            {
                OnLog($"[ERROR] Ошибка распаковки: {ex.Message}");
                return false;
            }
        }

        // Вспомогательные методы (при необходимости можно добавить из оригинального Program.cs)
        private static string MakePath(string basePath, string fileName)
        {
            // Реализация из оригинального Program.MakePath
            return Path.Combine(basePath, fileName.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}