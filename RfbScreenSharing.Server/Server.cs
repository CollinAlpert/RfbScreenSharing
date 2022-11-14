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

namespace RfbScreenSharing.Server;

public partial class Server
{
	private readonly IList<IPEndPoint> _endPoints;
	private static readonly CancellationTokenSource Cts = new();

	private Server()
	{
		_endPoints = new List<IPEndPoint>(1);
	}

	public static Task Main()
	{
		Console.CancelKeyPress += (_, _) => Cts.Cancel();

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
				var endpoint = new IPEndPoint(IPAddress.Any, 0);
				while (!Cts.IsCancellationRequested)
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

		while (!Cts.IsCancellationRequested)
		{
			if (_endPoints.Count == 0)
			{
				continue;
			}

			var bytes = await TakeScreenshotAsync().ConfigureAwait(false);
			foreach (var chunk in SplitToChunks(bytes, 1_500))
			{
				var sendTasks = _endPoints.Select(x => server.SendAsync(chunk, chunk.Length, x));
				await Task.WhenAll(sendTasks).ConfigureAwait(false);
			}

			var sendFinishedNotificationTasks = _endPoints.Select(x => server.SendAsync(Array.Empty<byte>(), 0, x));
			await Task.WhenAll(sendFinishedNotificationTasks).ConfigureAwait(false);
		}

		File.Delete("tmp.jpg");
	}

	private static async Task<byte[]> TakeScreenshotAsync()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var bitmap = new Bitmap(1440, 900);
			using var graphics = Graphics.FromImage(bitmap);
			graphics.CopyFromScreen(0, 0, 1440, 900, new Size(1440, 900), CopyPixelOperation.SourceCopy);
			await using var stream = new MemoryStream();
			bitmap.Save(stream, ImageFormat.Jpeg);

			return stream.ToArray();
		}

		Screenshot();

		return await File.ReadAllBytesAsync("tmp.jpg");
	}

	[LibraryImport("libscreenshot")]
	private static partial void Screenshot();

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