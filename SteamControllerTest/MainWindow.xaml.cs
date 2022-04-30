﻿using System;
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

using SteamControllerTest.ViewModels;

namespace SteamControllerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel mainWinVM = new MainWindowViewModel();
        public MainWindowViewModel MainWinVM => mainWinVM;

        public class ProfilePathEventArgs : EventArgs
        {
            private string filePath;
            public string FilePath
            {
                get { return filePath; }
            }

            public ProfilePathEventArgs(string filePath) : base()
            {
                this.filePath = filePath;
            }
        }

        public event EventHandler<ProfilePathEventArgs> ProfilePathChanged;

        public MainWindow()
        {
            InitializeComponent();

            mainWinVM.ProfilePathChanged += MainWinVM_ProfilePathChanged;

            DataContext = mainWinVM;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BackendManager manager = (App.Current as App).Manager;
            if (!manager.IsRunning)
            {
                StartCheckProcess();
            }
            else
            {
                StopCheckProcess();
            }
        }

        public async void StartCheckProcess()
        {
            serviceChangeBtn.IsEnabled = false;

            await Task.Run(async () =>
            {
                App thing = Application.Current as App;
                thing.Manager.Start();
                // Keep arbitrary delay for now. Not really needed though
                await Task.Delay(5000);
            });

            mainWinVM.ServiceBtnText = MainWindowViewModel.DEFAULT_STOP_TEXT;
            serviceChangeBtn.IsEnabled = true;
        }

        public async void StopCheckProcess()
        {
            serviceChangeBtn.IsEnabled = false;

            await Task.Run(async () =>
            {
                App thing = Application.Current as App;
                thing.Manager.Stop();
                // Keep arbitrary delay for now. Not really needed though
                await Task.Delay(5000);
            });

            mainWinVM.ServiceBtnText = MainWindowViewModel.DEFAULT_START_TEXT;
            serviceChangeBtn.IsEnabled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void FindProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
            {
                bool check = mainWinVM.CheckProfilePath(fileDialog.FileName);
                if (check)
                {
                    //startBtn.IsEnabled = true;
                }
            }
        }

        private void MainWinVM_ProfilePathChanged(object sender, EventArgs e)
        {
            ProfilePathChanged?.Invoke(this,
                new ProfilePathEventArgs(mainWinVM.ProfilePath));
        }
    }
}
