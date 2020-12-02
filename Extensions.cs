using System;
using System.Windows;
using System.Windows.Media;

namespace SpreadTrader
{
	public static class Extensions
	{
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
}
