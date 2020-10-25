using System;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
	class ClosableTab : TabItem
	{
		public string Title { set { ((CloseableHeader)this.Header).Label.Content = value; } }
		public ClosableTab()
		{
			CloseableHeader OurHeader = new CloseableHeader();
			this.Header = OurHeader;
			OurHeader.MouseEnter += (o, ea) => { OurHeader.Button.Background = OurHeader.Label.Background; };
			OurHeader.MouseLeave += (o, ea) => { OurHeader.Button.Background = OurHeader.Label.Background; };
			OurHeader.Button.Click += (o, ea) => { ((TabControl)this.Parent).Items.Remove(this); };
			OurHeader.Label.SizeChanged += (o, ea) => { OurHeader.Button.Margin = new Thickness(((CloseableHeader)this.Header).Label.ActualWidth + 5, 3, 4, 0); };
			OurHeader.Button.Background = OurHeader.Label.Background;
		}
	}
	public partial class CloseableHeader : UserControl
	{
		public CloseableHeader()
		{
			InitializeComponent();
		}
	}
}
