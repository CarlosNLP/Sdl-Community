﻿using System;
using System.IO;

namespace Sdl.Community.XLIFF.Manager.Common
{
	public class PathInfo
	{
		private const string SdlCommunityPathName = "SDL Community";
		private const string ApplicationPathName = "XLIFF.Manager";
		private const string SettingsPathName = "Settings";
		private const string LogsPathName = "Logs";
		private const string FlagsPathName = "Flags";
		private const string SettingsFileName = "Settings.xml";
		private const string FlagsFileName = "Flags.zip";

		private string _sdlCommunityFolderPath;
		private string _applicationFolderPath;
		private string _settingsFolderPath;
		private string _logsFolderPath;
		private string _flagsFolderPath;

		public string SdlCommunityFolderPath
		{
			get
			{
				if (!string.IsNullOrEmpty(_sdlCommunityFolderPath))
				{
					return _sdlCommunityFolderPath;
				}

				_sdlCommunityFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					SdlCommunityPathName);

				if (!Directory.Exists(_sdlCommunityFolderPath))
				{
					Directory.CreateDirectory(_sdlCommunityFolderPath);
				}

				return _sdlCommunityFolderPath;
			}
		}

		public string ApplicationFolderPath
		{
			get
			{
				if (!string.IsNullOrEmpty(_applicationFolderPath))
				{
					return _applicationFolderPath;
				}

				_applicationFolderPath = Path.Combine(SdlCommunityFolderPath, ApplicationPathName);
				if (!Directory.Exists(_applicationFolderPath))
				{
					Directory.CreateDirectory(_applicationFolderPath);
				}

				return _applicationFolderPath;
			}
		}

		public string ApplicationLogsFolderPath
		{
			get
			{
				if (!string.IsNullOrEmpty(_logsFolderPath))
				{
					return _logsFolderPath;
				}

				_logsFolderPath = Path.Combine(ApplicationFolderPath, LogsPathName);
				if (!Directory.Exists(_logsFolderPath))
				{
					Directory.CreateDirectory(_logsFolderPath);
				}

				return _logsFolderPath;
			}
		}

		public string SettingsFolderPath
		{
			get
			{
				if (!string.IsNullOrEmpty(_settingsFolderPath))
				{
					return _settingsFolderPath;
				}

				_settingsFolderPath = Path.Combine(ApplicationFolderPath, SettingsPathName);
				if (!Directory.Exists(_settingsFolderPath))
				{
					Directory.CreateDirectory(_settingsFolderPath);
				}

				return _settingsFolderPath;
			}
		}

		public string FlagsFolderPath
		{
			get
			{
				if (!string.IsNullOrEmpty(_flagsFolderPath))
				{
					return _flagsFolderPath;
				}

				_flagsFolderPath = Path.Combine(ApplicationFolderPath, FlagsPathName);
				if (!Directory.Exists(_flagsFolderPath))
				{
					Directory.CreateDirectory(_flagsFolderPath);
				}

				return _flagsFolderPath;
			}
		}

		public string SettingsFilePath => Path.Combine(SettingsFolderPath, SettingsFileName);

		public string FlagsFilePath => Path.Combine(FlagsFolderPath, FlagsFileName);
	}
}
