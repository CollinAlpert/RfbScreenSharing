using System;
using System.Collections.Generic;
using BenchmarkDotNet.Running;

namespace RfbScreenSharing.Server.Tests
{
	public class Program
	{
		public static void Main(string[] args)
		{
			BenchmarkRunner.Run<BenchmarkContainer>();
		}
	}

	public static class Extensions
	{
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var element in source)
			{
				action(element);
			}
		}
	}
}