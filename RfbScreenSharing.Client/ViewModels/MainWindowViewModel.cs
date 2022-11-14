using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace RfbScreenSharing.Client.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	private IBitmap? _imageSource;
	private CancellationTokenSource? _cts;

	private static readonly byte[] RegistrationDatagram = { 1 };
	private static readonly byte[] UnRegistrationDatagram = { 0 };

	public MainWindowViewModel()
	{
		StartCommand = ReactiveCommand.CreateFromTask(RunAsync);
		StopCommand = ReactiveCommand.Create(() => _cts?.Cancel());
	}

	public IBitmap? ImageSource
	{
		get => _imageSource;
		set => this.RaiseAndSetIfChanged(ref _imageSource, value);
	}

	public string? ServerHost { get; set; }

	public ReactiveCommand<Unit, Unit> StartCommand { get; set; }
	public ReactiveCommand<Unit, Unit> StopCommand { get; set; }

	private async Task RunAsync()
	{
		if(string.IsNullOrWhiteSpace(ServerHost))
		{
			throw new InvalidOperationException("Please enter a server host!");
		}

		_cts = new CancellationTokenSource();
		var client = new UdpClient();
		var registrationEndpoint = new IPEndPoint(IPAddress.Parse(ServerHost), 1338);
		await client.SendAsync(RegistrationDatagram, 1, registrationEndpoint).ConfigureAwait(false);

		var stream = new MemoryStream();
		while (!_cts.IsCancellationRequested)
		{
			stream.SetLength(0);
			while (true)
			{
				var response = await client.ReceiveAsync().ConfigureAwait(false);
				if (response.Buffer.Length == 0)
				{
					break;
				}

				await stream.WriteAsync(response.Buffer).ConfigureAwait(false);
			}

			stream.Position = 0;
			ImageSource = new Bitmap(stream);
		}

		await stream.DisposeAsync();

		await client.SendAsync(UnRegistrationDatagram, 1, registrationEndpoint).ConfigureAwait(false);
	}
}