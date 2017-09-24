using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace TerrariaRupeeReplacer {
	/**<summary>Handles overriding coin related functions.</summary>*/
	public class CoinReplacer {
		//=========== CLASSES ============
		#region Classes

		/**<summary>Aquire this all ahead of time to reduce reflection slowdown.</summary>*/
		protected static class TerrariaReflection {
			//=========== MEMBERS ============
			#region Members

			/**<summary>internal LocalizedText(string key, string text)</summary>*/
			public static ConstructorInfo LocalizedText_ctor;

			/**<summary>private Dictionary<string, List<string>> LanguageManager._categoryGroupedKeys</summary>*/
			public static FieldInfo LanguageManager_categoryGroupedKeys;
			/**<summary>private Dictionary<string, LocalizedText> LanguageManager._localizedTexts</summary>*/
			public static FieldInfo LanguageManager_localizedTexts;

			/**<summary>internal void LocalizedText.SetValue(string text)</summary>*/
			public static MethodInfo LocalizedText_SetValue;

			#endregion
			//========= CONSTRUCTORS =========
			#region Constructors

			/**<summary>Aquire all of the reflection infos ahead of time to reduce reflection slowdown.</summary>*/
			static TerrariaReflection() {
				LocalizedText_ctor					= typeof(LocalizedText).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();

				LanguageManager_categoryGroupedKeys	= typeof(LanguageManager).GetField("_categoryGroupedKeys", BindingFlags.NonPublic | BindingFlags.Instance);
				LanguageManager_localizedTexts		= typeof(LanguageManager).GetField("_localizedTexts", BindingFlags.NonPublic | BindingFlags.Instance);

				LocalizedText_SetValue				= typeof(LocalizedText).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance);
				
				// Hooray for reflection!
			}

			#endregion
		}

		#endregion
		//========== CONSTANTS ===========
		#region Constants
		//--------------------------------
		#region Replacements

		protected static readonly Color PlatinumColor = new Color(220, 220, 198);
		protected static readonly Color GoldColor = new Color(224, 201, 92);
		protected static readonly Color SilverColor = new Color(181, 192, 193);
		protected static readonly Color CopperColor = new Color(246, 138, 96);
		protected static readonly string Platinum = "Platinum";
		protected static readonly string Gold = "Gold";
		protected static readonly string Silver = "Silver";
		protected static readonly string Copper = "Copper";

		protected static readonly bool CoinGun = true;
		protected static readonly bool LuckyCoin = true;
		protected static readonly bool CoinRing = true;
		protected static readonly bool CoinPortal = true;

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
		/**<summary>The path of the config file.</summary>*/
		public static readonly string LocalizationDirectory = Path.Combine(Environment.CurrentDirectory, "Localization");

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
				attribute = (node != null ? node.Attributes["Color"] : null);
				if (attribute != null) {
					name = attribute.InnerText;
					if (Rupees.ContainsKey(name)) {
						CopperColor = Rupees[name];
						Copper = name;
					}
				}
				node = doc.SelectSingleNode("/RupeeReplacer/SilverCoin");
				attribute = (node != null ? node.Attributes["Color"] : null);
				if (attribute != null) {
					name = attribute.InnerText;
					if (Rupees.ContainsKey(name)) {
						SilverColor = Rupees[name];
						Silver = name;
					}
				}
				node = doc.SelectSingleNode("/RupeeReplacer/GoldCoin");
				attribute = (node != null ? node.Attributes["Color"] : null);
				if (attribute != null) {
					name = attribute.InnerText;
					if (Rupees.ContainsKey(name)) {
						GoldColor = Rupees[name];
						Gold = name;
					}
				}
				node = doc.SelectSingleNode("/RupeeReplacer/PlatinumCoin");
				attribute = (node != null ? node.Attributes["Color"] : null);
				if (attribute != null) {
					name = attribute.InnerText;
					if (Rupees.ContainsKey(name)) {
						PlatinumColor = Rupees[name];
						Platinum = name;
					}
				}

				node = doc.SelectSingleNode("/RupeeReplacer/CoinGun");
				attribute = (node != null ? node.Attributes["Enabled"] : null);
				if (attribute != null) {
					bool.TryParse(attribute.InnerText, out CoinGun);
				}
				node = doc.SelectSingleNode("/RupeeReplacer/LuckyCoin");
				attribute = (node != null ? node.Attributes["Enabled"] : null);
				if (attribute != null) {
					bool.TryParse(attribute.InnerText, out LuckyCoin);
				}
				node = doc.SelectSingleNode("/RupeeReplacer/CoinRing");
				attribute = (node != null ? node.Attributes["Enabled"] : null);
				if (attribute != null) {
					bool.TryParse(attribute.InnerText, out CoinRing);
				}
				node = doc.SelectSingleNode("/RupeeReplacer/CoinPortal");
				attribute = (node != null ? node.Attributes["Enabled"] : null);
				if (attribute != null) {
					bool.TryParse(attribute.InnerText, out CoinPortal);
				}
			}
			catch { }
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers
		
		/**<summary>Sets the light color of a coin.</summary>*/
		private static void SetCoinLight(ref float r, ref float g, ref float b, Color color) {
			const float baseline = 0.2f;
			r *= Math.Max((float)color.R / 255f, baseline);
			g *= Math.Max((float)color.G / 255f, baseline);
			b *= Math.Max((float)color.B / 255f, baseline);
		}
		/**<summary>Adds a translation entry to the localization manager.</summary>*/
		private static void AddTranslation(string key1, string key2, string value) {
			var _localizedTexts = (Dictionary<string, LocalizedText>)TerrariaReflection.LanguageManager_localizedTexts.GetValue(LanguageManager.Instance);
			var _categoryGroupedKeys = (Dictionary<string, List<string>>)TerrariaReflection.LanguageManager_categoryGroupedKeys.GetValue(LanguageManager.Instance);

			string keyFull = key1 + "." + key2;
			
			// Don't rename lucky coin if not resprited
			if (keyFull == "ItemName.LuckyCoin" && !LuckyCoin)
				return;

			if (_localizedTexts.ContainsKey(keyFull)) {
				TerrariaReflection.LocalizedText_SetValue.Invoke(_localizedTexts[keyFull], new object[] { value });
			}
			else {
				LocalizedText locText = (LocalizedText)TerrariaReflection.LocalizedText_ctor.Invoke(new object[] { keyFull, value });
				_localizedTexts.Add(keyFull, locText);
				if (!_categoryGroupedKeys.ContainsKey(key1)) {
					_categoryGroupedKeys.Add(key1, new List<string>());
				}
				_categoryGroupedKeys[key1].Add(key2);
			}
		}
		/**<summary>Gets the name of a rupee color.</summary>*/
		private static string GetColorName(string name, bool lower) {
			return Language.GetTextValue("RupeeColors." + (lower ? name.ToLower() : name));
		}
		/**<summary>Gets the name of a rupee color.</summary>*/
		private static string GetRupeeName(bool lower, bool plural) {
			return Language.GetTextValue("Rupee." + (lower ? "rupee" : "Rupee") + (plural ? "s" : ""));
		}

		#endregion
		//========== OVERRIDES ===========
		#region Overrides
		//--------------------------------
		#region Terraria.Main

		/**<summary>Patches coin buy and sell text.</summary>*/
		public static Color OnNPCShopPrice(Color color, int num4, string[] array, int storeValue) {
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
				text += plat + " " + GetColorName(Platinum, true) + " ";
			}
			if (gold > 0) {
				text += gold + " " + GetColorName(Gold, true) + " ";
			}
			if (silver > 0) {
				text += silver + " " + GetColorName(Silver, true) + " ";
			}
			if (copper > 0) {
				text += copper + " " + GetColorName(Copper, true) + " ";
			}
			text += GetRupeeName(true, coinValue != 1000000 && coinValue != 10000 &&
				coinValue != 100 && coinValue != 1);

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
					(int)((byte)((float)PlatinumColor.R * mouseTextColor)),
					(int)((byte)((float)PlatinumColor.G * mouseTextColor)),
					(int)((byte)((float)PlatinumColor.B * mouseTextColor)),
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
			/*int cost = Main.reforgeItem.value;
			if (Main.player[Main.myPlayer].discount) {
				cost = (int)((double)cost * 0.8);
			}
			cost /= 3;*/
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
				text = string.Concat(new object[] {
						text,
						"[c/",
						Colors.AlphaDarken(PlatinumColor).Hex3(),
						":",
						plat,
						" ",
						GetColorName(Platinum, true),
						"] "
				});
				rupeeColor = PlatinumColor;
			}
			if (gold > 0) {
				text = string.Concat(new object[] {
						text,
						"[c/",
						Colors.AlphaDarken(GoldColor).Hex3(),
						":",
						gold,
						" ",
						GetColorName(Gold, true),
						"] "
				});
				rupeeColor = GoldColor;
			}
			if (silver > 0) {
				text = string.Concat(new object[] {
						text,
						"[c/",
						Colors.AlphaDarken(SilverColor).Hex3(),
						":",
						silver,
						" ",
						GetColorName(Silver, true),
						"] "
				});
				rupeeColor = SilverColor;
			}
			if (copper > 0) {
				text = string.Concat(new object[] {
						text,
						"[c/",
						Colors.AlphaDarken(CopperColor).Hex3(),
						":",
						copper,
						" ",
						GetColorName(Copper, true),
						"] "
				});
				rupeeColor = CopperColor;
			}
			text = string.Concat(new object[] {
					text,
					"[c/",
					Colors.AlphaDarken(rupeeColor/*Color.White*/).Hex3(),
					":",
					GetRupeeName(true, cost != 1000000 && cost != 10000 && cost != 100 && cost != 1),
					"]"
			});
			return text;
		}
		/**<summary>Patches the tax collect amount text.</summary>*/
		public static void OnTaxCollect(ref string focusText, ref Color color2, ref int num6) {
			string text = "";
			int plat = 0;
			int gold = 0;
			int silver = 0;
			int copper = 0;
			int taxMoney = Main.player[Main.myPlayer].taxMoney;
			if (taxMoney < 0) {
				taxMoney = 0;
			}
			num6 = taxMoney;
			if (taxMoney >= 1000000) {
				plat = taxMoney / 1000000;
				taxMoney -= plat * 1000000;
			}
			if (taxMoney >= 10000) {
				gold = taxMoney / 10000;
				taxMoney -= gold * 10000;
			}
			if (taxMoney >= 100) {
				silver = taxMoney / 100;
				taxMoney -= silver * 100;
			}
			if (taxMoney >= 1) {
				copper = taxMoney;
			}
			if (plat > 0) {
				text += plat + " " + GetColorName(Platinum, true) + " ";
			}
			if (gold > 0) {
				text += gold + " " + GetColorName(Gold, true) + " ";
			}
			if (silver > 0) {
				text += silver + " " + GetColorName(Silver, true) + " ";
			}
			if (copper > 0) {
				text += copper + " " + GetColorName(Copper, true) + " ";
			}
			float mouseTextColor = (float)Main.mouseTextColor / 255f;
			int alpha = (int)Main.mouseTextColor;
			if (plat > 0) {
				color2 = new Color(
					(int)((byte)((float)PlatinumColor.R * mouseTextColor)),
					(int)((byte)((float)PlatinumColor.G * mouseTextColor)),
					(int)((byte)((float)PlatinumColor.B * mouseTextColor)),
				alpha);
			}
			else if (gold > 0) {
				color2 = new Color(
					(int)((byte)((float)GoldColor.R * mouseTextColor)),
					(int)((byte)((float)GoldColor.G * mouseTextColor)),
					(int)((byte)((float)GoldColor.B * mouseTextColor)),
				alpha);
			}
			else if (silver > 0) {
				color2 = new Color(
					(int)((byte)((float)SilverColor.R * mouseTextColor)),
					(int)((byte)((float)SilverColor.G * mouseTextColor)),
					(int)((byte)((float)SilverColor.B * mouseTextColor)),
				alpha);
			}
			else if (copper > 0) {
				color2 = new Color(
					(int)((byte)((float)CopperColor.R * mouseTextColor)),
					(int)((byte)((float)CopperColor.G * mouseTextColor)),
					(int)((byte)((float)CopperColor.B * mouseTextColor)),
				alpha);
			}
			if (text == "") {
				focusText = Lang.inter[89].Value;
			}
			else {
				text += GetRupeeName(true, num6 != 1000000 && num6 != 10000 &&
					num6 != 100 && num6 != 1);
				focusText = Lang.inter[89].Value + " (" + text + ")";
			}
		}
		/**<summary>Patches the tax collect amount text.</summary>*/
		public static void OnNurseHeal(ref string focusText, ref Color color2, ref int num6) {
			string text = "";
			int plat = 0;
			int gold = 0;
			int silver = 0;
			int copper = 0;
			int healCost = num6;
			if (healCost > 0) {
				healCost = (int)((double)healCost * 0.75);
				if (healCost < 1) {
					healCost = 1;
				}
			}
			if (healCost < 0) {
				healCost = 0;
			}
			num6 = healCost;
			if (healCost >= 1000000) {
				plat = healCost / 1000000;
				healCost -= plat * 1000000;
			}
			if (healCost >= 10000) {
				gold = healCost / 10000;
				healCost -= gold * 10000;
			}
			if (healCost >= 100) {
				silver = healCost / 100;
				healCost -= silver * 100;
			}
			if (healCost >= 1) {
				copper = healCost;
			}
			if (plat > 0) {
				text += plat + " " + GetColorName(Platinum, true) + " ";
			}
			if (gold > 0) {
				text += gold + " " + GetColorName(Gold, true) + " ";
			}
			if (silver > 0) {
				text += silver + " " + GetColorName(Silver, true) + " ";
			}
			if (copper > 0) {
				text += copper + " " + GetColorName(Copper, true) + " ";
			}
			float mouseTextColor = (float)Main.mouseTextColor / 255f;
			int alpha = (int)Main.mouseTextColor;
			if (plat > 0) {
				color2 = new Color(
					(int)((byte)((float)PlatinumColor.R * mouseTextColor)),
					(int)((byte)((float)PlatinumColor.G * mouseTextColor)),
					(int)((byte)((float)PlatinumColor.B * mouseTextColor)),
				alpha);
			}
			else if (gold > 0) {
				color2 = new Color(
					(int)((byte)((float)GoldColor.R * mouseTextColor)),
					(int)((byte)((float)GoldColor.G * mouseTextColor)),
					(int)((byte)((float)GoldColor.B * mouseTextColor)),
				alpha);
			}
			else if (silver > 0) {
				color2 = new Color(
					(int)((byte)((float)SilverColor.R * mouseTextColor)),
					(int)((byte)((float)SilverColor.G * mouseTextColor)),
					(int)((byte)((float)SilverColor.B * mouseTextColor)),
				alpha);
			}
			else if (copper > 0) {
				color2 = new Color(
					(int)((byte)((float)CopperColor.R * mouseTextColor)),
					(int)((byte)((float)CopperColor.G * mouseTextColor)),
					(int)((byte)((float)CopperColor.B * mouseTextColor)),
				alpha);
			}
			if (text == "") {
				focusText = Lang.inter[54].Value;
			}
			else {
				text += GetRupeeName(true, num6 != 1000000 && num6 != 10000 &&
					num6 != 100 && num6 != 1);
				focusText = Lang.inter[54].Value + " (" + text + ")";
			}
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
				text += string.Format("{0} {1} ", plat, GetColorName(Platinum, true));
			}
			if (gold > 0) {
				text += string.Format("{0} {1} ", gold, GetColorName(Gold, true));
			}
			if (silver > 0) {
				text += string.Format("{0} {1} ", silver, GetColorName(Silver, true));
			}
			if (copper > 0) {
				text += string.Format("{0} {1} ", copper, GetColorName(Copper, true));
			}
			text += GetRupeeName(true, value != 1000000 && value != 10000 &&
				value != 100 && value != 1);
			return text;
		}

		#endregion
		//--------------------------------
		#region Terraria.Dust

		/**<summary>Patches coin glowing during movement.</summary>*/
		public static void OnCoinGlow(Dust dust) {
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
				SetCoinLight(ref r, ref g, ref b, PlatinumColor);
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
		public static Vector2 OnCoinPickupText(Item newItem, int i) {
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
			if (Main.itemText[i].coinValue >= 1000000) {
				if (Main.itemText[i].lifeTime < 300)
					Main.itemText[i].lifeTime = 300;
				Main.itemText[i].color = PlatinumColor;
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
		public static void OnCoinPickupText2(Item newItem, int i) {
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
				Main.itemText[i].color = PlatinumColor;
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
				itemText.name += plat + string.Format(" {0} ", GetColorName(Platinum, false));
			}
			if (gold > 0) {
				itemText.name += gold + string.Format(" {0} ", GetColorName(Gold, false));
			}
			if (silver > 0) {
				itemText.name += silver + string.Format(" {0} ", GetColorName(Silver, false));
			}
			if (copper > 0) {
				itemText.name += copper + string.Format(" {0} ", GetColorName(Copper, false));
			}
			itemText.name += GetRupeeName(false, itemText.coinValue != 1000000 && itemText.coinValue != 10000 &&
				itemText.coinValue != 100 && itemText.coinValue != 1);
		}

		#endregion
		//--------------------------------
		#region Terraria.Localization.LanguageManager

		/**<summary>Patches coin names.</summary>*/
		public static void OnLoadLocalizations(GameCulture culture) {
			try {
				AddTranslation("Rupee", "Copper", "{$RupeeColors." + Copper + "}");
				AddTranslation("Rupee", "Silver", "{$RupeeColors." + Silver + "}");
				AddTranslation("Rupee", "Gold", "{$RupeeColors." + Gold + "}");
				AddTranslation("Rupee", "Platinum", "{$RupeeColors." + Platinum + "}");
				AddTranslation("Rupee", "copper", "{$RupeeColors." + Copper.ToLower() + "}");
				AddTranslation("Rupee", "silver", "{$RupeeColors." + Silver.ToLower() + "}");
				AddTranslation("Rupee", "gold", "{$RupeeColors." + Gold.ToLower() + "}");
				AddTranslation("Rupee", "platinum", "{$RupeeColors." + Platinum.ToLower() + "}");

				AddTranslation("ItemName", "CopperCoin", "{$Rupee.CopperRupee}");
				AddTranslation("ItemName", "SilverCoin", "{$Rupee.SilverRupee}");
				AddTranslation("ItemName", "GoldCoin", "{$Rupee.GoldRupee}");
				AddTranslation("ItemName", "PlatinumCoin", "{$Rupee.PlatinumRupee}");

				string filePath = Path.Combine(
					LocalizationDirectory,
					"Terraria.Localization.Content." + culture.CultureInfo.Name + ".RupeeReplacer.json"
				);
				string fileText = File.ReadAllText(filePath, Encoding.UTF8);
				foreach (KeyValuePair<string, Dictionary<string, string>> keyValuePair in JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileText)) {
					foreach (KeyValuePair<string, string> keyValuePair2 in keyValuePair.Value) {
						AddTranslation(keyValuePair.Key, keyValuePair2.Key, keyValuePair2.Value);
					}
				}
			}
			catch (Exception ex) {
				ErrorLogger.WriteException(ex);
			}
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
