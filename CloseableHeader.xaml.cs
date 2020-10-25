using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpreadTrader
{
	class ClosableTab : TabItem
	{
		//CloseableHeader OurHeader { get; set; }
		public string Title { set { ((CloseableHeader)this.Header).TabTitle.Content = value; } }
		public ClosableTab()
		{
			CloseableHeader OurHeader = new CloseableHeader();
			this.Header = OurHeader;
			OurHeader.button_close.MouseEnter += new MouseEventHandler(button_close_MouseEnter);
			OurHeader.button_close.MouseLeave += new MouseEventHandler(button_close_MouseLeave);
			OurHeader.button_close.Click += new RoutedEventHandler(button_close_Click);
			OurHeader.TabTitle.SizeChanged += new SizeChangedEventHandler(TabTitle_SizeChanged);
		}
		void button_close_MouseEnter(object sender, MouseEventArgs e)
		{
			((CloseableHeader)this.Header).button_close.Foreground = Brushes.Red;
		}
		void button_close_MouseLeave(object sender, MouseEventArgs e)
		{
			((CloseableHeader)this.Header).button_close.Foreground = Brushes.Black;
		}
		void button_close_Click(object sender, RoutedEventArgs e)
		{
			((TabControl)this.Parent).Items.Remove(this);
		}
		void TabTitle_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			((CloseableHeader)this.Header).button_close.Margin = new Thickness(((CloseableHeader)this.Header).TabTitle.ActualWidth + 5, 3, 4, 0);
		}
		protected override void OnSelected(RoutedEventArgs e)
		{
			base.OnSelected(e);
			((CloseableHeader)this.Header).button_close.Visibility = Visibility.Visible;
		}
		protected override void OnUnselected(RoutedEventArgs e)
		{
			base.OnUnselected(e);
			((CloseableHeader)this.Header).button_close.Visibility = Visibility.Hidden;
		}
		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			((CloseableHeader)this.Header).button_close.Visibility = Visibility.Visible;
			TabTitle_SizeChanged(null, null);
		}
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			if (!this.IsSelected)
			{
				((CloseableHeader)this.Header).button_close.Visibility = Visibility.Hidden;
			}
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
