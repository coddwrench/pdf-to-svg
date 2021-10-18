// Copyright 2015 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// This is part of java port of project hosted at https://github.com/google/woff2
namespace  IText.IO.Font.Woff2 {
    // Helper functions for woff2 variable length types: 255UInt16 and UIntBase128
    internal class VariableLength {
        // Based on section 6.1.1 of MicroType Express draft spec
        public static int Read255UShort(Buffer buf) {
            var kWordCode = 253;
            var kOneMoreByteCode2 = 254;
            var kOneMoreByteCode1 = 255;
            var kLowestUCode = 253;
            byte code = 0;
            code = buf.ReadByte();
            if (JavaUnsignedUtil.AsU8(code) == kWordCode) {
                var result = buf.ReadShort();
                return JavaUnsignedUtil.AsU16(result);
            }

            if (JavaUnsignedUtil.AsU8(code) == kOneMoreByteCode1) {
	            var result = buf.ReadByte();
	            return JavaUnsignedUtil.AsU8(result) + kLowestUCode;
            }

            if (JavaUnsignedUtil.AsU8(code) == kOneMoreByteCode2) {
	            var result = buf.ReadByte();
	            return JavaUnsignedUtil.AsU8(result) + kLowestUCode * 2;
            }

            return JavaUnsignedUtil.AsU8(code);
        }

        public static int ReadBase128(Buffer buf) {
            var result = 0;
            for (var i = 0; i < 5; ++i) {
                byte code = 0;
                code = buf.ReadByte();
                // Leading zeros are invalid.
                if (i == 0 && JavaUnsignedUtil.AsU8(code) == 0x80) {
                    throw new FontCompressionException(FontCompressionException.READ_BASE_128_FAILED);
                }
                // If any of the top seven bits are set then we're about to overflow.
                if ((result & unchecked((int)(0xfe000000))) != 0) {
                    throw new FontCompressionException(FontCompressionException.READ_BASE_128_FAILED);
                }
                result = (result << 7) | (code & 0x7f);
                if ((code & 0x80) == 0) {
                    return result;
                }
            }
            // Make sure not to exceed the size bound
            throw new FontCompressionException(FontCompressionException.READ_BASE_128_FAILED);
        }
    }
}
