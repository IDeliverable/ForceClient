using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace IDeliverable.ForceClient.Core.OrgAccess.Native
{
	public class DesktopClientListener : IDisposable
	{
		private const int mDefaultTimeout = 60 * 5; // 5 mins (in seconds)

		public DesktopClientListener(int port, string path = null)
		{
			path = path ?? "";
			path = path.TrimStart('/');

			Url = $"http://127.0.0.1:{port}/{path}";

			mWebHost =
				new WebHostBuilder()
					.UseKestrel()
					.UseUrls(Url)
					.Configure(ConfigureApp)
					.Build();

			mWebHost.Start();
		}

		private readonly IWebHost mWebHost;
		private readonly TaskCompletionSource<string> mTcs = new TaskCompletionSource<string>();

		public string Url { get; }

		public void Dispose()
		{
			// Why are we doing this?
			_ = Task.Delay(500).ContinueWith(_ => mWebHost.Dispose());
		}

		private void ConfigureApp(IApplicationBuilder app)
		{
			app.Run(async context =>
			{
				if (context.Request.Method == "GET")
					SetResult(context.Request.QueryString.Value, context);
				else if (context.Request.Method == "POST")
				{
					if (!context.Request.ContentType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
						context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
					else
					{
						using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
						{
							var body = await reader.ReadToEndAsync();
							SetResult(body, context);
						}
					}
				}
				else
					context.Response.StatusCode = 405;
			});
		}

		private void SetResult(string value, HttpContext ctx)
		{
			try
			{
				ctx.Response.StatusCode = 200;
				ctx.Response.ContentType = "text/html";
				ctx.Response.WriteAsync("<h1>Authentication successful.</h1><p>You can now close this browser tab.</p>");
				ctx.Response.Body.Flush();

				mTcs.TrySetResult(value);
			}
			catch
			{
				ctx.Response.StatusCode = 400;
				ctx.Response.ContentType = "text/html";
				ctx.Response.WriteAsync("<h1>Invalid request.</h1>");
				ctx.Response.Body.Flush();
			}
		}

		public Task<string> WaitForCallbackAsync(int timeoutInSeconds = mDefaultTimeout)
		{
			_ = Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds)).ContinueWith(_ => mTcs.TrySetCanceled());

			return mTcs.Task;
		}
	}
}
