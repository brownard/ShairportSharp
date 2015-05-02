using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arm7
{
    class BitOps
    {
        public static long xor(long x, long y)
        {
            return x ^ y;
        }

        public static long xor64(long x, long y)
        {
            return xor(x, y);
        }

        public static long and(long x, long y)
        {
            return x & y;
        }

        public static long and64(long x, long y)
        {
            return and(x, y);
        }

        public static long or(long x, long y)
        {
            return x | y;
        }

        public static long or64(long x, long y)
        {
            return or(x, y);
        }

        public static long not(long x)
        {
            return (~x) & 0xffffffff;
        }

        public static long lowest_set_bit(long val, int len)
        {
            for (int i = 0; i < len; i++)
            {
                if ((val & 1 << i) != 0)
                    return i;
            }
            return len;
        }

        public static long bit_count(long val, int len)
        {
            int count = 0;
            for (int i = 0; i < len; i++)
            {
                if ((val & 1 << i) != 0)
                    count++;
            }
            return count;
        }

        public static long clear_bit(long val, int pos)
        {
            return val & ~(1 << pos);
        }

        public static long clear_bits(long val, int start, int end)
        {
            if (val == 0)
                return val;
            //if (val < 0x80000000 && start < 31)
                return val & ~(((1 << (start + 1)) - 1) & ~((1 << end) - 1));
            //if (start < 31)
            //{
            //    var ret = val & ~(((1 << (start + 1)) - 1) & ~((1 << end) - 1));
            //    if (ret < 0)
            //        ret += 0x100000000;
            //    return ret;
            //}
            //var uints = Convert.ToString(val, 2);
            //var ret1 = "";
            //for (var i = 0; i < 32; i++)
            //{
            //    if ((32 - i - 1) <= start && (32 - i - 1) >= end)
            //        ret1 += "0";
            //    else
            //        ret1 += uints[i];
            //}
            //return Convert.ToInt64(ret1, 2);
        }

        public static long set_bits(long val, int start, int end, long val1)
        {
            return or(clear_bits(val, start, end), lsl(val1, end));
        }

        public static long set_bit(long val, int pos, long val1)
        {
            if (val1 != 0)
                return val | (val1 << pos);
            else
                return val & not(1 << pos);
        }

        public static long get_bit(long val, int pos)
        {
            return (val >> pos) & 1;
        }

        public static long get_bit64(long val, int pos)
        {
            return (val >> pos) & 1;
        }

        public static long zero_extend(long val, int n)
        {
            return val;
        }

        public static long zero_extend64(long val, int n)
        {
            return val;
        }

        public static long get_bits(long val, int start, int end)
        {
            //assert(end != undefined, "get_bits: missing 3rd argument");
            if (start == 31)
            {
                if (end != 0)
                    return val >> end;
                if (val > 0xffffffff)
                    and(val, 0xffffffff);
                else
                    return val;
            }
            //return this.and(uint >>> end, ((1 << (start - end + 1)) - 1));
            var ret = (val >> end) & ((1 << (start - end + 1)) - 1);
            if (ret >= 0x100000000)
                return ret - 0x100000000;
            else
                return ret;
        }

        public static long get_bits64(long val, int start, int end)
        {
            if (val < 0x80000000 && start < 31 && end < 31)
                return get_bits(val, start, end);
            var ulong_h = (long)Math.Floor((double)(val / 0x100000000));
            var ulong_l = val % 0x100000000;
            long ret = 0;
            if (start > 31)
            {
                if (start == 32)
                {
                    ret += get_bit(ulong_h, 0) << (31 - end + 1);
                }
                else
                {
                    if (end > 31)
                        ret += get_bits(ulong_h, start - 32, end - 32);
                    else
                        ret += get_bits(ulong_h, start - 31, 0) << (31 - end + 1);
                }
            }
            if (end <= 31)
            {
                if (end == 31)
                    ret += get_bit(ulong_l, 31);
                else
                    ret += get_bits(ulong_l, start < 31 ? start : 31, end);
            }
            return ret;
        }

        public static long sign_extend(long x, int x_len, int n)
        {
            var sign = get_bit(x, x_len - 1);
            if (sign != 0)
            {
                /*
                var extend = "";
                for (var i=0; i < (n-x_len); i++)
                    extend += "1";
                var str = extend + toStringBin(x, x_len);
                return parseInt32(str, 2);
                */
                long tmp;
                if (n == 32)
                    tmp = 0xffffffff;
                else
                    tmp = (1 << n) - 1;
                //return x | (tmp & ~((1 << x_len)-1));
                var ret = x | (tmp & ~((1 << x_len) - 1));
                if (ret < 0)
                    return ret + 0x100000000;
                else
                    return ret;
            }
            else
                return x;
        }

        public static long lsl(long x, int n)
        {
            var ret = x << n;
            if (ret >= 0 && ret >= x)
            {
                return ret;
            }
            else
            {
                return x * (long)Math.Pow(2, n);
            }
        }

        public static long lsr(long x, int n)
        {
            return (n == 32) ? 0 : x >> n;
        }

        public static long asr(long x, int n)
        {
            if (n == 32)
                return 0;
            var ret = x >> n;
            //if (ret < 0)
                //ret += 0x100000000;
            return ret;
        }

        public static long sint32(long x)
        {
            x &= 0xffffffff;
            if (x >= 0x80000000)
                return x - 0x100000000;
            return x;
        }

        public static long uint32(long x)
        {
            return x & 0xffffffff;
        }

        public static long toUint32(long x)
        {
            return x & 0xffffffff;
        }

        public static long copy_bits(long dest, int start, int end, long src)
        {
            return set_bits(dest, start, end, get_bits(src, start, end));
        }

        public static long copy_bit(long dest, int pos, long src)
        {
            return set_bit(dest, pos, get_bit(src, pos));
        }

        public static long ror(long value, int amount)
        {
            var m = amount % 32;
            //var lo = this.get_bits(value, m-1, 0);
            //var result = this.or(value >>> m, this.lsl(lo, (32-m)));
            var lo = value & ((1 << m) - 1);
            var result = (value >> m) + lsl(lo, (32 - m));
            //assert(result >= 0 && result <= 0xffffffff, "ror");
            return result;
        }

        public static long count_leading_zero_bits(long val)
        {
            var n = 0;
            for (int i = 31; i >= 0; i--)
            {
                if (get_bit(val, i) != 0)
                    break;
                n++;
            }
            return n;
        }

    }
}
