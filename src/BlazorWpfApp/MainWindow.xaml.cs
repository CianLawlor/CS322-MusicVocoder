// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WebviewAppTest;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using NAudio.Midi;
using NAudio.CoreAudioApi;
using Syncfusion.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppState _appState = new();
        private int inputIndex = -1;

        private string _pitchStatus;
        private string _recordStatus;
        private int _pitchValue = 0;
        private float pitchFactor = 1.0f;
        private SmbPitchShiftingSampleProvider pitch;
        private BufferedWaveProvider bufferedWaveProviderPitch;
        private BufferedWaveProvider bufferedWaveProviderNormal;
        MixingSampleProvider mixer = null;
        WaveIn sourceStream = null;
        WaveIn pitchStream = null;
        DirectSoundOut waveOut = null;


        public MainWindow()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            serviceCollection.AddSingleton<AppState>(_appState);
            serviceCollection.AddSyncfusionBlazor();

            _appState.init();

            _appState.inputChanged += new EventHandler(setInputIndex);
            _appState.recordStatusChanged += new EventHandler(onRecordStatusUpdate);
            _appState.pitchStatusChanged += new EventHandler(onPitchStatusUpdate);
            _appState.pitchValueChanged += new EventHandler(onPitchValueUpdate);


            GetAndSetInputs();
            GetAndSetMIDI();

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

            string[] output = new string[] { };
            foreach (var source in sources)
            {
                Array.Resize(ref output, output.Length + 1);
                output[output.GetUpperBound(0)] = source.ProductName;
                Array.Resize(ref output, output.Length + 1);
                output[output.GetUpperBound(0)] = source.Channels.ToString();
            }

            _appState.setInputDevices(output);
        }

        private void GetAndSetMIDI()
        {

            List<MidiInCapabilities> midiInput = new List<MidiInCapabilities>();

            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                Console.WriteLine(MidiIn.DeviceInfo(device).ProductName);
                midiInput.Add(MidiIn.DeviceInfo(device));
            }

            string[] midiList = new string[midiInput.Count];

            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                midiList[i] = midiInput[i].ProductName;
            }

            _appState.setMIDIDevices(midiList);
        }

        private void setInputIndex(object sender, EventArgs e)
        {
            inputIndex = ((AppState)sender).selectedInputDeviceIndex;
        }

        private void onPitchStatusUpdate(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(((AppState)sender).recordStatus + " - " + ((AppState)sender).pitchStatus);
            _pitchStatus = ((AppState)sender).pitchStatus;
        }

        private void onPitchValueUpdate(object sender, EventArgs e)
        {
            _pitchValue = ((AppState)sender).pitchValue;
            System.Diagnostics.Debug.WriteLine(_pitchValue);
        }

        private void onRecordStatusUpdate(object sender, EventArgs e)
        {
            _recordStatus = ((AppState)sender).recordStatus;
            // Defines a semitone
            var semitone = Math.Pow(2, 1.0 / 12);
            var semitoneCount = Int32.Parse("4");

            // Calculates semitones needed in either direction
            var upOneTone = 100.0f;
            var downOneTone = 1.0 / upOneTone;

            if (inputIndex != -1 && _recordStatus == "record")
            {
                sourceStream = new WaveIn();
                sourceStream.DeviceNumber = inputIndex;
                sourceStream.DataAvailable += InjectNormal;


                pitchStream = new WaveIn();
                pitchStream.DeviceNumber = inputIndex;
                pitchStream.DataAvailable += InjectPitch;

                sourceStream.WaveFormat = new NAudio.Wave.WaveFormat(rate: 48000, bits: 16, channels: 1);
                pitchStream.WaveFormat = new NAudio.Wave.WaveFormat(rate: 48000, bits: 16, channels: 1);

                bufferedWaveProviderNormal = new BufferedWaveProvider(sourceStream.WaveFormat);
                var normal = new SmbPitchShiftingSampleProvider(bufferedWaveProviderNormal.ToSampleProvider());

                bufferedWaveProviderPitch = new BufferedWaveProvider(pitchStream.WaveFormat);
                pitch = new SmbPitchShiftingSampleProvider(bufferedWaveProviderPitch.ToSampleProvider());
                pitch.PitchFactor = pitchFactor;

                waveOut = new NAudio.Wave.DirectSoundOut();

                if (_pitchStatus == "record" && _pitchValue != -1)
                {
                    pitchStream.StartRecording();
                }

                sourceStream.StartRecording();

                mixer = new MixingSampleProvider(new[] { normal, pitch });
                waveOut.Init(mixer);

                waveOut.Play();
            }
            else if (inputIndex != -1)
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
                if (mixer != null)
                {
                    mixer.RemoveAllMixerInputs();
                    mixer = null;
                }
                if (bufferedWaveProviderNormal != null)
                {
                    bufferedWaveProviderNormal.ClearBuffer();
                    bufferedWaveProviderNormal = null;
                }
                if (bufferedWaveProviderPitch != null)
                {
                    bufferedWaveProviderPitch.ClearBuffer();
                    bufferedWaveProviderPitch = null;
                }
            }
        }

        private double calculateSemitoneDiff(double voiceFreq, string midiFreq)
        {
            // The Initialisation can be Extracted to Run Once at start of program
            string[] notes = new string[] { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };

            List<int> noteNumbers = new List<int>();  // Holds all Notes Generated ([A1, A#1, B1 ... F8, F#8, G8])
            List<String> fullNotes = new List<String>();    // Holds Notes MIDI Number (https://newt.phys.unsw.edu.au/jw/graphics/notes.GIF)

            // Creates List of All Notes in Each Octave ([A1, A#1, B1 ... F8, F#8, G8]) (https://musicnotes101.files.wordpress.com/2010/04/piano.jpg)
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    fullNotes.Add(notes[j] + i);
                }
            }
            int count = 21;

            // Calculate MIDI Note Values (48 = A4, 49 = A#4...)
            for (int i = 0; i < fullNotes.Count; i++)
            {
                noteNumbers.Add(count);
                count++;
            }


            int index = fullNotes.IndexOf(midiFreq);        // Takes MIDI Input (C3) and Gets It's Index
            //System.Diagnostics.Debug.WriteLine(index);
            int noteID = noteNumbers[index];                // Using Index, Finds MIDI Inputs Corresponding MIDI Value

            double midiFreqNew = Math.Pow(2, (noteID - 49) / 12.0) * 440;   // Calculates MIDI Note Frequency from Value

            // Calculates Difference Between Fundamental Freq. of our Voice and MIDI Note, Then Converts it to Semitone Difference
            //System.Diagnostics.Debug.WriteLine(Math.Round(Math.Abs(voiceFreq - midiFreqNew) * Math.Pow(2, 1.0 / 12)));
            return Math.Round(Math.Abs(voiceFreq - midiFreqNew) * Math.Pow(2, 1.0 / 12));
        }

        double[] lastBuffer;

        private void InjectPitch(object sender, WaveInEventArgs e)
        {
            if (_recordStatus == "stop")
            {
                bufferedWaveProviderNormal = null;
                bufferedWaveProviderPitch = null;
            }
            if (bufferedWaveProviderPitch == null) return;
            if (sourceStream == null) return;
            //Check for pitch modify type
            //MIDI or Slider (Only one)
            //if (true)
            //{

            //}
            bufferedWaveProviderPitch.AddSamples(e.Buffer, 0, e.BytesRecorded); //Add the mic audio to the buffer

            int bytesPerSample = sourceStream.WaveFormat.BitsPerSample / 8;
            int SamplesRecorded = e.BytesRecorded / bytesPerSample;
            if (lastBuffer is null || lastBuffer.Length != SamplesRecorded)
                lastBuffer = new double[SamplesRecorded];
            for (int i = 0; i < SamplesRecorded; i++)
                lastBuffer[i] = BitConverter.ToInt16(e.Buffer, i * bytesPerSample);

            if (lastBuffer.Length > 0)
            {
                int sampleRate = 41_000;

                // How Many Times the Audio Should Be Measured Each Second

                double[] window = FftSharp.Window.Hanning(lastBuffer.Length);
                double[] windowed = FftSharp.Window.Apply(window, lastBuffer);
                double[] zeroPadded = FftSharp.Pad.ZeroPad(windowed);
                double[] psd = FftSharp.Transform.FFTpower(zeroPadded);

                double[] freq = FftSharp.Transform.FFTfreq(sampleRate, psd.Length);

                // Get the Fundamental Frequency by Finding Highest Power Index
                int maxIndex = psd.ToList().IndexOf(psd.Max());         // Finds the Frequency with the highest power
                double fundamentalFreq = freq[maxIndex];                // Sets 'fundamentalFreq' to that highest frequency
                System.Diagnostics.Debug.WriteLine("Max Freq Is " + fundamentalFreq);    // Logs our Fundamental Frequency

                // Get Difference Between Fundamental Pitch and MIDI Input in Semitones
                float semitoneDiff = (float)(fundamentalFreq - calculateSemitoneDiff(fundamentalFreq, "C#1"));
                System.Diagnostics.Debug.WriteLine(semitoneDiff);
                //pitch.PitchFactor = (float) fundamentalFreq - (float)semitoneDiff;
            }
        }

        private void InjectNormal(object sender, WaveInEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(_recordStatus);
            if (bufferedWaveProviderNormal == null) return;
            if (_recordStatus == "stop")
            {
                bufferedWaveProviderNormal = null;
                bufferedWaveProviderPitch = null;
                return;
            }
            bufferedWaveProviderNormal.AddSamples(e.Buffer, 0, e.BytesRecorded); //Add the mic audio to the buffer

            //System.Diagnostics.Debug.WriteLine(bufferedWaveProviderNormal.BufferedBytes);
        }
    }

    // Workaround for compiler error "error MC3050: Cannot find the type 'local:Main'"
    // It seems that, although WPF's design-time build can see Razor components, its runtime build cannot.
    public partial class Main { }
}