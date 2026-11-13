using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moka.Docs.Serve;

namespace Moka.Docs.Integration.Tests.Serve;

/// <summary>
///     Disposing the dev server must not throw. On Linux the managed listener released its
///     endpoints on <c>Stop</c> and again on <c>Dispose</c>, and the second release bound the port
///     anew: when something else held it by then, Dispose threw "Address already in use". That
///     failed <c>DevServerTests</c> now and then, and could end <c>serve</c> with an exception.
/// </summary>
public sealed class DevServerDisposeTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "mokadocs-devdispose-" + Guid.NewGuid().ToString("N"));

	public DevServerDisposeTests() => Directory.CreateDirectory(_root);

	public void Dispose()
	{
		try
		{
			Directory.Delete(_root, true);
		}
		catch (IOException)
		{
			// Best effort; the OS temp cleaner will get it.
		}
	}

	[Fact]
	public async Task Dispose_AfterStopWhenThePortIsTakenAgain_DoesNotThrow()
	{
		(DevServer server, int port) = await StartAsync();
		server.Stop();

		using var squatter = new TcpListener(IPAddress.Loopback, port);
		squatter.Start();

		Action dispose = server.Dispose;

		dispose.Should().NotThrow();
	}

	[Fact]
	public async Task Dispose_WithoutStop_ReleasesThePort()
	{
		(DevServer server, int port) = await StartAsync();

		server.Dispose();

		using var next = new TcpListener(IPAddress.Loopback, port);
		Action bind = next.Start;
		bind.Should().NotThrow("the server let go of its port");
	}

	private async Task<(DevServer Server, int Port)> StartAsync()
	{
		// A port picked here can be taken by a parallel test before the server binds it.
		for (int attempt = 1; ; attempt++)
		{
			int port = FreePort();
			var server = new DevServer(NullLogger<DevServer>.Instance, _root, port);
			try
			{
				await server.StartAsync(TestContext.Current.CancellationToken);
				return (server, port);
			}
			catch (IOException) when (attempt < 5)
			{
				server.Dispose();
			}
		}
	}

	private static int FreePort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}
}
