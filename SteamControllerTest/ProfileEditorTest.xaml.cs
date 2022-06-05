﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.Views;
using SteamControllerTest.ViewModels;
using SteamControllerTest.TriggerActions;
using SteamControllerTest.TouchpadActions;
using SteamControllerTest.StickActions;
using SteamControllerTest.GyroActions;

namespace SteamControllerTest
{
    /// <summary>
    /// Interaction logic for ProfileEditorTest.xaml
    /// </summary>
    public partial class ProfileEditorTest : Window
    {
        private ProfileEditorTestViewModel editorTestVM;

        public ProfileEditorTest()
        {
            InitializeComponent();
        }

        public void PostInit(Mapper mapper, ProfileEntity profileEnt, Profile currentProfile)
        {
            editorTestVM = new ProfileEditorTestViewModel(mapper, profileEnt, currentProfile);

            DataContext = editorTestVM;

            editorTestVM.Test();
        }

        private void ButtonActionEditButton_Click(object sender, RoutedEventArgs e)
        {
            string mapTag = (sender as Button).Tag.ToString();
            int ind = editorTestVM.ButtonBindingsIndexDict[mapTag];
            Debug.WriteLine(mapTag);
            
            ButtonMapAction tempAction = editorTestVM.ButtonBindings[ind].MappedAction;
            if (tempAction.GetType() == typeof(ButtonAction))
            {
                Trace.WriteLine($"TYPE {tempAction.GetType()}");

                ButtonFuncEditWindow btnFuncEditWin = new ButtonFuncEditWindow();
                btnFuncEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                btnFuncEditWin.ShowDialog();

                editorTestVM.ButtonBindings[ind].UpdateAction(btnFuncEditWin.BtnFuncEditVM.Action);

                //ButtonActionEditTest btnTestEdit = new ButtonActionEditTest();
                //btnTestEdit.PostInit(tempAction as ButtonAction, editorTestVM.DeviceMapper);
                //btnTestEdit.ShowDialog();
            }
            else if (tempAction.GetType() == typeof(ButtonNoAction))
            {
                ButtonFuncEditWindow btnFuncEditWin = new ButtonFuncEditWindow();
                btnFuncEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                btnFuncEditWin.ShowDialog();

                editorTestVM.ButtonBindings[ind].UpdateAction(btnFuncEditWin.BtnFuncEditVM.Action);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Control tempControl = sender as Control;
            TouchBindingItemsTest tempItem = tempControl.Tag as TouchBindingItemsTest;
            int selectedInd = editorTestVM.TouchpadBindings.IndexOf(tempItem);
            //Trace.WriteLine($"HAMHAM {tempItem}");
            //int selectedInd = editorTestVM.SelectTouchBindIndex;
            if (selectedInd >= 0)
            {
                editorTestVM.SelectTouchBindIndex = selectedInd;
                //TouchpadMapAction tempAction = editorTestVM.TouchpadBindings[selectedInd].MappedAction;
                TouchpadMapAction tempAction = tempItem.MappedAction;
                TouchpadBindEditWindow touchBindEditWin = new TouchpadBindEditWindow();
                touchBindEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                touchBindEditWin.ShowDialog();

                editorTestVM.TouchpadBindings[selectedInd].UpdateAction(touchBindEditWin.TouchBindEditVM.Action);
            }
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int ind = Convert.ToInt32((sender as Button).Tag);
            if (ind >= 0 && ind != editorTestVM.SelectedActionSetIndex)
            {
                IsEnabled = false;

                editorTestVM.SwitchActionSets(ind);

                await Task.Run(() =>
                {
                    editorTestVM.ActionResetEvent.Wait();
                });

                DataContext = null;

                editorTestVM.RefreshSetBindings();

                DataContext = editorTestVM;

                IsEnabled = true;
            }
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            int ind = Convert.ToInt32((sender as Button).Tag);
            if (ind >= 0 && ind != editorTestVM.SelectedActionLayerIndex)
            {
                IsEnabled = false;

                editorTestVM.SwitchActionLayer(ind);

                await Task.Run(() =>
                {
                    editorTestVM.ActionResetEvent.Wait();
                });

                DataContext = null;

                editorTestVM.RefreshLayerBindings();

                DataContext = editorTestVM;

                IsEnabled = true;
            }
        }

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;

            editorTestVM.TestFakeSave(editorTestVM.ProfileEnt, editorTestVM.DeviceMapper.ActionProfile);

            await Task.Run(() =>
            {
                editorTestVM.ActionResetEvent.Wait();
            });

            IsEnabled = true;
        }

