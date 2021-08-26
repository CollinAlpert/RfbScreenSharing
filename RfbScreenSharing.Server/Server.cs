using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RfbScreenSharing.Server
{
	public class Server
	{
		private readonly IList<IPEndPoint> _endPoints;

		private Server()
		{
			_endPoints = new List<IPEndPoint>(1);
		}

		public static Task Main()
		{
			var program = new Server();
			program.ListenForConnections();
			return program.StartTransmissionAsync();
		}

		private void ListenForConnections()
		{
			new Thread(
				() =>
					{
						var registrationServer = new UdpClient(1338);
						var endpoint = new IPEndPoint(IPAddress.Any, 5001);
						while (true)
						{
							var data = registrationServer.Receive(ref endpoint);
							if (data.Length == 1 && data[0] == 0)
							{
								_endPoints.Remove(endpoint);
							}

							if (data.Length == 1 && data[0] == 1)
							{
								_endPoints.Add(endpoint);
							}
						}
					}).Start();
		}

		private async Task StartTransmissionAsync()
		{
			using var server = new UdpClient();
			server.Client.SendTimeout = 2000;

			while (true)
			{
				if (_endPoints.Count == 0)
				{
					continue;
				}

				var bytes = await TakeScreenshotAsync().ConfigureAwait(false);
				var chunks = SplitToChunks(bytes, 65_507).ToList();
				foreach (var chunk in chunks)
				{
					var sendTasks = _endPoints.Select(x => server.SendAsync(chunk, chunk.Length, x));
					await Task.WhenAll(sendTasks).ConfigureAwait(false);
				}

				var sendFinishedNotificationTasks = _endPoints.Select(x => server.SendAsync(Array.Empty<byte>(), 0, x));
				await Task.WhenAll(sendFinishedNotificationTasks).ConfigureAwait(false);

				//break;
			}
		}

		private Task<byte[]> TakeScreenshotAsync()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return TakeScreenshotMacAsync();
			}

			return TakeScreenshotInternalAsync();
		}

		private async Task<byte[]> TakeScreenshotInternalAsync()
		{
			var bitmap = new Bitmap(1440, 900);
			using var graphics = Graphics.FromImage(bitmap);
			graphics.CopyFromScreen(0, 0, 1440, 900, new Size(1440, 900), CopyPixelOperation.SourceCopy);
			await using var stream = new MemoryStream();
			bitmap.Save(stream, ImageFormat.Jpeg);

			return stream.ToArray();
		}

		private static Task<byte[]> TakeScreenshotMacAsync()
		{
			Screenshot();

			return File.ReadAllBytesAsync("tmp.jpg");
		}
		
		[DllImport("libscreenshot.dylib")]
		private static extern void Screenshot();

		private static IEnumerable<byte[]> SplitToChunks(byte[] array, int chunkSize)
		{
			for (var i = 0; i < array.Length; i += chunkSize)
			{
				if (i + chunkSize >= array.Length)
				{
					yield return array[i..];
					yield break;
				}

				yield return array[i..(i + chunkSize)];
			}
		}
	}
}