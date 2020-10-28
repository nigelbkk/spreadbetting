using System.Windows.Controls;

namespace SpreadTrader
{
	class ClosableTab : TabItem
	{
		public string Title { set { ((ClosableHeader)this.Header).Label.Content = value; } }
		public ClosableTab()
		{
			ClosableHeader OurHeader = new ClosableHeader();
			this.Header = OurHeader;
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
			((TabControl) parent.Parent).Items.Remove(parent);
		}
	}
}
