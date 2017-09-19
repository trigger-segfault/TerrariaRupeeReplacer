
/*
 * Implementation of LZX decoding,
 * a java port of LzxDecoder.cs from MonoGame 
 */

/* This file was derived from libmspack
 * (C) 2003-2004 Stuart Caie.
 * (C) 2011 Ali Scissons.
 *
 * The LZX method was created by Jonathan Forbes and Tomi Poutanen, adapted
 * by Microsoft Corporation.
 *
 * This source file is Dual licensed; meaning the end-user of this source file
 * may redistribute/modify it under the LGPL 2.1 or MS-PL licenses.
 */
// LGPL License
/* GNU LESSER GENERAL PUBLIC LICENSE version 2.1
 * LzxDecoder is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1 
 */
// MS-PL License
/* 
 * MICROSOFT PUBLIC LICENSE
 * This source code is subject to the terms of the Microsoft Public License (Ms-PL). 
 *  
 * Redistribution and use in source and binary forms, with or without modification, 
 * is permitted provided that redistributions of the source code retain the above 
 * copyright notices and this file header. 
 *  
 * Additional copyright notices should be appended to the list above. 
 * 
 * For details, see <http://www.opensource.org/licenses/ms-pl.html>. 
 */
/*
 * This derived work is recognized by Stuart Caie and is authorized to adapt
 * any changes made to lzxd.c in his libmspack library and will still retain
 * this dual licensing scheme. Big thanks to Stuart Caie!
 * 
 * DETAILS
 * This file is a pure C# port of the lzxd.c file from libmspack, with minor
 * changes towards the decompression of XNB files. The original decompression
 * software of LZX encoded data was written by Suart Caie in his
 * libmspack/cabextract projects, which can be located at 
 * http://http://www.cabextract.org.uk/
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaRupeeReplacer.Xnb {
	/**<summary>An exception thrown during Xnb extraction.</summary>*/
	public class XnbException : Exception {
		public XnbException(string message) : base(message) { }
		public XnbException(string message, Exception innerException) : base(message, innerException) { }
	}

	/**<summary>An extractor for Xnb files.</summary>*/
	public static class XnbExtractor {
		//========== CONSTANTS ===========
		#region Constants
		
		private const int SurfaceFormatColor = 0;
		private const int SurfaceFormatDxt1 = 4;
		private const int SurfaceFormatDxt3 = 5;
		private const int SurfaceFormatDxt5 = 6;
		
		private const int HeaderSize = 14;

		#endregion
		//=========== MEMBERS ============
		#region Members

		/**<summary>The compression decoder.</summary>*/
		private static LzxDecoder lzxDecoder = new LzxDecoder();

		#endregion
		//========== EXTRACTING ==========
		#region Extracting

		/**<summary>Extracts and returns a bitmap.</summary>*/
		public static Bitmap ExtractBitmap(string inputFile) {
			BinaryReader reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(inputFile)));

			if (!reader.ReadAndCompareString("XNB")) {
				throw new XnbException("Not an XNB file: " + Path.GetFileName(inputFile) + ".");
			}

			// Ignore target platform, it shouldn't matter
			int targetPlatform = reader.ReadByte();

			int version = reader.ReadByte();
			if (version != 5) {
				throw new XnbException("Unsupported XNB version: " + version + ".");
			}

			bool compressed = (reader.ReadByte() & 0x80) != 0;

			int compressedSize = reader.ReadInt32();
			int decompressedSize = (compressed ? reader.ReadInt32() : compressedSize);

			if (compressed) {
				MemoryStream decompressedStream = new MemoryStream(decompressedSize);

				lzxDecoder.Decompress(reader, compressedSize - HeaderSize, decompressedStream, decompressedSize);

				decompressedStream.Position = 0;

				reader.Close();
				reader = new BinaryReader(decompressedStream);
			}

			int typeReaderCount = reader.Read7BitEncodedInt();

			// The first type reader is used for reading the primary asset
			string typeReaderName = reader.Read7BitEncodedString();
			// The type reader version - Dosen't matter
			reader.ReadInt32();

			// Type reader names MIGHT contain assembly information
			int assemblyInformationIndex = typeReaderName.IndexOf(',');
			if (assemblyInformationIndex != -1)
				typeReaderName = typeReaderName.Substring(0, assemblyInformationIndex);

			// Skip the remaining type readers, as all types are known
			for (int k = 1; k < typeReaderCount; k++) {
				reader.Read7BitEncodedString();
				reader.ReadInt32();
			}

			// Shared resources are unused by Terraria assets
			if (reader.Read7BitEncodedInt() != 0) {
				throw new XnbException("Shared resources are not supported.");
			}

			if (reader.Read7BitEncodedInt() != 1) {
				throw new XnbException("Primary asset is null; this shouldn't happen.");
			}

			// Switch on the type reader name, excluding assembly information
			switch (typeReaderName) {
			case "Microsoft.Xna.Framework.Content.Texture2DReader":
				return ReadTexture2D(reader);

			default:
				throw new XnbException("Unsupported asset type: " + typeReaderName + ".");
			}
		}
		/**<summary>Reads an Xnb Texture2D.</summary>*/
		private static Bitmap ReadTexture2D(BinaryReader reader) {
			int surfaceFormat = reader.ReadInt32();
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();

			// Mip count
			int mipCount = reader.ReadInt32();
			// Size
			int size = reader.ReadInt32();

			if (mipCount < 1) {
				throw new XnbException("Unexpected mipCount: " + mipCount + ".");
			}

			byte[] source = reader.ReadBytes(size);

			if (surfaceFormat != SurfaceFormatColor) {
				// https://github.com/mcgrue/FNA/blob/master/src/Content/ContentReaders/Texture2DReader.cs
				if (surfaceFormat == SurfaceFormatDxt1)
					source = DxtUtil.DecompressDxt1(source, width, height);
				else if (surfaceFormat == SurfaceFormatDxt3)
					source = DxtUtil.DecompressDxt3(source, width, height);
				else if (surfaceFormat == SurfaceFormatDxt5)
					source = DxtUtil.DecompressDxt5(source, width, height);
				else
					throw new XnbException("Unsupported surface format: " + surfaceFormat + ".");
			}

			// Swap R and B channels
			for (int j = 0; j < width * height; j++) {
				byte swap = source[j * 4 + 0];
				source[j * 4 + 0] = source[j * 4 + 2];
				source[j * 4 + 2] = swap;
			}

			// Write to the bitmap
			Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			IntPtr data = bmpData.Scan0;
			Marshal.Copy(source, 0, data, source.Length);
			bmp.UnlockBits(bmpData);

			return bmp;
		}

		#endregion
	}
}
