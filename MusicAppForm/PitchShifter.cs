using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using WaveIn = NAudio.Wave.WaveIn;

public class Class1
{
	public Class1()
	{
        InitializeComponent();
        Testing.Text = "2";
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

        // Gets Audio Path and Semitones from Input
        var inPath = @"C:\Users\Mark\Desktop\Testing.mp3";
        var semitoneCount = Int32.Parse(Testing.Text);

        // Defines a semitone and the input source
        var semitone = Math.Pow(2, 1.0 / 12);
        WaveIn mic = new WaveIn();

        // Calculates semitones needed in either direction
        var upOneTone = Math.Pow(semitone, semitoneCount);
        var downOneTone = 1.0 / upOneTone;

        using (var reader = new MediaFoundationReader(inPath))
        {
            // Takes our file in as a Sample Provider for pitching
            var pitch = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider());
            using (var device = new WaveOutEvent())
            {
                // Increase the Sample by our input pitch
                pitch.PitchFactor = (float)upOneTone;

                // Plays first 10 seconds from file (can be changed)
                device.Init(pitch.Take(TimeSpan.FromSeconds(10)));
                device.Play();
                while (device.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(500);
                }
            }
        }
    }
}
