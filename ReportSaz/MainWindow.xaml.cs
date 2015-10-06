using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using ReportPstn;

namespace ReportSaz
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string fileName;
        ProgressBarWindow progressBar;
        event RoutedEventHandler ReportComplete;

        public MainWindow()
        {
            InitializeComponent();
            ReportComplete += MenuItem_Click;
        }

        void MainWindow_ReportStart(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                taskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                progressBar = new ProgressBarWindow();
                progressBar.Owner = this;
                progressBar.ShowDialog();
            }));
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void EnableButton(bool isEnabled)
        {
            if (isEnabled == true)
            {
                btnExecute.IsEnabled = true;
                btnExecute.Opacity = 1;
            }
            else
            {
                btnExecute.IsEnabled = false;
                btnExecute.Opacity = 0.6;
            }
        }

        private void lblDrop_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                string filePath = droppedFilePaths[0];
                string extension = System.IO.Path.GetExtension(filePath);
                if (extension == ".xls" || extension == ".xlsx")
                {
                    fileName = filePath;
                    txtName.Text = filePath;
                    EnableButton(true);
                }
                else
                {
                    MessageBox.Show("Требуется файл Excel", "Неверный формат файла",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            fileName = string.Empty;
            Dispatcher.BeginInvoke(new Action(delegate()
                {
                    txtName.Text = "Перетащите файл сюда";
                    EnableButton(false);
                }));
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Файлы Excel|*.xls;*.xlsx";
            openDialog.CheckFileExists = true;
            if (openDialog.ShowDialog() == true)
            {
                fileName = openDialog.FileName;
                txtName.Text = openDialog.FileName;
                EnableButton(true);
            }
        }

        private async void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            EnableButton(false);
            Report report = new Report(fileName);
            report.Start += MainWindow_ReportStart;
            report.Complete += Save;
            try
            {
                await Task.Run(() => report.Create());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка чтения файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save(object sender, ReportEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                taskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                progressBar.Close();
            }));

            string defaultFilePath = e.DefaultFilePath;
            string saveDefault = string.Format("Сохранить отчет в файле по умолчанию:\n{0}?", defaultFilePath);
            MessageBoxResult result = MessageBox.Show(saveDefault, "Сохранение отчета",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (sender is Report)
            {
                Report report = (Report)sender;
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (System.IO.File.Exists(defaultFilePath))
                        {
                            throw new System.IO.IOException(string.Format("Файл {0} уже существует.", defaultFilePath));
                        }
                        else
                        {
                            report.Save();
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        ErrorSave(defaultFilePath, report, ex);
                    }
                }
                else
                {
                    SaveFile(defaultFilePath, report);
                }

                if (ReportComplete != null)
                {
                    ReportComplete(this, new RoutedEventArgs());
                }
            }
        }

        private static void ErrorSave(string defaultFilePath, Report report, System.IO.IOException ex)
        {
            string message = string.Format("{0}\nСохранить отчет в другом файле?", ex.Message);
            MessageBoxResult res = MessageBox.Show(message, "Ошибка сохранения отчета",
            MessageBoxButton.OKCancel, MessageBoxImage.Error);
            if (res == MessageBoxResult.OK)
            {
                SaveFile(defaultFilePath, report);
            }
        }

        private static void SaveFile(string defaultFilePath, Report report)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.InitialDirectory = System.IO.Path.GetDirectoryName(defaultFilePath);
            saveDialog.Filter = "Файлы Excel|*.xlsx";
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    report.Save(saveDialog.FileName);
                }
                catch (System.IO.IOException ex)
                {
                    ErrorSave(defaultFilePath, report, ex);
                    throw;
                }
            }
        }
    }
}
