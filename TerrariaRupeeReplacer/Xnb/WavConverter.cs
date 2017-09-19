using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaRupeeReplacer.Xnb {
	/**<summary>An exception thrown during Wav to Xnb conversion.</summary>*/
	public class WavException : Exception {
		public WavException(string message) : base(message) { }
		public WavException(string message, Exception innerException)
			: base(message, innerException) { }
	}

	/**<summary>A Wav to Xnb Converter.</summary>*/
	public static class WavConverter {
		//========== CONSTANTS ===========
		#region Constants

		private const string SoundEffectType = "Microsoft.Xna.Framework.Content.SoundEffectReader";

		#endregion
		//========== CONVERTING ==========
		#region Converting

		/**<summary>Converts the specified wave from stream and writes it to the output file.</summary>*/
		public static bool Convert(Stream inputStream, string outputFile) {
			ushort wFormatTag;
			ushort nChannels;
			uint nSamplesPerSec;
			uint nAvgBytesPerSec;
			ushort nBlockAlign;
			ushort wBitsPerSample;

			int dataChunkSize;
			byte[] waveData;
			
			using (BinaryReader reader = new BinaryReader(inputStream)) {
				string format = new string(reader.ReadChars(4));
				if (format != "RIFF")
					throw new WavException("Invalid file format: " + format + ".");

				uint fileLength = reader.ReadUInt32();
				if (fileLength != inputStream.Length-8)
					throw new WavException("File length mismatch: " + fileLength + " - should be " + inputStream.Length + ".");

				format = new string(reader.ReadChars(4));
				if (format != "WAVE")
					throw new WavException("No WAVE tag.");

				format = new string(reader.ReadChars(4));
				if (format != "fmt ")
					throw new WavException("No fmt tag.");

				int a = reader.ReadInt32();
				if (a < 16)
					throw new WavException("Incorrect format length.");
				a += (int)reader.BaseStream.Position;

				if ((wFormatTag = reader.ReadUInt16()) != 1)
					throw new Exception("Unimplemented wav codec (must be PCM).");

				nChannels = reader.ReadUInt16();

				nSamplesPerSec = reader.ReadUInt32();

				nAvgBytesPerSec = reader.ReadUInt32();

				nBlockAlign = reader.ReadUInt16();

				wBitsPerSample = reader.ReadUInt16();

				reader.BaseStream.Position = a;

				format = new string(reader.ReadChars(4));
				if (format != "data") throw new WavException("No data tag.");

				if (nAvgBytesPerSec != (nSamplesPerSec * nChannels * (wBitsPerSample / 8)))
					throw new WavException("Average bytes per second number incorrect.");
				if (nBlockAlign != (nChannels * (wBitsPerSample / 8)))
					throw new WavException("Block align number incorrect.");

				dataChunkSize = reader.ReadInt32();
				waveData = reader.ReadBytes(dataChunkSize);
			}

			using (FileStream stream = new FileStream(outputFile, FileMode.OpenOrCreate)) {
				using (BinaryWriter writer = new BinaryWriter(stream)) {
					stream.SetLength(0);
					
					writer.Write(Encoding.UTF8.GetBytes("XNB")); // Format identifier
					writer.Write(Encoding.UTF8.GetBytes("w")); // TargetPlatform Windows
					writer.Write((byte)5); // XNB format version
										    
					writer.Write((byte)0); // Flag bits:
										   // File Size TODO
					writer.Write(dataChunkSize + 105); //??  61?
					writer.Write7BitEncodedInt(1); // Type Reader count

					writer.Write(SoundEffectType); // String reader name

					writer.Write(0); // reader version number
					writer.Write((byte)0); // shared Resource Count
					writer.Write((byte)1); // Object Primary asset data....?

					writer.Write(18); // Format size
					// Format
					writer.Write((ushort)wFormatTag); // ushort wFormatTag;
					writer.Write((ushort)nChannels); // ushort nChannels;
					writer.Write((uint)nSamplesPerSec); // uint nSamplesPerSec;
					writer.Write((uint)nAvgBytesPerSec); // uint nAvgBytesPerSec;
					writer.Write((ushort)nBlockAlign); // ushort nBlockAlign;
					writer.Write((ushort)wBitsPerSample); // ushort wBitsPerSample;
					writer.Write((ushort)0); // ushort cbSize;
						
					writer.Write(dataChunkSize); // Uint32 Data Size
					writer.Write(waveData); // Byte[data size] data
						
					writer.Write(0); // int32 loop start
					writer.Write(dataChunkSize / nBlockAlign); // int32 loop length

					// int32 duration
					writer.Write((int)(1000 * dataChunkSize / (nChannels * wBitsPerSample * nSamplesPerSec / 8)));
				}
			}
			return true;
		}
		/**<summary>Converts the specified wave from file and writes it to the output file.</summary>*/
		public static bool Convert(string inputFile, string outputFile) {
			using (FileStream stream = new FileStream(inputFile, FileMode.Open)) {
				return Convert(stream, outputFile);
			}
		}

		#endregion
	}
}
