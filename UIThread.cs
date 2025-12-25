using System;
using System.Windows;
using System.Windows.Threading;

public static class UiThread
{
	public static void Run(Action action)
	{
		var dispatcher = Application.Current?.Dispatcher;

		if (dispatcher == null || dispatcher.CheckAccess())
		{
			action();
		}
		else
		{
			dispatcher.BeginInvoke(DispatcherPriority.DataBind, action);
		}
	}
}
