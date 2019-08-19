﻿using System;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using RestSharp;
using Sdl.Community.BeGlobalV4.Provider.Helpers;
using Sdl.Community.BeGlobalV4.Provider.Model;
using Sdl.Community.BeGlobalV4.Provider.Studio;
using DataFormat = RestSharp.DataFormat;

namespace Sdl.Community.BeGlobalV4.Provider.Service
{
	public class BeGlobalV4Translator
	{
		private readonly IRestClient _client;
		private readonly string _flavor;
		private readonly string _url = "https://translate-api.sdlbeglobal.com";
		public static readonly Log Log = Log.Instance;
		private readonly StudioCredentials _studioCredentials;



		private const string PluginName = "SDL BeGlobal (NMT) Translation Provider";

		public BeGlobalV4Translator(string flavor)
		{
			_flavor = flavor;
			 _studioCredentials = new StudioCredentials();
			var accessToken = string.Empty;

			Application.Current?.Dispatcher?.Invoke(() =>
			{
				accessToken = _studioCredentials.GetToken();
			});
			accessToken = _studioCredentials.GetToken();


			_client = new RestClient($"{_url}/v4")
			{
				CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
			};

			if (!string.IsNullOrEmpty(accessToken))
			{
				_client.AddDefaultHeader("Authorization", $"Bearer {accessToken}");
			}
		}

		public string TranslateText(string text, string sourceLanguage, string targetLanguage)
		{
			try
			{
				var request = new RestRequest("/mt/translations/async", Method.POST)
				{
					RequestFormat = DataFormat.Json
				};
				GetTraceId(request);

				string[] texts = { text };
				request.AddBody(new
				{
					input = texts,
					sourceLanguageId = sourceLanguage,
					targetLanguageId = targetLanguage,
					model = _flavor,
					inputFormat = "xliff"
				});
				var response = _client.Execute(request);
				if (!response.IsSuccessful || response.StatusCode != HttpStatusCode.OK)
				{
					ShowErrors(response);
				}
				dynamic json = JsonConvert.DeserializeObject(response.Content);

				var rawData = WaitForTranslation(json?.requestId?.Value);

				json = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(rawData));
				return json != null ? json.translation[0] : string.Empty;
			}
			catch (Exception e)
			{
				Log.Logger.Error($"Translate text method: {e.Message}\n {e.StackTrace}");
			}
			return string.Empty;
		}

		public int GetUserInformation()
		{

			var request = new RestRequest("/accounts/users/self")
			{
				RequestFormat = DataFormat.Json
			};
			var traceId = GetTraceId(request);

			var response = _client.Execute(request);
			var user = JsonConvert.DeserializeObject<UserDetails>(response.Content);
			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				// Get refresh token
				var token = _studioCredentials.EnsureValidConnection();
				if (!string.IsNullOrEmpty(token))
				{
					var newClient = new RestClient($"{_url}/v4")
					{
						CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
					};
					newClient.AddDefaultHeader("Authorization", $"Bearer {token}");
					var userInfoResponse = newClient.Execute(request);
					//_client.RemoveDefaultParameter("Authorization");
					//_client.AddDefaultHeader("Authorization", $"Bearer {token}");
					//var userInfoResponse = _client.Execute(request);

					if (userInfoResponse.StatusCode == HttpStatusCode.Unauthorized)
					{
						MessageBox.Show("Unauthorized: Please check your credentials", PluginName, MessageBoxButton.OK);

						Log.Logger.Error($"Unauthorized: Get UserInfo with Refresh Token: \n {token} \n Trace-Id: {traceId}");
					}
					else if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Unauthorized)
					{
						ShowErrors(response);
					}
				}
			}
			else if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Unauthorized)
			{
				ShowErrors(response);
			}

			return user?.AccountId ?? 0;
		}

		public SubscriptionInfo GetLanguagePairs(string accountId)
		{
			try
			{
				var request = new RestRequest($"/accounts/{accountId}/subscriptions/language-pairs")
				{
					RequestFormat = DataFormat.Json
				};
				GetTraceId(request);

				var response = _client.Execute(request);
				if (!response.IsSuccessful || response.StatusCode != HttpStatusCode.OK)
				{
					ShowErrors(response);
				}
				var subscriptionInfo = JsonConvert.DeserializeObject<SubscriptionInfo>(response.Content);
				return subscriptionInfo;
			}
			catch (Exception e)
			{
				Log.Logger.Error($"Subscription info method: {e.Message}\n {e.StackTrace}");
			}
			return new SubscriptionInfo();
		}

		private byte[] WaitForTranslation(string id)
		{
			try
			{
				IRestResponse response;
				string status;
				do
				{
					response = RestGet($"/mt/translations/async/{id}");
					if (!response.IsSuccessful || response.StatusCode != HttpStatusCode.OK)
					{
						ShowErrors(response);
					}

					dynamic json = JsonConvert.DeserializeObject(response.Content);
					status = json.translationStatus;

					if (!status.Equals("DONE", StringComparison.CurrentCultureIgnoreCase))
					{
						System.Threading.Thread.Sleep(300);
					}
					if (status.Equals("FAILED"))
					{
						ShowErrors(response);
					}
				} while (status.Equals("INIT", StringComparison.CurrentCultureIgnoreCase) ||
						 status.Equals("TRANSLATING", StringComparison.CurrentCultureIgnoreCase));

				response = RestGet($"/mt/translations/async/{id}/content");
				if (!response.IsSuccessful || response.StatusCode != HttpStatusCode.OK)
				{
					ShowErrors(response);
				}
				return response.RawBytes;
			}
			catch (Exception e)
			{
				Log.Logger.Error($"Wait for translation method: {e.Message}\n {e.StackTrace}");
			}
			return new byte[1];
		}

		private IRestResponse RestGet(string command)
		{
			var request = new RestRequest(command)
			{
				RequestFormat = DataFormat.Json
			};
			GetTraceId(request);

			var response = _client.Execute(request);
			return response;
		}
		private string GetTraceId(IRestRequest request)
		{
			var pluginVersion = VersionHelper.GetPluginVersion();
			var studioVersion = VersionHelper.GetStudioVersion();
			var traceId = $"BeGlobal {pluginVersion} - {studioVersion}.{Guid.NewGuid().ToString()}";
			request.AddHeader("Trace-ID", traceId);
			return traceId;
		}

		private void ShowErrors(IRestResponse response)
		{
			var responseContent = JsonConvert.DeserializeObject<ResponseError>(response.Content);

			if (response.StatusCode == HttpStatusCode.Forbidden)
			{
				MessageBox.Show("Forbidden: Please check your license", "BeGlobal translation provider", MessageBoxButton.OK);
				throw new Exception("Forbidden: Please check your license");
			}
			if (responseContent?.Errors != null)
			{
				foreach (var error in responseContent.Errors)
				{
					throw new Exception($"Error code: {error.Code}, {error.Description}");
				}
			}
			
			
		}
	}
}