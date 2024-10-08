﻿using Microsoft.UI.Xaml;
using HitomiScrollViewerLib.Views;

namespace HitomiScrollViewerApp {
    public partial class App : Application {
        private MainWindow _mainWindow;
        public App() {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args) {
            _mainWindow = new MainWindow();
            _mainWindow.Activate();
        }
    }
}
