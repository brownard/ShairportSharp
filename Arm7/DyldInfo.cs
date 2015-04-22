using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arm7
{
    class DyldInfo
    {
        #region Consts
        const int BIND_TYPE_POINTER = 1;
        const int BIND_TYPE_TEXT_ABSOLUTE32 = 2;
        const int BIND_TYPE_TEXT_PCREL32 = 3;

        const int BIND_SPECIAL_DYLIB_SELF = 0;
        const int BIND_SPECIAL_DYLIB_MAIN_EXECUTABLE = -1;
        const int BIND_SPECIAL_DYLIB_FLAT_LOOKUP = -2;

        const int BIND_SYMBOL_FLAGS_WEAK_IMPORT = 0x1;
        const int BIND_SYMBOL_FLAGS_NON_WEAK_DEFINITION = 0x8;

        const int BIND_OPCODE_MASK = 0xF0;
        const int BIND_IMMEDIATE_MASK = 0x0F;
        const int BIND_OPCODE_DONE = 0x00;
        const int BIND_OPCODE_SET_DYLIB_ORDINAL_IMM = 0x10;
        const int BIND_OPCODE_SET_DYLIB_ORDINAL_ULEB = 0x20;
        const int BIND_OPCODE_SET_DYLIB_SPECIAL_IMM = 0x30;
        const int BIND_OPCODE_SET_SYMBOL_TRAILING_FLAGS_IMM = 0x40;
        const int BIND_OPCODE_SET_TYPE_IMM = 0x50;
        const int BIND_OPCODE_SET_ADDEND_SLEB = 0x60;
        const int BIND_OPCODE_SET_SEGMENT_AND_OFFSET_ULEB = 0x70;
        const int BIND_OPCODE_ADD_ADDR_ULEB = 0x80;
        const int BIND_OPCODE_DO_BIND = 0x90;
        const int BIND_OPCODE_DO_BIND_ADD_ADDR_ULEB = 0xA0;
        const int BIND_OPCODE_DO_BIND_ADD_ADDR_IMM_SCALED = 0xB0;
        const int BIND_OPCODE_DO_BIND_ULEB_TIMES_SKIPPING_ULEB = 0xC0;


        const int REBASE_TYPE_POINTER = 1;
        const int REBASE_TYPE_TEXT_ABSOLUTE32 = 2;
        const int REBASE_TYPE_TEXT_PCREL32 = 3;

        const int REBASE_OPCODE_MASK = 0xF0;
        const int REBASE_IMMEDIATE_MASK = 0x0F;
        const int REBASE_OPCODE_DONE = 0x00;
        const int REBASE_OPCODE_SET_TYPE_IMM = 0x10;
        const int REBASE_OPCODE_SET_SEGMENT_AND_OFFSET_ULEB = 0x20;
        const int REBASE_OPCODE_ADD_ADDR_ULEB = 0x30;
        const int REBASE_OPCODE_ADD_ADDR_IMM_SCALED = 0x40;
        const int REBASE_OPCODE_DO_REBASE_IMM_TIMES = 0x50;
        const int REBASE_OPCODE_DO_REBASE_ULEB_TIMES = 0x60;
        const int REBASE_OPCODE_DO_REBASE_ADD_ADDR_ULEB = 0x70;
        const int REBASE_OPCODE_DO_REBASE_ULEB_TIMES_SKIPPING_ULEB = 0x80;
        #endregion

        public List<Symbol> binds;
        public List<Symbol> lazy_binds;

        public DyldInfo(BinaryReader f, List<int> segs)
        {
            f.BaseStream.Seek(Cmd.rebase_off, SeekOrigin.Begin);
            List<long> rebases = read_rebases(f, Cmd.rebase_size, segs);

            f.BaseStream.Seek(Cmd.bind_off, SeekOrigin.Begin);
            binds = read_binds(f, Cmd.bind_size, segs);

            f.BaseStream.Seek(Cmd.weak_bind_off, SeekOrigin.Begin);
            List<Symbol> week_binds = read_binds(f, Cmd.weak_bind_size, segs);

            f.BaseStream.Seek(Cmd.lazy_bind_off, SeekOrigin.Begin);
            lazy_binds = read_binds(f, Cmd.lazy_bind_size, segs);

            List<Symbol> exports = new List<Symbol>();
            walk_trie(f, Cmd.export_off, Cmd.export_off, Cmd.export_off + Cmd.export_size, "", exports);

        }

        string readString(BinaryReader br)
        {
            string r = "";
            while (true)
            {
                byte c = br.ReadByte();
                if (c == 0x00)
                    break;
                r += Convert.ToChar(c);
            }
            return r;
        }

        int readULeb128(BinaryReader br)
        {
            //Read an unsigned little-endian base-128 integer
            int res = 0;
            int bit = 0;
            while (true)
            {
                byte c = br.ReadByte();
                int s = c & 0x7f;
                res |= s << bit;
                bit += 7;
                if ((c & 0x80) == 0)
                    break;
            }
            return res;
        }

        int readSLeb128(BinaryReader br)
        {
            //Read a signed little-endian base-128 integer
            int res = 0;
            int bit = 0;
            byte c;
            while (true)
            {
                c = br.ReadByte();
                int s = c & 0x7f;
                res |= s << bit;
                bit += 7;
                if ((c & 0x80) == 0)
                    break;
            }
            if ((c & 0x40) != 0)
                res |= (-1) << bit;

            return res;
        }

        List<long> read_rebases(BinaryReader br, int size, List<int> segs, int ptrwidth = 4)
        {
            long addr = 0;
            List<long> rebases = new List<long>();

            long end = br.BaseStream.Position + size;
            while (br.BaseStream.Position < end)
            {
                byte c = br.ReadByte();
                int opcode = c & REBASE_OPCODE_MASK;
                int imm = c & REBASE_IMMEDIATE_MASK;

                if (opcode == REBASE_OPCODE_DONE)
                {

                }
                else if (opcode == REBASE_OPCODE_SET_TYPE_IMM)
                {
                    //assert imm == REBASE_TYPE_POINTER
                }
                else if (opcode == REBASE_OPCODE_SET_SEGMENT_AND_OFFSET_ULEB)
                    addr = segs[imm] + readULeb128(br);
                else if (opcode == REBASE_OPCODE_ADD_ADDR_ULEB)
                    addr = (addr + readULeb128(br)) % (long)Math.Pow(2, 64);
                else if (opcode == REBASE_OPCODE_ADD_ADDR_IMM_SCALED)
                    addr += imm * ptrwidth;
                else if (opcode == REBASE_OPCODE_DO_REBASE_IMM_TIMES)
                {
                    for (int i = 0; i < imm; i++)
                        rebases.Add(addr);
                    addr += ptrwidth;
                }
                else if (opcode == REBASE_OPCODE_DO_REBASE_ULEB_TIMES)
                {
                    int count = readULeb128(br);
                    for (int i = 0; i < count; i++)
                    {
                        rebases.Add(addr);
                        addr += ptrwidth;
                    }
                }
                else if (opcode == REBASE_OPCODE_DO_REBASE_ADD_ADDR_ULEB)
                {
                    rebases.Add(addr);
                    addr += ptrwidth + readULeb128(br);
                }
                else if (opcode == REBASE_OPCODE_DO_REBASE_ULEB_TIMES_SKIPPING_ULEB)
                {
                    int count1 = readULeb128(br);
                    int skip = readULeb128(br);
                    for (int i = 0; i < count1; i++)
                    {
                        rebases.Add(addr);
                        addr += skip + ptrwidth;
                    }
                }
                else
                    throw new NotImplementedException();
            }
            return rebases;
        }

        List<Symbol> read_binds(BinaryReader f, int size, List<int> segs, int ptrwidth = 4)
        {
            int libord = 0;
            string sym = null;
            long addr = 0;

            List<Symbol> symbols = new List<Symbol>();

            long end = f.BaseStream.Position + size;
            while (f.BaseStream.Position < end)
            {
                byte c = f.ReadByte();
                int opcode = c & BIND_OPCODE_MASK;
                int imm = c & BIND_IMMEDIATE_MASK;

                if (opcode == BIND_OPCODE_DONE)
                { }
                else if (opcode == BIND_OPCODE_SET_DYLIB_ORDINAL_IMM)
                    libord = imm;
                else if (opcode == BIND_OPCODE_SET_DYLIB_ORDINAL_ULEB)
                    libord = readULeb128(f);
                else if (opcode == BIND_OPCODE_SET_DYLIB_SPECIAL_IMM)
                    libord = imm != 0 ? (imm | 0xf0) : 0;
                else if (opcode == BIND_OPCODE_SET_SYMBOL_TRAILING_FLAGS_IMM)
                    sym = readString(f);
                else if (opcode == BIND_OPCODE_SET_TYPE_IMM)
                {
                    //assert imm == BIND_TYPE_POINTER
                }
                else if (opcode == BIND_OPCODE_SET_ADDEND_SLEB)
                    readSLeb128(f);
                else if (opcode == BIND_OPCODE_SET_SEGMENT_AND_OFFSET_ULEB)
                    addr = segs[imm] + readULeb128(f);
                else if (opcode == BIND_OPCODE_ADD_ADDR_ULEB)
                    addr = (addr + readULeb128(f)) % (long)Math.Pow(2, 64);
                else if (opcode == BIND_OPCODE_DO_BIND)
                {
                    symbols.Add(new Symbol(sym, addr, libord));
                    addr += ptrwidth;
                }
                else if (opcode == BIND_OPCODE_DO_BIND_ADD_ADDR_ULEB)
                {
                    symbols.Add(new Symbol(sym, addr, libord));
                    addr += ptrwidth + readULeb128(f);
                }
                else if (opcode == BIND_OPCODE_DO_BIND_ADD_ADDR_IMM_SCALED)
                {
                    symbols.Add(new Symbol(sym, addr, libord));
                    addr += (imm + 1) * ptrwidth;
                }
                else if (opcode == BIND_OPCODE_DO_BIND_ULEB_TIMES_SKIPPING_ULEB)
                {
                    int count = readULeb128(f);
                    int skip = readULeb128(f);
                    for (int i = 0; i < count; i++)
                        symbols.Add(new Symbol(sym, addr, libord));
                    addr += skip + ptrwidth;
                }
                else
                    throw new NotImplementedException();
            }
            return symbols;
        }

        void walk_trie(BinaryReader f, int start, int cur, int end, string prefix, List<Symbol> symbols)
        {
            if (cur >= end)
                return;

            f.BaseStream.Seek(cur, SeekOrigin.Begin);
            int termSize = f.ReadByte();
            if (termSize != 0)
            {
                string sym = prefix;
                readULeb128(f);
                int addr = readULeb128(f);
                symbols.Add(new Symbol(sym, addr, 0));
            }
            f.BaseStream.Seek(cur + termSize + 1, SeekOrigin.Begin);
            int childCount = f.ReadByte();
            for (int i = 0; i < childCount; i++)
            {
                string suffix = readString(f);
                int offset = readULeb128(f);
                long lastPos = f.BaseStream.Position;
                walk_trie(f, start, start + offset, end, prefix + suffix, symbols);
                f.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            }
        }
    }

    class Symbol
    {
        public Symbol(string sym, long addr, int libord)
        {
            this.sym = sym;
            this.addr = addr;
            this.libord = libord;
        }

        public string sym;
        public long addr;
        public int libord;
    }
}
