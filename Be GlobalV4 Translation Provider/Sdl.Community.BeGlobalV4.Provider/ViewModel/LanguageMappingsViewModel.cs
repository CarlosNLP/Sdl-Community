﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Sdl.Community.BeGlobalV4.Provider.Helpers;
using Sdl.Community.BeGlobalV4.Provider.Model;
using Sdl.Community.BeGlobalV4.Provider.Service;
using Sdl.Community.BeGlobalV4.Provider.Studio;
using Sdl.TranslationStudioAutomation.IntegrationApi;

namespace Sdl.Community.BeGlobalV4.Provider.ViewModel
{
	public class LanguageMappingsViewModel : BaseViewModel
	{
		private bool _reSendChecked;
		private ProjectsController _projectController;
		private LanguageMappingModel _selectedLanguageMapping;
		private LanguageMappingsService _languageMappingService;
		private ObservableCollection<LanguageMappingModel> _languageMappings;
		private BeGlobalWindowViewModel _beGlobalWindowViewModel;

		private ICommand _resetLanguageMappingsCommand;


		public LanguageMappingsViewModel(BeGlobalTranslationOptions options, BeGlobalWindowViewModel beGlobalWindowViewModel)
		{
			Options = options;
			_languageMappings = new ObservableCollection<LanguageMappingModel>();
			_beGlobalWindowViewModel = beGlobalWindowViewModel;

			_languageMappingService = new LanguageMappingsService();

			if (Options != null)
			{
				ReSendChecked = options.ResendDrafts;				
			}
		}
		
		public BeGlobalTranslationOptions Options { get; set; }
	
		public ObservableCollection<LanguageMappingModel> LanguageMappings
		{
			get => _languageMappings;
			set
			{
				_languageMappings = value;
				OnPropertyChanged(nameof(LanguageMappings));
			}
		}

		public List<string> MTCodeSourceList = new List<string>();
		public List<string> MTCodeTargetList = new List<string>();

		public LanguageMappingModel SelectedLanguageMapping
		{
			get => _selectedLanguageMapping;
			set
			{
				_selectedLanguageMapping = value;
				OnPropertyChanged(nameof(SelectedLanguageMapping));
			}
		}

		public bool ReSendChecked
		{
			get => _reSendChecked;
			set
			{
				_reSendChecked = value;
				if (LanguageMappings.Any())
				{
					Options.ResendDrafts = value;
				}
				OnPropertyChanged(nameof(ReSendChecked));
			}
		}

		public ICommand ResetLanguageMappingsCommand => _resetLanguageMappingsCommand ?? (_resetLanguageMappingsCommand = new RelayCommand(ResetLanguageMappings));


		/// <summary>
		/// Load the language mapping settings from .sdlproj settings group
		/// </summary>
		public void LoadLanguageMappings()
		{
			_projectController = AppInitializer.GetProjectController();
			var currentSettings = _languageMappingService.GetLanguageMappingSettings();			

			if (currentSettings != null && currentSettings.LanguageMappings.Any())
			{
				// clear current LanguageMappings list to avoid duplications inside the grid
				LanguageMappings.Clear();
				var mtCodes = AppInitializer.GetMTCodes();
				foreach (var languageMapping in currentSettings.LanguageMappings)
				{
					LanguageMappings.Add(languageMapping);

					var missingCodes = LoadNewCodes(languageMapping, mtCodes);

					// set the SelectedModelOption of the current LanguageMappings collection (otherwise it will not shown in the grid)
					var selectedLangModel = LanguageMappings.FirstOrDefault(l => l.ProjectLanguagePair.Equals(languageMapping.ProjectLanguagePair));
					var langMappingIndex = LanguageMappings.IndexOf(selectedLangModel);
					var selectedModelOption = LanguageMappings[langMappingIndex].Engines.FirstOrDefault(e => e.DisplayName.Equals(selectedLangModel.SelectedModelOption.DisplayName));
					LanguageMappings[langMappingIndex].SelectedModelOption = selectedModelOption;
				
					AddMissingCode(missingCodes.Keys.First(), LanguageMappings[langMappingIndex].MTCodesSource);
					AddMissingCode(missingCodes.Values.First(), LanguageMappings[langMappingIndex].MTCodesTarget);
				}
			}
			LoadProjectLanguagePairs();
		}		

		/// <summary>
		/// Save the language mapping settings inside the .sdlproj settings group
		/// </summary>
		public void SaveLanguageMappingSettings()
		{
			var savedSettings = _languageMappingService.GetLanguageMappingSettings();
			_languageMappingService.RemoveLanguageMappingSettings();
			savedSettings.LanguageMappings = LanguageMappings;
			_languageMappingService.SaveLanguageMappingSettings(savedSettings);
		}

