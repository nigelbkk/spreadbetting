using BetfairAPI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace SpreadTrader
{
	public partial class MarketDescription : Window
	{
		NodeViewModel node = null;
		public MarketDescription(Visual visual, Button b, NodeViewModel node)
		{
			this.node = node;
			InitializeComponent();

			Point coords = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice.Transform(b.PointToScreen(new Point(b.ActualWidth, b.ActualHeight)));
			Top = coords.Y;
			Left = coords.X;
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (node != null && node.Market != null && node.Market.description.rules != null)
				wb.NavigateToString(string.Format("<body style = \"font-family:Arial\" >{0}</body>", node.Market.description.rules));
			//if (node != null && node.Market != null && node.Market.description.rules != null)
			//	WebView.NavigateToString(string.Format("<body style = \"font-family:Arial\" >{0}</body>", node.Market.description.rules));
		}
	}
}
