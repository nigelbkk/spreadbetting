using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace SpreadTrader
{
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
			throw new NotImplementedException();
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
