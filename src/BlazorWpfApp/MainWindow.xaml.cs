// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WebviewAppTest;
using WebviewAppTest.Data;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using Syncfusion.Blazor;
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
        private string _pitchStatus = "stop";
        private int _pitchValue = 0;
        private BufferedWaveProvider bufferedWaveProviderPitch;
        private BufferedWaveProvider bufferedWaveProviderNormal;
        WaveIn sourceStream = null;
        WaveIn pitchStream = null;
        DirectSoundOut waveOut = null;


        public MainWindow()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            serviceCollection.AddSingleton<AppState>(_appState);
            serviceCollection.AddSingleton<WeatherForecastService>();
            serviceCollection.AddSyncfusionBlazor();

            _appState.selectedInputDeviceIndex = -1;
            _appState.inputChanged += new EventHandler(setInputIndex);
            _appState.recordStatusChanged += new EventHandler(onRecordStatusUpdate);
            _appState.pitchStatusChanged += new EventHandler(onPitchStatusUpdate);
            _appState.pitchValueChanged += new EventHandler(onPitchValueUpdate);


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

        private void onPitchStatusUpdate(object sender, EventArgs e)
        {
            _pitchStatus = ((AppState)sender).pitchStatus;
        }

        private void onPitchValueUpdate(object sender, EventArgs e)
        {
            _pitchValue = ((AppState)sender).pitchValue;
            System.Diagnostics.Debug.WriteLine(_pitchValue);
        }

        private void onRecordStatusUpdate(object sender, EventArgs e)
        {
            // Defines a semitone
            var semitone = Math.Pow(2, 1.0 / 12);
            var semitoneCount = Int32.Parse("4");

            // Calculates semitones needed in either direction
            var upOneTone = 100.0f;
            var downOneTone = 1.0 / upOneTone;

            if (inputIndex != -1 && ((AppState)sender).recordStatus == "record")
            {
                sourceStream = new WaveIn();
                sourceStream.DeviceNumber = inputIndex;
                sourceStream.DataAvailable += InjectNormal;


                pitchStream = new WaveIn();
                pitchStream.DeviceNumber = inputIndex;
                pitchStream.DataAvailable += InjectPitch;

                sourceStream.WaveFormat = new NAudio.Wave.WaveFormat(44100, 1);
                pitchStream.WaveFormat = new NAudio.Wave.WaveFormat(44100, 1);

                bufferedWaveProviderNormal = new BufferedWaveProvider(sourceStream.WaveFormat);
                var normal = new SmbPitchShiftingSampleProvider(bufferedWaveProviderNormal.ToSampleProvider());

                bufferedWaveProviderPitch = new BufferedWaveProvider(pitchStream.WaveFormat);
                var pitch = new SmbPitchShiftingSampleProvider(bufferedWaveProviderPitch.ToSampleProvider());
                pitch.PitchFactor = 1.5f;

                waveOut = new NAudio.Wave.DirectSoundOut();

                if(_pitchStatus == "record" && _pitchValue != -1) {
                    pitchStream.StartRecording();
                }

                sourceStream.StartRecording();

                var mixer = new MixingSampleProvider(new[] { normal, pitch });
                waveOut.Init(mixer);

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
                if (pitchStream != null)
                {
                    pitchStream.StopRecording();
                    pitchStream.Dispose();
                    pitchStream = null;
                }
                if (bufferedWaveProviderNormal != null)
                {
                    bufferedWaveProviderNormal.ClearBuffer();
                }
                if (bufferedWaveProviderPitch != null)
                {
                    bufferedWaveProviderPitch.ClearBuffer();
                }
            }
        }

        private void InjectPitch(object sender, WaveInEventArgs e)
        {
            Console.WriteLine(bufferedWaveProviderPitch == null);
            if (bufferedWaveProviderPitch == null) return;
            bufferedWaveProviderPitch.AddSamples(e.Buffer, 0, e.BytesRecorded); //Add the mic audio to the buffer

            System.Diagnostics.Debug.WriteLine(bufferedWaveProviderPitch.BufferedBytes);
        }

        private void InjectNormal(object sender, WaveInEventArgs e)
        {
            Console.WriteLine(bufferedWaveProviderNormal == null);
            if (bufferedWaveProviderNormal == null) return;
            bufferedWaveProviderNormal.AddSamples(e.Buffer, 0, e.BytesRecorded); //Add the mic audio to the buffer

            System.Diagnostics.Debug.WriteLine(bufferedWaveProviderNormal.BufferedBytes);
        }
    }

    // Workaround for compiler error "error MC3050: Cannot find the type 'local:Main'"
    // It seems that, although WPF's design-time build can see Razor components, its runtime build cannot.
    public partial class Main { }
}
