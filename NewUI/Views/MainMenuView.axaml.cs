using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Mesen.Config;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mesen.ViewModels;
using Mesen.Interop;
using Mesen.Windows;
using System.Collections.Generic;
using Mesen.Utilities;
using Mesen.Debugger.Windows;
using Mesen.Debugger.ViewModels;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Mesen.Localization;

namespace Mesen.Views
{
	public class MainMenuView : UserControl
	{
		private ConfigWindow? _cfgWindow = null;
		private MainMenuViewModel _model = null!;

		public MainMenuView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is MainMenuViewModel model) {
				_model = model;
			}
		}

		private void OnSaveStateMenuClick(object sender, RoutedEventArgs e)
		{
			_model.MainWindow.RecentGames.Init(GameScreenMode.SaveState);
		}

		private void OnLoadStateMenuClick(object sender, RoutedEventArgs e)
		{
			_model.MainWindow.RecentGames.Init(GameScreenMode.LoadState);
		}

		private void OnTileViewerClick(object sender, RoutedEventArgs e)
		{
			RomInfo romInfo = EmuApi.GetRomInfo();
			new TileViewerWindow {
				DataContext = new TileViewerViewModel(romInfo.ConsoleType.GetMainCpuType(), romInfo.ConsoleType),
			}.Show();
		}

		private void OnTilemapViewerClick(object sender, RoutedEventArgs e)
		{
			RomInfo romInfo = EmuApi.GetRomInfo();
			new TilemapViewerWindow {
				DataContext = new TilemapViewerViewModel(romInfo.ConsoleType.GetMainCpuType(), romInfo.ConsoleType),
			}.Show();
		}

		private void OnSpriteViewerClick(object sender, RoutedEventArgs e)
		{
			RomInfo romInfo = EmuApi.GetRomInfo();
			new SpriteViewerWindow {
				DataContext = new SpriteViewerViewModel(romInfo.ConsoleType.GetMainCpuType(), romInfo.ConsoleType),
			}.Show();
		}

		private void OnMemoryToolsClick(object sender, RoutedEventArgs e)
		{
			new MemoryToolsWindow {
				DataContext = new MemoryToolsViewModel(),
			}.Show();
		}

		private void OnDebuggerClick(object sender, RoutedEventArgs e)
		{
			new DebuggerWindow {
				DataContext = new DebuggerWindowViewModel(),
			}.Show();
		}

		private void OnAssemblerClick(object sender, RoutedEventArgs e)
		{
			RomInfo romInfo = EmuApi.GetRomInfo();
			new AssemblerWindow {
				DataContext = new AssemblerWindowViewModel(romInfo.ConsoleType.GetMainCpuType(), "")
			}.Show();
		}

		private void OnScriptWindowClick(object sender, RoutedEventArgs e)
		{
			RomInfo romInfo = EmuApi.GetRomInfo();
			new ScriptWindow {
				DataContext = new ScriptWindowViewModel(romInfo.ConsoleType.GetMainCpuType(), "")
			}.Show();
		}

		private void OnProfilerClick(object sender, RoutedEventArgs e)
		{
			new ProfilerWindow {
				DataContext = new ProfilerWindowViewModel()
			}.Show();
		}

		private void OnRegisterViewerClick(object sender, RoutedEventArgs e)
		{
			new RegisterViewerWindow {
				DataContext = new RegisterViewerWindowViewModel()
			}.Show();
		}

		private void OnTraceLoggerClick(object sender, RoutedEventArgs e)
		{
			new TraceLoggerWindow {
				DataContext = new TraceLoggerViewModel()
			}.Show();
		}

		private void OnEventViewerClick(object sender, RoutedEventArgs e)
		{
			RomInfo romInfo = EmuApi.GetRomInfo();
			new EventViewerWindow {
				DataContext = new EventViewerViewModel(romInfo.ConsoleType.GetMainCpuType())
			}.Show();
		}

		private void OpenConfig(ConfigWindowTab tab)
		{
			if(_cfgWindow == null) {
				_cfgWindow = new ConfigWindow { DataContext = new ConfigViewModel(tab) };
				_cfgWindow.Closed += cfgWindow_Closed;
				_cfgWindow.ShowCentered((Window)VisualRoot);
			} else {
				(_cfgWindow.DataContext as ConfigViewModel)!.SelectTab(tab);
				_cfgWindow.Activate();
			}
		}

		private void cfgWindow_Closed(object? sender, EventArgs e)
		{
			_cfgWindow = null;
			if(ConfigManager.Config.Preferences.DisableGameSelectionScreen && _model.MainWindow.RecentGames.Visible) {
				_model.MainWindow.RecentGames.Visible = false;
			} else if(!ConfigManager.Config.Preferences.DisableGameSelectionScreen && !_model.IsGameRunning) {
				_model.MainWindow.RecentGames.Init(GameScreenMode.RecentGames);
			}
		}

		private void OnPreferencesClick(object sender, RoutedEventArgs e)
		{
			OpenConfig(ConfigWindowTab.Preferences);
		}

		private void OnAudioConfigClick(object sender, RoutedEventArgs e)
		{
			OpenConfig(ConfigWindowTab.Audio);
		}

		private void OnVideoConfigClick(object sender, RoutedEventArgs e)
		{
			OpenConfig(ConfigWindowTab.Video);
		}

		private void OnEmulationConfigClick(object sender, RoutedEventArgs e)
		{
			OpenConfig(ConfigWindowTab.Emulation);
		}

		private void OnNesConfigClick(object sender, RoutedEventArgs e)
		{
			OpenConfig(ConfigWindowTab.Nes);
		}

		private void OnSnesConfigClick(object sender, RoutedEventArgs e)
		{
			OpenConfig(ConfigWindowTab.Snes);
		}

		private void OnGameboyConfigClick(object sender, RoutedEventArgs e)
		{
			OpenConfig(ConfigWindowTab.Gameboy);
		}

		private void OnLogWindowClick(object sender, RoutedEventArgs e)
		{
			new LogWindow().ShowCentered((Window)VisualRoot);
		}

		private async void OnStartAudioRecordingClick(object sender, RoutedEventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filters = new List<FileDialogFilter>() {
				new FileDialogFilter() { Name = "Wave files", Extensions = { "wav" } },
				new FileDialogFilter() { Name = "All files", Extensions = { "*" } }
			};
			sfd.Directory = ConfigManager.WaveFolder;
			sfd.InitialFileName = EmuApi.GetRomInfo().GetRomName() + ".wav";

			string filename = await sfd.ShowAsync(VisualRoot as Window);
			if(filename != null) {
				RecordApi.WaveRecord(filename);
			}
		}

		private void OnStopAudioRecordingClick(object sender, RoutedEventArgs e)
		{
			RecordApi.WaveStop();
		}

		private void OnStartVideoRecordingClick(object sender, RoutedEventArgs e)
		{
			new VideoRecordWindow() {
				DataContext = new VideoRecordConfigViewModel()
			}.ShowCenteredDialog((Window)VisualRoot);
		}

		private void OnStopVideoRecordingClick(object sender, RoutedEventArgs e)
		{
			RecordApi.AviStop();
		}

		private void OnToolSubMenuOpened(object sender, RoutedEventArgs e)
		{
			_model.UpdateToolSubMenus();
		}

		private async void OnPlayMovieClick(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Directory = ConfigManager.MovieFolder;
			ofd.Filters = new List<FileDialogFilter>() {
				new FileDialogFilter() { Name = "Mesen Movies", Extensions = { "msm" } },
				new FileDialogFilter() { Name = "All files", Extensions = { "*" } }
			};

			string[] filenames = await ofd.ShowAsync((Window)VisualRoot);
			if(filenames?.Length > 0) {
				RecordApi.MoviePlay(filenames[0]);
			}
		}

		private void OnRecordMovieClick(object sender, RoutedEventArgs e)
		{
			new MovieRecordWindow() {
				DataContext = new MovieRecordConfigViewModel()
			}.ShowCenteredDialog((Window)VisualRoot);
		}

		private void OnStopMovieClick(object sender, RoutedEventArgs e)
		{
			RecordApi.MovieStop();
		}

		private void OnCheatsClick(object sender, RoutedEventArgs e)
		{
			//TODO
		}

		private void OnResetClick(object sender, RoutedEventArgs e)
		{
			EmuApi.Reset();
		}

		private void OnPowerCycleClick(object sender, RoutedEventArgs e)
		{
			EmuApi.PowerCycle();
		}

		private void OnPowerOffClick(object sender, RoutedEventArgs e)
		{
			EmuApi.Stop();
		}

		private void OnNetplayConnectClick(object sender, RoutedEventArgs e)
		{
			new NetplayConnectWindow() {
				DataContext = ConfigManager.Config.Netplay.Clone()
			}.ShowCenteredDialog((Window)VisualRoot);
		}

		private void OnNetplayDisconnectClick(object sender, RoutedEventArgs e)
		{
			NetplayApi.Disconnect();
		}

		private void OnNetplayStartServerClick(object sender, RoutedEventArgs e)
		{
			new NetplayStartServerWindow() {
				DataContext = ConfigManager.Config.Netplay.Clone()
			}.ShowCenteredDialog((Window)VisualRoot);
		}

		private void OnNetplayStopServerClick(object sender, RoutedEventArgs e)
		{
			NetplayApi.StopServer();
		}

		private void OnNetplaySelectControllerOpened(object sender, RoutedEventArgs e)
		{
			int availableControllers = NetplayApi.NetPlayGetAvailableControllers();
			int currentPort = NetplayApi.NetPlayGetControllerPort();

			MenuItem menu = ((MenuItem)sender);
			List<MenuItem> items = new List<MenuItem>();
			for(int i = 0; i < 5; i++) {
				MenuItem item = new MenuItem() {
					Header = ResourceHelper.GetMessage("Player") + " " + (i + 1) + " (" + ResourceHelper.GetEnumText(ConfigApi.GetControllerType(i)) + ")",
					Icon = currentPort == i ? ImageUtilities.FromAsset("Assets/MenuItemChecked.png") : null!,
					IsEnabled = (availableControllers & (1 << i)) != 0,
				};

				int player = i;
				item.Click += (_, _) => { NetplayApi.NetPlaySelectController(player); };
				items.Add(item);
			}
			menu.Items = items;
		}

		private void OnRegionMenuOpened(object sender, RoutedEventArgs e)
		{
			MenuItem menu = ((MenuItem)sender);
			List<IControl> items = new List<IControl>();

			ConsoleType consoleType = EmuApi.GetRomInfo().ConsoleType;
			ConsoleRegion region;
			switch(consoleType) {
				case ConsoleType.Snes: region = ConfigManager.Config.Snes.Region; break;
				case ConsoleType.Nes: region = ConfigManager.Config.Nes.Region; break;
				default: region = ConsoleRegion.Auto; break;
			}

			void AddRegion(ConsoleRegion r)
			{
				MenuItem item = new MenuItem() {
					Header = ResourceHelper.GetEnumText(r),
					Icon = region == r ? ImageUtilities.FromAsset("Assets/MenuItemChecked.png") : null!
				};

				item.Click += (_, _) => {
					switch(consoleType) {
						case ConsoleType.Snes:
							ConfigManager.Config.Snes.Region = r;
							ConfigManager.Config.Snes.ApplyConfig();
							ConfigManager.SaveConfig();
							break;

						case ConsoleType.Nes:
							ConfigManager.Config.Nes.Region = r;
							ConfigManager.Config.Nes.ApplyConfig();
							ConfigManager.SaveConfig();
							break;

						default: break;
					}
				};
				items.Add(item);
			}

			AddRegion(ConsoleRegion.Auto);
			items.Add(new Separator());
			AddRegion(ConsoleRegion.Ntsc);
			AddRegion(ConsoleRegion.Pal);
			if(consoleType == ConsoleType.Nes) {
				AddRegion(ConsoleRegion.Dendy);
			}

			menu.Items = items;
		}

		private async void OnInstallHdPackClick(object sender, RoutedEventArgs e)
		{
			Window window = (Window)VisualRoot;

			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filters = new List<FileDialogFilter>() { new FileDialogFilter() { Name = "ZIP files", Extensions = { "zip" } } };

			string[]? filenames = await ofd.ShowAsync(window);

			if(filenames == null || filenames.Length == 0) {
				return;
			}

			try {
				using(FileStream stream = File.Open(filenames[0], FileMode.Open)) {
					ZipArchive zip = new ZipArchive(stream);

					//Find the hires.txt file
					ZipArchiveEntry? hiresEntry = null;
					foreach(ZipArchiveEntry entry in zip.Entries) {
						if(entry.Name == "hires.txt") {
							hiresEntry = entry;
							break;
						}
					}

					if(hiresEntry == null) {
 						await MesenMsgBox.Show(window, "InstallHdPackInvalidPack", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
						
					using Stream entryStream = hiresEntry.Open();
					using StreamReader reader = new StreamReader(entryStream);
					string hiresData = reader.ReadToEnd();
					RomInfo romInfo = EmuApi.GetRomInfo();

					//If there's a "supportedRom" tag, check if it matches the current ROM
					Regex supportedRomRegex = new Regex("<supportedRom>([^\\n]*)");
					Match match = supportedRomRegex.Match(hiresData);
					if(match.Success) {
						if(!match.Groups[1].Value.ToUpper().Contains(romInfo.Sha1.ToUpper())) {
							await MesenMsgBox.Show(window, "InstallHdPackWrongRom", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}
					}

					//Extract HD pack
					try {
						string targetFolder = Path.Combine(ConfigManager.HdPackFolder, romInfo.GetRomName());
						if(Directory.Exists(targetFolder)) {
							//Warn if the folder already exists
							if(await MesenMsgBox.Show(window, "InstallHdPackConfirmOverwrite", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, targetFolder) != DialogResult.OK) {
								return;
							}
						} else {
							Directory.CreateDirectory(targetFolder);
						}

						string hiresFileFolder = hiresEntry.FullName.Substring(0, hiresEntry.FullName.Length - "hires.txt".Length);
						foreach(ZipArchiveEntry entry in zip.Entries) {
							//Extract only the files in the same subfolder as the hires.txt file (and only if they have a name & size > 0)
							if(!string.IsNullOrWhiteSpace(entry.Name) && entry.Length > 0 && entry.FullName.StartsWith(hiresFileFolder)) {
								entry.ExtractToFile(Path.Combine(targetFolder, entry.Name), true);
							}
						}
					} catch(Exception ex) {
						await MesenMsgBox.Show(window, "InstallHdPackError", MessageBoxButtons.OK, MessageBoxIcon.Error, ex.ToString());
						return;
					}
							
					//Turn on HD Pack support automatically after installation succeeds
					if(!ConfigManager.Config.Nes.EnableHdPacks) {
						ConfigManager.Config.Nes.EnableHdPacks = true;
						ConfigManager.Config.Nes.ApplyConfig();
						ConfigManager.SaveConfig();
					}

					if(await MesenMsgBox.Show(window, "InstallHdPackConfirmReset", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) {
						//Power cycle game if the user agrees
						EmuApi.PowerCycle();
					}
				}
			} catch {
				//Invalid file (file missing, not a zip file, etc.)
				await MesenMsgBox .Show(window, "InstallHdPackInvalidZipFile", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}