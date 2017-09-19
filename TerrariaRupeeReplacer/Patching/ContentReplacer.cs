using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TerrariaRupeeReplacer.Properties;
using TerrariaRupeeReplacer.Xnb;

namespace TerrariaRupeeReplacer.Patching {
	/**<summary>The available rupee colors.</summary>*/
	public enum RupeeColors {
		Green,
		Blue,
		Yellow,
		Red,
		Purple,
		Orange,
		Silver,
		Gold
	}

	/**<summary>The class for patching content.</summary>*/
	public static class ContentReplacer {
		//============ ENUMS =============
		#region Enums

		/**<summary>The available rupee image types to replace.</summary>*/
		public enum ImageTypes {
			Animation,
			Bullet,
			Dust,
			Falling,
			Item,
			Portal,
			Tile
		}

		#endregion
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The list of content files needed to be backed up.</summary>*/
		public static readonly string[] FilesToBackup = {
			"Images/Coin_0.xnb", // Copper Coin Animation
			"Images/Coin_1.xnb", // Silver Coin Animation
			"Images/Coin_2.xnb", // Gold Coin Animation
			"Images/Coin_3.xnb", // Platinum Coin Animation

			"Images/Dust.xnb", // All dust particles

			"Images/Item_71.xnb", // Copper Coin
			"Images/Item_72.xnb", // Silver Coin
			"Images/Item_73.xnb", // Gold Coin
			"Images/Item_74.xnb", // Platinum Coin

			"Images/Item_855.xnb", // Lucky Coin
			"Images/Item_905.xnb", // Coin Gun
			"Images/Item_3034.xnb", // Coin Ring

			"Images/Projectile_158.xnb", // Copper Coin Bullet
			"Images/Projectile_159.xnb", // Silver Coin Bullet
			"Images/Projectile_160.xnb", // Gold Coin Bullet
			"Images/Projectile_161.xnb", // Platinum Coin Bullet

			"Images/Projectile_411.xnb", // Falling Copper Coins
			"Images/Projectile_412.xnb", // Falling Silver Coins
			"Images/Projectile_413.xnb", // Falling Gold Coins
			"Images/Projectile_414.xnb", // Falling Platinum Coins

			"Images/Projectile_518.xnb", // Coin Portal

			"Images/Tiles_330.xnb", // Placed Copper Coins
			"Images/Tiles_331.xnb", // Placed Silver Coins
			"Images/Tiles_332.xnb", // Placed Gold Coins
			"Images/Tiles_333.xnb", // Placed Platinum Coins

			// Coin Pickup
			"Sounds/Coin_0.xnb",
			"Sounds/Coin_1.xnb",
			"Sounds/Coin_2.xnb",
			"Sounds/Coin_3.xnb",
			"Sounds/Coin_4.xnb",

			// Place Coins
			"Sounds/Coins.xnb",
		};

		/**<summary>The temporary directory to use.</summary>*/
		private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "Trigger", "RupeeReplacer");

		#endregion
		//========== PROPERTIES ==========
		#region Properties
		//--------------------------------
		#region Directories

		/**<summary>The Terraria content directory.</summary>*/
		private static string ContentDir {
			get { return Path.Combine(Patcher.ExeDirectory, "Content"); }
		}
		/**<summary>The backup Terraria content directory.</summary>*/
		private static string BackupDir {
			get { return Path.Combine(Patcher.ExeDirectory, "BackupContent"); }
		}
		/**<summary>The Terraria content image directory.</summary>*/
		private static string ImageDir {
			get { return Path.Combine(ContentDir, "Images"); }
		}
		/**<summary>The Terraria content sound directory.</summary>*/
		private static string SoundDir {
			get { return Path.Combine(ContentDir, "Sounds"); }
		}

		#endregion
		//--------------------------------
		#region Rupee Settings

		/**<summary>The copper coin rupee color.</summary>*/
		public static RupeeColors Copper { get; set; } = RupeeColors.Green;
		/**<summary>The silver coin rupee color.</summary>*/
		public static RupeeColors Silver { get; set; } = RupeeColors.Blue;
		/**<summary>The gold coin rupee color.</summary>*/
		public static RupeeColors Gold { get; set; } = RupeeColors.Red;
		/**<summary>The platinum coin rupee color.</summary>*/
		public static RupeeColors Platinum { get; set; } = RupeeColors.Purple;

