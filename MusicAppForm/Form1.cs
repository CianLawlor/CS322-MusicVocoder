using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using NAudio.Midi;

namespace MusicAppForm
{
    public partial class Form1 : Form
    {
        WaveIn sourceStream = null;
        public MidiIn midiIn = null;
        DirectSoundOut waveOut = null;
        public string currentNote = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<WaveInCapabilities> sources = new List<WaveInCapabilities>();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                sources.Add(WaveIn.GetCapabilities(i));
            }

            sourceList.Items.Clear();

            foreach (var source in sources)
            {
                ListViewItem item = new ListViewItem(source.ProductName);
                item.SubItems.Add(new ListViewItem.ListViewSubItem(item, source.Channels.ToString()));
                sourceList.Items.Add(item);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (sourceList.SelectedItems.Count == 0) return;

            int selectIndex = sourceList.SelectedItems[0].Index;

            sourceStream = new WaveIn();
            sourceStream.DeviceNumber = selectIndex;
            sourceStream.WaveFormat = new NAudio.Wave.WaveFormat(44100, NAudio.Wave.WaveIn.GetCapabilities(selectIndex).Channels);

            WaveInProvider waveIn = new WaveInProvider(sourceStream);
            waveOut = new NAudio.Wave.DirectSoundOut();
            waveOut.Init(waveIn);

            sourceStream.StartRecording();
            waveOut.Play();
        }

        private void button4_Click(object sender, EventArgs e)
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

        private void button5_Click(object sender, EventArgs e)
        {
            button4_Click(sender, e);
            this.Close();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void sourceList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            List<MidiInCapabilities> midiInput = new List<MidiInCapabilities>();

            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                Console.WriteLine(MidiIn.DeviceInfo(device).ProductName);
                midiInput.Add(MidiIn.DeviceInfo(device));
            }

            midiList.Items.Clear();

            foreach (var device in midiInput)
            {
                ListViewItem item = new ListViewItem(device.ProductName);
                midiList.Items.Add(item);
            }
        }


        private void listView1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            midiIn = new MidiIn(0);
            Console.WriteLine("Started Connecting");
            midiIn.MessageReceived += midiIn_MessageReceived;
            midiIn.ErrorReceived += midiIn_ErrorReceived;
            midiIn.Start();
            Console.WriteLine("Successfully Connected");
        }

        void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}",
                e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            Console.WriteLine(String.Format("Time {0} Message 0x{1:X8} Event {2}", e.Timestamp, e.RawMessage, e.MidiEvent));
            int start = e.MidiEvent.indexOf("Vel") - 3; // Gets Index of Location of MIDI Note
            this.currentNote = e.MidiEvent.Substring(start, start + 3).Trim();  // Extract MIDI Note (C3) from MIDI Event String
        }

        private void button6_Click(object sender, EventArgs e)
        {
            
            double[] signal = FftSharp.SampleData.SampleAudio1();   // This Is Just A Sample Signal, Can Be Replaced By the Audio Buffer
            int sampleRate = 48_000;                                // How Many Times the Audio Should Be Measured Each Second

            double[] psd = FftSharp.Transform.FFTpower(signal);     // Gets the Power of each frequency from Signal (https://raw.githubusercontent.com/swharden/FftSharp/master/dev/quickstart/periodogram.png)
            double[] freq = FftSharp.Transform.FFTfreq(sampleRate, psd.Length);

            // Get the Fundamental Frequency by Finding Highest Power Index
            int maxIndex = psd.ToList().IndexOf(psd.Max());         // Finds the Frequency with the highest power
            double fundamentalFreq = freq[maxIndex];                // Sets 'fundamentalFreq' to that highest frequency
            Console.WriteLine("Max Freq Is " + fundamentalFreq);    // Logs our Fundamental Frequency

            // Get Difference Between Fundamental Pitch and MIDI Input in Semitones
            double semitoneDiff = calculateSemitoneDiff(fundamentalFreq, this.currentNote);
        }

        private double calculateSemitoneDiff(double voiceFreq, string midiFreq)
        {
            // The Initialisation can be Extracted to Run Once at start of program
            string notes = ["A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#"];

            List<String> noteNumbers = new List<String>();  // Holds all Notes Generated ([A1, A#1, B1 ... F8, F#8, G8])
            List<String> fullNotes = new List<String>();    // Holds Notes MIDI Number (https://newt.phys.unsw.edu.au/jw/graphics/notes.GIF)

            // Creates List of All Notes in Each Octave ([A1, A#1, B1 ... F8, F#8, G8]) (https://musicnotes101.files.wordpress.com/2010/04/piano.jpg)
            for (int i = 0; i < notes.Length; i++)
            {
                for (int j = 0; i < 8; i++)
                {
                    fullNotes.Add(notes[j].ToString() + i);
                }
            }

            // Calculate MIDI Note Values (48 = A4, 49 = A#4...)
            for (int i = 0; i < fullNotes.Length; i++)
            {
                noteNumbers.Add(i);
            }

            int index = fullNotes.IndexOf(midiFreq);        // Takes MIDI Input (C3) and Gets It's Index
            int noteID = noteNumbers[index];                // Using Index, Finds MIDI Inputs Corresponding MIDI Value
            double midiFreqNew = Math.Pow(2, (noteID - 49) / 12.0) * 440;   // Calculates MIDI Note Frequency from Value

            // Calculates Difference Between Fundamental Freq. of our Voice and MIDI Note, Then Converts it to Semitone Difference
            return Math.Round(Math.Abs(voiceFreq - midiFreqNew) * Math.Pow(2, 1.0 / 12));
        }
    }
}
