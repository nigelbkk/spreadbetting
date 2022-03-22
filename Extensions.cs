using System.Windows;
using System.Windows.Media;

namespace SpreadTrader
{
    public static class Extensions
    {
        public static MainWindow MainWindow { get { return Application.Current.MainWindow as MainWindow; } }
        public static T FindParentOfType<T>(this DependencyObject o) where T : DependencyObject
        {
            DependencyObject parent = o;
            do
            {
                parent = VisualTreeHelper.GetParent(parent);
                T v = parent as T;
                if (v != null) return v;
            }
            while (parent != null);
            return null;
        }
        public static readonly DependencyProperty InPlay = DependencyProperty.RegisterAttached("InPlay", typeof(bool), typeof(Extensions), new PropertyMetadata(default(bool)));
        public static void SetInPlay(UIElement element, bool value)
        {
            element.SetValue(InPlay, value);
        }
        public static bool GetInPlay(UIElement element)
        {
            return (bool)element.GetValue(InPlay);
        }
    }
    public class UpDownControlStake : Xceed.Wpf.Toolkit.IntegerUpDown
    {
        protected override int DecrementValue(int value, int increment)
        {
            if (value <= 2)
                return value;
            if (value <= 10)
                return value - 1;

            return (value - 10) - (value % 10);
        }
        protected override int IncrementValue(int value, int increment)
        {
            if (value < 10)
                return value + 1;

            return (value + 10) - (value) % 10;
        }
    }
}
