using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class ViewPageVM : ObservableObject {
        private static ViewPageVM _main;
        public static ViewPageVM Main => _main ??= new();

        public ViewSettingsModel ViewSettingsModel { get; } = ViewSettingsModel.Main;

        [ObservableProperty]
        private bool _isAutoScrolling;


    }
}
