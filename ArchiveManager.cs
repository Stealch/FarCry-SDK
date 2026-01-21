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
using Gibbed.ProjectData;

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

        private static void Main(string[] args)
        {
            bool showHelp = false;
            bool extractUnknowns = true;
            bool extractFiles = true;
            bool extractSubFats = true;
            bool unpackSubFats = false;
            string filterPattern = null;
            bool overwriteFiles = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {"o|overwrite", "overwrite existing files", v => overwriteFiles = v != null},
                {"nf|no-files", "don't extract files", v => extractFiles = v == null},
                {"nu|no-unknowns", "don't extract unknown files", v => extractUnknowns = v == null},
                {"ns|no-subfats", "don't extract subfats", v => extractSubFats = v == null},
                {"us|unpack-subfats", "unpack files from subfats", v => unpackSubFats = v != null},
                {"f|filter=", "only extract files using pattern", v => filterPattern = v},
                {"v|verbose", "be verbose", v => verbose = v != null},
                {"h|help", "show this message and exit", v => showHelp = v != null},
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_fat [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Unpack files from a Big File (FAT/DAT pair).");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string fatPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(fatPath, null) + "_unpack";
            string datPath;

            Regex filter = null;
            if (string.IsNullOrEmpty(filterPattern) == false)
            {
                filter = new Regex(filterPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            if (Path.GetExtension(fatPath) == ".dat")
            {
                datPath = fatPath;
                fatPath = Path.ChangeExtension(fatPath, ".fat");
            }
            else
            {
                datPath = Path.ChangeExtension(fatPath, ".dat");
            }

            if (verbose == true)
            {
                Console.WriteLine("Loading project...");
            }

            var manager = Gibbed.ProjectData.Manager.Load();
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
            }

            if (verbose == true)
            {
                Console.WriteLine("Reading FAT...");
            }

            BigFile fat;
            using (var input = File.OpenRead(fatPath))
            {
                fat = new BigFile();
                fat.Deserialize(input);
            }

            var hashes = manager.LoadListsFileNames(fat.Version);
            var subFatHashes = manager.LoadListsSubFatNames(fat.Version);

            using (var input = File.OpenRead(datPath))
            {
                if (extractFiles == true)
                {
                    Big.Entry[] entries;
                    if (extractSubFats == true &&
                        unpackSubFats == true)
                    {
                        entries =
                            fat.Entries.Concat(fat.SubFats.SelectMany(sf => sf.Entries))
                               .OrderBy(e => e.Offset)
                               .ToArray();
                    }
                    else
                    {
                        entries = fat.Entries.OrderBy(e => e.Offset).ToArray();
                    }

                    if (entries.Length > 0)
                    {
                        if (verbose == true)
                        {
                            Console.WriteLine("Unpacking files...");
                        }

                        long current = 0;
                        long total = entries.Length;
                        var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                        var duplicates = new Dictionary<ulong, int>();

                        foreach (var entry in entries)
                        {
                            current++;

                            if (subFatHashes.Contains(entry.NameHash) == true)
                            {
                                continue;
                            }

                            string entryName;
                            if (GetEntryName(input, fat, entry, hashes, extractUnknowns, out entryName) == false)
                            {
                                continue;
                            }

                            if (duplicates.ContainsKey(entry.NameHash) == true)
                            {
                                var number = duplicates[entry.NameHash]++;
                                var e = Path.GetExtension(entryName);
                                var nn =
                                    Path.ChangeExtension(
                                        Path.ChangeExtension(entryName, null) + "__DUPLICATE_" +
                                        number.ToString(CultureInfo.InvariantCulture),
                                        e);
                                entryName = Path.Combine("__DUPLICATE", nn);
                            }
                            else
                            {
                                duplicates[entry.NameHash] = 0;
                            }

                            if (filter != null &&
                                filter.IsMatch(entryName) == false)
                            {
                                continue;
                            }

                            var entryPath = Path.Combine(outputPath, entryName);
                            if (overwriteFiles == false &&
                                File.Exists(entryPath) == true)
                            {
                                continue;
                            }

                            if (verbose == true)
                            {
                                Console.WriteLine("[{0}/{1}] {2}",
                                                  current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                                                  total,
                                                  entryName);
                            }

                            input.Seek(entry.Offset, SeekOrigin.Begin);

                            var entryParent = Path.GetDirectoryName(entryPath);
                            if (string.IsNullOrEmpty(entryParent) == false)
                            {
                                Directory.CreateDirectory(entryParent);
                            }

                            using (var output = File.Create(entryPath))
                            {
                                EntryDecompression.Decompress(entry, input, output);
                            }
                        }
                    }
                }

                if (extractSubFats == true &&
                    unpackSubFats == false &&
                    fat.SubFats.Count > 0)
                {
                    if (verbose == true)
                    {
                        Console.WriteLine("Unpacking subfats...");
                    }

                    var subFatsFromFat = fat.SubFats.ToList();

                    long current = 0;
                    long total = subFatsFromFat.Count;
                    var padding = total.ToString(CultureInfo.InvariantCulture).Length;

                    foreach (var headerEntry in fat.Entries.Where(e => subFatHashes.Contains(e.NameHash) == true))
                    {
                        current++;

                        var subFat = new SubFatFile();
                        using (var temp = new MemoryStream())
                        {
                            EntryDecompression.Decompress(headerEntry, input, temp);
                            temp.Position = 0;
                            subFat.Deserialize(temp, fat);
                        }

                        var matchingSubFats = subFatsFromFat
                            .Where(sf => subFat.Entries.SequenceEqual(sf.Entries))
                            .ToArray();

                        if (matchingSubFats.Length == 0)
                        {
                            continue;
                        }

                        if (matchingSubFats.Length > 1)
                        {
                            throw new InvalidOperationException();
                        }

                        var entryName = subFatHashes[headerEntry.NameHash];
                        entryName = FilterEntryName(entryName);

                        var entryHeaderPath = Path.Combine(outputPath, "__SUBFAT", entryName);
                        if (overwriteFiles == false &&
                            File.Exists(entryHeaderPath) == true)
                        {
                            continue;
                        }

                        if (verbose == true)
                        {
                            Console.WriteLine("[{0}/{1}] {2}",
                                              current.ToString(CultureInfo.InvariantCulture).PadLeft(padding),
                                              total,
                                              entryName);
                        }

                        var entryParent = Path.GetDirectoryName(entryHeaderPath);
                        if (string.IsNullOrEmpty(entryParent) == false)
                        {
                            Directory.CreateDirectory(entryParent);
                        }

                        var entryDataPath = Path.ChangeExtension(entryHeaderPath, ".dat");

                        var rebuiltFat = new BigFile
                        {
                            Version = fat.Version,
                            Platform = fat.Platform,
                            Unknown74 = fat.Unknown74
                        };

                        using (var output = File.Create(entryDataPath))
                        {
                            var rebuiltEntries = new List<Big.Entry>();
                            foreach (var entry in subFat.Entries.OrderBy(e => e.Offset))
                            {
                                var rebuiltEntry = new Big.Entry
                                {
                                    NameHash = entry.NameHash,
                                    UncompressedSize = entry.UncompressedSize,
                                    CompressedSize = entry.CompressedSize,
                                    Offset = output.Position,
                                    CompressionScheme = entry.CompressionScheme
                                };

                                input.Seek(entry.Offset, SeekOrigin.Begin);
                                output.WriteFromStream(input, entry.CompressedSize);
                                output.Seek(output.Position.Align(16), SeekOrigin.Begin);

                                rebuiltEntries.Add(rebuiltEntry);
                            }
                            rebuiltFat.Entries.AddRange(rebuiltEntries.OrderBy(e => e.NameHash));
                        }

                        using (var output = File.Create(entryHeaderPath))
                        {
                            rebuiltFat.Serialize(output);
                        }

                        foreach (var matchingSubFat in matchingSubFats)
                        {
                            subFatsFromFat.Remove(matchingSubFat);
                        }
                    }

                    if (subFatsFromFat.Count > 0)
                    {
                        Console.WriteLine("Warning: could not identify {0} subfats", subFatsFromFat.Count);
                    }
                }
            }
        }
        private static bool GetEntryName(Stream input,
                                         BigFile fat,
                                         Big.Entry entry,
                                         Gibbed.ProjectData.HashList<ulong> hashes,
                                         bool extractUnknowns,
                                         out string entryName)
        {
            entryName = hashes[entry.NameHash];

            if (entryName == null)
            {
                if (extractUnknowns == false)
                {
                    return false;
                }

                string type;
                string extension;
                {
                    var guess = new byte[64];
                    int read = 0;

                    if (entry.CompressionScheme == Big.CompressionScheme.None)
                    {
                        if (entry.CompressedSize > 0)
                        {
                            input.Seek(entry.Offset, SeekOrigin.Begin);
                            read = input.Read(guess, 0, (int)Math.Min(entry.CompressedSize, guess.Length));
                        }
                    }
                    else
                    {
                        using (var temp = new MemoryStream())
                        {
                            EntryDecompression.Decompress(entry, input, temp);
                            temp.Position = 0;
                            read = temp.Read(guess, 0, (int)Math.Min(temp.Length, guess.Length));
                        }
                    }

                    var tuple = FileExtensions.Detect(guess, Math.Min(guess.Length, read));
                    type = tuple != null ? tuple.Item1 : "unknown";
                    extension = tuple != null ? tuple.Item2 : null;
                }

                entryName = entry.NameHash.ToString(fat.Version >= 9 ? "X16" : "X8");

                if (string.IsNullOrEmpty(extension) == false)
                {
                    entryName = Path.ChangeExtension(entryName, "." + extension);
                }

                if (string.IsNullOrEmpty(type) == false)
                {
                    entryName = Path.Combine(type, entryName);
                }

                entryName = Path.Combine("__UNKNOWN", entryName);
            }
            else
            {
                entryName = FilterEntryName(entryName);
            }

            return true;
        }

        private static string FilterEntryName(string entryName)
        {
            entryName = entryName.Replace("/", "\\");
            if (entryName.StartsWith("\\") == true)
            {
                entryName = entryName.Substring(1);
            }
            return entryName;
        }
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
    }
}