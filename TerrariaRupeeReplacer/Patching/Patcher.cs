using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using TerrariaRupeeReplacer.Properties;
using System.Globalization;
using System.Collections;
using TerrariaRupeeReplacer.Xnb;
using TerrariaRupeeReplacer.Util;
using System.Threading;

namespace TerrariaRupeeReplacer.Patching {
	/**<summary>An exception thrown when the patcher is unable to locate the instructions to change.</summary>*/
	public class PatcherException : Exception {
		public PatcherException(string message) : base(message) { }
	}
	/**<summary>An exception thrown when the executable has already been patched.</summary>*/
	public class AlreadyPatchedException : Exception {
		public AlreadyPatchedException() : base("This executable has already been patched!") { }
	}

	/**<summary>The class for handling modification to the Terraria executable.</summary>*/
	public class Patcher {
		//========== CONSTANTS ===========
		#region Constants

		/**<summary>The list of required dlls and files to transfer.</summary>*/
		public static readonly string[] RequireFiles = {
			"RupeeReplacer.dll"
		};
		/**<summary>The collection of localization files.</summary>*/
		public static readonly Dictionary<string, byte[]> LocalizationFiles = new Dictionary<string, byte[]>() {
			{ "de-DE",   Resources.Terraria_Localization_Content_de_DE_RupeeReplacer   },
			{ "en-US",   Resources.Terraria_Localization_Content_en_US_RupeeReplacer   },
			{ "es-ES",   Resources.Terraria_Localization_Content_es_ES_RupeeReplacer   },
			{ "fr-FR",   Resources.Terraria_Localization_Content_fr_FR_RupeeReplacer   },
			{ "it-IT",   Resources.Terraria_Localization_Content_it_IT_RupeeReplacer   },
			{ "pl-PL",   Resources.Terraria_Localization_Content_pl_PL_RupeeReplacer   },
			{ "pt-BR",   Resources.Terraria_Localization_Content_pt_BR_RupeeReplacer   },
			{ "ru-RU",   Resources.Terraria_Localization_Content_ru_RU_RupeeReplacer   },
			{ "zh-Hans", Resources.Terraria_Localization_Content_zh_Hans_RupeeReplacer }
		};

		/**<summary>The name of the static field used to signal the exe has been patched.</summary>*/
		public const string AlreadyPatchedStaticField = "TriggersRupeeReplacer";
		
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
		/**<summary>The Terraria.ItemText type.</summary>*/
		private static TypeDefinition ItemText;
		/**<summary>The Terraria.Dust type.</summary>*/
		private static TypeDefinition Dust;
		/**<summary>The Terraria.Localization.LanguageManager type.</summary>*/
		private static TypeDefinition LanguageManager;

		/**<summary>The CoinReplacer type.</summary>*/
		private static TypeDefinition CoinReplacer;

		#endregion
		//=========== PATCHING ===========
		#region Patching
		//--------------------------------
		#region Patch & Restore

		/**<summary>Restores the Terraria backup.</summary>*/
		public static void Restore(bool removeFiles) {
			File.Copy(BackupPath, ExePath, true);

			if (removeFiles) {
				RemoveRequiredFiles();
				RemoveLocalizationFiles();
			}
		}
		/**<summary>Patches the Terraria executable.</summary>*/
		public static void Patch() {
			// Backup the file first
			if (!File.Exists(BackupPath)) {
				File.Copy(ExePath, BackupPath, false);
			}

			// Do this first so we don't bork the executable if copying fails
			CopyRequiredFiles();
			CopyLocalizationFiles();

			// Load the assembly
			var resolver = new EmbeddedAssemblyResolver();
			var parameters = new ReaderParameters{ AssemblyResolver = resolver };
			AsmDefinition = AssemblyDefinition.ReadAssembly(ExePath, parameters);
			ModDefinition = AsmDefinition.MainModule;
			
			// Get links to Terraria types that will have their functions modified
			Main = IL.GetTypeDefinition(ModDefinition, "Main");
			Dust = IL.GetTypeDefinition(ModDefinition, "Dust");
			ItemText = IL.GetTypeDefinition(ModDefinition, "ItemText");
			LanguageManager = IL.GetTypeDefinition(ModDefinition, "LanguageManager");

			// Get link and import CoinReplacer type
			CoinReplacer = ModDefinition.Import(typeof(CoinReplacer)).Resolve();
			
			// Check if we've already been patched
			if (IL.GetFieldDefinition(Main, AlreadyPatchedStaticField, false) != null)
				throw new AlreadyPatchedException();

			// Add a static field to let us know this exe has already been patched
			var objectType = IL.GetTypeReference(ModDefinition, "System.Object");
			IL.AddStaticField(Main, AlreadyPatchedStaticField, objectType);
			
			// Patch Terraria
			Patch_Main_DrawInventory();
			Patch_Main_GUIChatDrawInner();
			Patch_Main_MouseText_DrawItemTooltip();
			Patch_Main_ValueToCoins();

			Patch_Dust_UpdateDust();

			Patch_ItemText_NewText();
			Patch_ItemText_ValueToName();

			Patch_LanguageManager_LoadLanguage();
			
			// Save the modifications
			AsmDefinition.Write(ExePath);
			// Wait for the exe to be closed by AsmDefinition.Write()
			Thread.Sleep(400);
			IL.MakeLargeAddressAware(ExePath);
		}

