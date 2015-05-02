using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arm7
{
    abstract class BaseMemoryController
    {
        public abstract long ld_byte(long addr);

        public abstract void st_byte(long addr, byte onebyte);

        public long ld_byte_fast(long addr)
        {
            return ld_byte(addr);
        }

        public void st_byte_fast(long addr, byte onebyte)
        {
            st_byte(addr, onebyte);
        }

        public long ld_halfword(long addr)
        {
            return (ld_byte(addr)
                    | (ld_byte(addr + 1) << 8));
        }

        public long ld_halfword_fast(long addr)
        {
            return ld_halfword(addr);
        }

        public void st_halfword(long addr, long halfword)
        {
            st_byte(addr, (byte)(halfword & 0xff));
            st_byte(addr + 1, (byte)(halfword >> 8));
        }

        public void st_halfword_fast(long addr, long halfword)
        {
            st_halfword(addr, halfword);
        }

        public long ld_word(long addr)
        {
            return (ld_halfword(addr)
                    | (ld_halfword(addr + 2) << 16));
        }

        public long ld_word_fast(long addr)
        {
            return ld_word(addr);
        }

        public void st_word(long addr, long word)
        {
            st_halfword(addr, word & 0xffff);
            st_halfword(addr + 2, word >> 16);
        }

        public void st_word_fast(long addr, long word)
        {
            st_word(addr, word);
        }

        public void st_word_unaligned(long addr, long word)
        {
            st_byte(addr, (byte)(word & 0xff));
            st_byte(addr + 1, (byte)((word >> 8) & 0xff));
            st_byte(addr + 2, (byte)((word >> 16) & 0xff));
            st_byte(addr + 3, (byte)(word >> 24));
        }
    }

    class VirtualMemoryController : BaseMemoryController
    {
        VirtualMemory vm;
        public VirtualMemoryController(VirtualMemory vm)
        {
            this.vm = vm;
        }

        public override long ld_byte(long addr)
        {
            PageStruct page = vm.Lookup(addr);
            return page.Data[page.Offset + addr % vm.page_size];
        }

        public override void st_byte(long addr, byte onebyte)
        {
            PageStruct page = vm.Lookup(addr);
            page.Data[page.Offset + addr % vm.page_size] = onebyte;
        }
    }

    class PageStruct
    {
        public byte[] Data { get; set; }
        public long Offset { get; set; }
    }

    class VirtualMemory
    {
        uint vm_size;
        public int page_size;
        long num_pages;
        PageStruct[] page_map;

        public VirtualMemory()
        {
            vm_size = uint.MaxValue;
            page_size = 4096;
            num_pages = vm_size / page_size;
            page_map = new PageStruct[num_pages];
        }

        public void Map(long addr, long size, byte[] region)
        {
            long rem1 = addr % page_size;
            long rem2 = size % page_size;
            if (rem1 != 0 || rem2 != 0)
                throw new Exception();

            for (long i = 0; i < size / page_size; i++)
            {
                long index = addr / page_size + i;
                page_map[index] = new PageStruct() { Data = region, Offset = i * page_size };
            }
        }

        public PageStruct Lookup(long addr)
        {
            return page_map[addr / page_size];
        }
    }

    class ARMv7VirtualMMU
    {
        VirtualMemory vm;

        public ARMv7VirtualMMU(VirtualMemory vm)
        {
            this.vm = vm;
        }

        public bool CheckUnaligned { get; set; }

        public long TranslateToPhysicalAddress(long addr, bool isWrite = false)
        {
            return addr;
        }
    }
}
