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
        private Color darkBackground = Color.FromArgb(45, 45, 48);
        private Color darkPanel = Color.FromArgb(63, 63, 70);
        private Color darkText = Color.FromArgb(241, 241, 241);
        private Color darkHighlight = Color.FromArgb(0, 122, 204);

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

        public MainForm()
        {
            InitializeComponent();
            ApplyDarkTheme();
            LanguageSelector.InitializeForm(this);
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
    }
}