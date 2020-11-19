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
		public NodeSelectionDelegate NodeChangeEventSink = null;
		private BackgroundWorker Worker = null;
		public NodeViewModel _MarketNode { get; set; }
		public NodeViewModel MarketNode { get { return _MarketNode;  } set { _MarketNode = value; NotifyPropertyChanged(""); } }
		public bool IsSelected { get; set; }
		public double BackBook { get { return _MarketNode.Market.MarketBook == null ? 0.00 : _MarketNode.Market.MarketBook.BackBook; } }
		public double LayBook { get { return _MarketNode.Market.MarketBook == null ? 0.00 : _MarketNode.Market.MarketBook.LayBook; } }
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
			MarketNode = new NodeViewModel(new BetfairAPI.BetfairAPI());
			LiveRunners = new List<LiveRunner>();
			InitializeComponent();

			NodeChangeEventSink += (node) =>
			{
				if (IsLoaded)
				{
					MarketNode = node;
					LiveRunners = MarketNode.GetLiveRunners();
					Debug.WriteLine("RunnersControl");
				}
			};

			Worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
			Worker.ProgressChanged += (o, e) =>
			{
				List<LiveRunner> NewRunners = e.UserState as List<LiveRunner>;
				// get last price traded and ifWin
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
					//break;
					System.Threading.Thread.Sleep(props.WaitBF);
				}
			};
			Worker.RunWorkerAsync();
		}
		public String GetRunnerName(Int64 SelectionID)
		{
			foreach(LiveRunner r in LiveRunners)
			{
				if (r.SelectionId == SelectionID)
					return r.Name;
			}
			return null;
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				Int32 idx = Convert.ToInt32(b.Tag);
				var parent = VisualTreeHelper.GetParent(b);
				var parent2 = VisualTreeHelper.GetParent(parent);
				var parent3 = VisualTreeHelper.GetParent(parent2);
				var parent4 = VisualTreeHelper.GetParent(parent3);
				var parent5 = VisualTreeHelper.GetParent(parent4);
				ContentPresenter cp = parent5 as ContentPresenter;
				LiveRunner live_runner = cp.Content as LiveRunner;

				var vb = VisualTreeHelper.GetChild(parent, 8);
				TextBox tb = VisualTreeHelper.GetChild(parent, 8) as TextBox;
				TextBox tl = VisualTreeHelper.GetChild(parent, 9) as TextBox;
				Int32 bs = Convert.ToInt32(tb.Text);
				Int32 ls = Convert.ToInt32(tl.Text);

				var grid = VisualTreeHelper.GetChild(b, 0);
				grid = VisualTreeHelper.GetChild(grid, 0);
				var sp = VisualTreeHelper.GetChild(grid, 0);
				var t1 = VisualTreeHelper.GetChild(sp, 0) as TextBlock;

				double odds = Convert.ToDouble(t1.Text);

				ConfirmationDialog dlg = new ConfirmationDialog(this, b, live_runner, idx >= 10 ? "Lay" : "Back", odds, idx >= 10 ? ls : bs);
				dlg.ShowDialog();
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SV1.Height = Math.Max(25, e.NewSize.Height - Header.Height);
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