		#endregion
		//--------------------------------
		#region Required Files

		/**<summary>Copies the required dlls and files to the Terraria folder.</summary>*/
		private static void CopyRequiredFiles() {
			try {
				foreach (string file in RequireFiles) {
					//string source = Path.Combine(AppDirectory, file);
					string destination = Path.Combine(ExeDirectory, file);
					//File.Copy(source, destination, true);
					EmbeddedResources.Extract(destination, file);
				}
			}
			catch (Exception ex) {
				throw new IOException("Error while trying to copy over required files.", ex);
			}
		}
		/**<summary>Removes all required dlls and files from the Terraria folder.</summary>*/
		private static void RemoveRequiredFiles() {
			try {
				foreach (string file in RequireFiles) {
					string path = Path.Combine(ExeDirectory, file);
					if (File.Exists(path))
						File.Delete(path);
				}
			}
			catch {
				// Oh well, no harm done if we don't remove these
			}
		}
		/**<summary>Copies over localization files.</summary>*/
		public static void CopyLocalizationFiles() {
			try {
				string localizationPath = Path.Combine(ExeDirectory, "Localization");
				if (!Directory.Exists(localizationPath))
					Directory.CreateDirectory(localizationPath);
				foreach (var pair in LocalizationFiles) {
					string path = Path.Combine(localizationPath, "Terraria.Localization.Content." + pair.Key + ".RupeeReplacer.json");
					File.WriteAllBytes(path, pair.Value);
				}
			}
			catch (Exception ex) {
				throw new IOException("Error while trying to copy over localization files.", ex);
			}
		}
		/**<summary>Removes all localization files.</summary>*/
		public static void RemoveLocalizationFiles() {
			try {
				string localizationPath = Path.Combine(ExeDirectory, "Localization");

				foreach (var pair in LocalizationFiles) {
					string path = Path.Combine(localizationPath, "Terraria.Localization.Content." + pair.Key + ".RupeeReplacer.json");
					if (File.Exists(path))
						File.Delete(path);
				}

				// Remove Localizations folder if empty
				if (!Directory.EnumerateFileSystemEntries(localizationPath).Any())
					Directory.Delete(localizationPath);
			}
			catch {
				// Oh well, no harm done if we don't remove these
			}
		}

		#endregion
		//--------------------------------
		#region Exception Throwing

		/**<summary>Performs a check to see if the starting point was found. Throws an exception otherwise.</summary>*/
		private static void CheckFailedToFindStart(int start, int index, string function) {
			if (start == -1)
				throw new PatcherException("Failed to find starting point '" + (index + 1) + "' for " + function);
		}
		/**<summary>Performs a check to see if the ending point was found. Throws an exception otherwise.</summary>*/
		private static void CheckFailedToFindEnd(int end, int index, string function) {
			if (end == -1)
				throw new PatcherException("Failed to find ending point '" + (index + 1) + "' for " + function);
		}
		/**<summary>Performs a check to see if the local variable was found. Throws an exception otherwise.</summary>*/
		private static void CheckFailedToFindVariable(VariableDefinition varDef, string varName, string function) {
			if (varDef == null)
				throw new PatcherException("Failed to find local variable '" + varName + "' for " + function);
		}

		#endregion
		//--------------------------------
		#region Patchers

