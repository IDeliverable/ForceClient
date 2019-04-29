using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IdentityModel.OidcClient.Browser;

namespace IDeliverable.ForceClient.Core.OrgAccess.Native
{
	public class DesktopClientBrowser : IBrowser
	{
		public DesktopClientBrowser(int port, string path = null)
		{
			Port = port;
			Path = path;
		}

		public int Port { get; }
		public string Path { get; }

		public async Task<BrowserResult> InvokeAsync(BrowserOptions options)
		{
			using (var listener = new DesktopClientListener(Port, Path))
			{
				OpenBrowser(options.StartUrl);

				try
				{
					var result = await listener.WaitForCallbackAsync();

					if (String.IsNullOrWhiteSpace(result))
						return new BrowserResult() { ResultType = BrowserResultType.UnknownError, Error = "Empty response." };

					return new BrowserResult() { Response = result, ResultType = BrowserResultType.Success };
				}
				catch (TaskCanceledException ex)
				{
					return new BrowserResult() { ResultType = BrowserResultType.Timeout, Error = ex.Message };
				}
				catch (Exception ex)
				{
					return new BrowserResult() { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
				}
			}
		}

		public static void OpenBrowser(string url)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				Process.Start("xdg-open", url);
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				Process.Start("open", url);
		}
	}
}
