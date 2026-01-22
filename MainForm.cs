using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FarCry_SDK
{
    public partial class MainForm : Form
    {
        private readonly Color darkBackground = Color.FromArgb(45, 45, 48);
        private readonly Color darkPanel = Color.FromArgb(63, 63, 70);
        private readonly Color darkText = Color.FromArgb(241, 241, 241);
        private readonly Color darkHighlight = Color.FromArgb(0, 122, 204);

        // Добавляем менеджер архивов
        private ArchiveManager _archiveManager;

        // Свойства для доступа к элементам меню из LanguageSelector
        public ToolStripMenuItem FileMenuItem => fileToolStripMenuItem;
        public ToolStripMenuItem CreateMenuItem => createToolStripMenuItem;
        public ToolStripMenuItem ProjectMenuItem => projectToolStripMenuItem;
        public ToolStripMenuItem DatabaseMenuItem => databaseToolStripMenuItem;
        public ToolStripMenuItem OpenMenuItem => openToolStripMenuItem;
        public ToolStripMenuItem SaveMenuItem => saveToolStripMenuItem;
        public ToolStripMenuItem SaveAsMenuItem => saveAsToolStripMenuItem;
        public ToolStripMenuItem ImportMenuItem => importToolStripMenuItem;
        public ToolStripMenuItem ExportMenuItem => exportToolStripMenuItem;
        public ToolStripMenuItem EditMenuItem => editToolStripMenuItem;
        public ToolStripMenuItem UndoMenuItem => undoToolStripMenuItem;
        public ToolStripMenuItem RedoMenuItem => redoToolStripMenuItem;
        public ToolStripMenuItem CopyMenuItem => copyToolStripMenuItem;
        public ToolStripMenuItem PasteMenuItem => pasteToolStripMenuItem;
        public ToolStripMenuItem SearchMenuItem => searchToolStripMenuItem;
        public ToolStripMenuItem SettingsMenuItem => settingsToolStripMenuItem;
        public ToolStripMenuItem BasicSettingsMenuItem => basicSettingsToolStripMenuItem;
        public ToolStripMenuItem LanguageMenuItem => languageToolStripMenuItem;
        public ToolStripMenuItem RussianMenuItem => russianToolStripMenuItem;
        public ToolStripMenuItem EnglishMenuItem => englishToolStripMenuItem;
        public ToolStripMenuItem HelpMenuItem => helpToolStripMenuItem;
        public ToolStripMenuItem ManualMenuItem => manualToolStripMenuItem;
        public ToolStripMenuItem AboutMenuItem => aboutToolStripMenuItem;
        public ToolStripMenuItem UnpackMenuItem => unpackToolStripMenuItem;
        public ToolStripMenuItem PackMenuItem => packToolStripMenuItem;
        public ToolStripMenuItem DataMenuItem => dataToolStripMenuItem;

        // Элементы для вывода логов (позже можно добавить TextBox или ListBox)
        // public TextBox LogTextBox => logTextBox;

        public MainForm()
        {
            InitializeComponent();
            ApplyDarkTheme();
            LanguageSelector.InitializeForm(this);

            // Инициализируем менеджер и подписываемся на события
            _archiveManager = new ArchiveManager();
            _archiveManager.LogMessage += ArchiveManager_LogMessage;
            _archiveManager.ProgressChanged += (sender, percent) =>
            {
                if (progressBar1.InvokeRequired)
                    progressBar1.Invoke(new Action(() => progressBar1.Value = percent));
                else
                    progressBar1.Value = percent;
            };
        }

        private void ApplyDarkTheme()
        {
            // Основные настройки формы
            this.BackColor = darkBackground;
            this.ForeColor = darkText;

            // Меню
            menuStrip1.BackColor = darkPanel;
            menuStrip1.ForeColor = darkText;

            // Прогресс-бар
            progressBar1.BackColor = darkPanel;
            progressBar1.ForeColor = darkHighlight;
        }

        // Обработчик сообщений от ArchiveManager
        private void ArchiveManager_LogMessage(object sender, string message)
        {
            // Выводим сообщения в отладочную консоль (позже можно в TextBox)
            System.Diagnostics.Debug.WriteLine($"[ArchiveManager] {message}");
        }

        // Обработчик прогресса от ArchiveManager
        private void ArchiveManager_ProgressChanged(object sender, int percent)
        {
            // Важно! Обновление UI должно быть в UI-потоке
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => progressBar1.Value = percent));
            }
            else
            {
                progressBar1.Value = percent;
            }
        }

        // === Обработчики событий меню ===
        private void createToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Обработчик для пункта "Создать"
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Обработчик для пункта "Настройки"
        }

        private void russianToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Переключение на русский язык
            LanguageSelector.SetLanguage("ru");
            UpdateLanguageMenuItems();
        }

        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Переключение на английский язык
            LanguageSelector.SetLanguage("en");
            UpdateLanguageMenuItems();
        }

        private void UpdateLanguageMenuItems()
        {
            // Обновляем галочки в меню выбора языка
            string currentLang = LanguageSelector.GetCurrentLanguage();
            russianToolStripMenuItem.Checked = (currentLang == "ru");
            englishToolStripMenuItem.Checked = (currentLang == "en");
        }

        // ГЛАВНОЕ: Обработчик распаковки
        private async void unpackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. Выбор .fat файла
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Far Cry Archive (*.fat)|*.fat|All files (*.*)|*.*";
                openDialog.Title = "Выберите .fat файл для распаковки";

                if (openDialog.ShowDialog() != DialogResult.OK)
                {
                    return; // Пользователь отменил выбор
                }

                // 2. Выбор папки для распаковки
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Выберите папку для распаковки";

                    if (folderDialog.ShowDialog() != DialogResult.OK)
                    {
                        return; // Пользователь отменил выбор
                    }

                    // 3. Запуск распаковки
                    try
                    {
                        this.Enabled = false; // Блокируем форму на время операции
                        progressBar1.Value = 0;

                        bool success = await _archiveManager.UnpackAsync(
                            openDialog.FileName,
                            folderDialog.SelectedPath,
                            null, // filterPattern (пока без фильтра)
                            true   // verbose (вывод подробных логов)
                        );

                        MessageBox.Show(
                            success ? "Распаковка успешно завершена!" : "В процессе распаковки произошла ошибка.",
                            "Результат",
                            MessageBoxButtons.OK,
                            success ? MessageBoxIcon.Information : MessageBoxIcon.Warning
                        );
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Произошла непредвиденная ошибка:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                    finally
                    {
                        this.Enabled = true; // Разблокируем форму
                    }
                }
            }
        }

        // Заглушка для упаковки (пока не реализована)
        private void packToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Функционал упаковки (Pack) ещё не реализован.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}