		/// <summary>
		/// Reset the language mappings to the default values
		/// </summary>
		/// <param name="parameter"></param>
		private void ResetLanguageMappings(object parameter)
		{
			if(LanguageMappings != null)
			{
				_languageMappingService.RemoveLanguageMappingSettings();

				var savedSettings = _languageMappingService.GetLanguageMappingSettings();
				_languageMappingService.SaveLanguageMappingSettings(savedSettings);
				LanguageMappings.Clear();
				LoadProjectLanguagePairs();
				_beGlobalWindowViewModel?.ValidateWindow(false);
			}
		}

		/// <summary>
		/// Load to the existing language mappings settings, the new codes which were added using the MTCodes window
		/// </summary>
		/// <param name="languageMapping">language mapping</param>
		/// <param name="mtCodes">mtcodes collection</param>
		/// <returns>dictionary of lists with missing source and target codes</returns>
		private Dictionary<List<string>, List<string>> LoadNewCodes(LanguageMappingModel languageMapping, List<MTCodeModel> mtCodes)
		{
			var sourceModel = mtCodes.FirstOrDefault(s => s.TradosCode.Equals(languageMapping.SourceTradosCode));
			var targetModel = mtCodes.FirstOrDefault(s => s.TradosCode.Equals(languageMapping.TargetTradosCode));

			var sCodes = new List<string>();
			sCodes.Add(sourceModel?.MTCodeMain);
			sCodes.Add(sourceModel?.MTCodeLocale);

			var tCodes = new List<string>();
			tCodes.Add(targetModel?.MTCodeMain);
			tCodes.Add(targetModel?.MTCodeLocale);

			var missingSourceCodes = sCodes.Where(s => languageMapping.MTCodesSource.All(m => m != s)).ToList();
			var missingTargetCodes = tCodes.Where(t => languageMapping.MTCodesTarget.All(m => m != t)).ToList();

			var result = new Dictionary<List<string>, List<string>>();
			result.Add(missingSourceCodes, missingTargetCodes);
			return result;
		}

		/// <summary>
		/// Add missing code to existing LanguageMappings configurations
		/// </summary>
		/// <param name="missingCodes"></param>
		/// <param name="langMappingIndex"></param>
		private void AddMissingCode(List<string> missingCodes, ObservableCollection<string> languageMappingCodes)
		{
			foreach (var code in missingCodes)
			{
				if (!string.IsNullOrEmpty(code))
				{
					languageMappingCodes.Add(code);
				}
			}
		}

		/// <summary>
		/// Load project language pairs and the default details
		/// (the information is loaded only when no other LanguageMappings settings were already made and saved inside the .sdlproj settings group)
		/// </summary>
		private void LoadProjectLanguagePairs()
		{
			if (LanguageMappings == null || !LanguageMappings.Any())
			{
				// load the MTCode (the load is needed, because user might add/remove codes from MTCodes grid
				var mtCodes = AppInitializer.GetMTCodes();

				var currentProjectInfo = _projectController?.CurrentProject?.GetProjectInfo();
				var sourceLanguage = currentProjectInfo?.SourceLanguage;
				var targetLanguages = currentProjectInfo?.TargetLanguages;
				var mtCodeSource = mtCodes?.FirstOrDefault(s => s.TradosCode.Equals(sourceLanguage?.CultureInfo?.Name)
				|| s.TradosCode.Equals(sourceLanguage?.IsoAbbreviation));
				if (mtCodeSource != null)
				{
					MTCodeSourceList.Clear();
					MTCodeSourceList.Add(mtCodeSource.MTCodeMain);
					if (!string.IsNullOrEmpty(mtCodeSource.MTCodeLocale))
					{
						MTCodeSourceList.Add(mtCodeSource.MTCodeLocale);
					}
				}

				MTCodeTargetList.Clear();
				foreach (var targetLanguage in targetLanguages)
				{
					var languagePair = $"{sourceLanguage.DisplayName} - {targetLanguage.DisplayName}";

					var mtCodeTarget = mtCodes?.FirstOrDefault(s => s.TradosCode.Equals(targetLanguage?.CultureInfo?.Name)
					|| s.TradosCode.Equals(targetLanguage?.IsoAbbreviation));
					if (mtCodeTarget != null)
					{
						MTCodeTargetList.Add(mtCodeTarget.MTCodeMain);
						if (!string.IsNullOrEmpty(mtCodeTarget.MTCodeLocale))
						{
							MTCodeTargetList.Add(mtCodeTarget.MTCodeLocale);
						}

						var languageMappingModel = new LanguageMappingModel
						{
							ProjectLanguagePair = languagePair,
							MTCodesSource = new ObservableCollection<string>(MTCodeSourceList),
							SelectedMTCodeSource = MTCodeSourceList[0],
							MTCodesTarget = new ObservableCollection<string>(MTCodeTargetList),
							SelectedMTCodeTarget = MTCodeTargetList[0],
							Engines = new ObservableCollection<TranslationModel>(),
							TargetTradosCode = mtCodeTarget.TradosCode,
							SourceTradosCode = mtCodeSource.TradosCode
						};
						LanguageMappings.Add(languageMappingModel);
						MTCodeTargetList.Clear();
					}
				}
			}
		}		
	}
}