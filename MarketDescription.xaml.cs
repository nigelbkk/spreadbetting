using BetfairAPI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
	public partial class MarketDescription : Window
	{
		public MarketDescription(Visual visual, Button b, NodeViewModel node)
		{
			InitializeComponent();
			if (node != null)
			{
				Market market = node.Tag as Market;
				if (market != null && market.description.rules != null)
					WebView.NavigateToString(string.Format("<body style = \"font-family:Arial\" >{0}</body>", market.description.rules));
				Point coords = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice.Transform(b.PointToScreen(new Point(b.ActualWidth, b.ActualHeight)));
				Top = coords.Y;
				Left = coords.X;
			}
		}
	}
}
