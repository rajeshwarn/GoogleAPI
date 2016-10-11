using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace GoogleAPI
{
	public class Authentication
	{
		//Google Drive scopes Documentation:   https://developers.google.com/drive/web/scopes
		private static readonly string[] Scopes = {
													DriveService.Scope.Drive,  // view and manage your files and documents
													DriveService.Scope.DriveAppdata,  // view and manage its own configuration data
													DriveService.Scope.DriveAppsReadonly,   // view your drive apps
													DriveService.Scope.DriveFile,   // view and manage files created by this app
													DriveService.Scope.DriveMetadataReadonly,   // view metadata for files
													DriveService.Scope.DriveReadonly,   // view files and documents on your drive
													DriveService.Scope.DriveScripts };  // modify your app scripts

		/// <summary>
		/// Authenticate to Google Using Oauth2
		/// Documentation https://developers.google.com/accounts/docs/OAuth2
		/// </summary>
		/// <param name="clientId">From Google Developer console https://console.developers.google.com</param>
		/// <param name="clientSecret">From Google Developer console https://console.developers.google.com</param>
		/// <param name="userName">A string used to identify a user.</param>
		/// <returns></returns>
		public static UserCredential AuthenticateOauth(string clientId, string clientSecret, string userName)
		{
			try
			{
				// here is where we Request the user to give us access, or use the Refresh Token that was previously stored in %AppData%
				return GoogleWebAuthorizationBroker
										.AuthorizeAsync(new ClientSecrets
																{
																	ClientId = clientId,
																	ClientSecret = clientSecret
																}
														, Scopes
														, userName
														, CancellationToken.None
														, new FileDataStore(Properties.Settings.Default.FileDataStore)
														, new ConfiguredLocalServerCodeReceiver()).Result;
			}
			catch (Exception ex)
			{

				Console.WriteLine(ex.InnerException);
				return null;

			}
		}

		/// <summary>
		/// Authenticating to Google using a Service account
		/// Documentation: https://developers.google.com/accounts/docs/OAuth2#serviceaccount
		/// </summary>
		/// <param name="serviceAccountEmail">From Google Developer console https://console.developers.google.com</param>
		/// <param name="keyFilePath">Location of the Service account key file downloaded from Google Developer console https://console.developers.google.com</param>
		/// <returns></returns>
		public static ServiceAccountCredential AuthenticateServiceAccount(string serviceAccountEmail, string keyFilePath)
		{

			// check the file exists
			if (!File.Exists(keyFilePath))
			{
				Console.WriteLine("An Error occurred - Key file does not exist");
				return null;
			}

			// When you set passown during creating certificate, you have to remember it, and provide it there
			var certificate = new X509Certificate2(keyFilePath, "notasecret", X509KeyStorageFlags.Exportable);
			try
			{
				return new ServiceAccountCredential(
													new ServiceAccountCredential.Initializer(serviceAccountEmail)
													{
														Scopes = Scopes
													}.FromCertificate(certificate));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.InnerException);
				return null;

			}
		}
	}
}