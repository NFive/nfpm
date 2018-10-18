using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NFive.PluginManager
{
	public class Rcon : IDisposable
	{
		private readonly UdpClient client;

		public IPAddress Host { get; }
		public int Port { get; }
		public string Password { get; }

		public Rcon(IPAddress host, int port, string password)
		{
			this.Host = host;
			this.Port = port;
			this.Password = password;

			this.client = new UdpClient();
			this.client.Connect(new IPEndPoint(this.Host, this.Port));
		}

		public async Task Send(string command)
		{
			var data = Encoding.UTF8.GetBytes($"    rcon {this.Password} {command}\n");
			data[0] = 0xFF;
			data[1] = 0xFF;
			data[2] = 0xFF;
			data[3] = 0xFF;

			await this.client.SendAsync(data, data.Length);
		}

		public async Task<string> Receive()
		{
			var response = await this.client.ReceiveAsync();
			var result = Encoding.UTF8.GetString(response.Buffer, 4, response.Buffer.Length - 4);

			return result.StartsWith("print ") ? result.Substring("print ".Length) : null;
		}

		public async Task<string> Command(string command)
		{
			await this.Send(command);
			return await this.Receive();
		}

		public void Dispose()
		{
			this.client.Close();
		}
	}
}
