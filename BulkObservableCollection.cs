using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SpreadTrader
{
	public class BulkObservableCollection<T> : ObservableCollection<T>
	{
		private int _suspendCount;

		public IDisposable SuspendNotifications()
		{
			_suspendCount++;
			return new Resume(this);
		}

		private void ResumeNotifications()
		{
			if (--_suspendCount == 0)
			{
				OnCollectionChanged(
					new NotifyCollectionChangedEventArgs(
						NotifyCollectionChangedAction.Reset));
			}
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (_suspendCount == 0)
				base.OnCollectionChanged(e);
		}

		private sealed class Resume : IDisposable
		{
			private readonly BulkObservableCollection<T> _c;
			public Resume(BulkObservableCollection<T> c) => _c = c;
			public void Dispose() => _c.ResumeNotifications();
		}
		protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (_suspendCount == 0)
				base.OnPropertyChanged(e);
		}
	}

}
