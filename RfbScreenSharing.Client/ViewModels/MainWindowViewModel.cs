using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace RfbScreenSharing.Client.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private IBitmap? _imageSource;

		public MainWindowViewModel()
		{
			StartCommand = ReactiveCommand.CreateFromTask(RunAsync);
		}

		public IBitmap? ImageSource
		{
			get => _imageSource;
			set
			{
				_imageSource = value;
				this.RaisePropertyChanged(nameof(ImageSource));
			}
		}

		public ReactiveCommand<Unit, Unit> StartCommand { get; set; }

		private static async Task<int> RegisterClientAsync()
		{
			var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1338);
			using var registrationEndpoint = new UdpClient();

			// Register this client in the server.
			await registrationEndpoint.SendAsync(new byte[] { 1 }, 1, endpoint).ConfigureAwait(false);

			return (registrationEndpoint.Client.LocalEndPoint as IPEndPoint)!.Port;
		}

		private async Task RunAsync()
		{
			var port = await RegisterClientAsync().ConfigureAwait(false);
			using var client = new UdpClient(port);

			while (true)
			{
				await using var stream = new MemoryStream();
				while (true)
				{
					var response = await client.ReceiveAsync().ConfigureAwait(false);
					if (response.Buffer.Length == 0)
					{
						break;
					}

					await stream.WriteAsync(response.Buffer).ConfigureAwait(false);
				}

				stream.Seek(0, SeekOrigin.Begin);
				ImageSource = new Bitmap(stream);
			}
		}
	}
}