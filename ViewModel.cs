using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpreadTrader
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		public void OnPropertyChanged(string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		public event PropertyChangedEventHandler PropertyChanged;
	}
	public static class Behaviours
	{
		public static readonly DependencyProperty ExpandingBehaviourProperty = DependencyProperty.RegisterAttached("ExpandingBehaviour", typeof(ICommand), typeof(Behaviours), new PropertyMetadata(OnExpandingBehaviourChanged));
		public static void SetExpandingBehaviour(DependencyObject o, ICommand value)
		{
			o.SetValue(ExpandingBehaviourProperty, value);
		}
		public static ICommand GetExpandingBehaviour(DependencyObject o)
		{
			return (ICommand)o.GetValue(ExpandingBehaviourProperty);
		}
		private static void OnExpandingBehaviourChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TreeViewItem tvi = d as TreeViewItem;
			if (tvi != null)
			{
				ICommand ic = e.NewValue as ICommand;
				if (ic != null)
				{
					tvi.Expanded += (s, a) =>
					{
						if (ic.CanExecute(a))
						{
							ic.Execute(a);
						}
						a.Handled = true;
					};
				}
			}
		}
	}
	public class RelayCommand : ICommand
	{
		private Action<object> execute;
		private Func<object, bool> canExecute;
		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
		public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
		{
			this.execute = execute;
			this.canExecute = canExecute;
		}
		public bool CanExecute(object parameter)
		{
			return this.canExecute == null || this.canExecute(parameter);
		}
		public void Execute(object parameter)
		{
			this.execute(parameter);
		}
	}
}
