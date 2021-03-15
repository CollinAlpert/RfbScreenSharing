using System.Diagnostics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace RfbScreenSharing.Server.Tests
{
	[MemoryDiagnoser]
	public class BenchmarkContainer
	{
		/*[GlobalSetup]
		public void Setup()
		{
		}*/

		[Benchmark(Baseline = true)]
		public void Benchmark()
		{
			RunBash("screencapture -t jpg -x tmp.jpg -r");
		}

		[Benchmark]
		public void Benchmark2()
		{
			Screenshot();
		}

		[Benchmark]
		public void Benchmark3()
		{
			Screenshot2();
		}

		[DllImport("libscreenshot.dylib")]
		private static extern void Screenshot();

		[DllImport("libscreenshot.dylib")]
		private static extern void Screenshot2();

		private static void RunBash(string cmd)
		{
			var escapedArgs = cmd.Replace("\"", "\\\"");

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "/bin/bash",
					Arguments = $"-c \"{escapedArgs}\"",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};
			process.Start();
			process.WaitForExit();
		}
	}
}