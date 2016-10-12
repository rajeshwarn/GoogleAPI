using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Drive.v2;
using Google.Apis.Json;
using log4net.Util;
using Org.BouncyCastle.Security;

namespace GoogleAPI
{
	class Program
	{
		private static readonly string USER_NAME = Environment.UserName;
		private static readonly string CommandTemplate = "Command: {0}\t\t\t- {1}.";
		private static readonly string ParametersTemplate = "\tParameter: {0}\t\t- {1}.";

		private static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				return ShowHelp();
			}

			var command = args[0];
			var parameters = new Dictionary<string, string>();

			for (var idx = 1; idx < args.Length; idx++)
			{
				var key = args[idx];
				var value = ++idx < args.Length ? args[idx] : null;
				parameters.Add(key, value);
			}

			switch (command)
			{
				case "-Init":
				case "-GetToken":
					return GetAccessToken();
				case "-UploadJS":
					return UploadJs(parameters).ConfigureAwait(false).GetAwaiter().GetResult();
				case "-ProjectListFiles":
					return GetProjectListFiles(parameters).ConfigureAwait(false).GetAwaiter().GetResult();
				default:
					Console.WriteLine($"The command {command} is not implemented.");
					return 1;
			}
		}

		private static async Task<int> GetProjectListFiles(Dictionary<string, string> parameters)
		{
			try
			{
				if (parameters.Count == 0)
					throw new InvalidParameterException(nameof(parameters));

				var projectId = parameters.ContainsKey("-ProjectId")
					? parameters["-ProjectId"]
					: null;
				if (projectId == null)
					throw new InvalidParameterException("The key \"-ProjectId\" is empty");

				var request = await CreateRequestForProjectFiles(projectId).ConfigureAwait(false);

				var response = request.GetResponse();

				GoogleProject googleProject;

				using (var responseStream = response.GetResponseStream())
				{
					googleProject = NewtonsoftJsonSerializer.Instance.Deserialize<GoogleProject>(responseStream);
				}

				googleProject.Files.ForEach(
					file => Console.WriteLine(
						$"Id: {file.Id},\tName: {file.Name},\t Type: {file.Type},\tSource: {file.Source.Substring(0, 30)}"));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return 1;
			}
			return 0;
		}

		private static async Task<int> UploadJs(Dictionary<string, string> parameters)
		{
			try
			{
				if(parameters.Count == 0)
					throw new InvalidParameterException(nameof(parameters));

				var projectId = parameters.ContainsKey("-ProjectId")
					? parameters["-ProjectId"]
					: null;
				if (projectId == null)
					throw new InvalidParameterException("The key \"-ProjectId\" is empty");

				var scriptId = parameters.ContainsKey("-ScriptId")
					? parameters["-ScriptId"]
					: null;
				if (scriptId == null)
					throw new InvalidParameterException("The key \"-ScriptId\" is empty");

				var scriptName = parameters.ContainsKey("-ScriptName")
					? parameters["-ScriptName"]
					: "Code";

				var scriptSource = parameters.ContainsKey("-ScriptSource")
					? parameters["-ScriptSource"]
					: null;
				if (scriptSource == null || !File.Exists(scriptSource))
					throw new InvalidParameterException("The key \"-ScriptSource\" is empty. Of file is not exists");

				var source = File.ReadAllText(scriptSource);

				var request = await CreateRequestForUploadGs(projectId, scriptId, scriptName, source).ConfigureAwait(false);

				var response = request.GetResponse();

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return 1;
			}
			return 0;
		}

		private static async Task<WebRequest> CreateRequestForUploadGs(string projectId,
										string scriptId,
										string scriptName,
										string source)
		{


			var userCredentials = GetUserCredential();
			if (userCredentials == null)
				throw new InvalidCredentialException(nameof(userCredentials));

			var uri = string.Format(Properties.Settings.Default.UploadProjectUri, projectId);
			var request = WebRequest.Create(uri);

			var token = await userCredentials.GetAccessTokenForRequestAsync().ConfigureAwait(false);

			request.Method = "PUT";
			request.Headers.Add("Authorization", $"Bearer {token}");

			var googleProject = new GoogleProject();
			googleProject.Files.Add(new GoogleProject.GoogleScript
			{
				Id = scriptId,
				Name = scriptName,
				Type = "server_js",
				Source = source
			});

			var content = NewtonsoftJsonSerializer.Instance.Serialize(googleProject);

			request.ContentType = "application/vnd.google-apps.script+json";
			var contentBytes = Encoding.UTF8.GetBytes(content);
			request.ContentLength = contentBytes.Length;

			using (var requestStream = request.GetRequestStream())
			{
				requestStream.Write(contentBytes, 0, contentBytes.Length);
			}

			return request;
		}

		private static async Task<WebRequest> CreateRequestForProjectFiles(string projectId)
		{
			var userCredentials = GetUserCredential();
			if (userCredentials == null)
				throw new InvalidCredentialException(nameof(userCredentials));

			var uri = string.Format(Properties.Settings.Default.ListFilesUri, projectId);
			var request = WebRequest.Create(uri);

			var token = await userCredentials.GetAccessTokenForRequestAsync().ConfigureAwait(false);

			request.Method = "GET";
			request.Headers.Add("Authorization", $"Bearer {token}");

			return request;
		}

		private static UserCredential GetUserCredential()
		{
			UserCredential userCredentials;

			using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
			{
				var secrets = GoogleClientSecrets.Load(stream).Secrets;

				var clietnId = secrets.ClientId;
				var clientSecret = secrets.ClientSecret;


				userCredentials = Authentication.AuthenticateOauth(clietnId, clientSecret, USER_NAME);

				if (userCredentials == null)
					throw new InvalidCredentialException();
			}

			return userCredentials;
		}

		private static int GetAccessToken()
		{
			try
			{
				var userCredentials = GetUserCredential();
				Console.WriteLine($"Access Token : {userCredentials.Token}");
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return 1;
			}
		}

		private static int ShowHelp()
		{ 
			Console.WriteLine("Google API: -command -param1[...-paramN]");
			PrintCommand("-Init", "Initialization OAuth2.0 storage");
			PrintEmptyLine();

			PrintCommand("-GetToken", "Print on screen valid access token");
			PrintEmptyLine();

			PrintCommand("-UploadJS", "Upload JS file into Google App Project on Google Drive");
			PrintParameter("-ProjectId", "Project Id, include uploading JS file");
			PrintParameter("-ScriptId", "Script Id in Google App Project");
			PrintParameter("-ScriptName", "Script name in Google App Project");
			PrintParameter("-ScriptSource", "Path to JS file");

			PrintCommand("-ProjectListFiles", "Get list of files by google app project Id");
			PrintParameter("-ProjectId", "Id of project");

			return 0;
		}

		private static string PrintInfo(string template, string name, string description)
		{
			if(string.IsNullOrWhiteSpace(template))
				throw new ArgumentException(nameof(template));
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentException(nameof(name));

			return string.Format(template, name, description);
		}

		private static void PrintCommand(string name, string description)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException(nameof(name));

			Console.WriteLine(PrintInfo(CommandTemplate, name, description));
		}

		private static void PrintParameter(string name, string description)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException(nameof(name));

			Console.WriteLine(PrintInfo(ParametersTemplate, name, description));
		}

		private static void PrintEmptyLine()
		{
			Console.WriteLine(Environment.NewLine);
		}
	}
}
