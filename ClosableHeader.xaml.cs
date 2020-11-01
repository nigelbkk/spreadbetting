using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	class ClosableTab : TabItem
	{
		public NodeSelectionDelegate NodeChangeEventSink;
		public string Title { set { ((ClosableHeader)this.Header).Label.Content = value; } }
		public ClosableTab()
		{
			ClosableHeader OurHeader = new ClosableHeader();
			this.Header = OurHeader;
			NodeChangeEventSink += (node) =>
			{
				if (IsSelected)
				{
					OurHeader.Label.Content = node.MarketName.Trim();
				}
			};
		}
	}
	public partial class ClosableHeader : UserControl
	{
		public ClosableHeader()
		{
			InitializeComponent();
		}
		private void Image_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			ClosableTab parent = this.Parent as ClosableTab;
			TabControl tabcontrol = parent.Parent as TabControl;
			tabcontrol.Items.Remove(parent);
		}
		private void Label_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ClosableTab parent = this.Parent as ClosableTab;
			ClosableHeader header = parent.Header as ClosableHeader;
			parent.Width = Math.Max(e.NewSize.Width + header.Image.Width, 20);
		}
	}
}
