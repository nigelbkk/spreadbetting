using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace SpreadTrader
{
	public class BulkObservableCollection<T> : ObservableCollection<T>
	{
		private int _suspendCount;

		public int SuspendCountDebug() => _suspendCount;

		public IDisposable SuspendNotifications()
		{
			_suspendCount++;
			Debug.WriteLine($"[SUSPEND] Count={_suspendCount}");
			return new Resume(this);
		}
		private void ResumeNotifications()
		{
			if (--_suspendCount == 0)
			{
				base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
				base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

				base.OnCollectionChanged(
					new NotifyCollectionChangedEventArgs(
						NotifyCollectionChangedAction.Reset));
			}
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLine(
				$"[COLLECTION CHANGED] Action={e.Action} " +
				$"Suspend={_suspendCount} " +
				$"Items={this.Count}");

			if (_suspendCount == 0)
			{
				base.OnCollectionChanged(e);
			}
			else
			{
				Debug.WriteLine($"[SUPPRESSED] Action={e.Action}");
			}
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
