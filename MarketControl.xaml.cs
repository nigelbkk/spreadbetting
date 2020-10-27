using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpreadTrader
{
	public partial class MarketControl : UserControl, INotifyPropertyChanged
	{
		public NodeSelectionDelegate NodeChangeEventSink = null;
		public NodeViewModel MarketNode { get; set; }
		private Properties.Settings props = Properties.Settings.Default;
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				Dispatcher.BeginInvoke(new Action(() => { PropertyChanged(this, new PropertyChangedEventArgs(info)); }));
			}
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			MarketNode = new NodeViewModel(new BetfairAPI.BetfairAPI());
		}
		public MarketControl()
		{
			InitializeComponent();
			NodeChangeEventSink += (node) =>
			{
				MarketNode = node;
				NotifyPropertyChanged("");
			};

			if (props.RowHeight1 > 0)
				RightGrid.RowDefinitions[0].Height = new GridLength(props.RowHeight1, GridUnitType.Pixel);

			if (props.RowHeight2 > 0)
				RightGrid.RowDefinitions[1].Height = new GridLength(props.RowHeight2, GridUnitType.Pixel);

			SV1.Height = Convert.ToDouble(RightGrid.RowDefinitions[0].Height.Value) - SV1_Header.Height - 20;
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button b = sender as Button;
			try
			{
				switch (b.Tag)
				{
					case "Market Description": new MarketDescription(this, b, MarketNode).ShowDialog(); break;
				}
			}
			catch (Exception xe)
			{
				Debug.WriteLine(xe.Message);
			}
		}
		private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			SV1.Height = Convert.ToDouble(RightGrid.RowDefinitions[0].Height.Value) - SV1_Header.Height - 20;
			props.RowHeight1 = Convert.ToInt32(RightGrid.RowDefinitions[0].Height.Value);
			props.RowHeight2 = Convert.ToInt32(RightGrid.RowDefinitions[1].Height.Value);
			props.Save();
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
		//private void PopulateBetsGrid()
		//{
		//	AllBets = new ObservableCollection<Bet>();
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//	AllBets.Add(new Bet("06/10/2017,Hexham,17:20,1.135047544,1545454,Final Fling,WIN,LAY,BF,40,1.7,LIMIT_ON_CLOSE,LAPSE") { });
		//}
	}
}
