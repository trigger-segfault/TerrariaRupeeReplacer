using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace TerrariaRupeeReplacer {
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

	/**<summary>Handles overriding coin related functions.</summary>*/
	public static class CoinReplacer {
		//=========== CLASSES ============
		#region Classes

		/**<summary>Aquire this all ahead of time to reduce reflection slowdown.</summary>*/
		private static class TerrariaReflection {
			//=========== MEMBERS ============
			#region Members

			public static ConstructorInfo ctor_LocalizedText;

			#endregion
			//========= CONSTRUCTORS =========
			#region Constructors

			/**<summary>Aquire all of the reflection infos ahead of time to reduce reflection slowdown.</summary>*/
			static TerrariaReflection() {
				ctor_LocalizedText       = typeof(LocalizedText).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();

				// Hooray for reflection!
			}

			#endregion
		}

		#endregion
		//========== CONSTANTS ===========
		#region Constants
		//--------------------------------
		#region Replacements

		private static readonly Color PlatColor = new Color(220, 220, 198);
		private static readonly Color GoldColor = new Color(224, 201, 92);
		private static readonly Color SilverColor = new Color(181, 192, 193);
		private static readonly Color CopperColor = new Color(246, 138, 96);
		private static readonly string PlatName = "Platinum";
		private static readonly string GoldName = "Gold";
		private static readonly string SilverName = "Silver";
		private static readonly string CopperName = "Copper";

		#endregion
		//--------------------------------
		#region Config

		/**<summary>The list of valid rupees and their colors.</summary>*/
		private static readonly Dictionary<string, Color> Rupees = new Dictionary<string, Color>{
			{ "Green",  new Color(84, 198, 61) },
			{ "Blue",   new Color(68, 145, 234) },
			{ "Yellow", new Color(249, 239, 47) },
			{ "Red",    new Color(224, 44, 65) },
			{ "Purple", new Color(169, 45, 226) },
			{ "Orange", new Color(242, 156, 29) },
			{ "Silver", new Color(181, 192, 193) },
			{ "Gold",   new Color(224, 201, 92) }
		};
		/**<summary>The name of the config file.</summary>*/
		public const string ConfigName = "RupeeConfig.xml";
		/**<summary>The path of the config file.</summary>*/
		public static readonly string ConfigPath = Path.Combine(Environment.CurrentDirectory, ConfigName);

		#endregion
		//--------------------------------
		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Sets up the coin replacer and loads the chosen rupee colors.</summary>*/
		static CoinReplacer() {
			// Load rupee config
			try {
				XmlDocument doc = new XmlDocument();
				XmlNode node;
				XmlAttribute attribute;
				string name;

				doc.Load(ConfigPath);

				node = doc.SelectSingleNode("/RupeeReplacer/CopperCoin");
				attribute = (node != null ? node.Attributes["Rupee"] : null);
				if (attribute != null) {
					name = attribute.InnerText;
					if (Rupees.ContainsKey(name)) {
						CopperColor = Rupees[name];
						CopperName = name;
					}
				}

				node = doc.SelectSingleNode("/RupeeReplacer/SilverCoin");
				attribute = (node != null ? node.Attributes["Rupee"] : null);
				if (attribute != null) {
					name = attribute.InnerText;
					if (Rupees.ContainsKey(name)) {
						SilverColor = Rupees[name];
						SilverName = name;
					}
				}

				node = doc.SelectSingleNode("/RupeeReplacer/GoldCoin");
				attribute = (node != null ? node.Attributes["Rupee"] : null);
				if (attribute != null) {
					name = attribute.InnerText;
					if (Rupees.ContainsKey(name)) {
						GoldColor = Rupees[name];
						GoldName = name;
					}
				}

				node = doc.SelectSingleNode("/RupeeReplacer/PlatinumCoin");
				attribute = (node != null ? node.Attributes["Rupee"] : null);
				if (attribute != null) {
					name = attribute.InnerText;
					if (Rupees.ContainsKey(name)) {
						PlatColor = Rupees[name];
						PlatName = name;
					}
				}
			}
			catch { }
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Sets the name of a coin.</summary>*/
		private static void SetCoinName(Dictionary<string, LocalizedText> locTexts, string coinID, string coinName) {
			LocalizedText locText = (LocalizedText)TerrariaReflection.ctor_LocalizedText.Invoke(new object[] { "ItemName." + coinID, coinName + " Rupee" });
			locTexts["ItemName." + coinID] = locText;
		}
		/**<summary>Sets the light color of a coin.</summary>*/
		private static void SetCoinLight(ref float r, ref float g, ref float b, Color color) {
			const float baseline = 0.2f;
			r *= Math.Max((float)color.R / 255f, baseline);
			g *= Math.Max((float)color.G / 255f, baseline);
			b *= Math.Max((float)color.B / 255f, baseline);
		}

		#endregion
		//========== OVERRIDES ===========
		#region Overrides
		//--------------------------------
		#region Terraria.Main

		/**<summary>Patches coin buy and sell text.</summary>*/
		public static Color OnCoinStoreValue(Color color, int num4, string[] array, int storeValue) {
			string text = "";
			int plat = 0;
			int gold = 0;
			int silver = 0;
			int copper = 0;
			int totalValue = storeValue * Main.HoverItem.stack;
			if (!Main.HoverItem.buy) {
				totalValue = storeValue / 5;
				if (totalValue < 1) {
					totalValue = 1;
				}
				totalValue *= Main.HoverItem.stack;
			}
			int coinValue = totalValue;
			if (totalValue < 1) {
				totalValue = 1;
			}
			if (totalValue >= 1000000) {
				plat = totalValue / 1000000;
				totalValue -= plat * 1000000;
			}
			if (totalValue >= 10000) {
				gold = totalValue / 10000;
				totalValue -= gold * 10000;
			}
			if (totalValue >= 100) {
				silver = totalValue / 100;
				totalValue -= silver * 100;
			}
			if (totalValue >= 1) {
				copper = totalValue;
			}
			if (plat > 0) {
				text += plat + " " + PlatName.ToLower() + " ";
			}
			if (gold > 0) {
				text += gold + " " + GoldName.ToLower() + " ";
			}
			if (silver > 0) {
				text += silver + " " + SilverName.ToLower() + " ";
			}
			if (copper > 0) {
				text += copper + " " + CopperName.ToLower() + " ";
			}
			text += "rupee";
			if (coinValue != 1000000 && coinValue != 10000 &&
				coinValue != 100 && coinValue != 1) {
				text += "s";
			}

			if (!Main.HoverItem.buy) {
				array[num4] = Lang.tip[49].Value + " " + text;
			}
			else {
				array[num4] = Lang.tip[50].Value + " " + text;
			}
			num4++;
			float mouseTextColor = (float)Main.mouseTextColor / 255f;
			int alpha = (int)Main.mouseTextColor;
			if (plat > 0) {
				color = new Color(
					(int)((byte)((float)PlatColor.R * mouseTextColor)),
					(int)((byte)((float)PlatColor.G * mouseTextColor)),
					(int)((byte)((float)PlatColor.B * mouseTextColor)),
				alpha);
			}
			else if (gold > 0) {
				color = new Color(
					(int)((byte)((float)GoldColor.R * mouseTextColor)),
					(int)((byte)((float)GoldColor.G * mouseTextColor)),
					(int)((byte)((float)GoldColor.B * mouseTextColor)),
				alpha);
			}
			else if (silver > 0) {
				color = new Color(
					(int)((byte)((float)SilverColor.R * mouseTextColor)),
					(int)((byte)((float)SilverColor.G * mouseTextColor)),
					(int)((byte)((float)SilverColor.B * mouseTextColor)),
				alpha);
			}
			else if (copper > 0) {
				color = new Color(
					(int)((byte)((float)CopperColor.R * mouseTextColor)),
					(int)((byte)((float)CopperColor.G * mouseTextColor)),
					(int)((byte)((float)CopperColor.B * mouseTextColor)),
				alpha);
			}
			return color;
		}
		/**<summary>Patches the reforge cost text.</summary>*/
		public static string OnReforgeCost(int cost) {
			string text = "";
			int plat = 0;
			int gold = 0;
			int silver = 0;
			int copper = 0;
			int totalCost = cost;
			if (totalCost < 1) {
				totalCost = 1;
			}
			if (totalCost >= 1000000) {
				plat = totalCost / 1000000;
				totalCost -= plat * 1000000;
			}
			if (totalCost >= 10000) {
				gold = totalCost / 10000;
				totalCost -= gold * 10000;
			}
			if (totalCost >= 100) {
				silver = totalCost / 100;
				totalCost -= silver * 100;
			}
			if (totalCost >= 1) {
				copper = totalCost;
			}
			Color rupeeColor = Color.White;
			if (plat > 0) {
				text = string.Concat(new object[]
				{
						text,
						"[c/",
						Colors.AlphaDarken(PlatColor).Hex3(),
						":",
						plat,
						" ",
						PlatName.ToLower(),
						"] "
				});
				rupeeColor = PlatColor;
			}
			if (gold > 0) {
				text = string.Concat(new object[]
				{
						text,
						"[c/",
						Colors.AlphaDarken(GoldColor).Hex3(),
						":",
						gold,
						" ",
						GoldName.ToLower(),
						"] "
				});
				rupeeColor = GoldColor;
			}
			if (silver > 0) {
				text = string.Concat(new object[]
				{
						text,
						"[c/",
						Colors.AlphaDarken(SilverColor).Hex3(),
						":",
						silver,
						" ",
						SilverName.ToLower(),
						"] "
				});
				rupeeColor = SilverColor;
			}
			if (copper > 0) {
				text = string.Concat(new object[]
				{
						text,
						"[c/",
						Colors.AlphaDarken(CopperColor).Hex3(),
						":",
						copper,
						" ",
						CopperName.ToLower(),
						"] "
				});
				rupeeColor = CopperColor;
			}
			text = string.Concat(new object[]
			{
					text,
					"[c/",
					Colors.AlphaDarken(rupeeColor/*Color.White*/).Hex3(),
					":",
					"rupee",
					(cost != 1000000 && cost != 10000 && cost != 100 && cost != 1 ? "s" : ""),
					"] "
			});
			return text;
		}
		/**<summary>Patches death dropped coins text.</summary>*/
		public static string OnValueToCoins(int value) {
			int i = value;
			int plat = 0;
			int gold = 0;
			int silver = 0;
			while (i >= 1000000) {
				i -= 1000000;
				plat++;
			}
			while (i >= 10000) {
				i -= 10000;
				gold++;
			}
			while (i >= 100) {
				i -= 100;
				silver++;
			}
			int copper = i;
			string text = "";
			if (plat > 0) {
				text += string.Format("{0} {1} ", plat, PlatName.ToLower());
			}
			if (gold > 0) {
				text += string.Format("{0} {1} ", gold, GoldName.ToLower());
			}
			if (silver > 0) {
				text += string.Format("{0} {1} ", silver, SilverName.ToLower());
			}
			if (copper > 0) {
				text += string.Format("{0} {1} ", copper, CopperName.ToLower());
			}
			text += "rupee";
			if (value != 1000000 && value != 10000 &&
				value != 100 && value != 1) {
				text += "s";
			}
			return text;
		}

		#endregion
		//--------------------------------
		#region Terraria.Dust

		/**<summary>Patches coin glowing during movement.</summary>*/
		public static void OnCoinSparkle(Dust dust) {
			dust.rotation += 0.1f * dust.scale;
			Color color = Lighting.GetColor((int)(dust.position.X / 16f), (int)(dust.position.Y / 16f));
			byte average = (byte)((color.R + color.G + color.B) / 3);
			float r = ((float)average / 270f + 1f) / 2f;
			float g = ((float)average / 270f + 1f) / 2f;
			float b = ((float)average / 270f + 1f) / 2f;
			r *= dust.scale * 0.9f;
			g *= dust.scale * 0.9f;
			b *= dust.scale * 0.9f;
			if (dust.alpha < 255) {
				dust.scale += 0.09f;
				if (dust.scale >= 1f) {
					dust.scale = 1f;
					dust.alpha = 255;
				}
			}
			else {
				if ((double)dust.scale < 0.8) {
					dust.scale -= 0.01f;
				}
				if ((double)dust.scale < 0.5) {
					dust.scale -= 0.01f;
				}
			}
			float brightness = 1f;
			if (dust.type == 244) {
				SetCoinLight(ref r, ref g, ref b, CopperColor);
				brightness = 0.9f;
			}
			else if (dust.type == 245) {
				SetCoinLight(ref r, ref g, ref b, SilverColor);
				brightness = 1f;
			}
			else if (dust.type == 246) {
				SetCoinLight(ref r, ref g, ref b, GoldColor);
				brightness = 1.1f;
			}
			else if (dust.type == 247) {
				SetCoinLight(ref r, ref g, ref b, PlatColor);
				brightness = 1.2f;
			}
			r *= brightness;
			g *= brightness;
			b *= brightness;
			Lighting.AddLight((int)(dust.position.X / 16f), (int)(dust.position.Y / 16f), r, g, b);
		}

		#endregion
		//--------------------------------
		#region Terraria.ItemText

		/**<summary>Patches coin pickup text.</summary>*/
		public static Vector2 OnCoinText(Item newItem, int i) {
			int value = 0;
			if (newItem.type == 71) {
				value += newItem.stack;
			}
			else if (newItem.type == 72) {
				value += 100 * newItem.stack;
			}
			else if (newItem.type == 73) {
				value += 10000 * newItem.stack;
			}
			else if (newItem.type == 74) {
				value += 1000000 * newItem.stack;
			}
			Main.itemText[i].coinValue += value;
			OnValueToName(Main.itemText[i]);
			Vector2 vector = Main.fontMouseText.MeasureString(Main.itemText[i].name);
			//Main.itemText[i].name = text;
			if (Main.itemText[i].coinValue >= 1000000) {
				if (Main.itemText[i].lifeTime < 300)
					Main.itemText[i].lifeTime = 300;
				Main.itemText[i].color = PlatColor;
			}
			else if (Main.itemText[i].coinValue >= 10000) {
				if (Main.itemText[i].lifeTime < 240)
					Main.itemText[i].lifeTime = 240;
				Main.itemText[i].color = GoldColor;
			}
			else if (Main.itemText[i].coinValue >= 100) {
				if (Main.itemText[i].lifeTime < 180)
					Main.itemText[i].lifeTime = 180;
				Main.itemText[i].color = SilverColor;
			}
			else if (Main.itemText[i].coinValue >= 1) {
				if (Main.itemText[i].lifeTime < 120)
					Main.itemText[i].lifeTime = 120;
				Main.itemText[i].color = CopperColor;
			}
			return vector;
		}
		/**<summary>Patches coin pickup text.</summary>*/
		public static void OnCoinText2(Item newItem, int i) {
			if (newItem.type == 71) {
				Main.itemText[i].coinValue += Main.itemText[i].stack;
			}
			else if (newItem.type == 72) {
				Main.itemText[i].coinValue += 100 * Main.itemText[i].stack;
			}
			else if (newItem.type == 73) {
				Main.itemText[i].coinValue += 10000 * Main.itemText[i].stack;
			}
			else if (newItem.type == 74) {
				Main.itemText[i].coinValue += 1000000 * Main.itemText[i].stack;
			}
			OnValueToName(Main.itemText[i]);
			Main.itemText[i].stack = 1;
			if (Main.itemText[i].coinValue >= 1000000) {
				if (Main.itemText[i].lifeTime < 300) {
					Main.itemText[i].lifeTime = 300;
				}
				Main.itemText[i].color = PlatColor;
				return;
			}
			if (Main.itemText[i].coinValue >= 10000) {
				if (Main.itemText[i].lifeTime < 240) {
					Main.itemText[i].lifeTime = 240;
				}
				Main.itemText[i].color = GoldColor;
				return;
			}
			if (Main.itemText[i].coinValue >= 100) {
				if (Main.itemText[i].lifeTime < 180) {
					Main.itemText[i].lifeTime = 180;
				}
				Main.itemText[i].color = SilverColor;
				return;
			}
			if (Main.itemText[i].coinValue >= 1) {
				if (Main.itemText[i].lifeTime < 120) {
					Main.itemText[i].lifeTime = 120;
				}
				Main.itemText[i].color = CopperColor;
			}
		}
		/**<summary>Patches coin pickup text.</summary>*/
		public static void OnValueToName(ItemText itemText) {
			int plat = 0;
			int gold = 0;
			int silver = 0;
			int copper = 0;
			int i = itemText.coinValue;
			while (i > 0) {
				if (i >= 1000000) {
					i -= 1000000;
					plat++;
				}
				else if (i >= 10000) {
					i -= 10000;
					gold++;
				}
				else if (i >= 100) {
					i -= 100;
					silver++;
				}
				else if (i >= 1) {
					i--;
					copper++;
				}
			}
			itemText.name = "";
			if (plat > 0) {
				itemText.name += plat + string.Format(" {0} ", PlatName);
			}
			if (gold > 0) {
				itemText.name += gold + string.Format(" {0} ", GoldName);
			}
			if (silver > 0) {
				itemText.name += silver + string.Format(" {0} ", SilverName);
			}
			if (copper > 0) {
				itemText.name += copper + string.Format(" {0} ", CopperName);
			}
			itemText.name += "Rupee";
			if (itemText.coinValue != 1000000 && itemText.coinValue != 10000 &&
				itemText.coinValue != 100 && itemText.coinValue != 1) {
				itemText.name += "s";
			}
		}

		#endregion
		//--------------------------------
		#region Terraria.Localization.LanguageManager

		/**<summary>Patches coin names.</summary>*/
		public static void OnLoadCoinNames(Dictionary<string, LocalizedText> locTexts) {
			SetCoinName(locTexts, "CopperCoin", CopperName);
			SetCoinName(locTexts, "SilverCoin", SilverName);
			SetCoinName(locTexts, "GoldCoin", GoldName);
			SetCoinName(locTexts, "PlatinumCoin", PlatName);
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
