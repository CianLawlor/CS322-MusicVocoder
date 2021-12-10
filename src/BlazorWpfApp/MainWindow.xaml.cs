// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WebviewAppTest;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using NAudio.Midi;
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

        public MidiIn midiIn = null;
        public string currentNote = null;

        private string _pitchStatus;
        private string _recordStatus;
        private string _pitchValue = "";
        private string _pitchMethod;
        private string _normalRecord;
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
            _appState.pitchMethodChanged += new EventHandler(onPitchMethodUpdate);
            _appState.normalRecordChanged += new EventHandler(onNormalRecordUpdate);

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

        private void connectMIDIDevice()
        {
            midiIn = new MidiIn(0);
            //Console.WriteLine("Started Connecting");
            midiIn.MessageReceived += midiIn_MessageReceived;
            midiIn.ErrorReceived += midiIn_ErrorReceived;
            midiIn.Start();
            //Console.WriteLine("Successfully Connected");
        }

        void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            //Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
               // e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            int start = e.MidiEvent.ToString().IndexOf("Vel") - 4; // Gets Index of Location of MIDI Note
            currentNote = e.MidiEvent.ToString().Substring(start, 3).Trim();  // Extract MIDI Note (C3) from MIDI Event String
            //System.Diagnostics.Debug.WriteLine(currentNote);
        }

        private void GetAndSetMIDI()
        {

            List<MidiInCapabilities> midiInput = new List<MidiInCapabilities>();

            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                //Console.WriteLine(MidiIn.DeviceInfo(device).ProductName);
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
            //System.Diagnostics.Debug.WriteLine(((AppState)sender).recordStatus + " - " + ((AppState)sender).pitchStatus);
            _pitchStatus = ((AppState)sender).pitchStatus;
        }

        private void onPitchValueUpdate(object sender, EventArgs e)
        {
            _pitchValue = ((AppState)sender).pitchValue;
            //System.Diagnostics.Debug.WriteLine(_pitchValue);
        }

        private void onNormalRecordUpdate(object sender, EventArgs e)
        {
            _normalRecord = ((AppState)sender).normalRecord;
            //System.Diagnostics.Debug.WriteLine(_pitchValue);
        }

        private void onPitchMethodUpdate(object sender, EventArgs e)
        {
            _pitchMethod = ((AppState)sender).pitchMethod;

            if(((AppState)sender).pitchMethod == "MIDI")
            {
                connectMIDIDevice();
            } else
            {
                if(midiIn != null)
                {
                    midiIn = null;
                }
            }
            //System.Diagnostics.Debug.WriteLine(_pitchValue);
        }

        private void onRecordStatusUpdate(object sender, EventArgs e)
        {
            _recordStatus = ((AppState)sender).recordStatus;

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

                waveOut = new NAudio.Wave.DirectSoundOut();

                if (_pitchStatus == "record" && _pitchMethod != "none" && _pitchValue != "")
                {
                    pitchStream.StartRecording();
                }

                if (_pitchStatus == "record" && _normalRecord == "enabled")
                {
                    sourceStream.StartRecording();
                }
                

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

        String lastNote = "";
        private double calculateSemitoneDiff(double voiceFreq, string midiFreq)
        {
            // The Initialisation can be Extracted to Run Once at start of program
            string[] notes = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            List<int> noteNumbers = new List<int>();  // Holds all Notes Generated ([A1, A#1, B1 ... F8, F#8, G8])
            List<String> fullNotes = new List<String>();    // Holds Notes MIDI Number (https://newt.phys.unsw.edu.au/jw/graphics/notes.GIF)

            // Creates List of All Notes in Each Octave ([A1, A#1, B1 ... F8, F#8, G8]) (https://musicnotes101.files.wordpress.com/2010/04/piano.jpg)
            
            
            for (int i = 0; i < 8; i++)
            {
                if (i == 0)
                {
                    fullNotes.Add("A0");
                    fullNotes.Add("A#0");
                    fullNotes.Add("B0");
                }
                else
                {
                    for (int j = 0; j < 12; j++)
                    {
                        fullNotes.Add(notes[j] + i);
                    }
                }
            }
            int count = 21;

            // Calculate MIDI Note Values (48 = A4, 49 = A#4...)
            for (int i = 0; i < fullNotes.Count; i++)
            {
                noteNumbers.Add(count);
                count++;
            }
            List<double> midiFreqs = new List<double>() { 27.5, 29.14, 30.87, 32.7, 34.65, 36.71, 38.89, 41.2, 43.65, 46.25, 49, 51.91, 55, 58.27, 61.74, 65.41, 69.3, 73.42, 77.78, 82.41, 87.31, 92.5, 98, 103.83, 110, 116.54, 123.47, 130.81, 138.59, 146.83, 155.56, 164.81, 174.61, 185, 196, 207.65, 220, 233.08, 246.94, 261.63, 277.18, 293.66, 311.13, 329.63, 349.23, 369.99, 392, 415.3, 440, 466.16, 493.88, 523.25, 554.37, 587.33, 622.25, 659.25, 698.46, 739.99, 783.99, 830.61, 880, 932.33, 987.77, 1046.5, 1108.73, 1174.66, 1244.51, 1318.51, 1396.91, 1479.98, 1567.98, 1661.22, 1760, 1864.66, 1975.53, 2093, 2217.46, 2349.32, 2489.02, 2637.02, 2793.83, 2959.96, 3135.96, 3322.44, 3520, 3729.31, 3951.07, 4186.01, 4434.92, 4698.63, 4978.03, 5274.04, 5587.65, 5919.91, 6271.93, 6644.88, 7040, 7458.62, 7902.13};
            
            
            // Calculate MIDI Note Values (48 = A4, 49 = A#4...)

            int index = fullNotes.IndexOf(midiFreq);        // Takes MIDI Input (C3) and Gets It's Index
            int noteID = noteNumbers[index];                // Using Index, Finds MIDI Inputs Corresponding MIDI Value

            //System.Diagnostics.Debug.WriteLine(noteID);
            //(a / 32) * (2 ** ((note - 9) / 12));
            double midiFreqNew = Math.Pow(2, ((noteID - 8) / 12.0)) * (440/32);   // Calculates MIDI Note Frequency from Value

            int closestFreq = midiFreqs.IndexOf(midiFreqs.OrderBy((v) => Math.Abs(v - voiceFreq)).First());
            String newNote = "";
            try
            {
                newNote = fullNotes[closestFreq];
            } catch(Exception ex)
            {
                newNote = lastNote;
            }

            lastNote = newNote;
            //System.Diagnostics.Debug.WriteLine(newNote);
            // Calculates Difference Between Fundamental Freq. of our Voice and MIDI Note, Then Converts it to Semitone Difference
            //System.Diagnostics.Debug.WriteLine(Math.Round(Math.Abs(voiceFreq - midiFreqNew)));
            return (closestFreq - index);
        }

        float lastPitch = 1.0f;
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

            var floatBuffer = new List<double>();
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, index);
                double sample32 = (double)sample;
                sample32 /= (double)Int16.MaxValue;
                floatBuffer.Add(sample32);
            }

            if (floatBuffer.Count > 0)
            {
            int sampleRate = 48_000;

            // How Many Times the Audio Should Be Measured Each Second

            double[] window = FftSharp.Window.Hanning(floatBuffer.Count);
            double[] windowed = FftSharp.Window.Apply(window, floatBuffer.ToArray());
            double[] zeroPadded = FftSharp.Pad.ZeroPad(windowed);
            double[] psd = FftSharp.Transform.FFTpower(zeroPadded);

            double[] freq = FftSharp.Transform.FFTfreq(sampleRate, psd.Length);

            // Get the Fundamental Frequency by Finding Highest Power Index
            int maxIndex = psd.ToList().IndexOf(psd.Max());         // Finds the Frequency with the highest power
            double fundamentalFreq = freq[maxIndex];                // Sets 'fundamentalFreq' to that highest frequency

            // Get Difference Between Fundamental Pitch and MIDI Input in Semitones
            double semitoneDiff = calculateSemitoneDiff(fundamentalFreq, _pitchValue);
                System.Diagnostics.Debug.WriteLine(semitoneDiff);
            var diff = Math.Pow(semitoneDiff, 1.0 / 12);

            diff = (diff >= 0) ? diff * diff : 1 / diff;

            if(diff is double.NaN)
            {
                    diff = lastPitch;
            } else
            {
                    lastPitch = (float) diff;
            }
            //System.Diagnostics.Debug.WriteLine("Diff " + diff);
                pitch.PitchFactor = (float) diff;
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