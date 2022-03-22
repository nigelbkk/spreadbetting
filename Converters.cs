using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace SpreadTrader
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush matched = Application.Current.FindResource("MatchedColor") as SolidColorBrush;
            SolidColorBrush unmatched = Application.Current.FindResource("UnmatchedColor") as SolidColorBrush;
            bool input = bool.Parse(value.ToString());
            switch (input)
            {
                case true:
                    return matched;
                case false:
                    return unmatched;
                default:
                    return DependencyProperty.UnsetValue;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class SideColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush back = Application.Current.FindResource("Back0Color") as SolidColorBrush;
            SolidColorBrush lay = Application.Current.FindResource("Lay0Color") as SolidColorBrush;
            bool input = bool.Parse(value.ToString());
            switch (input)
            {
                case true:
                    return back;
                case false:
                    return lay;
                default:
                    return DependencyProperty.UnsetValue;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String sign = (double)value < 0 ? "-" : "";
            if (double.IsNaN((double)value))
                return string.Empty;

            UInt32 val = System.Convert.ToUInt32(Math.Abs((double)value));
            if (val == 0)
                return string.Empty;

            String cs = val.ToString();
            if (cs.Length > 3)
            {
                cs = cs.Insert(cs.Length - 3, ",");
            }
            if (cs.Length > 7)
            {
                cs = cs.Insert(cs.Length - 7, ",");
            }
            cs = String.Format("{0}{1}{2}", sign, "£", cs);
            return cs;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = System.Convert.ToDouble(value);
            return String.Format("{0:0.00}%", val);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = System.Convert.ToDouble(value);
            return String.Format("{0:0}ms", val);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class OddsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = System.Convert.ToDouble(value);
            if (val == 0)
                return "0.00";// string.Empty;

            String valString = val.ToString();
            String[] cs = valString.Split('.');

            Int32 dps = cs.Length < 2 ? 0 : cs[1].Length;
            switch (dps)
            {
                //case 0: return String.Format("{0:0}", val);
                //case 1: return String.Format("{0:0.0}", val);
                //case 2: return String.Format("{0:0.00}", val);
                default: return String.Format("{0:0.00}", val);
            }
            //return valString;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class OddsConverterBlank : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = System.Convert.ToDouble(value);
            if (val == 0)
                return string.Empty;

            String valString = val.ToString();
            String[] cs = valString.Split('.');

            Int32 dps = cs.Length < 2 ? 0 : cs[1].Length;
            switch (dps)
            {
                case 0: return String.Format("{0:0}", val);
                case 1: return String.Format("{0:0.0}", val);
                case 2: return String.Format("{0:0.00}", val);
            }
            return valString;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StakeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = System.Convert.ToDouble(value);
            if (val == 0)
                return string.Empty;

            return String.Format("{0:0}", val);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }
    public class StakeConverter2DP : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = System.Convert.ToDouble(value);
            return String.Format("{0:0.00}", val);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }
    public class RowToIndexConverter : MarkupExtension, IValueConverter
    {
        static RowToIndexConverter convertor;
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DataGridRow row = value as DataGridRow;

            if (row != null)
            {
                return row.GetIndex() + 1;
            }
            else
            {
                return -1;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (convertor == null)
            {
                convertor = new RowToIndexConverter();
            }
            return convertor;
        }
        public RowToIndexConverter()
        {
        }
    }
}
