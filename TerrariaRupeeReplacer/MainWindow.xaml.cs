using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TerrariaRupeeReplacer.Patching;
using TerrariaRupeeReplacer.Windows;
using Path = System.IO.Path;
using File = System.IO.File;
using Directory = System.IO.Directory;
using TerrariaRupeeReplacer.Properties;
using Microsoft.Win32;
using System.Xml;
using System.Diagnostics;
using System.Timers;
using TerrariaRupeeReplacer.Util;

namespace TerrariaRupeeReplacer {
	/**<summary>The main window running Terraria Item Modifier.</summary>*/
	public partial class MainWindow : Window {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The lowest supported version of Vanilla Terraria.</summary>*/
		private static readonly Version VanillaSupportedVersionLow = new Version("1.3.5.3");
		/**<summary>The highest supported version of Vanilla Terraria.</summary>*/
		private static readonly Version VanillaSupportedVersionHigh = new Version("1.3.5.3");
		
		/**<summary>The lowest supported version of tModLoader Terraria.</summary>*/
		private static readonly Version TModSupportedVersionLow = new Version("1.3.5.1");
		/**<summary>The highest supported version of tModLoader Terraria.</summary>*/
		private static readonly Version TModSupportedVersionHigh = new Version("1.3.5.1");
		/**<summary>The supported version of tModLoader.</summary>*/
		private static readonly Version TModSupportedVersion = new Version("0.10.1.0");

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>The lowest supported version of the selected Terraria.</summary>*/
		private Version SupportedVersionLow {
			get { return (Patcher.IsTMod ? TModSupportedVersionLow : VanillaSupportedVersionLow); }
		}
		/**<summary>The highest supported version of the selected Terraria.</summary>*/
		private Version SupportedVersionHigh {
			get { return (Patcher.IsTMod ? TModSupportedVersionHigh : VanillaSupportedVersionHigh); }
		}
		/**<summary>The name of the executable type.</summary>*/
		private string TypeName {
			get { return (Patcher.IsTMod ? "tModLoader Terraria" : "Vanilla Terraria"); }
		}

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The timer for the portal animation.</summary>*/
		private Timer portalTimer = new Timer(1000 / 60 * 6);
		/**<summary>The current animation frame of the portal.</summary>*/
		private int portalFrame = 0;

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the main window.</summary>*/
		public MainWindow() {
			InitializeComponent();

			LoadSettings();

			// Setup portal animation timer
			portalTimer.AutoReset = true;
			portalTimer.Elapsed += OnPortalTimer;

			// Setup version in title
			this.Title += " - v" + VanillaSupportedVersionHigh.ToString();

			// Setup supported version tooltips
			string vanillaText = "Supports: v" + VanillaSupportedVersionLow;
			if (VanillaSupportedVersionLow != VanillaSupportedVersionHigh)
				vanillaText += " to v" + VanillaSupportedVersionHigh;
			radioButtonVanilla.ToolTip = vanillaText;

			string tmodText = "Supports: v" + TModSupportedVersionLow;
			if (TModSupportedVersionLow != TModSupportedVersionHigh)
				radioButtonTMod.ToolTip = " to v" + TModSupportedVersionHigh;
			tmodText += " (v" + TModSupportedVersion + ")";
			radioButtonTMod.ToolTip = tmodText;

			// Disable drag/drop text in textboxes so you can scroll their contents easily
			DataObject.AddCopyingHandler(textBoxExe, OnTextBoxCancelDrag);

			// Remove quotes from "Copy Path" command on paste
			DataObject.AddPastingHandler(textBoxExe, OnTextBoxQuotesPaste);
		}

		#endregion
		//=========== SETTINGS ===========
		#region Settings

