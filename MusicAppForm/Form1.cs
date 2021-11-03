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

namespace MusicAppForm
{
    public partial class Form1 : Form
    {
        WaveIn sourceStream = null;
        DirectSoundOut waveOut = null;
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

            for( int i = 0; i < WaveIn.DeviceCount; i++)
            {
                sources.Add(WaveIn.GetCapabilities(i));
            }

            sourceList.Items.Clear();

            foreach(var source in sources)
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


        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            Console.WriteLine($"Event received from '{midiDevice.Name}' at {DateTime.Now}: {e.Event}");
        }

        private void OnEventSent(object sender, MidiEventSentEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            Console.WriteLine($"Event sent to '{midiDevice.Name}' at {DateTime.Now}: {e.Event}");
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void sourceList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var outputDevice = OutputDevice.GetByName("MIDI Device"))
            {
                outputDevice.EventSent += OnEventSent;

                using (var inputDevice = InputDevice.GetByName("MIDI Device"))
                {
                    inputDevice.EventReceived += OnEventReceived;
                    inputDevice.StartEventsListening();

                    outputDevice.SendEvent(new NoteOnEvent());
                    outputDevice.SendEvent(new NoteOffEvent());
                }
            }

        }
    }
}
