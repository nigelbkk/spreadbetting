using Microsoft.Win32;
using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
    public partial class Settings : Window
    {
        Properties.Settings props = Properties.Settings.Default;
        private enum defaultActions
        {
            Cancel,
            KeepinPlay,
            ConverttoSP,
            KeepOffsetinPlay,
            KeepOffsetatSP
        }
        public Settings(Visual visual, Button b)
        {
            Point coords = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice.Transform(b.PointToScreen(new Point(b.ActualWidth, b.ActualHeight)));
            Top = coords.Y;
            Left = coords.X;
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.InitialDirectory = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.System)).FullName + "\\" + "Media";
            dlg.FileName = props.MatchedBetAlert;
            dlg.Filter = "WAV Files (*.wav)|*.wav|OGG Files (*.ogg)|*.ogg|MP3 Files (*.mp3)|*.mp3|MP4 Files (*.mp4)|*.mp4";
            if (dlg.ShowDialog() == true)
            {
                props.MatchedBetAlert = dlg.FileName;
            }
        }
        private void Button_Click3(object sender, RoutedEventArgs e)
        {
            SoundPlayer snd = new SoundPlayer(props.MatchedBetAlert);
            snd.Play();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            String[] cs = props.StakesPreselect.Split(',');

            if (cs.Length < 3)
			{
                var Result = MessageBox.Show("Would you like to fix it now?", "The Stakes Preselect should have 3 values", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (Result == MessageBoxResult.Yes)
				{
                    e.Cancel = true;
                    return;
				}
            }

            Properties.Settings.Default.Save();
        }
    }
}
