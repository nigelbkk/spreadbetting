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
				NodeViewModel MarketNode = node;
				OurHeader.Label.Content = node.MarketName;
				OurHeader.Width = OurHeader.Label.ActualWidth + 40;
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
	}
}
