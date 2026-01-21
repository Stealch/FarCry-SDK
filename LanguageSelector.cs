using System;
using System.Windows.Forms;

namespace FarCry_SDK
{
    public static class LanguageSelector
    {
        private static string currentLanguage = "ru";

        public static void SetLanguage(string languageCode)
        {
            currentLanguage = languageCode;
            ApplyLanguage();
        }

        public static string GetCurrentLanguage()
        {
            return currentLanguage;
        }

        private static void ApplyLanguage()
        {
            // Получаем главную форму

            if (Application.OpenForms["MainForm"] is MainForm mainForm)
            {
                // Обновляем текст элементов формы
                UpdateFormText(mainForm);
            }
        }

        private static void UpdateFormText(MainForm form)
        {
            if (currentLanguage == "ru")
            {
                // Русский язык
                form.Text = "Far Cry SDK";
                form.FileMenuItem.Text = "Файл";
                form.DataMenuItem.Text = "Данные";
                form.EditMenuItem.Text = "Правка";
                form.SearchMenuItem.Text = "Поиск";
                form.SettingsMenuItem.Text = "Настройки";
                form.HelpMenuItem.Text = "Справка";

                // Подменю Файл
                form.CreateMenuItem.Text = "Создать";
                form.OpenMenuItem.Text = "Открыть";
                form.SaveMenuItem.Text = "Сохранить";
                form.SaveAsMenuItem.Text = "Сохранить как";
                form.ImportMenuItem.Text = "Импорт";
                form.ExportMenuItem.Text = "Экспорт";
                form.ProjectMenuItem.Text = "Проект";
                form.DatabaseMenuItem.Text = "Базу данных";

                //Подменю Данные
                form.UnpackMenuItem.Text = "Распаковать";
                form.PackMenuItem.Text = "Запаковать";


                // Подменю Правка
                form.UndoMenuItem.Text = "Отменить (CTRL+Z)";
                form.RedoMenuItem.Text = "Повторить (CTRL+Y)";
                form.CopyMenuItem.Text = "Копировать";
                form.PasteMenuItem.Text = "Вставить";

                // Подменю Настройки
                form.BasicSettingsMenuItem.Text = "Основные";
                form.LanguageMenuItem.Text = "Язык";
                form.RussianMenuItem.Text = "Русский";
                form.EnglishMenuItem.Text = "English";

                // Подменю Справка
                form.ManualMenuItem.Text = "Инструкция";
                form.AboutMenuItem.Text = "О программе";
            }
            else if (currentLanguage == "en")
            {
                // Английский язык
                form.Text = "Far Cry SDK";
                form.FileMenuItem.Text = "File";
                form.EditMenuItem.Text = "Edit";
                form.SearchMenuItem.Text = "Search";
                form.SettingsMenuItem.Text = "Settings";
                form.HelpMenuItem.Text = "Help";

                // Подменю File
                form.CreateMenuItem.Text = "Create";
                form.OpenMenuItem.Text = "Open";
                form.SaveMenuItem.Text = "Save";
                form.SaveAsMenuItem.Text = "Save As";
                form.ImportMenuItem.Text = "Import";
                form.ExportMenuItem.Text = "Export";
                form.ProjectMenuItem.Text = "Project";
                form.DatabaseMenuItem.Text = "Database";

                // Подменю Edit
                form.UndoMenuItem.Text = "Undo (CTRL+Z)";
                form.RedoMenuItem.Text = "Redo (CTRL+Y)";
                form.CopyMenuItem.Text = "Copy";
                form.PasteMenuItem.Text = "Paste";

                // Подменю Settings
                form.BasicSettingsMenuItem.Text = "Basic";
                form.LanguageMenuItem.Text = "Language";
                form.RussianMenuItem.Text = "Russian";
                form.EnglishMenuItem.Text = "English";

                // Подменю Help
                form.ManualMenuItem.Text = "Manual";
                form.AboutMenuItem.Text = "About";
            }
        }

        public static void InitializeForm(MainForm form)
        {
            // Инициализация формы текущим языком
            UpdateFormText(form);
        }
    }
}