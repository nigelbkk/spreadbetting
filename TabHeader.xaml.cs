using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpreadTrader
{
            //    //public MarketSelectionDelegate OnMarketSelected;
            //    //public StreamUpdateDelegate StreamUpdateEventSink = null;
            //    //public string Title { 
            //    //    set 
            //    //    { 
            //    //        ((TabHeader)this.Header).Label.Content = value; 
            //    //    }
            //    //    get
            //    //    {
            //    //        return ((TabHeader)this.Header).Label.Content.ToString();
            //    //    }
            //    //}
            //    public ClosableTab()
            //    {
            //        TabHeader OurHeader = new TabHeader();
            //        this.Header = OurHeader;
            //        OnMarketSelected += (node) =>
            //        {
            //            if (IsSelected)
            //            {
            //                OurHeader.Label.Content = node.MarketName.Trim();
            //            }
            //        };
            //        StreamingAPI.Callback += (marketid, liveRunners, tradedVolume, inplay) =>
            //        {
            //            if (marketid != "")         //TODO
            //            {
            //                this.Dispatcher.Invoke(() =>
            //                {
            //                    OurHeader.Label.Foreground = inplay ? Brushes.LightGreen : Brushes.DarkGray;
            //                });
            //            }
            //        };
            //        Content = new TabContent();
            //    }
        //}
    public partial class NewTabItem : TabItem
    {
        TabContent tabContent = null;
        public MarketHeader marketHeader { get; set; }
        public NewTabItem()
        {
        }
        private void Label_SizeChanged(object sender, SizeChangedEventArgs e)
        {
//            ClosableTab parent = this.Parent as ClosableTab;
            //TabHeader header = parent.Header as TabHeader;
            //parent.Width = Math.Max(e.NewSize.Width + header.CloseButton.Width, 20);
        }
        private void Image_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image img = sender as Image;
  //          ClosableTab tab = this.Parent as ClosableTab;
//            TabControl tc = tab.Parent as TabControl;
  //          tc.Items.Remove(tab);
        }
    }
}