		/**<summary>Loads the application settings.</summary>*/
		private void LoadSettings() {
			Patcher.ExePath = Settings.Default.ExePath;
			if (string.IsNullOrEmpty(Patcher.ExePath)) {
				Patcher.ExePath = "";
				if (!string.IsNullOrEmpty(TerrariaLocator.TerrariaPath)) {
					Patcher.ExePath = TerrariaLocator.TerrariaPath;
				}
			}
			Patcher.IsTMod = Settings.Default.IsTMod;

			var rupeeColors = (RupeeColors[])Enum.GetValues(typeof(RupeeColors));
			foreach (RupeeColors color in rupeeColors) {
				if (Settings.Default.Copper == color.ToString())
					ContentReplacer.Copper = color;
				if (Settings.Default.Silver == color.ToString())
					ContentReplacer.Silver = color;
				if (Settings.Default.Gold == color.ToString())
					ContentReplacer.Gold = color;
				if (Settings.Default.Platinum == color.ToString())
					ContentReplacer.Platinum = color;

				AddComboBoxItem(comboBoxCopper, color);
				AddComboBoxItem(comboBoxSilver, color);
				AddComboBoxItem(comboBoxGold, color);
				AddComboBoxItem(comboBoxPlat, color);
			}

			ContentReplacer.CoinGun = Settings.Default.CoinGun;
			ContentReplacer.LuckyCoin = Settings.Default.LuckyCoin;
			ContentReplacer.CoinRing = Settings.Default.CoinRing;
			ContentReplacer.CoinPortal = Settings.Default.CoinPortal;

			// Try and load Terraria's current rupee configuration
			ContentReplacer.LoadXmlConfiguration();

			// Setup controls
			textBoxExe.Text = Patcher.ExePath;

			if (Patcher.IsTMod)
				radioButtonTMod.IsChecked = true;
			else
				radioButtonVanilla.IsChecked = true;

			if (ContentReplacer.CoinPortal)
				portalTimer.Start();

			comboBoxCopper.SelectedIndex = (int)ContentReplacer.Copper;
			comboBoxSilver.SelectedIndex = (int)ContentReplacer.Silver;
			comboBoxGold.SelectedIndex = (int)ContentReplacer.Gold;
			comboBoxPlat.SelectedIndex = (int)ContentReplacer.Platinum;

			checkBoxRupeeGun.IsChecked = ContentReplacer.CoinGun;
			checkBoxLuckyRupee.IsChecked = ContentReplacer.LuckyCoin;
			checkBoxRupeeRing.IsChecked = ContentReplacer.CoinRing;
			checkBoxRupeePortal.IsChecked = ContentReplacer.CoinPortal;

			imagePortal.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Content/Images/RupeePortal" + ContentReplacer.Gold.ToString() + ".png"));
		}
		/**<summary>Saves the application settings.</summary>*/
		private void SaveSettings() {
			//UpdateRupeeSettings();
			Settings.Default.ExePath	= Patcher.ExePath;
			Settings.Default.IsTMod     = Patcher.IsTMod;

			Settings.Default.Copper		= ContentReplacer.Copper.ToString();
			Settings.Default.Silver		= ContentReplacer.Silver.ToString();
			Settings.Default.Gold		= ContentReplacer.Gold.ToString();
			Settings.Default.Platinum	= ContentReplacer.Platinum.ToString();

			Settings.Default.CoinGun = ContentReplacer.CoinGun;
			Settings.Default.LuckyCoin = ContentReplacer.LuckyCoin;
			Settings.Default.CoinRing = ContentReplacer.CoinRing;
			Settings.Default.CoinPortal = ContentReplacer.CoinPortal;
			Settings.Default.Save();
		}
		

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Creates a rupee combobox item.</summary>*/
		private void AddComboBoxItem(ComboBox comboBox, RupeeColors color) {
			ComboBoxItem item = new ComboBoxItem();
			item.Height = 24;

			Grid grid = new Grid();
			ColumnDefinition c0 = new ColumnDefinition();
			c0.Width = new GridLength(20);
			ColumnDefinition c1 = new ColumnDefinition();
			c1.Width = new GridLength(1, GridUnitType.Star);
			grid.ColumnDefinitions.Add(c0);
			grid.ColumnDefinitions.Add(c1);

			Image rupeeImage = new Image();
			rupeeImage.HorizontalAlignment = HorizontalAlignment.Center;
			rupeeImage.VerticalAlignment = VerticalAlignment.Center;
			rupeeImage.Width = 10;
			rupeeImage.Height = 22;
			rupeeImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Content/Images/RupeeItem" + color.ToString() + ".png"));
			grid.Children.Add(rupeeImage);
			Grid.SetColumn(rupeeImage, 0);

			TextBlock rupeeName = new TextBlock();
			rupeeName.HorizontalAlignment = HorizontalAlignment.Left;
			rupeeName.VerticalAlignment = VerticalAlignment.Center;
			rupeeName.Text = color.ToString();
			rupeeName.Padding = new Thickness(4, 0, 0, 0);
			grid.Children.Add(rupeeName);
			Grid.SetColumn(rupeeName, 1);

			item.Content = grid;

			comboBox.Items.Add(item);

			imagePortal.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Content/Images/RupeePortal" + ContentReplacer.Gold.ToString() + ".png"));
		}
		/**<summary>Checks if the path is valid.</summary>*/
		private bool ValidPathTest() {
			if (Patcher.ExePath == "") {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The Terraria path cannot be empty!", "Invalid Path");
				return false;
			}
			try {
				Path.GetDirectoryName(Patcher.ExePath);
				return true;
			}
			catch (ArgumentException) {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "You must enter a valid Terraria path!", "Invalid Path");
				return false;
			}
		}
		/**<summary>Checks if the path is valid.</summary>*/
		private bool CheckSupportedVersion(string exePath) {
			Version version = IL.GetAssemblyVersion(exePath);
			MessageBoxResult result = MessageBoxResult.Yes;
			if (version < SupportedVersionLow) {
				result = TriggerMessageBox.Show(this, MessageIcon.Warning,
					"The Terraria executable version (v" + version.ToString() + ") is lower than the least " +
					"supported version of " + TypeName + " (v" + SupportedVersionLow.ToString() + "). " +
					"Are you sure you still want to patch?", "Unsupported Version", MessageBoxButton.YesNo);
			}
			else if (version > SupportedVersionHigh) {
				result = TriggerMessageBox.Show(this, MessageIcon.Warning,
					"The Terraria executable version (v" + version.ToString() + ") is higher than the highest " +
					"supported version of " + TypeName + " (v" + SupportedVersionHigh.ToString() + "). " +
					"Are you sure you still want to patch?", "Unsupported Version", MessageBoxButton.YesNo);
			}
			return (result == MessageBoxResult.Yes);
		}

