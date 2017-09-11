using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaRupeeReplacer.Patching {
	/**<summary>An exception thrown when the patcher is unable to locate the instructions to change.</summary>*/
	public class PatcherException : Exception {
		public PatcherException(string message) : base(message) { }
	}

	/**<summary>The class for handling modification to the Terraria executable.</summary>*/
	public static class Patcher {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The list of required dlls and files to transfer.</summary>*/
		public static readonly string[] RequireFiles = {
			"RupeeReplacer.dll"
		};

		#endregion
		//========== PROPERTIES ==========
		#region Properties

		/**<summary>True if patching TModloader.</summary>*/
		public static bool IsTMod { get; set; } = false;
		/**<summary>The path to terraria's executable.</summary>*/
		public static string ExePath { get; set; } = "";
		/**<summary>Gets the path to terraria's backup.</summary>*/
		public static string BackupPath {
			get { return ExePath + ".bak"; }
		}
		/**<summary>Gets the directory of the Terraria executable.</summary>*/
		public static string ExeDirectory {
			get { return Path.GetDirectoryName(ExePath); }
		}
		/**<summary>Gets the directory of this application.</summary>*/
		public static string AppDirectory {
			get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
		}

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The Terraria assembly.</summary>*/
		private static AssemblyDefinition AsmDefinition;
		/**<summary>The Terraria module.</summary>*/
		private static ModuleDefinition ModDefinition;

		/**<summary>The Terraria.Main type.</summary>*/
		private static TypeDefinition Main;
		/**<summary>The Terraria.Item type.</summary>*/
		private static TypeDefinition Item;
		/**<summary>The Terraria.ItemText type.</summary>*/
		private static TypeDefinition ItemText;
		/**<summary>The Terraria.Dust type.</summary>*/
		private static TypeDefinition Dust;
		/**<summary>The Terraria.Light type.</summary>*/
		private static TypeDefinition Lighting;
		/**<summary>The Terraria.Localization.LanguageManager type.</summary>*/
		private static TypeDefinition LanguageManager;

		/**<summary>The CoinReplacer type.</summary>*/
		private static TypeDefinition CoinReplacer;

		#endregion
		//=========== PATCHING ===========
		#region Patching
		//--------------------------------
		#region Main
			
		/**<summary>Restores the Terraria backup.</summary>*/
		public static void Restore() {
			File.Copy(BackupPath, ExePath, true);
		}
		/**<summary>Patches the Terraria executable.</summary>*/
		public static void Patch() {
			// Backup the file first
			if (!File.Exists(BackupPath)) {
				File.Copy(ExePath, BackupPath, false);
			}

			// Load the assembly
			AsmDefinition = AssemblyDefinition.ReadAssembly(ExePath);
			ModDefinition = AsmDefinition.MainModule;

			// Get links to Terraria types
			Main = IL.GetTypeDefinition(ModDefinition, "Main");
			Dust = IL.GetTypeDefinition(ModDefinition, "Dust");
			Item = IL.GetTypeDefinition(ModDefinition, "Item");
			ItemText = IL.GetTypeDefinition(ModDefinition, "ItemText");
			Lighting = IL.GetTypeDefinition(ModDefinition, "Lighting");
			LanguageManager = IL.GetTypeDefinition(ModDefinition, "LanguageManager");
			
			// Get links to CoinReplacer functions
			CoinReplacer = ModDefinition.Import(typeof(CoinReplacer)).Resolve();
			
			// Patch Terraria
			Patch_Main_DrawInventory();
			Patch_Main_MouseText_DrawItemTooltip();
			Patch_Main_ValueToCoins();

			Patch_Dust_UpdateDust();

			Patch_ItemText_NewText();
			Patch_ItemText_ValueToName();

			Patch_LanguageManager_LoadLanguage();

			// Save the modifications
			AsmDefinition.Write(ExePath);
			IL.MakeLargeAddressAware(ExePath);

			CopyRequiredFiles();
		}
		/**<summary>Copies the required dlls and files to the Terraria folder.</summary>*/
		private static void CopyRequiredFiles() {
			try {
				foreach (string dll in RequireFiles) {
					string source = Path.Combine(AppDirectory, dll);
					string destination = Path.Combine(ExeDirectory, dll);
					File.Copy(source, destination, true);
				}
			}
			catch (Exception ex) {
				throw new IOException("Error while trying to copy over required files.", ex);
			}
		}

		#endregion
		//--------------------------------
		#region Patchers

		/**<summary>Patches reforge cost text.</summary>*/
		private static void Patch_Main_DrawInventory() {
			var drawInventory = IL.GetMethodDefinition(Main, "DrawInventory", 0);
			var mainSpriteBatch = IL.GetFieldDefinition(Main, "spriteBatch");
			var mainInvBottom = IL.GetFieldDefinition(Main, "invBottom");

			var onReforgeCost = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnReforgeCost"));

			int start = IL.ScanForInstructionPattern(drawInventory,
				new ILCheck(OpCodes.Ldstr, ""),
				new ILCheck(OpCodes.Stloc_S, drawInventory.Body.Variables[IsTMod ? 111 : 102]),
				new ILCheck(OpCodes.Ldc_I4_0)
			);
			if (start == -1)
				throw new PatcherException("Failed to find starting point for Terraria.Main.DrawInventory");
			int end = IL.ScanForInstructionPattern(drawInventory, start,
				new ILCheck(OpCodes.Ldsfld, mainSpriteBatch),
				new ILCheck(OpCodes.Ldloc_S, drawInventory.Body.Variables[IsTMod ? 107 : 98]),
				new ILCheck(OpCodes.Ldc_I4, 130),
				new ILCheck(OpCodes.Add),
				new ILCheck(OpCodes.Conv_R4),
				new ILCheck(OpCodes.Ldarg_0),
				new ILCheck(OpCodes.Ldfld, mainInvBottom),
				new ILCheck(OpCodes.Conv_R4),
				new ILCheck(OpCodes.Ldc_I4_1),
				new ILCheck(OpCodes.Call)
			);
			if (end == -1)
				throw new PatcherException("Failed to find ending point for Terraria.Main.DrawInventory");
			var il = drawInventory.Body.GetILProcessor();
			for (int i = start; i < end; i++) {
				il.Body.Instructions.RemoveAt(start);
			}
			IL.MethodInsert(drawInventory, start, new[] {
				Instruction.Create(OpCodes.Ldloc_S, drawInventory.Body.Variables[IsTMod ? 110 : 101]),
				Instruction.Create(OpCodes.Call, onReforgeCost),
				Instruction.Create(OpCodes.Stloc_S, drawInventory.Body.Variables[IsTMod ? 111 : 102])
			});
		}
		/**<summary>Patches store buy and sell price text.</summary>*/
		private static void Patch_Main_MouseText_DrawItemTooltip() {
			var mouseText_DrawItemTooltip = IL.GetMethodDefinition(Main, "MouseText_DrawItemTooltip", 4);
			var getStoreValue = IL.GetMethodDefinition(Item, "GetStoreValue", 0);

			var onCoinStoreValue = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnCoinStoreValue"));

			int start = IL.ScanForInstructionPattern(mouseText_DrawItemTooltip,
				new ILCheck(OpCodes.Ldsfld),
				new ILCheck(OpCodes.Callvirt, getStoreValue),
				new ILCheck(OpCodes.Ldc_I4_0),
				new ILCheck(OpCodes.Ble)
			);
			if (start == -1)
				throw new PatcherException("Failed to find starting point for Terraria.Main.MouseText_DrawItemTooltip");
			start += 4;
			int end = IL.ScanForInstructionPattern(mouseText_DrawItemTooltip, start,
				new ILCheck(OpCodes.Ldc_R4, 246f),
				new ILCheck(OpCodes.Ldloc_S, mouseText_DrawItemTooltip.Body.Variables[IsTMod ? 11 : 9]),
				new ILCheck(OpCodes.Mul),
				new ILCheck(OpCodes.Conv_U1),

				new ILCheck(OpCodes.Ldc_R4, 138f),
				new ILCheck(OpCodes.Ldloc_S, mouseText_DrawItemTooltip.Body.Variables[IsTMod ? 11 : 9]),
				new ILCheck(OpCodes.Mul),
				new ILCheck(OpCodes.Conv_U1),

				new ILCheck(OpCodes.Ldc_R4, 96f),
				new ILCheck(OpCodes.Ldloc_S, mouseText_DrawItemTooltip.Body.Variables[IsTMod ? 11 : 9]),
				new ILCheck(OpCodes.Mul),
				new ILCheck(OpCodes.Conv_U1),

				new ILCheck(OpCodes.Ldloc_S, mouseText_DrawItemTooltip.Body.Variables[IsTMod ? 12 : 10]),
				new ILCheck(OpCodes.Call)
			);
			if (end == -1)
				throw new PatcherException("Failed to find ending point for Terraria.Main.MouseText_DrawItemTooltip");
			end += 14;
			var il = mouseText_DrawItemTooltip.Body.GetILProcessor();
			for (int i = start; i < end; i++) {
				il.Body.Instructions.RemoveAt(start);
			}
			IL.MethodInsert(mouseText_DrawItemTooltip, start, new[] {
				Instruction.Create(OpCodes.Ldloc_0),
				Instruction.Create(OpCodes.Ldloc_S, mouseText_DrawItemTooltip.Body.Variables[5]),
				Instruction.Create(OpCodes.Ldloc_S, mouseText_DrawItemTooltip.Body.Variables[6]),
				Instruction.Create(OpCodes.Ldloc_S, mouseText_DrawItemTooltip.Body.Variables[IsTMod ? 56 : 33]),
				Instruction.Create(OpCodes.Call, onCoinStoreValue),
				Instruction.Create(OpCodes.Stloc_0),
				Instruction.Create(OpCodes.Ldloc_S, mouseText_DrawItemTooltip.Body.Variables[5]),
				Instruction.Create(OpCodes.Ldc_I4_1),
				Instruction.Create(OpCodes.Add),
				Instruction.Create(OpCodes.Stloc_S, mouseText_DrawItemTooltip.Body.Variables[5])
			});
		}
		/**<summary>Patches death coin drop text.</summary>*/
		private static void Patch_Main_ValueToCoins() {
			var valueToCoins = IL.GetMethodDefinition(Main, "ValueToCoins", 1);

			var onValueToCoins = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnValueToCoins"));

			var il = valueToCoins.Body.GetILProcessor();
			il.Body.Instructions.Clear();
			IL.MethodAppend(valueToCoins, new[] {
				Instruction.Create(OpCodes.Ldarg_0),
				Instruction.Create(OpCodes.Call, onValueToCoins),
				Instruction.Create(OpCodes.Ret)
			});
		}
		/**<summary>Patches coin glowing when moving.</summary>*/
		private static void Patch_Dust_UpdateDust() {
			var dustType = IL.GetFieldDefinition(Dust, "type");
			var updateDust = IL.GetMethodDefinition(Dust, "UpdateDust", 0);
			var addLight = IL.GetMethodDefinition(Lighting, "AddLight", 5);

			var onCoinSparkle = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnCoinSparkle"));

			int start = IL.ScanForInstructionPattern(updateDust,
				IsTMod ? new ILCheck(OpCodes.Ldloc_S, updateDust.Body.Variables[4]) : new ILCheck(OpCodes.Ldloc_3),
				new ILCheck(OpCodes.Ldfld, dustType),
				new ILCheck(OpCodes.Ldc_I4, (int)244),
				new ILCheck(OpCodes.Blt),
				IsTMod ? new ILCheck(OpCodes.Ldloc_S, updateDust.Body.Variables[4]) : new ILCheck(OpCodes.Ldloc_3),
				new ILCheck(OpCodes.Ldfld, dustType),
				new ILCheck(OpCodes.Ldc_I4, (int)247),
				new ILCheck(OpCodes.Bgt)
			);
			if (start == -1)
				throw new PatcherException("Failed to find starting point for Terraria.Dust.UpdateDust");
			start += 8;
			int end = IL.ScanForInstructionPattern(updateDust, start,
				new ILCheck(OpCodes.Call, addLight)
			);
			if (end == -1)
				throw new PatcherException("Failed to find ending point for Terraria.Dust.UpdateDust");
			end += 1;

			var il = updateDust.Body.GetILProcessor();
			for (int i = start; i < end; i++)
				il.Body.Instructions.RemoveAt(start);
			IL.MethodInsert(updateDust, start, new[] {
				Instruction.Create(OpCodes.Ldloc_3),
				Instruction.Create(OpCodes.Call, onCoinSparkle)
			});
		}
		/**<summary>Patches coin pickup text.</summary>*/
		private static void Patch_ItemText_ValueToName() {
			var valueToName = IL.GetMethodDefinition(ItemText, "ValueToName", false, 0);

			var onValueToName = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnValueToName"));

			var il = valueToName.Body.GetILProcessor();
			il.Body.Instructions.Clear();
			IL.MethodAppend(valueToName, new[] {
				Instruction.Create(OpCodes.Ldarg_0),
				Instruction.Create(OpCodes.Call, onValueToName),
				Instruction.Create(OpCodes.Ret)
			});
		}
		/**<summary>Patches coin pickup text.</summary>*/
		private static void Patch_ItemText_NewText() {
			var newText = IL.GetMethodDefinition(ItemText, "NewText", 4);
			var coinText = IL.GetFieldDefinition(ItemText, "coinText");
			var mainItemText = IL.GetFieldDefinition(Main, "itemText");

			var onCoinText = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnCoinText"));
			var onCoinText2 = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnCoinText2"));
			
			int start = IL.ScanForNthInstructionPattern(newText, 1,
				new ILCheck(OpCodes.Ldloc_0),
				new ILCheck(OpCodes.Brfalse),
				new ILCheck(OpCodes.Ldsfld, mainItemText),
				new ILCheck(OpCodes.Ldloc_2),
				new ILCheck(OpCodes.Ldelem_Ref),
				new ILCheck(OpCodes.Ldfld, coinText),
				new ILCheck(OpCodes.Brfalse)
			);
			if (start == -1)
				throw new PatcherException("Failed to find first starting point for Terraria.ItemText.NewText");
			start += 7;
			int end = IL.ScanForInstructionPattern(newText, start,
				new ILCheck(OpCodes.Ldc_I4, (int)246),
				new ILCheck(OpCodes.Ldc_I4, (int)138),
				new ILCheck(OpCodes.Ldc_I4_S, (sbyte)96),
				new ILCheck(OpCodes.Newobj),
				new ILCheck(OpCodes.Stfld)
			);
			if (end == -1)
				throw new PatcherException("Failed to find first ending point for Terraria.ItemText.NewText");
			end += 5;
			var il = newText.Body.GetILProcessor();
			for (int i = start; i < end; i++) {
				il.Body.Instructions.RemoveAt(start);
			}
			IL.MethodInsert(newText, start, new[] {
				Instruction.Create(OpCodes.Ldarg_0),
				Instruction.Create(OpCodes.Ldloc_2),
				Instruction.Create(OpCodes.Call, onCoinText),
				Instruction.Create(OpCodes.Stloc_S, newText.Body.Variables[5])
			});

			start = IL.ScanForInstructionPattern(newText, end + 5,
				new ILCheck(OpCodes.Ldsfld, mainItemText),
				new ILCheck(OpCodes.Ldloc_1),
				new ILCheck(OpCodes.Ldelem_Ref),
				new ILCheck(OpCodes.Ldfld, coinText),
				new ILCheck(OpCodes.Brfalse)
			);
			if (start == -1)
				throw new PatcherException("Failed to find second starting point for Terraria.ItemText.NewText");
			start += 5;
			end = IL.ScanForInstructionPattern(newText, start,
				new ILCheck(OpCodes.Ldc_I4, (int)246),
				new ILCheck(OpCodes.Ldc_I4, (int)138),
				new ILCheck(OpCodes.Ldc_I4_S, (sbyte)96),
				new ILCheck(OpCodes.Newobj),
				new ILCheck(OpCodes.Stfld)
			);
			if (end == -1)
				throw new PatcherException("Failed to find second ending point for Terraria.ItemText.NewText");
			end += 5;

			for (int i = start; i < end; i++) {
				il.Body.Instructions.RemoveAt(start);
			}
			IL.MethodInsert(newText, start, new[] {
				Instruction.Create(OpCodes.Ldarg_0),
				Instruction.Create(OpCodes.Ldloc_1),
				Instruction.Create(OpCodes.Call, onCoinText2)
			});
		}
		/**<summary>Patches coin names.</summary>*/
		private static void Patch_LanguageManager_LoadLanguage() {
			var loadLanguage = IL.GetMethodDefinition(LanguageManager, "LoadLanguage", 1);
			var _localizedTexts = IL.GetFieldDefinition(LanguageManager, "_localizedTexts");

			var onLoadCoinNames = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnLoadCoinNames"));

			IL.MethodInsert(loadLanguage, loadLanguage.Body.Instructions.Count - 2, new[] {
				Instruction.Create(OpCodes.Ldfld, _localizedTexts),
				Instruction.Create(OpCodes.Call, onLoadCoinNames),
				Instruction.Create(OpCodes.Ldarg_0)
			});
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
