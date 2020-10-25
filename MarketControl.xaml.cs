using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
	public partial class MarketControl : UserControl, INotifyPropertyChanged
	{
		public NodeViewModel RootNode { get; set; }
		public NodeViewModel SelectedNode { get; set; }
		public ObservableCollection<Bet> AllBets { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
		public MarketControl()
		{
			InitializeComponent();
		}
		private void OnNotification(String cs)
		{
			Dispatcher.BeginInvoke(new Action(() => { /*Status = cs;*/ NotifyPropertyChanged(""); }));
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Market Description":
						new MarketDescription(this, b, SelectedNode).ShowDialog(); break;
					case "Settings":
						new Settings().ShowDialog(); break;
					case "Refresh":
						RootNode = new NodeViewModel(new BetfairAPI.BetfairAPI(), OnNotification, OnSelectionChanged); break;
					case "Favourites":
						new Favourites(this, b, NodeViewModel.Betfair.GetEventTypes().OrderBy(o => o.eventType.name).ToList()); break;
				}
			}
			catch (Exception xe)
			{
//				Status = xe.Message.ToString();
			}
		}
		private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			SV1.Height = Convert.ToDouble(RightGrid.RowDefinitions[0].Height.Value) - SV1_Header.Height - 20;
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
		private void OnSelectionChanged(NodeViewModel node)
		{
			SelectedNode = node;
			Dispatcher.BeginInvoke(new Action(() => { NotifyPropertyChanged(""); }));
		}
		private void PopulateBetsGrid()
		{
			AllBets = new ObservableCollection<Bet>();
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
			AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			//RootNode = new NodeViewModel(new BetfairAPI.BetfairAPI(), OnNotification, OnSelectionChanged);
		}
	}
}