		#endregion
		//============ EVENTS ============
		#region Events
		//--------------------------------
		#region Regular

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			portalTimer.Stop();
			SaveSettings();
		}
		private void OnPortalTimer(object sender, ElapsedEventArgs e) {
			Dispatcher.Invoke(() => {
				portalFrame = (portalFrame + 1) % 4;
				imagePortal.Viewbox = new Rect(0, portalFrame * 34, 32, 32);
			});
		}
		private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
			// Make text boxes lose focus on click away
			FocusManager.SetFocusedElement(this, this);
		}
		private void OnTextBoxCancelDrag(object sender, DataObjectCopyingEventArgs e) {
			if (e.IsDragDrop)
				e.CancelCommand();
		}
		private void OnTextBoxQuotesPaste(object sender, DataObjectPastingEventArgs e) {
			var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
			if (!isText) return;

			var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
			if (text.StartsWith("\"") || text.EndsWith("\"")) {
				text = text.Trim('"');
				Clipboard.SetText(text);
			}
		}

		#endregion
		//--------------------------------
		#region Patching

		private void OnPatch(object sender = null, RoutedEventArgs e = null) {
			MessageBoxResult result;
			if (!ValidPathTest())
				return;
			if (!File.Exists(Patcher.ExePath)) {
				TriggerMessageBox.Show(this, MessageIcon.Error, "Could not find Terraria executable!", "Missing Exe");
				return;
			}
			result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to patch the current Terraria executable?", "Patch Terraria", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.No)
				return;
			if (!CheckSupportedVersion(Patcher.ExePath))
				return;
			try {
				Patcher.Patch();
				ContentReplacer.Replace();
				ContentReplacer.SaveXmlConfiguration();
				TriggerMessageBox.Show(this, MessageIcon.Info, "Terraria successfully patched!", "Terraria Patched");
			}
			catch (AlreadyPatchedException) {
				TriggerMessageBox.Show(this, MessageIcon.Error, "This executable has already been patched by Rupee Replacer! Use Restore & Patch instead.", "Already Patched");
			}
			catch (Exception ex) {
				result = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while patching Terraria! Would you like to see the error?", "Patch Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(ex, true);
			}
		}
		private void OnRestore(object sender = null, RoutedEventArgs e = null) {
			MessageBoxResult result;
			bool cleanup = false;
			if (!ValidPathTest())
				return;
			if (!File.Exists(Patcher.BackupPath)) {
				TriggerMessageBox.Show(this, MessageIcon.Error, "Could not find Terraria backup!", "Missing Backup");
				return;
			}
			result = TriggerMessageBox.Show(this, MessageIcon.Question, "Would you like to restore the current Terraria executable to its backup and cleanup the required files or just restore the backup?", "Restore Terraria", MessageBoxButton.YesNoCancel, "Cleanup & Restore", "Restore Only");
			if (result == MessageBoxResult.Cancel)
				return;
			cleanup = result == MessageBoxResult.Yes;
			if (File.Exists(Patcher.ExePath) && IL.GetAssemblyVersion(Patcher.BackupPath) < IL.GetAssemblyVersion(Patcher.ExePath)) {
				result = TriggerMessageBox.Show(this, MessageIcon.Warning, "The backed up Terraria executable is an older game version. Are you sure you want to restore it?", "Older Version", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.No)
					return;
			}
			try {
				Patcher.Restore(cleanup);
				bool someMissing = ContentReplacer.Restore();
				// Clean up directory and remove config file
				if (cleanup) {
					string configPath = Path.Combine(Patcher.ExeDirectory, CoinReplacer.ConfigName);
					string logPath = Path.Combine(Patcher.ExeDirectory, ErrorLogger.LogName);
					if (File.Exists(configPath))
						File.Delete(configPath);
					if (File.Exists(logPath))
						File.Delete(logPath);
				}
				if (someMissing)
					TriggerMessageBox.Show(this, MessageIcon.Info, "Terraria executable restored but some backup content files were missing!", "Missing Content");
				else
					TriggerMessageBox.Show(this, MessageIcon.Info, "Terraria successfully restored!", "Terraria Restored");
			}
			catch (Exception ex) {
				result = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while restoring Terraria! Would you like to see the error?", "Restore Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(ex, true);
			}
		}
		private void OnRestoreAndPatch(object sender, RoutedEventArgs e) {
			MessageBoxResult result;
			if (!ValidPathTest())
				return;
			if (!File.Exists(Patcher.BackupPath)) {
				TriggerMessageBox.Show(this, MessageIcon.Error, "Could not find Terraria backup!", "Missing Backup");
				return;
			}
			result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to restore Terraria from its backup and then patch it?", "Patch & Restore Terraria", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.No)
				return;
			if (File.Exists(Patcher.ExePath) && IL.GetAssemblyVersion(Patcher.BackupPath) < IL.GetAssemblyVersion(Patcher.ExePath)) {
				result = TriggerMessageBox.Show(this, MessageIcon.Warning, "The backed up Terraria executable is an older game version. Are you sure you want to restore it?", "Older Version", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.No)
					return;
			}
			if (!CheckSupportedVersion(Patcher.BackupPath))
				return;
			try {
				Patcher.Restore(false);
			}
			catch (Exception ex) {
				result = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while restoring Terraria! Would you like to see the error?", "Restore Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(ex, true);
				return;
			}
			try {
				Patcher.Patch();
				ContentReplacer.Replace();
				ContentReplacer.SaveXmlConfiguration();
				TriggerMessageBox.Show(this, MessageIcon.Info, "Terraria successfully restored and patched!", "Terraria Repatched");
			}
			catch (AlreadyPatchedException) {
				TriggerMessageBox.Show(this, MessageIcon.Error, "The backup executable has already been patched by Rupee Replacer!", "Already Patched");
			}
			catch (Exception ex) {
				result = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while patching Terraria! Would you like to see the error?", "Patch Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(ex, true);
			}
		}
		private void OnUpdateContent(object sender, RoutedEventArgs e) {
			MessageBoxResult result;
			if (!ValidPathTest())
				return;
			result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to update the rupee content files?", "Update Rupees", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.No)
				return;
			try {
				ContentReplacer.Replace();
				ContentReplacer.SaveXmlConfiguration();
				TriggerMessageBox.Show(this, MessageIcon.Info, "Rupee content files successfully updated!", "Rupees Updated");
			}
			catch (Exception ex) {
				result = TriggerMessageBox.Show(this, MessageIcon.Error, "An error occurred while updating rupee content files! Would you like to see the error?", "Patch Error", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
					ErrorMessageBox.Show(ex, true);
				return;
			}
		}

		#endregion
		//--------------------------------
		#region Settings

		private void OnExeBrowse(object sender, RoutedEventArgs e) {
			OpenFileDialog fileDialog = new OpenFileDialog();

			fileDialog.Title = "Find Terraria Executable";
			fileDialog.AddExtension = true;
			fileDialog.DefaultExt = ".exe";
			fileDialog.Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*";
			fileDialog.FilterIndex = 0;
			fileDialog.CheckFileExists = true;
			try {
				fileDialog.InitialDirectory = Path.GetFullPath(Patcher.ExeDirectory);
			}
			catch { }
			var result = fileDialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				Patcher.ExePath = fileDialog.FileName;
				textBoxExe.Text = fileDialog.FileName;
				SaveSettings();
			}
		}
		private void OnExeChanged(object sender, TextChangedEventArgs e) {
			Patcher.ExePath = textBoxExe.Text;
		}
		private void OnVanillaSelected(object sender, RoutedEventArgs e) {
			Patcher.IsTMod = false;
		}
		private void OnTModSelected(object sender, RoutedEventArgs e) {
			Patcher.IsTMod = true;
		}
		private void OnCopperCoinChanged(object sender, SelectionChangedEventArgs e) {
			ContentReplacer.Copper = (RupeeColors)comboBoxCopper.SelectedIndex;
		}
		private void OnSilverCoinChanged(object sender, SelectionChangedEventArgs e) {
			ContentReplacer.Silver = (RupeeColors)comboBoxSilver.SelectedIndex;
		}
		private void OnGoldCoinChanged(object sender, SelectionChangedEventArgs e) {
			ContentReplacer.Gold = (RupeeColors)comboBoxGold.SelectedIndex;
			imagePortal.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Content/Images/RupeePortal" + ContentReplacer.Gold.ToString() + ".png"));
		}
		private void OnPlatinumCoinChanged(object sender, SelectionChangedEventArgs e) {
			ContentReplacer.Platinum = (RupeeColors)comboBoxPlat.SelectedIndex;
		}
		private void OnRupeeGunChecked(object sender, RoutedEventArgs e) {
			ContentReplacer.CoinGun = checkBoxRupeeGun.IsChecked.Value;
		}
		private void OnLuckyRupeeChecked(object sender, RoutedEventArgs e) {
			ContentReplacer.LuckyCoin = checkBoxLuckyRupee.IsChecked.Value;
		}
		private void OnRupeeRingChecked(object sender, RoutedEventArgs e) {
			ContentReplacer.CoinRing = checkBoxRupeeRing.IsChecked.Value;
		}
		private void OnRupeePortalChecked(object sender, RoutedEventArgs e) {
			ContentReplacer.CoinPortal = checkBoxRupeePortal.IsChecked.Value;
			if (ContentReplacer.CoinPortal)
				portalTimer.Start();
			else
				portalTimer.Stop();
		}

		#endregion
		//--------------------------------
		#region Menu Items

		private void OnLaunchTerraria(object sender, RoutedEventArgs e) {
			try {
				if (File.Exists(Patcher.ExePath)) {
					ProcessStartInfo start = new ProcessStartInfo();
					start.FileName = Patcher.ExePath;
					start.Arguments = TerrariaLocator.FindTerraLauncherSaveDirectory(Patcher.ExePath);
					start.WorkingDirectory = Patcher.ExeDirectory;
					Process.Start(start);
				}
				else {
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not locate the Terraria executable! Cannot launch Terraria.", "Missing Executable");
				}
			}
			catch {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The current path to Terraria is invalid! Cannot launch Terraria.", "Invalid Path");
			}
		}
		private void OnOpenTerrariaFolder(object sender, RoutedEventArgs e) {
			try {
				if (Directory.Exists(Patcher.ExeDirectory))
					Process.Start(Patcher.ExeDirectory);
				else
					TriggerMessageBox.Show(this, MessageIcon.Warning, "Could not locate the Terraria folder! Cannot open folder.", "Missing Folder");
			}
			catch {
				TriggerMessageBox.Show(this, MessageIcon.Warning, "The current path to Terraria is invalid! Cannot open folder.", "Invalid Path");
			}
		}
		private void OnExit(object sender, RoutedEventArgs e) {
			Close();
		}

		private void OnAbout(object sender, RoutedEventArgs e) {
			AboutWindow.Show(this);
		}
		private void OnHelp(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaRupeeReplacer/wiki");
		}
		private void OnCredits(object sender, RoutedEventArgs e) {
			CreditsWindow.Show(this);
		}
		private void OnViewOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start("https://github.com/trigger-death/TerrariaRupeeReplacer");
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