		/**<summary>True if the Coin Gun is reprited.</summary>*/
		public static bool CoinGun { get; set; } = true;
		/**<summary>True if the Lucky Coin is reprited and renamed.</summary>*/
		public static bool LuckyCoin { get; set; } = true;
		/**<summary>True if the Coin Ring is reprited.</summary>*/
		public static bool CoinRing { get; set; } = true;
		/**<summary>True if the Coin Portal is reprited.</summary>*/
		public static bool CoinPortal { get; set; } = true;

		#endregion
		//--------------------------------
		#endregion
		//============ CONFIG ============
		#region Config

		/**<summary>Loads the current rupee configuration in Terraria.</summary>*/
		public static void LoadXmlConfiguration() {
			try {
				string configPath = Path.Combine(Patcher.ExeDirectory, CoinReplacer.ConfigName);

				XmlDocument doc = new XmlDocument();
				XmlNode node;
				XmlAttribute attribute;

				doc.Load(configPath);

				RupeeColors rupeeValue;
				bool boolValue;

				node = doc.SelectSingleNode("/RupeeReplacer/CopperCoin");
				attribute = (node != null ? node.Attributes["Color"] : null);
				if (attribute != null && Enum.TryParse(attribute.InnerText, out rupeeValue)) {
					Copper = rupeeValue;
				}
				node = doc.SelectSingleNode("/RupeeReplacer/SilverCoin");
				attribute = (node != null ? node.Attributes["Color"] : null);
				if (attribute != null && Enum.TryParse(attribute.InnerText, out rupeeValue)) {
					Silver = rupeeValue;
				}
				node = doc.SelectSingleNode("/RupeeReplacer/GoldCoin");
				attribute = (node != null ? node.Attributes["Color"] : null);
				if (attribute != null && Enum.TryParse(attribute.InnerText, out rupeeValue)) {
					Gold = rupeeValue;
				}
				node = doc.SelectSingleNode("/RupeeReplacer/PlatinumCoin");
				attribute = (node != null ? node.Attributes["Color"] : null);
				if (attribute != null && Enum.TryParse(attribute.InnerText, out rupeeValue)) {
					Platinum = rupeeValue;
				}

				node = doc.SelectSingleNode("/RupeeReplacer/CoinGun");
				attribute = (node != null ? node.Attributes["Enabled"] : null);
				if (attribute != null && bool.TryParse(attribute.InnerText, out boolValue)) {
					CoinGun = boolValue;
				}
				node = doc.SelectSingleNode("/RupeeReplacer/LuckyCoin");
				attribute = (node != null ? node.Attributes["Enabled"] : null);
				if (attribute != null && bool.TryParse(attribute.InnerText, out boolValue)) {
					LuckyCoin = boolValue;
				}
				node = doc.SelectSingleNode("/RupeeReplacer/CoinRing");
				attribute = (node != null ? node.Attributes["Enabled"] : null);
				if (attribute != null && bool.TryParse(attribute.InnerText, out boolValue)) {
					CoinRing = boolValue;
				}
				node = doc.SelectSingleNode("/RupeeReplacer/CoinPortal");
				attribute = (node != null ? node.Attributes["Enabled"] : null);
				if (attribute != null && bool.TryParse(attribute.InnerText, out boolValue)) {
					CoinPortal = boolValue;
				}
			}
			catch { }
		}
		/**<summary>Saves the xml to be modified for use in Terraria.</summary>*/
		public static void SaveXmlConfiguration() {
			try {
				string configPath = Path.Combine(Patcher.ExeDirectory, CoinReplacer.ConfigName);

				XmlDocument doc = new XmlDocument();
				doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));

				XmlElement replacer = doc.CreateElement("RupeeReplacer");
				doc.AppendChild(replacer);

				XmlElement element = doc.CreateElement("CopperCoin");
				element.SetAttribute("Color", Copper.ToString());
				replacer.AppendChild(element);

				element = doc.CreateElement("SilverCoin");
				element.SetAttribute("Color", Silver.ToString());
				replacer.AppendChild(element);

				element = doc.CreateElement("GoldCoin");
				element.SetAttribute("Color", Gold.ToString());
				replacer.AppendChild(element);

				element = doc.CreateElement("PlatinumCoin");
				element.SetAttribute("Color", Platinum.ToString());
				replacer.AppendChild(element);


				element = doc.CreateElement("CoinGun");
				element.SetAttribute("Enabled", CoinGun.ToString());
				replacer.AppendChild(element);

				element = doc.CreateElement("LuckyCoin");
				element.SetAttribute("Enabled", LuckyCoin.ToString());
				replacer.AppendChild(element);

