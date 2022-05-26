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
using SteamControllerTest.Views;
using SteamControllerTest.ViewModels.StickActionPropViewModels;
using SteamControllerTest.StickActions;
using SteamControllerTest.ButtonActions;

namespace SteamControllerTest.Views.StickActionPropControls
{
    /// <summary>
    /// Interaction logic for StickPadActionControl.xaml
    /// </summary>
    public partial class StickPadActionControl : UserControl
    {
        public class DirButtonBindingArgs : EventArgs
        {
            private AxisDirButton dirBtn;
            public AxisDirButton DirBtn => dirBtn;

            public DirButtonBindingArgs(AxisDirButton dirBtn)
            {
                this.dirBtn = dirBtn;
            }
        }

        private StickPadActionPropViewModel stickPadActVM;
        public StickPadActionPropViewModel StickPadActVM => stickPadActVM;

        public event EventHandler<int> ActionTypeIndexChanged;
        public event EventHandler<DirButtonBindingArgs> RequestFuncEditor;

        public StickPadActionControl()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, StickMapAction action)
        {
            stickPadActVM = new StickPadActionPropViewModel(mapper, action);
            DataContext = stickPadActVM;

            stickSelectControl.PostInit(mapper, action);
            stickSelectControl.StickActSelVM.SelectedIndexChanged += StickActSelVM_SelectedIndexChanged;
        }

        private void StickActSelVM_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionTypeIndexChanged?.Invoke(this,
                stickSelectControl.StickActSelVM.SelectedIndex);
        }

        private void btnUpEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(stickPadActVM.Action.EventCodes4[(int)StickPadAction.DpadDirections.Up]));
        }

        private void btnDownEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(stickPadActVM.Action.EventCodes4[(int)StickPadAction.DpadDirections.Down]));
        }

        private void btnLeftEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(stickPadActVM.Action.EventCodes4[(int)StickPadAction.DpadDirections.Left]));
        }

        private void btnRightEdit_Click(object sender, RoutedEventArgs e)
        {
            RequestFuncEditor?.Invoke(this,
                new DirButtonBindingArgs(stickPadActVM.Action.EventCodes4[(int)StickPadAction.DpadDirections.Right]));
        }
    }
}