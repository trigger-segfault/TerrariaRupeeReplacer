using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using TerrariaRupeeReplacer.Properties;
using TerrariaRupeeReplacer.Xnb;

namespace TerrariaRupeeReplacer.Patching {
	/**<summary>The class for patching content.</summary>*/
	public static class ContentReplacer {
		//============ ENUMS =============
		#region Enums

		/**<summary>The available rupee image types to replace.</summary>*/
		public enum ImageTypes {
			Animation,
			Dust,
			Falling,
			Item,
			Tile
		}

		#endregion
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The list of content files needed to be backed up.</summary>*/
		public static readonly string[] FilesToBackup = {
			"Images/Coin_0.xnb",
			"Images/Coin_1.xnb",
			"Images/Coin_2.xnb",
			"Images/Coin_3.xnb",

			"Images/Dust.xnb",

			"Images/Item_71.xnb",
			"Images/Item_72.xnb",
			"Images/Item_73.xnb",
			"Images/Item_74.xnb",

			"Images/Projectile_411.xnb",
			"Images/Projectile_412.xnb",
			"Images/Projectile_413.xnb",
			"Images/Projectile_414.xnb",

			"Images/Tiles_330.xnb",
			"Images/Tiles_331.xnb",
			"Images/Tiles_332.xnb",
			"Images/Tiles_333.xnb",

			"Sounds/Coin_0.xnb",
			"Sounds/Coin_1.xnb",
			"Sounds/Coin_2.xnb",
			"Sounds/Coin_3.xnb",
			"Sounds/Coin_4.xnb",
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
		#region Rupee Colors

		/**<summary>The copper coin rupee color.</summary>*/
		public static RupeeColors Copper { get; set; } = RupeeColors.Green;
		/**<summary>The silver coin rupee color.</summary>*/
		public static RupeeColors Silver { get; set; } = RupeeColors.Blue;
		/**<summary>The gold coin rupee color.</summary>*/
		public static RupeeColors Gold { get; set; } = RupeeColors.Red;
		/**<summary>The platinum coin rupee color.</summary>*/
		public static RupeeColors Platinum { get; set; } = RupeeColors.Purple;

		#endregion
		//--------------------------------
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

			// Replace normal images
			ReplaceImage(ImageTypes.Animation, Copper, "Coin_0.xnb");
			ReplaceImage(ImageTypes.Animation, Silver, "Coin_1.xnb");
			ReplaceImage(ImageTypes.Animation, Gold, "Coin_2.xnb");
			ReplaceImage(ImageTypes.Animation, Platinum, "Coin_3.xnb");

			ReplaceImage(ImageTypes.Item, Copper, "Item_71.xnb");
			ReplaceImage(ImageTypes.Item, Silver, "Item_72.xnb");
			ReplaceImage(ImageTypes.Item, Gold, "Item_73.xnb");
			ReplaceImage(ImageTypes.Item, Platinum, "Item_74.xnb");

			ReplaceImage(ImageTypes.Falling, Copper, "Projectile_411.xnb");
			ReplaceImage(ImageTypes.Falling, Silver, "Projectile_412.xnb");
			ReplaceImage(ImageTypes.Falling, Gold, "Projectile_413.xnb");
			ReplaceImage(ImageTypes.Falling, Platinum, "Projectile_414.xnb");

			ReplaceImage(ImageTypes.Tile, Copper, "Tiles_330.xnb");
			ReplaceImage(ImageTypes.Tile, Silver, "Tiles_331.xnb");
			ReplaceImage(ImageTypes.Tile, Gold, "Tiles_332.xnb");
			ReplaceImage(ImageTypes.Tile, Platinum, "Tiles_333.xnb");

			// Replace dust particles
			ReplaceDust();

			// Replace sounds
			ReplaceSound("Collect1", "Coin_0.xnb");
			ReplaceSound("Collect1", "Coin_1.xnb");
			ReplaceSound("Collect1", "Coin_2.xnb");
			ReplaceSound("Collect2", "Coin_3.xnb");
			ReplaceSound("Collect2", "Coin_4.xnb");
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
		private static void ReplaceImage(ImageTypes type, RupeeColors color, string outputFile) {
			PngConverter.Convert(GetImage(type, color), Path.Combine(ImageDir, outputFile));
		}
		/**<summary>Replaces a sound content file.</summary>*/
		private static void ReplaceSound(string name, string outputFile) {
			WavConverter.Convert(GetSound(name), Path.Combine(SoundDir, outputFile));
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
