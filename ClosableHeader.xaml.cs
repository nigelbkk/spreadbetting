using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
	class ClosableTab : TabItem
	{
		public MarketSelectionDelegate OnMarketSelected;
		public StreamUpdateDelegate StreamUpdateEventSink = null;
		public string Title { set { ((ClosableHeader)this.Header).Label.Content = value; } }
		public ClosableTab()
		{
			ClosableHeader OurHeader = new ClosableHeader();
			this.Header = OurHeader;
			OnMarketSelected += (node) =>
			{
				if (IsSelected)
				{
					OurHeader.Label.Content = node.MarketName.Trim();
				}
			};
			StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, inplay) =>
			{
				if (marketid != "")			//TODO
				{
					this.Dispatcher.Invoke(() =>
					{
						OurHeader.Label.Foreground = inplay ? Brushes.LightGreen : Brushes.DarkGray;
					});
				}
			};
			Content = new TabContentControl();
		}
	}
	public partial class ClosableHeader : UserControl
	{
		public ClosableHeader()
		{
			InitializeComponent();
		}
		//private void CloseTab_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		//{
		//	ClosableTab parent = this.Parent as ClosableTab;
		//	TabControl tabcontrol = parent.Parent as TabControl;
		//	tabcontrol.Items.Remove(parent);
		//}
		private void Label_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ClosableTab parent = this.Parent as ClosableTab;
			ClosableHeader header = parent.Header as ClosableHeader;
			parent.Width = Math.Max(e.NewSize.Width + header.CloseButton.Width, 20);
		}
		private void Image_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Image img = sender as Image;
			ClosableTab tab = this.Parent as ClosableTab;
			TabControl tc = tab.Parent as TabControl;
			tc.Items.Remove(tab);
		}
	}
}
