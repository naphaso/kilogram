using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Utils {
    public static class Extensions {
        public static IEnumerable<uint> ToUtf32(this string chars) {
            int num = 0;
            while (num < chars.Length) {
                string str = chars;
                int num1 = num;
                int num2 = num1;
                num = num1 + 1;
                char chr = str[num2];
                if (chr < '\uD800' || chr >= '\uE000') {
                    yield return chr;
                } else {
                    uint num3 = chr;
                    if ((num3 & 64512) != 55296) {
                        throw new InvalidOperationException(string.Concat("1st char in surrogate pair - Unexpected char ", num3.ToString("x")));
                    }
                    num3 = (num3 & 1023) << 10;
                    string str1 = chars;
                    int num4 = num;
                    int num5 = num4;
                    num = num4 + 1;
                    uint num6 = str1[num5];
                    if ((num6 & 64512) != 56320) {
                        throw new InvalidCastException(string.Concat("2nd char in surrogate pair - unexpected char ", num6.ToString("x")));
                    }
                    yield return (num3 | num6 & 1023) + 65536;
                }
            }
        }

        public static IEnumerable<char> ToUtf16(this IEnumerable<uint> chars) {
            uint num;
            uint current;
            using (IEnumerator<uint> enumerator = chars.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    current = enumerator.Current;
                    if (current >= 55296 && current < 57344) {
                        num = current;
                        throw new InvalidOperationException(string.Concat("Invalid char ", num.ToString("x")));
                    }
                    if (current < 65536 || current >= 1114112) {
                        yield return (char)(ushort)current;
                    } else {
                        uint num1 = 55296;
                        uint num2 = 56320;
                        uint num3 = current;
                        num3 = num3 - 65536;
                        num2 = num2 | num3 & 1023;
                        num3 = num3 >> 10;
                        num1 = num1 | num3 & 1023;
                        yield return (char)(ushort)num1;
                        yield return (char)(ushort)num2;
                    }
                }
            }
        }

    }
}
