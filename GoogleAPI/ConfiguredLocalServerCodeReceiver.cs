using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Logging;
using System.Linq.Expressions;

namespace GoogleAPI
{
	public class ConfiguredLocalServerCodeReceiver: ICodeReceiver
	{
		private static readonly ILogger Logger = ApplicationContext.Logger.ForType<LocalServerCodeReceiver>();
		private const string LoopbackCallback = "http://localhost:{0}/google-auth/";
		private const string ClosePageResponse = "<html>\r\n  <head><title>OAuth 2.0 Authentication Token Received</title></head>\r\n  <body>\r\n    Received verification code. You may now close this window.\r\n    <script type='text/javascript'>\r\n      // This doesn't work on every browser.\r\n      window.setTimeout(function() {\r\n          window.open('', '_self', ''); \r\n          window.close(); \r\n        }, 1000);\r\n      if (window.opener) { window.opener.checkToken(); }\r\n    </script>\r\n  </body>\r\n</html>";

		public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken)
		{
			var fileName = url.Build().ToString();
			AuthorizationCodeResponseUrl authorizationCodeResponseUrl;
			using (var httpListener = new HttpListener())
			{
				httpListener.Prefixes.Add(RedirectUri);
				try
				{
					httpListener.Start();
					Logger.Debug("Open a browser with \"{0}\" URL", (object)fileName);
					Process.Start(fileName);
					var httpListenerContext = await httpListener.GetContextAsync().ConfigureAwait(false);
					var coll = httpListenerContext.Request.QueryString;
					using (var streamWriter = new StreamWriter(httpListenerContext.Response.OutputStream))
					{
						streamWriter.WriteLine(ClosePageResponse);
						streamWriter.Flush();
					}
					httpListenerContext.Response.OutputStream.Close();
					var allKeys = coll.AllKeys;
					Expression<Func<string, string>> elementSelector = k => coll[k];
					authorizationCodeResponseUrl = new AuthorizationCodeResponseUrl(allKeys.ToDictionary(k => k, elementSelector.Compile()));
				}
				finally
				{
					httpListener.Close();
				}
			}
			return authorizationCodeResponseUrl;
		}

		public string RedirectUri => string.Format(LoopbackCallback, GetPort());

		private static int GetPort()
		{
			return Properties.Settings.Default.Port;	
		}
	}
}
