/*
 * This file is part of SC4LuaFileHash, a utility that
 * that recreates the Lua file name hashing used by SimCity 4.
 *
 * Copyright (C) 2025 Nicholas Hayes
 *
 * SC4LuaFileHash is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 *
 * SC4LuaFileHash is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with SC4LuaFileHash.
 * If not, see <http://www.gnu.org/licenses/>.
 */

namespace SC4LuaFileHash
{
    internal static class RZHash
    {
        private static readonly uint[] crc24HashTable = BuildCrc24HashTable();

        public static uint Crc24(ReadOnlySpan<char> span, bool caseInsensitive)
        {
            uint crc = 0x00B704CE;

            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];

                if (caseInsensitive)
                {
                    c = char.ToLower(c);
                }

                crc = (crc << 8) ^ crc24HashTable[((crc >> 16) ^ c) & 0xFF];
            }

            return crc & 0x00FFFFFF;
        }

        private static uint[] BuildCrc24HashTable()
        {
            uint[] table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i << 16;

                for (int j = 0; j < 8; j++)
                {
                    crc <<= 1;
                    if ((crc & 0x01000000) != 0)
                    {
                        crc ^= 0x01864CFB;
                    }
                }

                table[i] = crc;
            }

            return table;
        }
    }
}
