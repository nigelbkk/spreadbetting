using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpreadTrader
{
    public partial class SliderControl : UserControl, INotifyPropertyChanged
    {
        private System.Timers.Timer timer = null;
        private BetfairPrices betfairPrices = new BetfairPrices();
        private Int32 base_index
        {
            get
            {
                Int32 b = betfairPrices.Index(BasePrice);
                b = Math.Max(b, 10);
                return Math.Min(b, 338);
            }
        }
        public LiveRunner Favorite { get; set; }
        public bool FavoriteSelected { get { return Favorite != null; } }
        public double CutStakes { get; set; }
        public double MoveBack { get; set; }
        public double MoveLay { get; set; }
        public PriceSize[] BackValues { get; set; }
        public PriceSize[] LayValues { get; set; }
        public static bool AutoBackLay { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        private void OnMessageReceived(string messageName, object data)
        {
            if (messageName == "Favorite Changed")
            {
                dynamic d = data;
                Debug.WriteLine($"BetsManager: {messageName} : {d.Name}");
                Favorite = d.Favorite;
                BasePrice = Favorite.LastPriceTraded;
                SyncPrices();
                NotifyPropertyChanged("");
            }
        }
        public SliderControl()
        {
            CutStakes = 5;
            MoveBack = 10;
            MoveLay = 22;
            BackValues = new PriceSize[9];
            LayValues = new PriceSize[9];
            for (Int32 i = 0; i < 9; i++)
            {
                BackValues[i] = new PriceSize(betfairPrices[i], 20 + 1 * 10);
                LayValues[i] = new PriceSize(betfairPrices[i], 20 + 1 * 10);
            }
            BasePrice = props.BasePrice;
            InitializeComponent();
            ControlMessenger.MessageSent += OnMessageReceived;
        }
        private double _BasePrice;
        public double BasePrice { get { return _BasePrice; } set { _BasePrice = value; } }// = Math.Max(value, 1.01); } }
        public void MoveStakes(Int32 newvalue, Int32 oldvalue)
        {
            for (Int32 i = 0; i < 9; i++)
            {
                PriceSize vb = BackValues[i];
                PriceSize vl = LayValues[i];
                if (newvalue > oldvalue)
                {
                    vb.size = 10 * newvalue;
                    vl.size = 10 * newvalue;
                    BackValues[i] = vb;
                    LayValues[i] = vl;
                }
                else if (oldvalue > newvalue)
                {
                    vb.size = 10 * newvalue;
                    vl.size = 10 * newvalue;
                    BackValues[i] = vb;
                    LayValues[i] = vl;
                }
            }
        }
        public void SyncPrices()
        {
            if (BackValues != null && BasePrice < 1000 && BasePrice >= 1.01)
            {
                Int32 offset = Convert.ToInt32(MoveBack);
                for (int i = 0; i < 9; i++)
                {
                    BackValues[i].price = betfairPrices[base_index + offset + i];
                }
                offset = Convert.ToInt32(MoveLay) - 31;
                for (int i = 0; i < 9; i++)
                {
                    LayValues[i].price = betfairPrices[base_index + offset + i];
                }
                NotifyPropertyChanged("");
            }
        }
        private Properties.Settings props = Properties.Settings.Default;
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            SyncPrices();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (BasePrice < 1000 && BasePrice > 1.01)
            {
                switch (b.Tag)
                {
                    case "Execute":
                        if (Favorite != null)
                        {
                            ControlMessenger.Send("Execute Bets", new { Favorite = Favorite, LayValues = LayValues.ToList(), BackValues = BackValues.ToList() });
                        }

                        //if (OnSubmitBets != null)
                        //                  {
                        //                      try
                        //                      {
                        //                          if (Favorite != null)
                        //                          {
                        //			ControlMessenger.Send("Execute Bets", new { Favorite = Favorite, LayValues = LayValues, BackValues = BackValues });
                        //                             //OnSubmitBets(Favorite, LayValues, BackValues);
                        //                          }
                        //                      }
                        //                      catch (Exception xe)
                        //                      {
                        //                          Debug.WriteLine(xe.Message);
                        //                          Extensions.MainWindow.Status = xe.Message;
                        //                      }
                        //                  }
                        break;
                    default: return;
                }
                SyncPrices();
            }
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MoveStakes((Int32)e.NewValue, (Int32)e.OldValue);
        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tx = sender as TextBox;
            if (!String.IsNullOrEmpty(tx.Text))
            {
                BasePrice = BetfairAPI.BetfairAPI.BetfairPrice(Convert.ToDouble(tx.Text));
                props.BasePrice = BasePrice;
                SyncPrices();
                tx.Text = Convert.ToString(BasePrice);
                props.Save();
            }
        }
        private void Button_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
           //Debug.WriteLine("Down");
            Button b = sender as Button;
            String Tag = b.Tag as String;
            timer = new System.Timers.Timer(75);

            timer.Enabled = true;
            timer.AutoReset = false;
            timer.Elapsed += (o, _e) =>
            {

                switch (Tag)
                {
                    case "-":
                        BasePrice = betfairPrices.Previous(BasePrice);
                        break;
                    case "+": BasePrice = betfairPrices.Next(BasePrice); break;
                }
                timer.Stop();
                timer.Enabled = false;
                Debug.WriteLine(BasePrice);
                SyncPrices();
                timer.Start();
            };
        }
        private void Button_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //Debug.WriteLine("Up");
            timer.Enabled = false;
            timer.Stop();
        }
    }
}