        private void AddLayerBtn_Click(object sender, RoutedEventArgs e)
        {
            editorTestVM.AddLayer();
        }

        private void RemoveLayerBtn_Click(object sender, RoutedEventArgs e)
        {
            // Skip for default ActionLayer
            if (editorTestVM.SelectedActionLayerIndex <= 0) return;

            editorTestVM.RemoveLayer();
            SwapActionLayer(editorTestVM.SelectedActionLayerIndex);
        }

        private void SwapActionLayer(int ind)
        {
            IsEnabled = false;

            editorTestVM.SwitchActionLayer(ind);

            editorTestVM.ActionResetEvent.Wait();

            DataContext = null;

            editorTestVM.RefreshLayerBindings();

            DataContext = editorTestVM;

            IsEnabled = true;
        }

        private void AddSetBtn_Click(object sender, RoutedEventArgs e)
        {
            editorTestVM.AddSet();
        }

        private void RemoveSetBtn_Click(object sender, RoutedEventArgs e)
        {
            // Skip for default ActionSet
            if (editorTestVM.SelectedActionSetIndex <= 0) return;

            editorTestVM.RemoveSet();
            SwapActionSet(editorTestVM.SelectedActionSetIndex);
        }

        private void SwapActionSet(int ind)
        {
            IsEnabled = false;

            editorTestVM.SwitchActionSets(ind);

            editorTestVM.ActionResetEvent.Wait();

            DataContext = null;

            editorTestVM.RefreshSetBindings();

            DataContext = editorTestVM;

            IsEnabled = true;
        }

        private void TiggerActionEdit_Button_Click(object sender, RoutedEventArgs e)
        {
            //int selectedInd = editorTestVM.SelectTriggerBindIndex;
            Control tempControl = sender as Control;
            TriggerBindingItemsTest tempItem = tempControl.Tag as TriggerBindingItemsTest;
            int selectedInd = editorTestVM.TriggerBindings.IndexOf(tempItem);
            if (selectedInd >= 0)
            {
                editorTestVM.SelectTriggerBindIndex = selectedInd;
                TriggerMapAction tempAction = editorTestVM.TriggerBindings[selectedInd].MappedAction;
                TriggerBindEditWindow trigBindEditWin = new TriggerBindEditWindow();
                trigBindEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                trigBindEditWin.ShowDialog();

                editorTestVM.TriggerBindings[selectedInd].UpdateAction(trigBindEditWin.TrigBindEditVM.Action);
            }
        }

        private void StickActionEdit_Button_Click(object sender, RoutedEventArgs e)
        {
            //int selectedInd = editorTestVM.SelectStickBindIndex;
            Control tempControl = sender as Control;
            StickBindingItemsTest tempItem = tempControl.Tag as StickBindingItemsTest;
            int selectedInd = editorTestVM.StickBindings.IndexOf(tempItem);
            if (selectedInd >= 0)
            {
                editorTestVM.SelectStickBindIndex = selectedInd;
                StickMapAction tempAction = editorTestVM.StickBindings[selectedInd].MappedAction;
                StickBindEditWindow stickBindEditWin = new StickBindEditWindow();
                stickBindEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                stickBindEditWin.ShowDialog();

                editorTestVM.StickBindings[selectedInd].UpdateAction(stickBindEditWin.StickBindEditVM.Action);
            }
        }

        private void GyroActionEdit_Button_Click(object sender, RoutedEventArgs e)
        {
            //int selectedInd = editorTestVM.SelectGyroBindIndex;
            Control tempControl = sender as Control;
            GyroBindingItemsTest tempItem = tempControl.Tag as GyroBindingItemsTest;
            int selectedInd = editorTestVM.GyroBindings.IndexOf(tempItem);
            if (selectedInd >= 0)
            {
                editorTestVM.SelectGyroBindIndex = selectedInd;
                GyroMapAction tempAction = editorTestVM.GyroBindings[selectedInd].MappedAction;
                GyroBindEditWindow gyroBindEditWin = new GyroBindEditWindow();
                gyroBindEditWin.PostInit(editorTestVM.DeviceMapper, tempAction);
                gyroBindEditWin.ShowDialog();

                editorTestVM.GyroBindings[selectedInd].UpdateAction(gyroBindEditWin.GyroBindEditVM.Action);
            }
        }
    }
}
