using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpreadTrader
{
	public static class StreamRecorder
	{
		private static readonly StreamWriter _writer;
		private static readonly object _lock = new object();

		static StreamRecorder()
		{
			String filePath = $"orders-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
			_writer = new StreamWriter(filePath, append: false);
		}

		public static void Record(string json)
		{
			lock (_lock)
			{
				_writer.WriteLine(json);
				_writer.Flush(); // ensures it's on disk immediately
			}
		}

		public static void Dispose()
		{
			_writer.Dispose();
		}
	}
}
