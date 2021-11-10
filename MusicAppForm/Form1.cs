using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace MusicAppForm
{
    public partial class Form1 : Form
    {
        private BufferedWaveProvider bufferedWaveProvider;
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

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void sourceList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        //Pitch Shift Button
        private void button3_Click(object sender, EventArgs e)
        {
            waveOut = new NAudio.Wave.DirectSoundOut();
            // Defines a semitone
            var semitone = Math.Pow(2, 1.0 / 12);
            var semitoneCount = Int32.Parse("4");

            // Calculates semitones needed in either direction
            var upOneTone = 100.0f;
            var downOneTone = 1.0 / upOneTone;

            if (sourceStream == null)
            {
                if (sourceList.SelectedItems.Count == 0) return;

                int selectIndex = sourceList.SelectedItems[0].Index;

                sourceStream = new WaveIn();
                sourceStream.DeviceNumber = selectIndex;
                sourceStream.DataAvailable += Inject;
                sourceStream.WaveFormat = new WaveFormat(44100, 1);
            }

            Console.WriteLine("Running Pitch Shift");
            bufferedWaveProvider = new BufferedWaveProvider(sourceStream.WaveFormat);
            var pitch = new SmbPitchShiftingSampleProvider(bufferedWaveProvider.ToSampleProvider());
            pitch.PitchFactor = 1.5f;

            waveOut.Init(pitch);
            sourceStream.StartRecording();
            waveOut.Play();
        }

        private void Inject(object sender, WaveInEventArgs e)
        {
            Console.WriteLine(bufferedWaveProvider == null);
            if (bufferedWaveProvider == null) return;
            bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded); //Add the mic audio to the buffer
        }
    }
}
