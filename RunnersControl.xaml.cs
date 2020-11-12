using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpreadTrader
{
	public partial class RunnersControl : UserControl, INotifyPropertyChanged
	{
		private BackgroundWorker Worker = null;
		public NodeViewModel MarketNode { get; set; }
		public	bool IsSelected { get; set; }
		public List<LiveRunner> LiveRunners { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public RunnersControl()
		{
			InitializeComponent();
			MarketNode = new NodeViewModel(new BetfairAPI.BetfairAPI());
			Worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
			Worker.ProgressChanged += (o, e) =>
			{
				List<LiveRunner> NewRunners = e.UserState as List<LiveRunner>;
				if (LiveRunners != null)
				{
					for (int i = 0; i < NewRunners.Count; i++)
					{
						//LiveRunner rold = LiveRunners[i];
						//LiveRunner rnew = NewRunners[i];
						//rnew.LastPrice = rnew.prices[3].price;
						//if (rold.prices[0].price != rnew.prices[0].price)
						//{
						//	rnew.LastPrice = rnew.prices[0].price;
						//}
					}
				}
				LiveRunners = NewRunners;
				MarketNode.UpdateRate = e.ProgressPercentage;
				NotifyPropertyChanged("");
			};
			Worker.DoWork += (o, ea) =>
			{
				BackgroundWorker sender = o as BackgroundWorker;
				while (!sender.CancellationPending)
				{
					try
					{
						if (MarketNode != null && MarketNode.MarketName != null && IsSelected)
						{
							//Debug.WriteLine(MarketNode.MarketName);
							DateTime LastUpdate = DateTime.UtcNow;
							var lr = MarketNode.GetLiveRunners();
							Int32 rate = (Int32) ((DateTime.UtcNow - LastUpdate).TotalMilliseconds);
							sender.ReportProgress(rate, lr);
						}
					}
					catch (Exception xe)
					{
						Debug.WriteLine(xe.Message);
					}
					System.Threading.Thread.Sleep(props.WaitBF);
				}
			};
			Worker.RunWorkerAsync();
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
				}
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			TextBox textbox = sender as TextBox;
			if (e.Key == Key.Return || e.Key == Key.Escape)
			{
				Grid grid = textbox.Parent as Grid;
				Label label = grid.Children[0] as Label;
				label.Visibility = Visibility.Visible;
				textbox.Visibility = Visibility.Hidden;
			}
		}
		private void label_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Label label = sender as Label;
			Grid grid = label.Parent as Grid;
			TextBox textbox = grid.Children[1] as TextBox;
			Application.Current.Dispatcher.Invoke(new Action(() =>
			{
				textbox.Focus();
				Keyboard.Focus(textbox);
			}));
			label.Visibility = Visibility.Hidden;
			textbox.Visibility = Visibility.Visible;
			NotifyPropertyChanged("");
		}
		private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			TextBox textbox = sender as TextBox;
			Grid grid = textbox.Parent as Grid;
			Label label = grid.Children[0] as Label;
			label.Visibility = Visibility.Visible;
			textbox.Visibility = Visibility.Hidden;
			NotifyPropertyChanged("");
		}
		private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SV1.Height = Math.Max(25, e.NewSize.Height - SV1_Header.Height);
		}
	}
	public static class Extensions
	{
		public static T FindParentOfType<T>(this DependencyObject child) where T : DependencyObject
		{
			DependencyObject parentDepObj = child;
			do
			{
				parentDepObj = VisualTreeHelper.GetParent(parentDepObj);
				T parent = parentDepObj as T;
				if (parent != null) return parent;
			}
			while (parentDepObj != null);
			return null;
		}
	}
}