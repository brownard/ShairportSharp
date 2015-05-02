using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arm7
{
    public class Loader
    {
        const long BREAKPOINT_INSTRUCTION = 0xFEDEFFE7;
        const int STACK_BOTTOM = 0x70000000;
        const long EXIT_ADDRESS = 0xF4F4F4F4;
        const int USER_MODE = 0x10;

        int heap_base = 0x40000000;
        List<Region> regions;
        DyldInfo dyldInfo;
        CPU cpu;
        VirtualMemory mem;
        VirtualMemoryController memctrl;
        Dictionary<long, string> hle_breakpoints = new Dictionary<long, string>();
        Dictionary<string, Action<CPU>> hle;

        public void Init(Stream airtunesdStream)
        {
            using (BinaryReader br = new BinaryReader(airtunesdStream))
            {
                regions = new List<Region>();
                regions.Add(new Region { addr = 4096, size = 1982464, data = br.ReadBytes(1982464) });
                br.BaseStream.Seek(1982464, SeekOrigin.Begin);
                regions.Add(new Region { addr = 1986560, size = 110592, data = br.ReadBytes(110592) });
                regions.Add(new Region { addr = 1986560 + 110592, size = 1228800 - 110592 });
                br.BaseStream.Seek(2093056, SeekOrigin.Begin);
                regions.Add(new Region { addr = 3215360, size = 53248, data = br.ReadBytes(53248) });
                regions.Add(new Region { addr = 3215360 + 53248, size = 57344 - 53248 });
                List<int> segments = new List<int> { 0, 4096, 1986560, 3215360 };
                br.BaseStream.Seek(0, SeekOrigin.Begin);
                dyldInfo = new DyldInfo(br, segments);
            }

            int stackSize = 0x20000;
            regions.Add(new Region { addr = STACK_BOTTOM - stackSize, size = stackSize });
            regions.Add(new Region { addr = heap_base, size = 0x200000 });

            long scratchAddr = 0x80000000;
            regions.Add(new Region { addr = scratchAddr, size = 0x20000 });

            mem = new VirtualMemory();
            foreach (Region region in regions)
                mem.Map(region.addr, region.size, region.data ?? new byte[region.size]);

            memctrl = new VirtualMemoryController(mem);

            loadBindings(dyldInfo.binds, scratchAddr);
            loadBindings(dyldInfo.lazy_binds, scratchAddr);

            ARMv7VirtualMMU mmu = new ARMv7VirtualMMU(mem);
            cpu = new CPU(memctrl, mmu);

            initHleMethods();
        }

        public long Call(int function, params long[] args)
        {
            int sp = STACK_BOTTOM;
            for (int i = args.Length - 1; i > 3; i--)
            {
                sp -= 4;
                cpu.st_word(sp, args[i]);
            }

            cpu.regs[0] = args.Length > 0 ? args[0] : 0;
            cpu.regs[1] = args.Length > 1 ? args[1] : 0;
            cpu.regs[2] = args.Length > 2 ? args[2] : 0;
            cpu.regs[3] = args.Length > 3 ? args[3] : 0;
            cpu.regs[13] = sp;
            cpu.regs[14] = EXIT_ADDRESS;
            cpu.regs[15] = function;
            cpu.cpsr.m = USER_MODE; //user mode
            run(EXIT_ADDRESS);
            return cpu.regs[0];
        }

        public long Malloc(int size)
        {
            long r = heap_base;
            heap_base += size;
            return r;
        }

        public void CopyIn(long addr, byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
                cpu.st_byte(addr + i, data[i]);
        }

        public byte[] CopyOut(long addr, long length)
        {
            byte[] r = new byte[length];
            for (int i = 0; i < length; i++)
                r[i] = (byte)cpu.ld_byte(addr + i);
            return r;
        }

        public long LoadWord(long addr)
        {
            return cpu.ld_word(addr);
        }

        public void StoreWord(long addr, long word)
        {
            cpu.st_word(addr, word);
        }

        void loadBindings(List<Symbol> bindings, long scratchAddr)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                long saddr = scratchAddr + i * 4;
                if (bindings[i].sym == "___stack_chk_guard")
                {
                    memctrl.st_word(saddr, 0);
                }
                else
                {
                    memctrl.st_word(saddr, BREAKPOINT_INSTRUCTION);
                    hle_breakpoints[saddr] = bindings[i].sym;
                }
                memctrl.st_word(bindings[i].addr, saddr);
            }
        }

        void run(long exit_addr = 0)
        {
            int cnt = 0;
            long pc = 0;
            long inst = 0;
            while (true)
            {
                cnt++;
                cpu.branch_to = -1;

                pc = cpu.regs[15];
                if (pc == exit_addr)
                    break;

                inst = cpu.fetch_instruction(pc);
                //Breakpoint insts
                if (inst == BREAKPOINT_INSTRUCTION)
                {
                    string name;
                    if (hle_breakpoints.TryGetValue(pc, out name))
                    {
                        Action<CPU> hleAction;
                        if (hle.TryGetValue(name, out hleAction))
                        {
                            hleAction(cpu);
                            continue;
                        }
                        else
                        {
                            throw new Exception("No hle for " + name);
                        }
                    }
                }

                if (cpu.is_valid(inst))
                {
                    if (cpu.cond(inst))
                    {
                        string inst_name = cpu.decode(inst, pc);
                        cpu.exec(inst_name, inst, pc);
                    }
                }
                else
                {
                    throw new Exception("invalid instruction " + inst.ToString("X"));
                }

                if (cpu.branch_to != -1)
                    cpu.regs[15] = cpu.branch_to;
                else
                    cpu.regs[15] = pc + 4;
            }
        }

        #region HLE Methods

        void initHleMethods()
        {
            hle = new Dictionary<string, Action<CPU>>()
            {
                {"_printf", hle_printf},
                {"_malloc", hle_malloc},
                {"_memcpy", hle_memcpy},
                {"_memset", hle_memset},
                {"_arc4random", hle_arc4random},
                {"___umodsi3", hle_umodsi3},
                {"___modsi3", hle_modsi3},
                {"___udivsi3", hle_udivsi3}
            };
        }

        void hle_printf(CPU cpu)
        {
            string format = cpu.ld_string(cpu.regs[0]);
            int num_params = format.Split('%').Length - 1;
            if (num_params > 3)
                //have to do stack shit.,,
                throw new NotImplementedException();
            else
            {
                string f = ""; //format % tuple(cpu.regs[1:num_params+1])
                int arg = 1;
                for (int i = 0; i < format.Length; i++)
                {
                    if (arg <= num_params && format[i] == '%')
                        f += cpu.regs[arg++];
                    else
                        f += format[i];
                }
                Console.WriteLine(f);
            }
            cpu.regs[0] = format.Length;
            cpu.regs[15] = cpu.regs[14];
        }

        void hle_malloc(CPU cpu)
        {
            long arg = cpu.regs[0];
            long r = Malloc((int)arg);
            if (r == 0)
                cpu.regs[0] = 0;
            else
                cpu.regs[0] = BitOps.uint32(r);
            cpu.regs[15] = cpu.regs[14];
        }

        void hle_memcpy(CPU cpu)
        {
            long dst = cpu.regs[0];
            long src = cpu.regs[1];
            long size = cpu.regs[2];
            for (int i = 0; i < size; i++)
                cpu.st_byte(dst + i, cpu.ld_byte(src + i));
            cpu.regs[0] = 0;
            cpu.regs[15] = cpu.regs[14];
        }

        void hle_memset(CPU cpu)
        {
            long dst = cpu.regs[0];
            long c = cpu.regs[1];
            long length = cpu.regs[2];
            for (long i = 0; i < length; i++)
                cpu.st_byte(dst + i, c);
            cpu.regs[0] = 0;
            cpu.regs[15] = cpu.regs[14];
        }

        void hle_arc4random(CPU cpu)
        {
            cpu.regs[0] = BitOps.uint32(4);
            cpu.regs[15] = cpu.regs[14];
        }

        void hle_umodsi3(CPU cpu)
        {
            long a = cpu.regs[0];
            long b = cpu.regs[1];
            long r;
            if (b == 0)
                r = 0;
            else
                r = a % b;
            cpu.regs[0] = BitOps.uint32(r);
            cpu.regs[15] = cpu.regs[14];
        }

        void hle_modsi3(CPU cpu)
        {
            long a = cpu.regs[0];
            long b = cpu.regs[1];
            long r = a - (a / b) * b;
            cpu.regs[0] = BitOps.uint32(r);
            cpu.regs[15] = cpu.regs[14];
        }

        void hle_udivsi3(CPU cpu)
        {
            long a = cpu.regs[0];
            long b = cpu.regs[1];
            long r = a / b;
            cpu.regs[0] = BitOps.uint32(r);
            cpu.regs[15] = cpu.regs[14];
        }

        #endregion
    }
}
