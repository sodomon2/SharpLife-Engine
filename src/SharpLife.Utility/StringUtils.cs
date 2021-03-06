﻿/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpLife.Utility
{
    public static class StringUtils
    {
        /// <summary>
        /// Determines the length in bytes of a null terminated string
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static int NullTerminatedByteLength(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            var stringLength = 0;

            while (stringLength < buffer.Length && buffer[stringLength] != (byte)'\0')
            {
                ++stringLength;
            }

            if (stringLength == buffer.Length)
            {
                throw new ArgumentException("Buffer does not contain a null terminated string", nameof(buffer));
            }

            return stringLength;
        }

        /// <summary>
        /// Encodes a string using the given encoding into the given buffer, and adds a null terminator
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="input"></param>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static int EncodeNullTerminatedString(Encoding encoding, string input, byte[] buffer, int index, int count)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var span = new Span<byte>(buffer, index, count);

            var bytesWritten = encoding.GetBytes(input, span);

            //Null terminate
            span[bytesWritten] = (byte)'\0';

            return bytesWritten;
        }

        /// <summary>
        /// <see cref="EncodeNullTerminatedString(Encoding, string, byte[], int, int)"/>
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="input"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static int EncodeNullTerminatedString(Encoding encoding, string input, byte[] buffer)
        {
            return EncodeNullTerminatedString(encoding, input, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Gets a string from a null terminated byte buffer
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GetStringFromNullTerminated(Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            var result = encoding.GetString(bytes);

            //Check if there's a null terminator, remove it if so
            var nullTerminatorIndex = result.IndexOf('\0');

            if (nullTerminatorIndex != -1)
            {
                result = result.Substring(0, nullTerminatorIndex);
            }

            return result;
        }

        public static class NewlineFormat
        {
            public const string Unix = "\n";

            public const string CarriageReturn = "\r";

            public const string Windows = "\r\n";

            public const string RISC = "\n\r";
        }

        /// <summary>
        /// Normalizes the newlines in a string to a given newline format
        /// </summary>
        /// <param name="input"></param>
        /// <param name="format">The format to normalize to. Defaults to <see cref="NewlineFormat.Unix"/></param>
        /// <returns></returns>
        public static string NormalizeNewlines(this string input, string format = NewlineFormat.Unix)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return Regex.Replace(input, @"\r\n|\n\r|\n|\r", format);
        }

        /// <summary>
        /// Returns whether this string equals the substring in the given string
        /// </summary>
        /// <param name="input"></param>
        /// <param name="compareTo"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool Equals(this string input, string compareTo, int index)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return string.CompareOrdinal(input, 0, compareTo, index, input.Length) == 0;
        }
    }
}