		/**<summary>Patches reforge cost text.</summary>*/
		private static void Patch_Main_DrawInventory() {
			string functionName = "Terraria.Main.DrawInventory";

			var drawInventory = IL.GetMethodDefinition(Main, "DrawInventory", 0);
			var onReforgeCost = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnReforgeCost"));

			var checks = new IL.OperandCheck[] {
				IL.CheckField(OpCodes.Ldsfld, "Main::reforgeItem"),
				IL.CheckField(OpCodes.Ldfld, "Item::type"),
				IL.Check(OpCodes.Ldc_I4_0),
				IL.Check(OpCodes.Ble),
				IL.CheckSkipIndefinite(), // TMod fix
				IL.CheckField(OpCodes.Ldsfld, "Main::reforgeItem"),
				IL.CheckField(OpCodes.Ldfld, "Item::value"),
				IL.VarCheck(LocOpCodes.Stloc),
				IL.CheckSkipIndefinite(),
				IL.CheckField(OpCodes.Ldsfld, "Main::player"),
				IL.CheckField(OpCodes.Ldsfld, "Main::myPlayer"),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "Player::discount"),
				IL.Check(OpCodes.Brfalse_S),
				IL.CheckSkipIndefinite(),
				IL.VarCheck(LocOpCodes.Stloc),
				IL.Check(OpCodes.Ldstr, ""),
				IL.Check(LocOpCodes.Stloc)
			};

			int start = IL.ScanForInstructionPatternEnd(drawInventory, checks);
			CheckFailedToFindStart(start, 0, functionName);

			var num60 = IL.ScanForVariablePattern(drawInventory, checks);
			CheckFailedToFindVariable(num60, "num60", functionName);
			
			var text3 = IL.ScanForVariablePattern(drawInventory, start - 2,
				IL.Check(OpCodes.Ldstr, ""),
				IL.VarCheck(LocOpCodes.Stloc)
			);
			CheckFailedToFindVariable(text3, "text3", functionName);

			int end = IL.ScanForInstructionPattern(drawInventory, start,
				IL.CheckField(OpCodes.Ldsfld, "Main::spriteBatch"),
				IL.Check(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Ldc_I4, 130),
				IL.Check(OpCodes.Add),
				IL.Check(OpCodes.Conv_R4),
				IL.Check(OpCodes.Ldarg_0),
				IL.CheckField(OpCodes.Ldfld, "Main::invBottom"),
				IL.Check(OpCodes.Conv_R4),
				IL.Check(OpCodes.Ldc_I4_1),
				IL.CheckMethod(OpCodes.Call, "ItemSlot::DrawSavings")
			);
			CheckFailedToFindEnd(end, 0, functionName);

			IL.MethodReplaceRange(drawInventory, start, end,
				Instruction.Create(OpCodes.Ldloc_S, num60),
				Instruction.Create(OpCodes.Call, onReforgeCost),
				Instruction.Create(OpCodes.Stloc_S, text3)
			);
		}
		/**<summary>Patches heal cost tax collect text.</summary>*/
		private static void Patch_Main_GUIChatDrawInner() {
			string functionName = "Terraria.Main.GUIChatDrawInner";

			var guiChatDrawInner = IL.GetMethodDefinition(Main, "GUIChatDrawInner", 0);
			var onTaxCollect = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnTaxCollect"));
			var onNurseHeal = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnNurseHeal"));

			var focusText = IL.ScanForVariablePattern(guiChatDrawInner,
				IL.CheckField(OpCodes.Ldsfld, "Main::player"),
				IL.CheckField(OpCodes.Ldsfld, "Main::myPlayer"),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "Player::sign"),
				IL.Check(OpCodes.Ldc_I4_M1),
				IL.Check(OpCodes.Ble_S),
				IL.CheckField(OpCodes.Ldsfld, "Main::editSign"),
				IL.Check(OpCodes.Brfalse_S),
				IL.CheckField(OpCodes.Ldsfld, "Lang::inter"),
				IL.Check(OpCodes.Ldc_I4_S, (sbyte)47),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckMethod(OpCodes.Callvirt, "LocalizedText::get_Value"),
				IL.VarCheck(LocOpCodes.Stloc),
				IL.Check(OpCodes.Br),
				IL.CheckField(OpCodes.Ldsfld, "Lang::inter"),
				IL.Check(OpCodes.Ldc_I4_S, (sbyte)48),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckMethod(OpCodes.Callvirt, "LocalizedText::get_Value"),
				IL.VarCheck(LocOpCodes.Stloc),
				IL.Check(OpCodes.Br)
			);
			CheckFailedToFindVariable(focusText, "focusText", functionName);