				element = doc.CreateElement("CoinRing");
				element.SetAttribute("Enabled", CoinRing.ToString());
				replacer.AppendChild(element);

				element = doc.CreateElement("CoinPortal");
				element.SetAttribute("Enabled", CoinPortal.ToString());
				replacer.AppendChild(element);

				doc.Save(configPath);
			}
			catch (Exception ex) {
				throw new Exception("Failed to save " + CoinReplacer.ConfigName, ex);
			}
		}

		#endregion
		//========== REPLACING ===========
		#region Replacing

		/**<summary>Create images displaying all of the rupees.</summary>*/
		/*static ContentReplacer() {
			int spacing = 14;
			int heightOffset = 4;
			var rupeeColors = (RupeeColors[])Enum.GetValues(typeof(RupeeColors));
			List<Bitmap> rupeeImages = new List<Bitmap>();
			foreach (RupeeColors color in rupeeColors) {
				rupeeImages.Add(GetImage(ImageTypes.Animation, color));
			}
			for (int i = 0; i < 4; i++) {
				Bitmap gBmp = new Bitmap(spacing + rupeeColors.Length * (10 + spacing), 22 + heightOffset * 2, PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage(gBmp)) {
					g.Clear(Color.White);
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
					for (int j = 0; j < rupeeColors.Length; j++) {
						g.DrawImage(
							rupeeImages[j],
							spacing + j * (10 + spacing), heightOffset,
							new Rectangle(0, 2 + ((i + j) % 4) * 24, 10, 22),
							GraphicsUnit.Pixel
						);
					}
				}
				gBmp.Save("Rupees" + i + ".png", ImageFormat.Png);
			}
			Bitmap gBmp2 = new Bitmap(spacing + rupeeColors.Length * (10 + spacing), 22 + heightOffset * 2, PixelFormat.Format32bppArgb);
			using (Graphics g = Graphics.FromImage(gBmp2)) {
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				for (int j = 0; j < rupeeColors.Length; j++) {
					g.DrawImage(
						rupeeImages[j],
						spacing + j * (10 + spacing), heightOffset,
						new Rectangle(0, 2, 10, 22),
						GraphicsUnit.Pixel
					);
				}
			}
			gBmp2.Save("Rupees.png", ImageFormat.Png);
		}*/
		/**<summary>Restores the backed up content files.</summary>*/
		public static bool Restore() {
			bool someMissing = false;
			foreach (string file in FilesToBackup) {
				string content = Path.Combine(ContentDir, file);
				string backup = Path.Combine(BackupDir, file);
				if (File.Exists(backup)) {
					File.Copy(backup, content, true);
				}
				else if (!someMissing) {
					someMissing = true;
				}
			}
			return someMissing;
		}
		/**<summary>Replaces the coin content with the specified colored rupee content.</summary>*/
		public static void Replace() {
			BackupContent();

			Restore();

			// Replace normal images
			// Coin Animations
			ReplaceImage(ImageTypes.Animation, Copper, "Coin_0.xnb");
			ReplaceImage(ImageTypes.Animation, Silver, "Coin_1.xnb");
			ReplaceImage(ImageTypes.Animation, Gold, "Coin_2.xnb");
			ReplaceImage(ImageTypes.Animation, Platinum, "Coin_3.xnb");

			// Coin Sprites
			ReplaceImage(ImageTypes.Item, Copper, "Item_71.xnb");
			ReplaceImage(ImageTypes.Item, Silver, "Item_72.xnb");
			ReplaceImage(ImageTypes.Item, Gold, "Item_73.xnb");
			ReplaceImage(ImageTypes.Item, Platinum, "Item_74.xnb");

			// Coin Gun Bullets
			ReplaceImage(ImageTypes.Bullet, Copper, "Projectile_158.xnb");
			ReplaceImage(ImageTypes.Bullet, Silver, "Projectile_159.xnb");
			ReplaceImage(ImageTypes.Bullet, Gold, "Projectile_160.xnb");
			ReplaceImage(ImageTypes.Bullet, Platinum, "Projectile_161.xnb");

			// Falling Coins
			ReplaceImage(ImageTypes.Falling, Copper, "Projectile_411.xnb");
			ReplaceImage(ImageTypes.Falling, Silver, "Projectile_412.xnb");
			ReplaceImage(ImageTypes.Falling, Gold, "Projectile_413.xnb");
			ReplaceImage(ImageTypes.Falling, Platinum, "Projectile_414.xnb");

			// Coin Piles
			ReplaceImage(ImageTypes.Tile, Copper, "Tiles_330.xnb");
			ReplaceImage(ImageTypes.Tile, Silver, "Tiles_331.xnb");
			ReplaceImage(ImageTypes.Tile, Gold, "Tiles_332.xnb");
			ReplaceImage(ImageTypes.Tile, Platinum, "Tiles_333.xnb");

			// Conditional
			if (CoinGun)	ReplaceImage("RupeeGun", "Item_905.xnb");
			if (LuckyCoin)	ReplaceImage("LuckyRupee", "Item_855.xnb");
			if (CoinRing)	ReplaceImage("RupeeRing", "Item_3034.xnb");
			if (CoinPortal)	ReplaceImage(ImageTypes.Portal, Gold, "Projectile_518.xnb");

			// Dust Particles
			ReplaceDust();

			// Coin Pickup
			ReplaceSound("Collect1", "Coin_0.xnb");
			ReplaceSound("Collect1", "Coin_1.xnb");
			ReplaceSound("Collect1", "Coin_2.xnb");
			ReplaceSound("Collect2", "Coin_3.xnb");
			ReplaceSound("Collect2", "Coin_4.xnb");

			// Coin Pile Place
			ReplaceSound("Place", "Coins.xnb");
		}
		/**<summary>Replaces the coin textures in the dust texture.</summary>*/
		private static void ReplaceDust() {
			Image dustBmp = XnbExtractor.ExtractBitmap(Path.Combine(ImageDir, "Dust.xnb"));

			Bitmap gBmp = new Bitmap(dustBmp);
			using (Graphics g = Graphics.FromImage(gBmp)) {
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				Point point = new Point(440, 60);
				int spacing = 10;
				g.DrawImage(GetImage(ImageTypes.Dust, Copper), point);		point.X += spacing;
				g.DrawImage(GetImage(ImageTypes.Dust, Silver), point);		point.X += spacing;
				g.DrawImage(GetImage(ImageTypes.Dust, Gold), point);		point.X += spacing;
				g.DrawImage(GetImage(ImageTypes.Dust, Platinum), point);	point.X += spacing;
			}
			PngConverter.Convert(gBmp, Path.Combine(ImageDir, "Dust.xnb"));
		}
		/**<summary>Backs up the Terraria content that gets replaced.</summary>*/
		private static void BackupContent() {
			Directory.CreateDirectory(Path.Combine(BackupDir, "Images"));
			Directory.CreateDirectory(Path.Combine(BackupDir, "Sounds"));
			foreach (string file in FilesToBackup) {
				string content = Path.Combine(ContentDir, file);
				string backup = Path.Combine(BackupDir, file);
				if (!File.Exists(backup) && File.Exists(content)) {
					File.Copy(content, backup, true);
				}
			}
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Replaces an image content file.</summary>*/
		private static void ReplaceImage(string name, string outputFile) {
			PngConverter.Convert(GetImage(name), Path.Combine(ImageDir, outputFile));
		}
		/**<summary>Replaces an image content file.</summary>*/
		private static void ReplaceImage(ImageTypes type, RupeeColors color, string outputFile) {
			PngConverter.Convert(GetImage(type, color), Path.Combine(ImageDir, outputFile));
		}
		/**<summary>Replaces a sound content file.</summary>*/
		private static void ReplaceSound(string name, string outputFile) {
			WavConverter.Convert(GetSound(name), Path.Combine(SoundDir, outputFile));
		}
		/**<summary>Gets an image resource.</summary>*/
		private static Bitmap GetImage(string name) {
			ResourceManager rm = new ResourceManager("TerrariaRupeeReplacer.Properties.Resources", typeof(Resources).Assembly);
			return (Bitmap)rm.GetObject(name);
		}
		/**<summary>Gets an image resource.</summary>*/
		private static Bitmap GetImage(ImageTypes type, RupeeColors color) {
			ResourceManager rm = new ResourceManager("TerrariaRupeeReplacer.Properties.Resources", typeof(Resources).Assembly);
			return (Bitmap)rm.GetObject("Rupee" + type + color);
		}
		/**<summary>Gets a sound resource.</summary>*/
		private static Stream GetSound(string name) {
			ResourceManager rm = new ResourceManager("TerrariaRupeeReplacer.Properties.Resources", typeof(Resources).Assembly);
			var soundStream = (Stream)rm.GetObject("Rupee" + name);
			soundStream.Position = 0;
			return soundStream;
		}

		#endregion
	}
}
