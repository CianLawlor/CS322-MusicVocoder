// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WebviewAppTest;
using WebviewAppTest.Data;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System.Collections.Generic;
using System;

namespace BlazorWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppState _appState = new();
        private int inputIndex = -1;
        private BufferedWaveProvider bufferedWaveProvider;
        WaveIn sourceStream = null;
        DirectSoundOut waveOut = null;


        public MainWindow()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            serviceCollection.AddSingleton<AppState>(_appState);
            serviceCollection.AddSingleton<WeatherForecastService>();

            _appState.selectedInputDeviceIndex = -1;
            _appState.inputChanged += new EventHandler(setInputIndex);
            _appState.recordStatusChanged += new EventHandler(onRecordStatusUpdate);

            GetAndSetInputs();


            Resources.Add("services", serviceCollection.BuildServiceProvider());

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            _appState.selectedInputDeviceIndex = 7;
        }

        private void GetAndSetInputs()
        {
            List<WaveInCapabilities> sources = new List<WaveInCapabilities>();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                sources.Add(WaveIn.GetCapabilities(i));
            }

            string[] output = new string[] {}; 
            foreach (var source in sources)
            {
                Array.Resize(ref output, output.Length + 1);
                output[output.GetUpperBound(0)] = source.ProductName;
                Array.Resize(ref output, output.Length + 1);
                output[output.GetUpperBound(0)] = source.Channels.ToString();
            }

            _appState.setInputDevices(output);
        }

        private void setInputIndex(object sender, EventArgs e)
        {
            inputIndex = ((AppState)sender).selectedInputDeviceIndex;
        }

        private void onRecordStatusUpdate(object sender, EventArgs e)
        {
            if(inputIndex != -1 && ((AppState)sender).recordStatus == "record")
            {
                sourceStream = new WaveIn();
                sourceStream.DeviceNumber = inputIndex;
                sourceStream.WaveFormat = new NAudio.Wave.WaveFormat(44100, NAudio.Wave.WaveIn.GetCapabilities(inputIndex).Channels);

                WaveInProvider waveIn = new WaveInProvider(sourceStream);
                waveOut = new NAudio.Wave.DirectSoundOut();
                waveOut.Init(waveIn);

                sourceStream.StartRecording();
                waveOut.Play();
            } else if (inputIndex != -1 && ((AppState)sender).recordStatus == "stop")
            {
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
                if (sourceStream != null)
                {
                    sourceStream.StopRecording();
                    sourceStream.Dispose();
                    sourceStream = null;
                }
            }
        }
    }

    // Workaround for compiler error "error MC3050: Cannot find the type 'local:Main'"
    // It seems that, although WPF's design-time build can see Razor components, its runtime build cannot.
    public partial class Main { }
}