			var color2 = IL.ScanForVariablePattern(guiChatDrawInner,
				IL.CheckField(OpCodes.Ldsfld, "Main::mouseTextColor"),
				IL.Check(OpCodes.Ldc_I4_2),
				IL.Check(OpCodes.Mul),
				IL.Check(OpCodes.Ldc_I4, (int)255),
				IL.Check(OpCodes.Add),
				IL.Check(OpCodes.Ldc_I4_3),
				IL.Check(OpCodes.Div),
				IL.Check(LocOpCodes.Stloc),
				IL.VarCheck(LocOpCodes.Ldloca),
				IL.Check(LocOpCodes.Ldloc),
				IL.CheckRepeat(3),
				IL.CheckMethod(OpCodes.Call, "Color::.ctor")
			);
			CheckFailedToFindVariable(color2, "color2", functionName);

			var num6 = IL.ScanForVariablePattern(guiChatDrawInner,
				IL.CheckField(OpCodes.Ldsfld, "Main::player"),
				IL.CheckField(OpCodes.Ldsfld, "Main::myPlayer"),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "Player::statLifeMax2"),
				IL.CheckField(OpCodes.Ldsfld, "Main::player"),
				IL.CheckField(OpCodes.Ldsfld, "Main::myPlayer"),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "Player::statLife"),
				IL.Check(OpCodes.Sub),
				IL.VarCheck(LocOpCodes.Stloc)
			);
			CheckFailedToFindVariable(num6, "num6", functionName);

			int start = IL.ScanForInstructionPatternEnd(guiChatDrawInner,
				IL.CheckField(OpCodes.Ldsfld, "Main::player"),
				IL.CheckField(OpCodes.Ldsfld, "Main::myPlayer"),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "Player::taxMoney"),
				IL.Check(OpCodes.Ldc_I4_0),
				IL.Check(OpCodes.Bgt_S),
				IL.CheckField(OpCodes.Ldsfld, "Lang::inter"),
				IL.Check(OpCodes.Ldc_I4_S, (sbyte)89),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckMethod(OpCodes.Callvirt, "LocalizedText::get_Value"),
				IL.Check(LocOpCodes.Stloc, focusText),
				IL.Check(OpCodes.Br)
			);
			CheckFailedToFindStart(start, 0, functionName);

			int end = IL.ScanForInstructionPatternEnd(guiChatDrawInner, start,
				IL.CheckField(OpCodes.Ldsfld, "Lang::inter"),
				IL.Check(OpCodes.Ldc_I4_S, (sbyte)89),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckMethod(OpCodes.Callvirt, "LocalizedText::get_Value"),
				IL.Check(OpCodes.Ldstr, " ("),
				IL.Check(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Ldstr, ")"),
				IL.CheckMethod(OpCodes.Call, "String::Concat"),
				IL.Check(LocOpCodes.Stloc, focusText)
			);
			CheckFailedToFindEnd(end, 0, functionName);

			end = IL.MethodReplaceRange(guiChatDrawInner, start, end,
				Instruction.Create(OpCodes.Ldloca_S, focusText),
				Instruction.Create(OpCodes.Ldloca_S, color2),
				Instruction.Create(OpCodes.Ldloca_S, num6),
				Instruction.Create(OpCodes.Call, onTaxCollect)
			);

			start = IL.ScanForInstructionPatternEnd(guiChatDrawInner, end,
				IL.CheckField(OpCodes.Ldsfld, "Main::npc"),
				IL.CheckField(OpCodes.Ldsfld, "Main::player"),
				IL.CheckField(OpCodes.Ldsfld, "Main::myPlayer"),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "Player::talkNPC"),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "NPC::type"),
				IL.Check(OpCodes.Ldc_I4_S, (sbyte)18),
				IL.Check(OpCodes.Bne_Un)
			);
			CheckFailedToFindStart(start, 1, functionName);

			end = IL.ScanForInstructionPatternEnd(guiChatDrawInner, start,
				IL.CheckField(OpCodes.Ldsfld, "Lang::inter"),
				IL.Check(OpCodes.Ldc_I4_S, (sbyte)54),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckMethod(OpCodes.Callvirt, "LocalizedText::get_Value"),
				IL.Check(OpCodes.Ldstr, " ("),
				IL.Check(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Ldstr, ")"),
				IL.CheckMethod(OpCodes.Call, "String::Concat"),
				IL.Check(LocOpCodes.Stloc, focusText)
			);
			CheckFailedToFindEnd(end, 1, functionName);

			IL.MethodReplaceRange(guiChatDrawInner, start, end,
				Instruction.Create(OpCodes.Ldloca_S, focusText),
				Instruction.Create(OpCodes.Ldloca_S, color2),
				Instruction.Create(OpCodes.Ldloca_S, num6),
				Instruction.Create(OpCodes.Call, onNurseHeal)
			);
		}
		/**<summary>Patches store buy and sell price text.</summary>*/
		private static void Patch_Main_MouseText_DrawItemTooltip() {
			string functionName = "Terraria.Main.MouseText_DrawItemTooltip";

			var mouseText_DrawItemTooltip = IL.GetMethodDefinition(Main, "MouseText_DrawItemTooltip", 4);
			var onNPCShopPrice = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnNPCShopPrice"));
			
			var storeValue = IL.ScanForVariablePattern(mouseText_DrawItemTooltip,
				IL.CheckField(OpCodes.Ldsfld, "Main::npcShop"),
				IL.Check(OpCodes.Ldc_I4_0),
				IL.Check(OpCodes.Ble),
				IL.CheckField(OpCodes.Ldsfld, "Main::HoverItem"),
				IL.CheckMethod(OpCodes.Callvirt, "Item::GetStoreValue"),
				IL.VarCheck(LocOpCodes.Stloc)
			);
			CheckFailedToFindVariable(storeValue, "storeValue", functionName);

			int start = IL.ScanForInstructionPatternEnd(mouseText_DrawItemTooltip,
				IL.Check(OpCodes.Ldsfld),
				IL.CheckMethod(OpCodes.Callvirt, "Item::GetStoreValue"),
				IL.Check(OpCodes.Ldc_I4_0),
				IL.Check(OpCodes.Ble)
			);
			CheckFailedToFindStart(start, 0, functionName);

			var array = IL.ScanForVariablePattern(mouseText_DrawItemTooltip, start,
				IL.CheckField(OpCodes.Ldsfld, "Main::HoverItem"),
				IL.CheckField(OpCodes.Ldfld, "Item::buy"),
				IL.Check(OpCodes.Brtrue_S),
				IL.VarCheck(LocOpCodes.Ldloc),
				IL.Check(LocOpCodes.Ldloc),
				IL.CheckField(OpCodes.Ldsfld, "Lang::tip")
			);
			CheckFailedToFindVariable(array, "array", functionName);
			var num4 = IL.ScanForVariablePattern(mouseText_DrawItemTooltip, start,
				IL.CheckField(OpCodes.Ldsfld, "Main::HoverItem"),
				IL.CheckField(OpCodes.Ldfld, "Item::buy"),
				IL.Check(OpCodes.Brtrue_S),
				IL.Check(LocOpCodes.Ldloc),
				IL.VarCheck(LocOpCodes.Ldloc),
				IL.CheckField(OpCodes.Ldsfld, "Lang::tip")
			);
			CheckFailedToFindVariable(num4, "num4", functionName);

			var array4 = IL.ScanForVariablePattern(mouseText_DrawItemTooltip, start,
				IL.VarCheck(LocOpCodes.Ldloc),
				IL.Check(LocOpCodes.Ldloc, num4),
				IL.Check(OpCodes.Ldstr, "Price"),
				IL.Check(OpCodes.Stelem_Ref)
			);
			if (IsTMod) {
				CheckFailedToFindVariable(array4, "array4", functionName);
			}

			var checks = new IL.OperandCheck[] {
				IL.VarCheck(LocOpCodes.Ldloca),
				IL.Check(OpCodes.Ldc_R4, 246f),
				IL.Check(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Mul),
				IL.Check(OpCodes.Conv_U1),

				IL.Check(OpCodes.Ldc_R4, 138f),
				IL.Check(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Mul),
				IL.Check(OpCodes.Conv_U1),

				IL.Check(OpCodes.Ldc_R4, 96f),
				IL.Check(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Mul),
				IL.Check(OpCodes.Conv_U1),

				IL.Check(LocOpCodes.Ldloc),
				IL.CheckMethod(OpCodes.Call, "Color::.ctor")
			};

			var color = IL.ScanForVariablePattern(mouseText_DrawItemTooltip, start, checks);
			CheckFailedToFindVariable(color, "color", functionName);

			int end = IL.ScanForInstructionPatternEnd(mouseText_DrawItemTooltip, start, checks);
			CheckFailedToFindEnd(end, 0, functionName);

			start = IL.MethodReplaceRange(mouseText_DrawItemTooltip, start, end,
				Instruction.Create(OpCodes.Ldloc_S, color),
				Instruction.Create(OpCodes.Ldloc_S, num4),
				Instruction.Create(OpCodes.Ldloc_S, array),
				Instruction.Create(OpCodes.Ldloc_S, storeValue),
				Instruction.Create(OpCodes.Call, onNPCShopPrice),
				Instruction.Create(OpCodes.Stloc_S, color)
			);
			if (IsTMod) {
				start = IL.MethodInsert(mouseText_DrawItemTooltip, start,
					Instruction.Create(OpCodes.Ldloc_S, array4),
					Instruction.Create(OpCodes.Ldloc_S, num4),
					Instruction.Create(OpCodes.Ldstr, "Price"),
					Instruction.Create(OpCodes.Stelem_Ref)
				);
			}
			IL.MethodInsert(mouseText_DrawItemTooltip, start,
				Instruction.Create(OpCodes.Ldloc_S, num4),
				Instruction.Create(OpCodes.Ldc_I4_1),
				Instruction.Create(OpCodes.Add),
				Instruction.Create(OpCodes.Stloc_S, num4)
			);
		}
		/**<summary>Patches death coin drop text.</summary>*/
		private static void Patch_Main_ValueToCoins() {
			var valueToCoins = IL.GetMethodDefinition(Main, "ValueToCoins", 1);
			var onValueToCoins = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnValueToCoins"));
			
			IL.MethodReplace(valueToCoins,
				Instruction.Create(OpCodes.Ldarg_0),
				Instruction.Create(OpCodes.Call, onValueToCoins),
				Instruction.Create(OpCodes.Ret)
			);
		}
		/**<summary>Patches coin glowing when moving.</summary>*/
		private static void Patch_Dust_UpdateDust() {
			string functionName = "Terraria.Dust.UpdateDust";

			var updateDust = IL.GetMethodDefinition(Dust, "UpdateDust", 0);
			var onCoinGlow = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnCoinGlow"));

			var checks = new IL.OperandCheck[] {
				IL.VarCheck(LocOpCodes.Ldloc),
				IL.CheckField(OpCodes.Ldfld, "Dust::type"),
				IL.Check(OpCodes.Ldc_I4, (int)244),
				IL.Check(OpCodes.Blt),
				IL.VarCheck(LocOpCodes.Ldloc),
				IL.CheckField(OpCodes.Ldfld, "Dust::type"),
				IL.Check(OpCodes.Ldc_I4, (int)247),
				IL.Check(OpCodes.Bgt)
			};

			int start = IL.ScanForInstructionPatternEnd(updateDust, checks);
			CheckFailedToFindStart(start, 0, functionName);

			var dust = IL.ScanForVariablePattern(updateDust, checks);
			CheckFailedToFindVariable(dust, "dust", functionName);

			int end = IL.ScanForInstructionPatternEnd(updateDust, start,
				IL.CheckMethod(OpCodes.Call, "Lighting::AddLight")
			);
			CheckFailedToFindEnd(end, 0, functionName);
			
			IL.MethodReplaceRange(updateDust, start, end,
				Instruction.Create(OpCodes.Ldloc_S, dust),
				Instruction.Create(OpCodes.Call, onCoinGlow)
			);
		}
		/**<summary>Patches coin pickup text.</summary>*/
		private static void Patch_ItemText_ValueToName() {
			var valueToName = IL.GetMethodDefinition(ItemText, "ValueToName", false, 0);
			var onValueToName = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnValueToName"));
			
			IL.MethodReplace(valueToName,
				Instruction.Create(OpCodes.Ldarg_0),
				Instruction.Create(OpCodes.Call, onValueToName),
				Instruction.Create(OpCodes.Ret)
			);
		}
		/**<summary>Patches coin pickup text.</summary>*/
		private static void Patch_ItemText_NewText() {
			string functionName = "Terraria.ItemText.NewText";

			var newText = IL.GetMethodDefinition(ItemText, "NewText", 4);
			var onCoinPickupText = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnCoinPickupText"));
			var onCoinPickupText2 = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnCoinPickupText2"));
			
			var checks = new IL.OperandCheck[] {
				IL.Check(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Brfalse),
				IL.CheckField(OpCodes.Ldsfld, "Main::itemText"),
				IL.VarCheck(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "ItemText::coinText"),
				IL.Check(OpCodes.Brfalse)
			};

			int start = IL.ScanForNthInstructionPatternEnd(newText, 1, checks);
			CheckFailedToFindStart(start, 0, functionName);

			var i = IL.ScanForNthVariablePattern(newText, 1, checks);
			CheckFailedToFindVariable(i, "i", functionName);

			var vector = IL.ScanForVariablePattern(newText, start,
				IL.CheckField(OpCodes.Ldsfld, "Main::fontMouseText"),
				IL.Check(LocOpCodes.Ldloc),
				IL.CheckMethod(OpCodes.Callvirt, "DynamicSpriteFont::MeasureString"),
				IL.VarCheck(LocOpCodes.Stloc)
			);
			CheckFailedToFindVariable(vector, "vector", functionName);

			int end = IL.ScanForInstructionPatternEnd(newText, start,
				IL.Check(OpCodes.Ldc_I4, (int)246),
				IL.Check(OpCodes.Ldc_I4, (int)138),
				IL.Check(OpCodes.Ldc_I4_S, (sbyte)96),
				IL.Check(OpCodes.Newobj),
				IL.Check(OpCodes.Stfld)
			);
			CheckFailedToFindEnd(end, 0, functionName);

			IL.MethodReplaceRange(newText, start, end, 
				Instruction.Create(OpCodes.Ldarg_0),
				Instruction.Create(OpCodes.Ldloc_S, i),
				Instruction.Create(OpCodes.Call, onCoinPickupText),
				Instruction.Create(OpCodes.Stloc_S, vector)
			);

			checks = new IL.OperandCheck[] {
				IL.CheckField(OpCodes.Ldsfld, "Main::itemText"),
				IL.VarCheck(LocOpCodes.Ldloc),
				IL.Check(OpCodes.Ldelem_Ref),
				IL.CheckField(OpCodes.Ldfld, "ItemText::coinText"),
				IL.Check(OpCodes.Brfalse)
			};

			start = IL.ScanForInstructionPatternEnd(newText, end + 5, checks);
			CheckFailedToFindStart(start, 1, functionName);

			var num2 = IL.ScanForVariablePattern(newText, end + 5, checks);
			CheckFailedToFindVariable(num2, "num2", functionName);

			end = IL.ScanForInstructionPatternEnd(newText, start,
				IL.Check(OpCodes.Ldc_I4, (int)246),
				IL.Check(OpCodes.Ldc_I4, (int)138),
				IL.Check(OpCodes.Ldc_I4_S, (sbyte)96),
				IL.Check(OpCodes.Newobj),
				IL.Check(OpCodes.Stfld)
			);
			CheckFailedToFindEnd(end, 1, functionName);
			
			IL.MethodReplaceRange(newText, start, end, 
				Instruction.Create(OpCodes.Ldarg_0),
				Instruction.Create(OpCodes.Ldloc_S, num2),
				Instruction.Create(OpCodes.Call, onCoinPickupText2)
			);
		}
		/**<summary>Patches coin names.</summary>*/
		private static void Patch_LanguageManager_LoadLanguage() {
			string functionName = "Terraria.LanguageManager.LoadLanguage";

			var loadLanguage = IL.GetMethodDefinition(LanguageManager, "LoadLanguage", 1);
			var onLoadLocalizations = ModDefinition.Import(IL.GetMethodDefinition(CoinReplacer, "OnLoadLocalizations"));

			int start = IL.ScanForInstructionPatternEnd(loadLanguage,
				IL.CheckMethod(OpCodes.Call, "LanguageManager::LoadFilesForCulture")
			);
			CheckFailedToFindStart(start, 0, functionName);

			IL.MethodInsert(loadLanguage, start,
				Instruction.Create(OpCodes.Ldarg_1),
				Instruction.Create(OpCodes.Call, onLoadLocalizations)
			);
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
