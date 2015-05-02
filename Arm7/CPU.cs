using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arm7
{
    class CPU
    {
        const int USR_MODE = 0x10;
        const int FIQ_MODE = 0x11;
        const int IRQ_MODE = 0x12;
        const int SVC_MODE = 0x13;
        const int MON_MODE = 0x16;
        const int ABT_MODE = 0x17;
        const int UND_MODE = 0x1b;
        const int SYS_MODE = 0x1f;

        Dictionary<long, string> mode2string;
        Dictionary<long, bool> is_good_mode;

        public long[] regs;
        long[] regs_usr;

        Dictionary<long, long> regs_svc;
        Dictionary<long, long> regs_mon;
        Dictionary<long, long> regs_abt;
        Dictionary<long, long> regs_und;
        Dictionary<long, long> regs_irq;
        Dictionary<long, long> regs_fiq;

        VirtualMemoryController memctlr;
        ARMv7VirtualMMU mmu;
        public PSR cpsr;
        PSR spsr_svc;
        PSR spsr_mon;
        PSR spsr_abt;
        PSR spsr_und;
        PSR spsr_irq;
        PSR spsr_fiq;

        int shift_t = 0;
        int shift_n = 0;
        long carry_out = 0;
        long overflow = 0;
        public long branch_to = 0;

        const int SRType_LSL = 0;
        const int SRType_LSR = 1;
        const int SRType_ASR = 2;
        const int SRType_RRX = 3;
        const int SRType_ROR = 4;

        Dictionary<string, bool> no_cond_insts;
        Dictionary<string, bool> allow_unaligned;

        bool saturated;
        //bool is_halted;
        string current;

        public CPU(VirtualMemoryController memctlr, ARMv7VirtualMMU mmu)
        {
            this.memctlr = memctlr;
            this.mmu = mmu;

            mode2string = new Dictionary<long, string>();
            mode2string[USR_MODE] = "USR";
            mode2string[FIQ_MODE] = "FIQ";
            mode2string[IRQ_MODE] = "IRQ";
            mode2string[SVC_MODE] = "SVC";
            mode2string[MON_MODE] = "MON";
            mode2string[ABT_MODE] = "ABT";
            mode2string[UND_MODE] = "UND";
            mode2string[SYS_MODE] = "SYS";

            is_good_mode = new Dictionary<long, bool>();
            is_good_mode[USR_MODE] = true;
            is_good_mode[FIQ_MODE] = true;
            is_good_mode[IRQ_MODE] = true;
            is_good_mode[SVC_MODE] = true;
            is_good_mode[ABT_MODE] = true;
            is_good_mode[UND_MODE] = true;
            is_good_mode[SYS_MODE] = true;

            this.regs = new long[16];
            for (int i = 0; i < 16; i++)
                this.regs[i] = 0;
            /*
             * regs[10]: SL:
             * regs[11]: FP:
             * regs[12]: IP: A general register
             * regs[13]: SP: Stack pointer
             * regs[14]: LR: Link register
             * regs[15]: PC: Program counter
             */

            this.regs_usr = new long[16];
            for (int i = 0; i < 16; i++)
                this.regs_usr[i] = 0;

            this.regs_svc = new Dictionary<long, long>();
            this.regs_svc[13] = 0;
            this.regs_svc[14] = 0;
            this.regs_mon = new Dictionary<long, long>();
            this.regs_mon[13] = 0;
            this.regs_mon[14] = 0;
            this.regs_abt = new Dictionary<long, long>();
            this.regs_abt[13] = 0;
            this.regs_abt[14] = 0;
            this.regs_und = new Dictionary<long, long>();
            this.regs_und[13] = 0;
            this.regs_und[14] = 0;
            this.regs_irq = new Dictionary<long, long>();
            this.regs_irq[13] = 0;
            this.regs_irq[14] = 0;
            this.regs_fiq = new Dictionary<long, long>();
            this.regs_fiq[8] = 0;
            this.regs_fiq[9] = 0;
            this.regs_fiq[10] = 0;
            this.regs_fiq[11] = 0;
            this.regs_fiq[12] = 0;
            this.regs_fiq[13] = 0;
            this.regs_fiq[14] = 0;

            cpsr = new PSR();
            spsr_svc = new PSR();
            spsr_mon = new PSR();
            spsr_abt = new PSR();
            spsr_und = new PSR();
            spsr_irq = new PSR();
            spsr_fiq = new PSR();

            this.shift_t = 0;
            this.shift_n = 0;
            this.carry_out = 0;
            this.overflow = 0;

            this.no_cond_insts = new Dictionary<string, bool>();
            this.no_cond_insts["cps"] = true;
            this.no_cond_insts["clrex"] = true;
            this.no_cond_insts["dsb"] = true;
            this.no_cond_insts["dmb"] = true;
            this.no_cond_insts["isb"] = true;

            this.allow_unaligned = new Dictionary<string, bool>();
            this.allow_unaligned["ldrh"] = true;
            this.allow_unaligned["ldrht"] = true;
            this.allow_unaligned["ldrsh_imm"] = true;
            this.allow_unaligned["ldrsh_reg"] = true;
            this.allow_unaligned["ldrsht"] = true;
            this.allow_unaligned["strh_imm"] = true;
            this.allow_unaligned["strh_reg"] = true;
            this.allow_unaligned["strht"] = true;
            this.allow_unaligned["tbh"] = true;
            this.allow_unaligned["ldr_imm"] = true;
            this.allow_unaligned["ldr_reg"] = true;
            this.allow_unaligned["ldr_lit"] = true;
            this.allow_unaligned["ldrt"] = true;
            this.allow_unaligned["str_imm"] = true;
            this.allow_unaligned["str_reg"] = true;
            this.allow_unaligned["strt"] = true;

            //this.is_halted = false;
            this.current = "";
        }

        public void Save() { }
        public void Restore() { }
        public void Dump() { }
        public void DumpStack() { }

        long get_pc()
        {
            return regs[15] + 8;
        }

        long reg(long i)
        {
            if (i == 15)
                return get_pc();
            return regs[i];
        }

        bool is_bad_mode(int mode)
        {
            switch (mode)
            {
                case SVC_MODE:
                case IRQ_MODE:
                case USR_MODE:
                case ABT_MODE:
                case FIQ_MODE:
                case UND_MODE:
                case SYS_MODE:
                    return false;
                case MON_MODE: // !HaveSecurityExt()
                default:
                    return true;
            }
        }

        bool is_priviledged()
        {
            return cpsr.m != USR_MODE;
        }

        bool is_user_or_system()
        {
            return cpsr.m == USR_MODE || cpsr.m == SYS_MODE;
        }

        bool is_secure()
        {
            return false;
        }

        int scr_get_aw()
        {
            return 1;
        }

        int scr_get_fw()
        {
            return 1;
        }

        int nsacr_get_rfr()
        {
            return 0;
        }

        //def sctlr_get_nmfi(self):
        //return self.coprocs[15].sctlr_get_nmfi()

        PSR parse_psr(long value)
        {
            PSR psr = new PSR();
            psr.n = value >> 31;
            psr.z = (value >> 30) & 1;
            psr.c = (int)(value >> 29) & 1;
            psr.v = (value >> 28) & 1;
            psr.q = (value >> 27) & 1;
            psr.e = (value >> 9) & 1;
            psr.a = (value >> 8) & 1;
            psr.i = (value >> 7) & 1;
            psr.f = (value >> 6) & 1;
            psr.t = (value >> 5) & 1;
            psr.m = value & 0x1f;
            return psr;
        }

        long psr_to_value(PSR psr)
        {
            long value = psr.m;
            value += psr.t << 5;
            value += psr.f << 6;
            value += psr.i << 7;
            value += psr.a << 8;
            value += psr.e << 9;
            value += psr.q << 27;
            value += psr.v << 28;
            value += psr.c << 29;
            value += psr.z << 30;
            value += psr.n << 31;
            return value;
        }

        PSR clone_psr(PSR src)
        {
            PSR dst = new PSR();
            dst.n = src.n;
            dst.z = src.z;
            dst.c = src.c;
            dst.v = src.v;
            dst.q = src.q;
            dst.e = src.e;
            dst.a = src.a;
            dst.i = src.i;
            dst.f = src.f;
            dst.t = src.t;
            dst.m = src.m;
            return dst;
        }

        void set_current_spsr(PSR spsr)
        {
            switch (this.cpsr.m)
            {
                case USR_MODE:
                    throw new Exception("set_current_spsr user");
                case FIQ_MODE:
                    this.spsr_fiq = spsr;
                    break;
                case IRQ_MODE:
                    this.spsr_irq = spsr;
                    break;
                case SVC_MODE:
                    this.spsr_svc = spsr;
                    break;
                case MON_MODE:
                    this.spsr_mon = spsr;
                    break;
                case ABT_MODE:
                    this.spsr_abt = spsr;
                    break;
                case UND_MODE:
                    this.spsr_und = spsr;
                    break;
                case SYS_MODE:
                    throw new Exception("set_current_spsr system user");
                default:
                    throw new Exception("set_current_spsr unknown");
            }
        }

        PSR get_current_spsr()
        {
            switch (this.cpsr.m)
            {
                case USR_MODE:
                    throw new Exception("get_current_spsr user");
                case FIQ_MODE:
                    return this.spsr_fiq;
                case IRQ_MODE:
                    return spsr_irq;
                case SVC_MODE:
                    return spsr_svc;
                case MON_MODE:
                    return spsr_mon;
                case ABT_MODE:
                    return spsr_abt;
                case UND_MODE:
                    return spsr_und;
                case SYS_MODE:
                    throw new Exception("get_current_spsr system user");
                default:
                    throw new Exception("get_current_spsr unknown");
            }
        }

        PSR spsr_write_by_instr0(PSR spsr, PSR psr, long bytemask)
        {
            if (this.is_user_or_system())
                this.abort_unpredictable("spsr_write_by_instr0", 0);
            if ((bytemask & 8) > 0)
            {
                spsr.n = psr.n;
                spsr.z = psr.z;
                spsr.c = psr.c;
                spsr.v = psr.v;
                spsr.q = psr.q;
            }
            if ((bytemask & 4) > 0)
            {
                //spsr.ge = psr.ge;
            }
            if ((bytemask & 2) > 0)
            {
                spsr.e = psr.e;
                spsr.a = psr.a;
            }
            if ((bytemask & 1) > 0)
            {
                spsr.i = psr.i;
                spsr.f = psr.f;
                spsr.t = psr.t;
                if (!this.is_good_mode[psr.m])
                    this.abort_unpredictable("spsr_write_by_instr0", psr.m);
                else
                    spsr.m = psr.m;
            }
            return spsr;
        }

        void spsr_write_by_instr(PSR psr, long bytemask)
        {
            PSR spsr = get_current_spsr();
            spsr_write_by_instr0(spsr, psr, bytemask);
            set_current_spsr(spsr);
        }

        void cpsr_write_by_instr(PSR psr, long bytemask, bool affect_execstate)
        {
            //var is_priviledged = this.is_priviledged();
            //bool nmfi = sctlr_get_nmfi() == 1;

            //if ((bytemask & 8) > 0)
            //{
            //    this.cpsr.n = psr.n;
            //    this.cpsr.z = psr.z;
            //    this.cpsr.c = psr.c;
            //    this.cpsr.v = psr.v;
            //    this.cpsr.q = psr.q;
            //}
            //if ((bytemask & 2) > 0)
            //{
            //    this.cpsr.e = psr.e;
            //    if (is_priviledged && (this.is_secure() || this.scr_get_aw() == 1))
            //        this.cpsr.a = psr.a;
            //}
            //if ((bytemask & 1) > 0)
            //{
            //    if (is_priviledged)
            //    {
            //        this.cpsr.i = psr.i;
            //    }
            //    if (is_priviledged && (this.is_secure() || this.scr_get_fw() == 1) && (!nmfi || psr.f == 0))
            //        this.cpsr.f = psr.f;
            //    if (affect_execstate)
            //        this.cpsr.t = psr.t;
            //    if (is_priviledged)
            //    {
            //        if (!this.is_good_mode[psr.m])
            //            this.abort_unpredictable("cpsr_write_by_instr", psr.m);
            //        else
            //        {
            //            if (!this.is_secure() && psr.m == MON_MODE)
            //                this.abort_unpredictable("cpsr_write_by_instr", psr.m);
            //            if (!this.is_secure() && psr.m == FIQ_MODE && this.nsacr_get_rfr() == 1)
            //                this.abort_unpredictable("cpsr_write_by_instr", psr.m);
            //            if (this.cpsr.m != psr.m)
            //                this.change_mode(psr.m);
            //        }
            //    }
            //}
        }

        void save_to_regs(long mode)
        {
            switch (mode)
            {
                case USR_MODE:
                    this.regs_usr[13] = this.regs[13];
                    this.regs_usr[14] = this.regs[14];
                    break;
                case FIQ_MODE:
                    this.regs_fiq[8] = this.regs[8];
                    this.regs_fiq[9] = this.regs[9];
                    this.regs_fiq[10] = this.regs[10];
                    this.regs_fiq[11] = this.regs[11];
                    this.regs_fiq[12] = this.regs[12];
                    this.regs_fiq[13] = this.regs[13];
                    this.regs_fiq[14] = this.regs[14];
                    break;
                case IRQ_MODE:
                    this.regs_irq[13] = this.regs[13];
                    this.regs_irq[14] = this.regs[14];
                    break;
                case SVC_MODE:
                    this.regs_svc[13] = this.regs[13];
                    this.regs_svc[14] = this.regs[14];
                    break;
                case MON_MODE:
                    this.regs_mon[13] = this.regs[13];
                    this.regs_mon[14] = this.regs[14];
                    break;
                case ABT_MODE:
                    this.regs_abt[13] = this.regs[13];
                    this.regs_abt[14] = this.regs[14];
                    break;
                case UND_MODE:
                    this.regs_und[13] = this.regs[13];
                    this.regs_und[14] = this.regs[14];
                    break;
                case SYS_MODE:
                    throw new Exception("save_to_regs system");
                default:
                    throw new Exception("save_to_regs unknown: " + mode.ToString("X"));
            }
        }

        void restore_from_regs(long mode)
        {
            switch (mode)
            {
                case USR_MODE:
                    this.regs[13] = this.regs_usr[13];
                    this.regs[14] = this.regs_usr[14];
                    break;
                case FIQ_MODE:
                    this.regs[8] = this.regs_fiq[8];
                    this.regs[9] = this.regs_fiq[9];
                    this.regs[10] = this.regs_fiq[10];
                    this.regs[11] = this.regs_fiq[11];
                    this.regs[12] = this.regs_fiq[12];
                    this.regs[13] = this.regs_fiq[13];
                    this.regs[14] = this.regs_fiq[14];
                    break;
                case IRQ_MODE:
                    this.regs[13] = this.regs_irq[13];
                    this.regs[14] = this.regs_irq[14];
                    break;
                case SVC_MODE:
                    this.regs[13] = this.regs_svc[13];
                    this.regs[14] = this.regs_svc[14];
                    break;
                case MON_MODE:
                    this.regs[13] = this.regs_mon[13];
                    this.regs[14] = this.regs_mon[14];
                    break;
                case ABT_MODE:
                    this.regs[13] = this.regs_abt[13];
                    this.regs[14] = this.regs_abt[14];
                    break;
                case UND_MODE:
                    this.regs[13] = this.regs_und[13];
                    this.regs[14] = this.regs_und[14];
                    break;
                case SYS_MODE:
                    throw new Exception("restore_from_regs system");
                default:
                    throw new Exception("restore_from_regs unknown: " + mode.ToString("X"));
            }
        }

        void change_mode(long mode)
        {
            if (mode < 1)
                throw new Exception("Invalid mode: " + mode);
            //if (this.options.enable_logger)
            //    logger.log("changing mode from " + this.mode2string[this.cpsr.m] + " to " + this.mode2string[mode]);
            save_to_regs(this.cpsr.m);
            this.cpsr.m = mode;
            restore_from_regs(this.cpsr.m);
        }

        void set_apsr(long val, bool set_overflow)
        {
            this.cpsr.n = val >> 31;
            this.cpsr.z = (val == 0) ? 1 : 0;
            this.cpsr.c = (int)this.carry_out;
            if (set_overflow)
                this.cpsr.v = this.overflow;
            //if (this.options.enable_logger)
            //    this.log_apsr();
        }

        void store_regs(long[] regs)
        {
            for (int i = 0; i < 16; i++)
                regs[i] = this.regs[i];
        }

        bool coproc_accepted(int cp)
        {
            return cp == 15;
        }

        /*
         ARMv7_CPU.prototype.coproc_get_word = function(cp, inst) {
            return this.coprocs[cp].get_word(inst);
        };

        ARMv7_CPU.prototype.coproc_send_word = function(cp, inst, word) {
            return this.coprocs[cp].send_word(inst, word);
        };

        ARMv7_CPU.prototype.coproc_internal_operation = function(cp, inst) {
            this.log_value(cp, "cp");
            throw "coproc";
            return this.coprocs[cp].internal_operation(inst);
        }; 
        */

        long align(long value, long align)
        {
            if ((value & 3) != 0)
                throw new Exception("align");
            return value;
        }

        bool unaligned_support()
        {
            return true;
        }

        void abort_unknown_inst(long inst, long addr)
        {
            throw new Exception("Unknown instruction: " + inst.ToString("X"));
        }

        void abort_simdvfp_inst(long inst, long addr)
        {
            throw new Exception("SIMD or VFP instruction: " + inst.ToString("X"));
        }

        void abort_not_impl(string name, long inst, long addr)
        {
            throw new Exception(name + " not implemented: " + inst.ToString("X"));
        }

        void abort_undefined_instruction(string category, long inst, long addr)
        {
            throw new Exception(string.Format("Undefined instruction in {0}: {1}", category, inst.ToString("X")));
        }

        void abort_unpredictable(string category, long value)
        {
            throw new Exception(string.Format("Unpredictable in {0}: {1}", category, value.ToString("X")));
        }

        void abort_unpredictable_instruction(string category, long inst, long addr)
        {
            throw new Exception(string.Format("Unpredictable instruction in {0}: {1}", category, inst.ToString("X")));
        }

        void abort_decode_error(long inst, long addr)
        {
            throw new Exception("Decode error: " + inst.ToString("X"));
        }

        bool allow_unaligned_access()
        {
            return !mmu.CheckUnaligned;
        }

        public string ld_string(long addr)
        {
            string r = "";
            while (true)
            {
                var c = ld_byte(addr);
                if (c == 0)
                    break;
                r += (char)c;
                addr++;
            }
            return r;
        }

        public long ld_word(long addr)
        {
            long phyaddr;
            if ((addr & 3) > 0)
            {
                if (!this.allow_unaligned_access())
                {
                    throw new Exception("Unaligned ld_word: " + this.current + "@" + addr);
                }
                else
                {
                    long val = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        phyaddr = mmu.TranslateToPhysicalAddress(addr + i);
                        val = bitops.set_bits(val, 8 * i + 7, 8 * i, memctlr.ld_byte(phyaddr));
                    }
                    return val;
                }
            }
            else
            {
                phyaddr = this.mmu.TranslateToPhysicalAddress(addr);
                return this.memctlr.ld_word(phyaddr);
            }
        }

        public void st_word(long addr, long word)
        {
            long phyaddr;
            if ((addr & 3) > 0)
            {
                if (!this.allow_unaligned_access())
                {
                    throw new Exception("Unaligned st_word: " + this.current + "@" + addr);
                }
                else
                {
                    for (var i = 0; i < 4; i++)
                    {
                        phyaddr = mmu.TranslateToPhysicalAddress(addr + i);
                        memctlr.st_byte(phyaddr, (int)bitops.get_bits(word, 8 * i + 7, 8 * i));
                    }
                }
            }
            else
            {
                phyaddr = mmu.TranslateToPhysicalAddress(addr, true);
                memctlr.st_word(phyaddr, word);
            }
        }

        long ld_halfword(long addr)
        {
            long phyaddr;
            if ((addr & 1) > 0)
            {
                if (!this.allow_unaligned_access())
                {
                    throw new Exception("Unaligned ld_halfword: " + this.current + "@" + addr);
                }
                else
                {
                    long val = 0;
                    for (int i = 0; i < 2; i++)
                    {
                        phyaddr = mmu.TranslateToPhysicalAddress(addr + i);
                        val = bitops.set_bits(val, 8 * i + 7, 8 * i, memctlr.ld_byte(phyaddr));
                    }
                    return val;
                }
            }
            else
            {
                phyaddr = mmu.TranslateToPhysicalAddress(addr);
                return memctlr.ld_halfword(phyaddr);
            }
        }

        void st_halfword(long addr, long hw)
        {
            long phyaddr;
            if ((addr & 1) > 0)
            {
                if (!this.allow_unaligned_access())
                {
                    throw new Exception("Unaligned st_halfword: " + this.current + "@" + addr);
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        phyaddr = mmu.TranslateToPhysicalAddress(addr + i);
                        memctlr.st_byte(phyaddr, (int)bitops.get_bits(hw, 8 * i + 7, 8 * i));
                    }
                }
            }
            else
            {
                phyaddr = mmu.TranslateToPhysicalAddress(addr, true);
                memctlr.st_halfword(phyaddr, hw);
            }
        }

        public long ld_byte(long addr)
        {
            long phyaddr = mmu.TranslateToPhysicalAddress(addr);
            return memctlr.ld_byte(phyaddr);
        }

        public void st_byte(long addr, long b)
        {
            long phyaddr = mmu.TranslateToPhysicalAddress(addr, true);
            memctlr.st_byte(phyaddr, (int)b);
        }

        public long fetch_instruction(long addr)
        {
            long phyaddr = mmu.TranslateToPhysicalAddress(addr);
            return memctlr.ld_word_fast(phyaddr);
        }



        string shift_type_name(int type)
        {
            switch (type)
            {
                case SRType_LSL: return "lsl";
                case SRType_LSR: return "lsr";
                case SRType_ASR: return "asr";
                case SRType_RRX: return "rrx";
                case SRType_ROR: return "ror";
                default: return "unknown";
            }
        }

        long shift(long value, int type, int amount, int carry_in)
        {
            return shift_c(value, type, amount, carry_in);
        }

        void decode_imm_shift(long type, long imm5)
        {
            /*
            * 0: LSL
            * 1: LSR
            * 2: ASR
            * 3: RRX or ROR (ARM encoding)
            * 3: RRX (In this emulator)
            * 4: ROR (In this emulator)
            */
            switch (type)
            {
                case 0:
                    this.shift_t = (int)type;
                    this.shift_n = (int)imm5;
                    break;
                case 1:
                case 2:
                    this.shift_t = (int)type;
                    if (imm5 == 0)
                        this.shift_n = 32;
                    else
                        this.shift_n = (int)imm5;
                    break;
                case 3:
                    if (imm5 == 0)
                    {
                        this.shift_t = (int)type;
                        this.shift_n = 1;
                    }
                    else
                    {
                        this.shift_t = SRType_ROR;
                        this.shift_n = (int)imm5;
                    }
                    break;
                default:
                    throw new Exception("decode_imm_shift");
            }
        }


        long shift_c(long value, int type, int amount, int carry_in)
        {
            long result;
            if (amount == 0)
            {
                this.carry_out = carry_in;
                return value;
            }
            else
            {
                switch (type)
                {
                    // FIXME
                    case 0: // LSL
                        long val64 = value << amount;
                        this.carry_out = (int)(val64 >> 32) & 1;
                        return (int)(val64 & 0xffffffff);
                    //assert(amount > 0, "lsl: amount > 0");
                    //var val64 = new Number64(0, value);
                    //var extended = val64.lsl(amount);
                    //this.carry_out = extended.high & 1;
                    //return extended.low;
                    case 1: // LSR
                        //assert(amount > 0, "lsr: amount > 0");
                        this.carry_out = (amount == 32) ? 0 : ((value >> (amount - 1)) & 1);
                        result = bitops.lsr(value, amount);
                        //assert(result >= 0, "lsr: result = " + result.toString());
                        return result;
                    case 2: // ASR
                        //assert(amount > 0, "asr: amount > 0");
                        this.carry_out = (amount == 32) ? 0 : ((value >> (amount - 1)) & 1);
                        result = bitops.asr(value, amount);
                        return result;
                    case 3: // RRX
                        this.carry_out = value & 1;
                        result = bitops.set_bit(value >> 1, 31, carry_in);
                        //assert(result >= 0, "rrx");
                        return result;
                    case 4: // ROR
                        return ror_c(value, amount, true);
                    default:
                        throw new Exception("shift_c");
                }
            }
        }

        long ror_c(long value, int amount, bool write)
        {
            //assert(amount !== 0);
            long result = bitops.ror(value, amount);
            //assert(result >= 0, "ror");
            if (write)
                this.carry_out = result >> 31;
            return result;
        }

        long ror(long val, int rotation)
        {
            if (rotation == 0)
                return val;
            return ror_c(val, rotation, false);
        }

        int is_zero_bit(int val)
        {
            if (val == 0)
                return 1;
            else
                return 0;
        }

        long expand_imm_c(long imm12, int carry_in)
        {
            var unrotated_value = imm12 & 0xff;
            var amount = 2 * (imm12 >> 8);
            if (amount == 0)
            {
                this.carry_out = carry_in;
                return unrotated_value;
            }
            return ror_c(unrotated_value, (int)amount, true);
        }

        long expand_imm(long imm12)
        {
            return expand_imm_c(imm12, this.cpsr.c);
        }

        long add_with_carry(long x, long y, int carry_in)
        {
            var unsigned_sum = x + y + carry_in;
            var signed_sum = bitops.sint32(x) + bitops.sint32(y) + carry_in;
            //var result = bitops.get_bits64(unsigned_sum, 31, 0);
            var result = unsigned_sum & 0xffffffff;
            //if (result < 0)
            //result += 0x100000000;
            this.carry_out = (result == unsigned_sum) ? 0 : 1;
            this.overflow = (bitops.sint32(result) == signed_sum) ? 0 : 1;
            return result;
        }

        int decode_reg_shift(int type)
        {
            this.shift_t = type;
            return type;
        }

        string cond_postfix(int inst)
        {
            long cond = bitops.get_bits(inst, 31, 28);
            switch (cond)
            {
                case 0: return "eq";
                case 1: return "ne";
                case 2: return "cs";
                case 3: return "cc";
                case 4: return "mi";
                case 8: return "hi";
                case 9: return "ls";
                case 0xa: return "ge";
                case 0xb: return "lt";
                case 0xc: return "gt";
                case 0xd: return "le";
                default:
                    return "";
            }
        }

        public bool is_valid(long inst)
        {
            return (inst != 0xe1a00000 && inst != 0); // NOP or NULL?
        }

        public bool cond(long inst)
        {
            long cond = inst >> 28;
            bool ret = false;
            switch (cond >> 1)
            {
                case 0:
                    ret = this.cpsr.z == 1; // EQ or NE
                    break;
                case 1:
                    ret = this.cpsr.c == 1; // CS or CC
                    break;
                case 2:
                    ret = this.cpsr.n == 1; // MI or PL
                    break;
                case 3:
                    ret = this.cpsr.v == 1; // VS or VC
                    break;
                case 4:
                    ret = this.cpsr.c == 1 && this.cpsr.z == 0; // HI or LS
                    break;
                case 5:
                    ret = this.cpsr.n == this.cpsr.v; // GE or LT
                    break;
                case 6:
                    ret = this.cpsr.n == this.cpsr.v && this.cpsr.z == 0; // GT or LE
                    break;
                case 7:
                    ret = true; // AL
                    break;
                default:
                    break;
            }
            if ((cond & 1) > 0 && cond != 0xf)
                ret = !ret;
            return ret;
        }


        #region Instruction Execution

        void adc_imm(long inst, long addr)
        {
            //this.print_inst("ADC (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);
            var ret = this.add_with_carry(this.reg(n), imm32, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    set_apsr(ret, true);
            }
            //this.print_inst_imm(addr, inst, "adc", s, d, n, imm32);
        }

        void add_imm(long inst, long addr)
        {
            //this.print_inst("ADD (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);
            var ret = this.add_with_carry(this.reg(n), imm32, 0);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    set_apsr(ret, true);
            }
            //this.print_inst_imm(addr, inst, "add", s, d, n, imm32);
        }

        void adr_a1(long inst, long addr)
        {
            //this.print_inst("ADR A1", inst, addr);
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);
            var ret = this.align(this.get_pc(), 4) + imm32;
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
            }
            //this.print_inst_imm(addr, inst, "adr", null, d, null, imm32);
        }

        void adr_a2(long inst, long addr)
        {
            //this.print_inst("ADR A2", inst, addr);
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);
            var ret = this.align(this.get_pc(), 4) - imm32;
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
            }
            //this.print_inst_imm(addr, inst, "adr", null, d, null, imm32);
        }

        void and_imm(long inst, long addr)
        {
            //this.print_inst("AND (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm_c(imm12, this.cpsr.c);

            var valn = this.reg(n);
            var ret = bitops.and(valn, imm32);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "and", s, d, n, imm32);
        }

        void asr_imm(long inst, long addr)
        {
            //this.print_inst("ASR (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var m = inst & 0xf;
            this.decode_imm_shift(2, imm5);
            var ret = this.shift_c(this.reg(m), SRType_ASR, (int)this.shift_n, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "asr", s, d, m, imm5);
        }

        void bic_imm(long inst, long addr)
        {
            //this.print_inst("BIC (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;

            var valn = this.reg(n);
            var imm32 = this.expand_imm_c(imm12, this.cpsr.c);
            var ret = bitops.and(valn, bitops.not(imm32));
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "bic", s, d, n, bitops.sint32(imm32));
        }

        void b(long inst, long addr)
        {
            //this.print_inst("B", inst, addr);
            long imm24 = inst & 0x00ffffff;
            //imm32 = SignExtend(imm24:'00', 32);
            //var imm32 = bitops.sign_extend(imm24 << 2, 26, 32);
            long imm26 = imm24 << 2;
            long imm32 = imm26;
            if ((imm26 & 0x02000000) > 0)
                imm32 = imm26 | 0xfc000000;
            this.branch_to = this.get_pc() + imm32;
            if (this.branch_to >= 0x100000000)
                this.branch_to -= 0x100000000;
            //this.print_inst_branch(addr, inst, "b", this.branch_to);
        }

        void bl_imm(long inst, long addr)
        {
            //this.print_inst("BL, BLX (immediate)", inst, addr);
            //var imm24 = bitops.get_bits(inst, 23, 0);
            //var imm32 = bitops.sign_extend(imm24 << 2, 26, 32);
            long imm24 = inst & 0x00ffffff;
            long imm26 = imm24 << 2;
            long imm32 = imm26;
            if ((imm26 & 0x02000000) > 0)
                imm32 = imm26 | 0xfc000000;
            this.regs[14] = this.get_pc() - 4;
            // BranchWritePC(Align(PC,4) + imm32);
            this.branch_to = this.align(bitops.lsl((this.get_pc()) >> 2, 2), 4) + imm32;
            if (this.branch_to >= 0x100000000)
                this.branch_to -= 0x100000000;
            //this.print_inst_branch(addr, inst, "bl", this.branch_to);
        }

        void cmn_imm(long inst, long addr)
        {
            //this.print_inst("CMN (immediate)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var imm12 = inst & 0xfff;

            var valn = this.reg(n);
            var imm32 = this.expand_imm(imm12);
            var ret = this.add_with_carry(valn, imm32, 0);
            this.set_apsr(ret, true);
            //this.print_inst_imm(addr, inst, "cmn", null, null, n, imm32);
        }

        void cmp_imm(long inst, long addr)
        {
            //this.print_inst("CMP (immediate)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var imm12 = inst & 0xfff;
            var valn = this.reg(n);
            var imm32 = this.expand_imm(imm12);
            var ret = this.add_with_carry(valn, bitops.not(imm32), 1);
            this.set_apsr(ret, true);
            //this.print_inst_imm(addr, inst, "cmp", null, null, n, imm32);
        }

        void eor_imm(long inst, long addr)
        {
            //this.print_inst("EOR (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm_c(imm12, this.cpsr.c);

            var valn = this.reg(n);
            var ret = bitops.xor(valn, imm32);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "eor", s, d, n, imm32);
        }

        void ldr_imm(long inst, long addr)
        {
            //this.print_inst("LDR (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm12 = (inst & 0xfff);

            if (n == 13 && p == 0 && u == 1 && w == 0 && imm12 == 4)
            {
                // POP A2
                if (t == 15)
                    this.branch_to = this.ld_word(this.regs[13]);
                else
                    this.regs[t] = this.ld_word(this.regs[13]);
                this.regs[13] = this.regs[13] + 4;
                //this.print_inst_unimpl(addr, inst, "pop");
                return;
            }
            var imm32 = imm12;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            var data = this.ld_word(address);
            if (is_wback)
                this.regs[n] = offset_addr;
            if (t == 15)
                this.branch_to = data;
            else
                this.regs[t] = data;
            //this.print_inst_imm(addr, inst, "ldr", null, t, n, imm32, true, is_wback, is_add, is_index);
        }

        void ldrb_imm(long inst, long addr)
        {
            //this.print_inst("LDRB (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm32 = inst & 0xfff;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            var data = this.ld_byte(address);
            this.regs[t] = data;
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_imm(addr, inst, "ldrb", null, t, n, imm32, true, is_wback, is_add, is_index);
        }

        void ldrd_imm(long inst, long addr)
        {
            //this.print_inst("LDRD (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm4h = (inst >> 8) & 0xf;
            var imm4l = inst & 0xf;
            var t2 = t + 1;
            var imm32 = (imm4h << 4) + imm4l;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            this.regs[t] = this.ld_word(address);
            this.regs[t2] = this.ld_word(address + 4);
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_imm(addr, inst, "ldrd", null, t, n, imm32, true, is_wback, is_add, is_index);
        }

        void ldrsh_imm(long inst, long addr)
        {
            //this.print_inst("LDRSH (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm4h = (inst >> 8) & 0xf;
            var imm4l = inst & 0xf;
            var imm32 = (imm4h << 4) + imm4l;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            var data = this.ld_halfword(address);
            if (is_wback)
                this.regs[n] = offset_addr;
            this.regs[t] = bitops.sign_extend(data, 16, 32);
            //this.print_inst_imm(addr, inst, "ldrsh", null, t, n, imm32, true, is_wback, is_add, is_index);
        }

        void ldrsh_reg(long inst, long addr)
        {
            //this.print_inst("LDRSH (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset = this.shift(this.reg(m), SRType_LSL, 0, this.cpsr.c);
            var offset_addr = valn + (is_add ? offset : -offset);
            var address = is_index ? offset_addr : valn;
            var data = this.ld_halfword(address);
            if (is_wback)
                this.regs[n] = offset_addr;
            this.regs[t] = bitops.sign_extend(data, 16, 32);
            //this.print_inst_reg(addr, inst, "ldrsh", null, t, n, m, this.SRType_LSL, 0);
        }

        void lsl_imm(long inst, long addr)
        {
            //this.print_inst("LSL (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var m = inst & 0xf;

            var valm = this.reg(m);
            this.decode_imm_shift(0, imm5);
            var ret = this.shift_c(valm, SRType_LSL, (int)this.shift_n, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "lsl", s, d, m, imm5);
        }

        void lsr_imm(long inst, long addr)
        {
            //this.print_inst("LSR (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var m = inst & 0xf;

            var valm = this.reg(m);
            this.decode_imm_shift(1, imm5);
            var ret = this.shift_c(valm, SRType_LSR, this.shift_n, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "lsr", s, d, m, imm5);
        }

        void mov_imm_a1(long inst, long addr)
        {
            //this.print_inst("MOV (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm_c(imm12, this.cpsr.c);

            var ret = imm32;
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s > 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "mov", s, d, null, imm32);
        }

        void mov_imm_a2(long inst, long addr)
        {
            //this.print_inst("MOV (immediate) A2", inst, addr);
            var imm4 = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = (imm4 << 12) + imm12;

            var ret = imm32;
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
            }
            //this.print_inst_imm(addr, inst, "movw", false, d, null, imm32);
        }

        void movt(long inst, long addr)
        {
            //this.print_inst("MOVT", inst, addr);
            var imm4 = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm16 = (imm4 << 12) + imm12;

            this.regs[d] = bitops.set_bits(this.reg(d), 16, 31, imm16);
            //this.print_inst_imm(addr, inst, "movw", false, d, null, imm32);
        }

        void msr_imm_sys(long inst, long addr)
        {
            //this.print_inst("MSR (immediate) (system level)", inst, addr);
            var r = inst & (1 << 22);
            var mask = (inst >> 16) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);

            if (r != 0)
            {
                // SPSRWriteByInstr(R[n], mask);
                this.spsr_write_by_instr(this.parse_psr(imm32), mask);
            }
            else
            {
                // CPSRWriteByInstr(R[n], mask, FALSE);
                this.cpsr_write_by_instr(this.parse_psr(imm32), mask, false);
            }
            //this.print_inst_msr(addr, inst, null, imm32);
        }

        void mvn_imm(long inst, long addr)
        {
            //this.print_inst("MVN (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm_c(imm12, this.cpsr.c);

            var ret = bitops.not(imm32);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "mvn", s, d, null, imm32);
        }

        void orr_imm(long inst, long addr)
        {
            //this.print_inst("ORR (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;

            var valn = this.reg(n);
            var imm32 = this.expand_imm_c(imm12, this.cpsr.c);
            var ret = bitops.or(valn, imm32);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "orr", s, d, n, imm32);
        }

        /*
         ARMv7_CPU.prototype.hint_preload_data = function(address) {
            // FIXME
            this.log_value(address, "preload address");
         }; 
         */

        void pld_imm(long inst, long addr)
        {
            //this.print_inst("PLD (immediate, literal)", inst, addr);
            var u = (inst >> 23) & 1;
            var n = (inst >> 16) & 0xf;
            var imm12 = inst & 0xfff;

            var valn = this.reg(n);
            var imm32 = imm12;
            var is_add = u == 1;
            var base_l = (n == 15) ? this.align(this.get_pc(), 4) : valn;
            var address = base_l + (is_add ? imm32 : -imm32);
            //this.hint_preload_data(address);
            //this.print_inst_imm(addr, inst, "pld", null, null, n, imm32, true, null, is_add, true);
        }

        void rsb_imm(long inst, long addr)
        {
            //this.print_inst("RSB (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);
            var valn = this.reg(n);
            var ret = this.add_with_carry(bitops.not(valn), imm32, 1);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_imm(addr, inst, "rsb", s, d, n, imm32);
        }

        void rsc_imm(long inst, long addr)
        {
            //this.print_inst("RSC (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);

            var valn = this.reg(n);
            var ret = this.add_with_carry(bitops.not(valn), imm32, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_imm(addr, inst, "rsc", s, d, n, imm32);
        }

        void ror_imm(long inst, long addr)
        {
            //this.print_inst("ROR (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var m = inst & 0xf;

            var valm = this.reg(m);
            this.decode_imm_shift(3, imm5);
            var ret = this.shift_c(valm, SRType_ROR, this.shift_n, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "ror", s, d, m, imm5);
        }

        void rrx(long inst, long addr)
        {
            //this.print_inst("RRX", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var m = inst & 0xf;

            var valm = this.reg(m);
            var ret = this.shift_c(valm, SRType_RRX, 1, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_imm(addr, inst, "rrx", s, d, m, null);
            //this.print_inst_unimpl(addr, inst, "rrx");
        }

        void sbc_imm(long inst, long addr)
        {
            //this.print_inst("SBC (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);

            var valn = this.reg(n);
            var ret = this.add_with_carry(valn, bitops.not(imm32), this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_imm(addr, inst, "sbc", s, d, n, imm32);
        }

        void str_imm(long inst, long addr)
        {
            //this.print_inst("STR (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            long address;
            if (n == 13 && p == 1 && u == 0 && w == 1 && imm12 == 4)
            {
                // PUSH A2
                var sp = this.reg(13);
                address = sp - 4;
                this.st_word(address, this.reg(t));
                this.regs[13] = sp - 4;
                return;
            }
            var imm32 = imm12;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;
            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            address = is_index ? offset_addr : valn;
            var valt = this.reg(t);
            this.st_word(address, valt);
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_imm(addr, inst, "str", null, t, n, imm32, true, is_wback, is_add, is_index);
        }

        void strb_imm(long inst, long addr)
        {
            //this.print_inst("STRB (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm32 = inst & 0xfff;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            this.st_byte(address, this.reg(t) & 0xff);
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_imm(addr, inst, "strb", null, t, n, imm32, true, is_wback, is_add, is_index);
        }

        void sub_imm(long inst, long addr)
        {
            //this.print_inst("SUB (immediate)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm12 = inst & 0xfff;
            var imm32 = this.expand_imm(imm12);

            var ret = this.add_with_carry(this.reg(n), bitops.not(imm32), 1);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_imm(addr, inst, "sub", s, d, n, imm32);
        }

        void teq_imm(long inst, long addr)
        {
            //this.print_inst("TEQ (immediate)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var imm12 = inst & 0xfff;

            var valn = this.reg(n);
            var imm32 = this.expand_imm_c(imm12, this.cpsr.c);
            var ret = bitops.xor(valn, imm32);
            this.set_apsr(ret, false);
            //this.print_inst_imm(addr, inst, "teq", null, null, n, imm32);
        }

        void tst_imm(long inst, long addr)
        {
            //this.print_inst("TST (immediate)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var imm12 = inst & 0xfff;

            var valn = this.reg(n);
            var imm32 = this.expand_imm_c(imm12, this.cpsr.c);
            var ret = bitops.and(valn, imm32);
            this.set_apsr(ret, false);
            //this.print_inst_imm(addr, inst, "tst", null, null, n, imm32);
        }

        void ldr_lit(long inst, long addr)
        {
            //this.print_inst("LDR (literal)", inst, addr);
            var u = inst & (1 << 23);
            var t = (inst >> 12) & 0xf;
            var imm32 = inst & 0xfff;

            var base_l = this.align(this.get_pc(), 4);
            var address = base_l + (u != 0 ? imm32 : -imm32);
            var data = this.ld_word(address);
            if (t == 15)
                this.branch_to = data;
            else
                this.regs[t] = data;
            //this.print_inst_imm(addr, inst, "ldr", null, t, 15, imm32, true, null, u, true);
        }

        void adc_reg(long inst, long addr)
        {
            //this.print_inst("ADC (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = this.add_with_carry(valn, shifted, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_reg(addr, inst, "adc", s, d, n, m, this.shift_t, this.shift_n);
        }

        void add_reg(long inst, long addr)
        {
            //this.print_inst("ADD (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = this.add_with_carry(valn, shifted, 0);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_reg(addr, inst, "add", s, d, n, m, this.shift_t, this.shift_n);
        }

        void and_reg(long inst, long addr)
        {
            //this.print_inst("AND (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift_c(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = bitops.and(valn, shifted);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_reg(addr, inst, "and", s, d, n, m, this.shift_t, this.shift_n);
        }

        void asr_reg(long inst, long addr)
        {
            //this.print_inst("ASR (register)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var m = (inst >> 8) & 0xf;
            var n = inst & 0xf;

            var shift_n = bitops.get_bits(this.reg(m), 7, 0);
            var ret = this.shift_c(this.reg(n), SRType_ASR, (int)shift_n, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_reg(addr, inst, "asr", s, d, n, m);
        }

        void bic_reg(long inst, long addr)
        {
            //this.print_inst("BIC (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift_c(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = bitops.and(valn, bitops.not(shifted));
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_reg(addr, inst, "bic", s, d, n, m, this.shift_t, this.shift_n);
        }

        void bfc(long inst, long addr)
        {
            //this.print_inst("BFC", inst, addr);
            var msbit = (inst >> 16) & 0x1f;
            var d = (inst >> 12) & 0xf;
            var lsbit = (inst >> 7) & 0x1f;

            if (msbit >= lsbit)
                this.regs[d] = bitops.clear_bits(this.regs[d], (int)msbit, (int)lsbit);
            else
                this.abort_unpredictable_instruction("BFC", inst, addr);
            //this.print_inst_ubfx(addr, inst, "bfc", d, null, msbit, lsbit);
        }

        void bfi(long inst, long addr)
        {
            //this.print_inst("BFI", inst, addr);
            var msbit = (inst >> 16) & 0x1f;
            var d = (inst >> 12) & 0xf;
            var lsbit = (inst >> 7) & 0x1f;
            var n = inst & 0xf;

            if (msbit >= lsbit)
                this.regs[d] = bitops.set_bits(this.regs[d], (int)msbit, (int)lsbit, bitops.get_bits(this.reg(n), (int)(msbit - lsbit), 0));
            else
                this.abort_unpredictable_instruction("BFI", inst, addr);
            //this.print_inst_ubfx(addr, inst, "bfi", d, n, msbit, lsbit);
        }

        void blx_reg(long inst, long addr)
        {
            //this.print_inst("BLX (register)", inst, addr);
            var m = inst & 0xf;

            var next_instr_addr = this.get_pc() - 4;
            this.regs[14] = next_instr_addr;
            this.branch_to = this.reg(m);
            //this.print_inst_reg(addr, inst, "blx", null, null, null, m);
            //this.print_inst_branch(addr, inst, "blx", this.branch_to, m);
        }

        void bx(long inst, long addr)
        {
            //this.print_inst("BX", inst, addr);
            var m = inst & 0xf;

            this.branch_to = this.reg(m);
            //this.print_inst_branch(addr, inst, "bx", this.branch_to, m);
        }

        void cdp_a1(long inst, long addr)
        {
            //this.print_inst("CDP, CDP2 A1?", inst, addr);
            var t = (inst >> 12) & 0xf;
            var cp = (inst >> 8) & 0xf;

            if ((cp >> 1) == 5)
            {
                this.abort_simdvfp_inst(inst, addr);
            }
            if (!this.coproc_accepted((int)cp))
            {
                throw new Exception("GenerateCoprocessorException(): " + cp);
            }
            else
            {
                //this.coproc_internal_operation(cp, inst);
            }
            //this.print_inst_mcrmrc(inst, "cdp", t, cp);
            //this.print_inst_unimpl(addr, inst, "cdp");
        }

        void clz(long inst, long addr)
        {
            //this.print_inst("CLZ", inst, addr);
            var d = (inst >> 12) & 0xf;
            var m = inst & 0xf;

            this.regs[d] = bitops.count_leading_zero_bits(this.reg(m));
            //this.print_inst_reg(addr, inst, "clz", null, d, null, m);
        }

        void cmn_reg(long inst, long addr)
        {
            //this.print_inst("CMN (register)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = this.add_with_carry(valn, shifted, 0);
            this.set_apsr(ret, true);
            //this.print_inst_reg(addr, inst, "cmn", null, null, n, m, this.shift_t, this.shift_n);
        }

        void cmp_reg(long inst, long addr)
        {
            //this.print_inst("CMP (register)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = this.add_with_carry(valn, bitops.not(shifted), 1);
            this.set_apsr(ret, true);
            //this.print_inst_reg(addr, inst, "cmp", null, null, n, m, this.shift_t, this.shift_n);
        }

        void eor_reg(long inst, long addr)
        {
            //this.print_inst("EOR (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift_c(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = bitops.xor(valn, shifted);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_reg(addr, inst, "eor", s, d, n, m, this.shift_t, this.shift_n);
        }

        void ldr_reg(long inst, long addr)
        {
            //this.print_inst("LDR (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            this.decode_imm_shift(type, imm5);
            var offset = this.shift(this.reg(m), this.shift_t, this.shift_n, this.cpsr.c);
            var offset_addr = (valn + (is_add ? offset : -offset)) & 0xffffffff;
            var address = is_index ? offset_addr : valn;
            address = bitops.get_bits64(address, 31, 0); // XXX
            var data = this.ld_word(address);
            if (is_wback)
                this.regs[n] = offset_addr;
            if (t == 15)
                this.branch_to = data;
            else
                this.regs[t] = data;
            //this.print_inst_reg(addr, inst, "ldr", null, t, n, m, this.shift_t, this.shift_n, true, is_wback);
        }

        void ldrb_reg(long inst, long addr)
        {
            //this.print_inst("LDRB (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            this.decode_imm_shift(type, imm5);
            var valn = this.reg(n);
            var offset = this.shift(this.reg(m), this.shift_t, this.shift_n, this.cpsr.c);
            var offset_addr = (valn + (is_add ? offset : -offset)) & 0xffffffff;
            var address = is_index ? offset_addr : valn;
            var data = this.ld_byte(address);
            this.regs[t] = data;
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "ldrb", null, t, n, m, this.shift_t, this.shift_n, true, is_wback, is_index);
        }

        void ldrd_reg(long inst, long addr)
        {
            //this.print_inst("LDRD (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var t2 = t + 1;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var valm = this.reg(m);
            var offset_addr = (valn + (is_add ? valm : -valm)) & 0xffffffff;
            var address = is_index ? offset_addr : valn;
            this.regs[t] = this.ld_word(address);
            this.regs[t2] = this.ld_word(address + 4);
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "ldrd", null, t, n, m, null, null, true, is_wback, is_index);
        }

        void ldrex(long inst, long addr)
        {
            //this.print_inst("LDREX", inst, addr);
            var n = bitops.get_bits(inst, 19, 16);
            var t = bitops.get_bits(inst, 15, 12);

            var imm32 = 0;
            var address = (this.reg(n) + imm32) & 0xffffffff;
            // SetExclusiveMonitors(address,4);
            // R[t] = MemA[address,4];
            this.regs[t] = this.ld_word(address);
            //this.print_inst_reg(addr, inst, "ldrex", null, t, n, null, null, null, true, false);
        }

        void ldrexd(long inst, long addr)
        {
            //this.print_inst("LDREXD", inst, addr);
            var n = bitops.get_bits(inst, 19, 16);
            var t = bitops.get_bits(inst, 15, 12);
            var t2 = t + 1;

            var address = this.reg(n);
            // SetExclusiveMonitors(address,8);
            // value = MemA[address,8];
            // R[t] = value<31:0>
            // R[t2] = value<63:31>
            this.regs[t] = this.ld_word(address);
            this.regs[t2] = this.ld_word((address + 4) & 0xffffffff);
            //this.print_inst_reg(addr, inst, "ldrexd", null, t, n, null, null, null, true, false);
        }

        void ldrt_a1(long inst, long addr)
        {
            //this.print_inst("LDRT A1", inst, addr);
            var u = (inst >> 23) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm32 = inst & 0xfff;
            var is_add = u == 1;

            var valn = this.reg(n);
            var offset = imm32;
            var offset_addr = (valn + (is_add ? offset : -offset)) & 0xffffffff;
            var address = valn;
            address = bitops.get_bits64(address, 31, 0); // XXX
            var data = this.ld_word(address);
            if (t == 15)
                this.branch_to = data;
            else
                this.regs[t] = data;
            //this.print_inst_reg(addr, inst, "ldrt", null, t, n, m, this.shift_t, this.shift_n, true, is_wback);
        }

        void lsl_reg(long inst, long addr)
        {
            //this.print_inst("LSL (register)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var m = (inst >> 8) & 0xf;
            var n = inst & 0xf;

            var shift_n = bitops.get_bits(this.reg(m), 7, 0);
            var ret = this.shift_c(this.reg(n), SRType_LSL, (int)shift_n, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_reg(addr, inst, "lsl", s, d, n, m);
        }

        void lsr_reg(long inst, long addr)
        {
            //this.print_inst("LSR (register)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var m = (inst >> 8) & 0xf;
            var n = inst & 0xf;

            var shift_n = bitops.get_bits(this.reg(m), 7, 0);
            var ret = this.shift_c(this.reg(n), SRType_LSR, (int)shift_n, this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_reg(addr, inst, "lsr", s, d, n, m);
        }

        void mcr_a1(long inst, long addr)
        {
            //this.print_inst("MCR, MCR2 A1", inst, addr);
            var t = (inst >> 12) & 0xf;
            var cp = (inst >> 8) & 0xf;

            if ((cp >> 1) == 5)
            {
                this.abort_simdvfp_inst(inst, addr);
            }
            if (!this.coproc_accepted((int)cp))
            {
                throw new Exception("GenerateCoprocessorException()");
            }
            else
            {
                //this.coproc_send_word(cp, inst, this.regs[t]);
            }
            //this.print_inst_mcrmrc(addr, inst, "mcr", t, cp);
        }

        void mla(long inst, long addr)
        {
            //this.print_inst("MLA", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 16) & 0xf;
            var a = (inst >> 12) & 0xf;
            var m = (inst >> 8) & 0xf;
            var n = inst & 0xf;

            var ope1 = this.reg(n);
            var ope2 = this.reg(m);
            var addend = this.reg(a);
            //var n64_ope1 = new Number64(0, ope1);
            //var n64_ope2 = new Number64(0, ope2);
            //var n64_addend = new Number64(0, addend);
            //var n64 = n64_ope1.mul(n64_ope2);
            //var ret = n64.add(n64_addend);
            //this.regs[d] = ret.low;

            var ret = (ope1 * ope2 + addend) & 0xffffffff;
            this.regs[d] = ret;

            if (s != 0)
            {
                this.cpsr.n = (ret >> 31) & 1;
                this.cpsr.z = (ret == 0) ? 1 : 0;
                //this.log_apsr();
            }
            //this.print_inst_reg(addr, inst, "mla", s, d, n, m); // FIXME
        }

        void mls(long inst, long addr)
        {
            ////this.print_inst("MLS", inst, addr);
            //var d = (inst >> 16) & 0xf;
            //var a = (inst >> 12) & 0xf;
            //var m = (inst >> 8) & 0xf;
            //var n = inst & 0xf;

            //var ope1 = this.reg(n);
            //var ope2 = this.reg(m);
            //var addend = this.reg(a);
            //var n64_ope1 = new Number64(0, ope1);
            //var n64_ope2 = new Number64(0, ope2);
            //var n64_addend = new Number64(0, addend);
            //var n64 = n64_ope1.mul(n64_ope2);
            //var ret = n64_addend.sub(n64);
            //this.regs[d] = ret.low;
            ////this.print_inst_mul(addr, inst, "mls", null, n, d, m, a);
        }

        void subs_pc_lr_a2(long inst, long addr)
        {
            var opcode = (inst >> 21) & 0xf;
            var n = (inst >> 16) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            this.decode_imm_shift(type, imm5);
            var operand2 = this.shift(this.reg(m), this.shift_t, this.shift_n, this.cpsr.c);
            long ret;
            switch (opcode)
            {
                case 0:
                    ret = bitops.and(this.reg(n), operand2);
                    break;
                case 1:
                    ret = bitops.xor(this.reg(n), operand2);
                    break;
                case 2:
                    ret = this.add_with_carry(this.reg(n), bitops.not(operand2), 1);
                    break;
                case 3:
                    ret = this.add_with_carry(bitops.not(this.reg(n)), operand2, 1);
                    break;
                case 4:
                    ret = this.add_with_carry(this.reg(n), operand2, 0);
                    break;
                case 5:
                    ret = this.add_with_carry(this.reg(n), operand2, this.cpsr.c);
                    break;
                case 6:
                    ret = this.add_with_carry(this.reg(n), bitops.not(operand2), this.cpsr.c);
                    break;
                case 7:
                    ret = this.add_with_carry(bitops.not(this.reg(n)), operand2, this.cpsr.c);
                    break;
                case 0xc:
                    ret = bitops.or(this.reg(n), operand2);
                    break;
                case 0xd:
                    ret = operand2;
                    break;
                case 0xe:
                    ret = bitops.and(this.reg(n), bitops.not(operand2));
                    break;
                case 0xf:
                    ret = bitops.not(operand2);
                    break;
                default:
                    throw new Exception("subs_pc_lr_a2: unknown opcode");
            }
            this.cpsr_write_by_instr(this.get_current_spsr(), 15, true);
            this.branch_to = ret;
            //this.print_inst_unimpl(addr, inst, "subs");
        }

        void mov_reg(long inst, long addr)
        {
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            if (d == 15 && s != 0)
            {
                //this.print_inst("SUBS PC LR A2", inst, addr);
                this.subs_pc_lr_a2(inst, addr);
                return;
            }

            var ret = this.reg(m);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                {
                    this.cpsr.n = ret >> 31;
                    this.cpsr.z = (ret == 0) ? 1 : 0;
                    // FIXME: APSR.C = carry;
                    // I guess carry == 0
                    //this.cpsr.c(bitops.get_bit(value, 29));
                    //this.abort_not_impl("MOV (register) flag", inst, addr);
                    //this.log_apsr();
                }
            }
            //this.print_inst_reg(addr, inst, "mov", s, d, null, m);
        }

        void mrc_a1(long inst, long addr)
        {
            var t = (inst >> 12) & 0xf;
            var cp = (inst >> 8) & 0xf;
            if ((cp >> 1) == 5)
            {
                this.abort_simdvfp_inst(inst, addr);
            }
            if (!this.coproc_accepted((int)cp))
            {
                throw new Exception("GenerateCoprocessorException()");
            }
            else
            {
                //var value = this.coproc_get_word(cp, inst);
                //if (t != 15)
                //{
                //    this.regs[t] = value;
                //}
                //else
                //{
                //    this.cpsr.n = (value >> 31) & 1;
                //    this.cpsr.z = (value >> 30) & 1;
                //    this.cpsr.c = (value >> 29) & 1;
                //    this.cpsr.v = (value >> 28) & 1;
                //    //this.log_apsr();
                //}
            }
            //this.print_inst_mcrmrc(addr, inst, "mrc", t, cp);
        }

        void mrs(long inst, long addr)
        {
            //this.print_inst("MRS", inst, addr);
            var read_spsr = inst & (1 << 22);
            var d = (inst >> 12) & 0xf;

            if (read_spsr != 0)
            {
                if (this.is_user_or_system())
                    this.abort_unpredictable_instruction("MRS", inst, addr);
                else
                    this.regs[d] = this.psr_to_value(this.get_current_spsr());
            }
            else
            {
                // CPSR AND '11111000 11111111 00000011 11011111'
                this.regs[d] = bitops.and(this.psr_to_value(this.cpsr), 0xf8ff03df);
            }
            //this.print_inst_mrs(addr, inst, d);
        }

        void msr_reg_sys(long inst, long addr)
        {
            //this.print_inst("MSR (register) (system level)", inst, addr);
            var r = inst & (1 << 22);
            var mask = (inst >> 16) & 0xf;
            var n = inst & 0xf;

            if (r != 0)
            {
                // SPSRWriteByInstr(R[n], mask);
                this.spsr_write_by_instr(this.parse_psr(this.reg(n)), mask);
            }
            else
            {
                // CPSRWriteByInstr(R[n], mask, FALSE);
                this.cpsr_write_by_instr(this.parse_psr(this.reg(n)), mask, false);
            }
            //this.print_inst_msr(addr, inst, n);
        }

        void mul(long inst, long addr)
        {
            //this.print_inst("MUL", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 16) & 0xf;
            var m = (inst >> 8) & 0xf;
            var n = inst & 0xf;

            var ope1 = this.reg(n);
            var ope2 = this.reg(m);
            //var n64_ope1 = new Number64(0, ope1);
            //var n64_ope2 = new Number64(0, ope2);
            //var ret = n64_ope1.mul(n64_ope2);

            var ret = (ope1 * ope2) & 0xffffffff;
            this.regs[d] = ret;
            if (s != 0)
            {
                //this.cpsr.n = bitops.get_bit(ret.low, 31);
                this.cpsr.n = ret >> 31;
                this.cpsr.z = (ret == 0) ? 1 : 0;
                //this.log_apsr();
            }
            //this.print_inst_reg(addr, inst, "mul", s, d, n, m); // FIXME
        }

        void mvn_reg(long inst, long addr)
        {
            //this.print_inst("MVN (register)", inst, addr);
            var s = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift_c(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = bitops.not(shifted);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_reg(addr, inst, "mvn", s, d, null, m, this.shift_t, this.shift_n);
        }

        void orr_reg(long inst, long addr)
        {
            //this.print_inst("ORR (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift_c(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = bitops.or(valn, shifted);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_reg(addr, inst, "orr", s, d, n, m, this.shift_t, this.shift_n);
        }

        void rev(long inst, long addr)
        {
            //this.print_inst("REV", inst, addr);
            var d = bitops.get_bits(inst, 15, 12);
            var m = bitops.get_bits(inst, 3, 0);

            var valm = this.reg(m);
            long ret = 0;
            ret = bitops.set_bits(ret, 31, 24, bitops.get_bits(valm, 7, 0));
            ret = bitops.set_bits(ret, 23, 16, bitops.get_bits(valm, 15, 8));
            ret = bitops.set_bits(ret, 15, 8, bitops.get_bits(valm, 23, 16));
            ret = bitops.set_bits(ret, 7, 0, bitops.get_bits(valm, 31, 24));
            this.regs[d] = ret;
            //this.print_inst_reg(addr, inst, "rev", null, d, null, m);
        }

        void rev16(long inst, long addr)
        {
            //this.print_inst("REV16", inst, addr);
            var d = bitops.get_bits(inst, 15, 12);
            var m = bitops.get_bits(inst, 3, 0);

            var valm = this.reg(m);
            long ret = 0;
            ret = bitops.set_bits(ret, 31, 24, bitops.get_bits(valm, 23, 16));
            ret = bitops.set_bits(ret, 23, 16, bitops.get_bits(valm, 31, 24));
            ret = bitops.set_bits(ret, 15, 8, bitops.get_bits(valm, 7, 0));
            ret = bitops.set_bits(ret, 7, 0, bitops.get_bits(valm, 15, 8));
            this.regs[d] = ret;
            //this.print_inst_reg(addr, inst, "rev16", null, d, null, m);
        }

        void rsb_reg(long inst, long addr)
        {
            //this.print_inst("RSB (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = this.add_with_carry(bitops.not(valn), shifted, 1);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_reg(addr, inst, "rsb", s, d, n, m, this.shift_t, this.shift_n);
        }

        void sbc_reg(long inst, long addr)
        {
            //this.print_inst("SBC (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = this.add_with_carry(valn, bitops.not(shifted), this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_reg(addr, inst, "sbc", s, d, n, m, this.shift_t, this.shift_n);
        }

        void sbfx(long inst, long addr)
        {
            //this.print_inst("SBFX", inst, addr);
            var widthminus1 = (inst >> 16) & 0x1f;
            var d = (inst >> 12) & 0xf;
            var lsbit = (inst >> 7) & 0x1f;
            var n = inst & 0xf;

            var msbit = lsbit + widthminus1;
            if (msbit <= 31)
                this.regs[d] = bitops.sign_extend(bitops.get_bits(this.reg(n), (int)msbit, (int)lsbit), (int)(msbit - lsbit + 1), 32);
            else
                this.abort_unpredictable_instruction("SBFX", inst, addr);
            //this.print_inst_ubfx(addr, inst, "sbfx", d, n, lsbit, widthminus1 + 1);
        }

        void smlal(long inst, long addr)
        {
            ////this.print_inst("SMLAL", inst, addr);
            //var s = inst & 0x00100000;
            //var dhi = (inst >> 16) & 0xf;
            //var dlo = (inst >> 12) & 0xf;
            //var m = (inst >> 8) & 0xf;
            //var n = inst & 0xf;

            //var n64_n = new Number64(0, this.reg(n));
            //var n64_m = new Number64(0, this.reg(m));
            //var n64 = new Number64(this.reg(dhi), this.reg(dlo));
            //var ret = n64_n.mul(n64_m).add(n64);
            //this.regs[dhi] = ret.high;
            //this.regs[dlo] = ret.low;
            //if (s)
            //{
            //    this.cpsr.n = bitops.get_bit(ret.high, 31);
            //    this.cpsr.z = ret.is_zero() ? 1 : 0;
            //    this.log_apsr();
            //}
            //this.print_inst_mul(addr, inst, "smlal", s, dhi, dlo, n, m);
        }

        void smull(long inst, long addr)
        {
            //this.print_inst("SMULL", inst, addr);
            var s = inst & 0x00100000;
            var dhi = (inst >> 16) & 0xf;
            var dlo = (inst >> 12) & 0xf;
            var m = (inst >> 8) & 0xf;
            var n = inst & 0xf;

            //var n64_n = new Number64(0, this.reg(n));
            //var n64_m = new Number64(0, this.reg(m));
            //var ret = n64_n.mul(n64_m);
            //this.regs[dhi] = ret.high;
            //this.regs[dlo] = ret.low;

            var ret = bitops.sint32(this.reg(n)) * bitops.sint32(this.reg(m));
            ret = ret % (long)Math.Pow(2, 64);
            this.regs[dhi] = ret >> 32;
            this.regs[dlo] = ret & 0xffffffff;

            if (s != 0)
            {
                //this.cpsr.n = bitops.get_bit(ret.high, 31);
                //this.cpsr.z = ret.is_zero() ? 1 : 0;
                this.cpsr.n = ret >> 63;
                this.cpsr.z = ret == 0 ? 1 : 0;
                //this.log_apsr();
            }
            //this.print_inst_mul(addr, inst, "smull", s, dhi, dlo, n, m);
        }

        void swp(long inst, long addr)
        {
            //this.print_inst("SWP(B?)", inst, addr);
            var B = (inst >> 22) & 0x1;
            var Rn = (inst >> 16) & 0xF;
            var Rd = (inst >> 12) & 0xF;
            var Rm = inst & 0xF;

            var valn = this.reg(Rn);
            var valm = this.reg(Rm);

            long address = valn;

            if (B != 0)
            {
                var data = this.ld_byte(address);
                this.st_byte(address, bitops.get_bits(valm, 7, 0));
                this.regs[Rd] = data;
            }
            else
            {
                var data = this.ld_word(address);
                this.st_word(address, valm);
                this.regs[Rd] = data;
            }
            //this.print_inst_reg(addr, inst, "swp" + B ? "B" : "", null, Rn, Rd, Rm, null, null, false, false); 
        }

        void strex(long inst, long addr)
        {
            //this.print_inst("STREX", inst, addr);
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var t = inst & 0xf;
            var imm32 = 0;

            var address = this.reg(n) + imm32;
            // ExclusiveMonitorsPass(address,4)
            this.st_word(address, this.reg(t));
            this.regs[d] = 0;
            // FIXME
            //this.print_inst_reg(addr, inst, "strex", null, t, n, d, null, null, true, false);
        }

        void strexd(long inst, long addr)
        {
            //this.print_inst("STREXD", inst, addr);
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var t = inst & 0xf;
            var t2 = t + 1;

            var address = this.reg(n);
            // ExclusiveMonitorsPass(address,8)
            this.st_word(address, this.reg(t));
            this.st_word(address + 4, this.reg(t2));
            this.regs[d] = 0;
            // FIXME
            //this.print_inst_reg(addr, inst, "strexd", null, t, n, d, null, null, true, false);
        }

        void sub_reg(long inst, long addr)
        {
            //this.print_inst("SUB (register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = this.add_with_carry(valn, bitops.not(shifted), 1);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (s != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_reg(addr, inst, "sub", s, d, n, m, this.shift_t, this.shift_n);
        }

        void sxtb(long inst, long addr)
        {
            //this.print_inst("SXTB", inst, addr);
            var d = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var rotation = ((inst >> 10) & 3) << 3;

            var rotated = this.ror(this.reg(m), (int)rotation);
            this.regs[d] = bitops.sign_extend(bitops.get_bits64(rotated, 7, 0), 8, 32);
            //this.print_inst_reg(addr, inst, "sxtb", null, d, null, m);
        }

        void sxth(long inst, long addr)
        {
            //this.print_inst("SXTH", inst, addr);
            var d = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var rotation = ((inst >> 10) & 3) << 3;

            var rotated = this.ror(this.reg(m), (int)rotation);
            this.regs[d] = bitops.sign_extend(bitops.get_bits64(rotated, 15, 0), 16, 32);
            //this.print_inst_reg(addr, inst, "sxth", null, d, null, m);
        }

        void sxtah(long inst, long addr)
        {
            ////this.print_inst("SXTAH", inst, addr);
            //var n = (inst >> 16) & 0xf;
            //var d = (inst >> 12) & 0xf;
            //var m = inst & 0xf;
            //var rotation = ((inst >> 10) & 3) << 3;

            //var rotated = this.ror(this.reg(m), rotation);
            //var n64 = new Number64(0, this.reg(n));
            //this.regs[d] = n64.add(bitops.sign_extend(bitops.get_bits64(rotated, 15, 0), 16, 32)).low;
            ////this.print_inst_reg(addr, inst, "sxtah", null, d, null, m);
        }

        void teq_reg(long inst, long addr)
        {
            //this.print_inst("TEQ (register)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var valn = this.reg(n);
            var valm = this.reg(m);
            this.decode_imm_shift(type, imm5);
            var shifted = this.shift(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = bitops.xor(valn, shifted);
            this.set_apsr(ret, false);
            //this.print_inst_reg(addr, inst, "teq", null, null, n, m, this.shift_t, this.shift_n);
        }

        void tst_reg(long inst, long addr)
        {
            //this.print_inst("TST (register)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            this.decode_imm_shift(type, imm5);
            var valn = this.reg(n);
            var valm = this.reg(m);
            var shifted = this.shift_c(valm, this.shift_t, this.shift_n, this.cpsr.c);
            var ret = bitops.and(valn, shifted);
            this.set_apsr(ret, false);
            //this.print_inst_reg(addr, inst, "tst", null, null, n, m, this.shift_t, this.shift_n);
        }

        void ubfx(long inst, long addr)
        {
            //this.print_inst("UBFX", inst, addr);
            var widthminus1 = bitops.get_bits(inst, 20, 16);
            var d = bitops.get_bits(inst, 15, 12);
            var lsbit = bitops.get_bits(inst, 11, 7);
            var n = bitops.get_bits(inst, 3, 0);

            var msbit = lsbit + widthminus1;
            if (msbit <= 31)
                this.regs[d] = bitops.get_bits(this.reg(n), (int)msbit, (int)lsbit);
            else
                this.abort_unpredictable_instruction("UBFX", inst, addr);
            //this.print_inst_ubfx(addr, inst, "ubfx", d, n, lsbit, widthminus1 + 1);
        }

        void umlal(long inst, long addr)
        {
            ////this.print_inst("UMLAL", inst, addr);
            //var s = inst & 0x00100000;
            //var dhi = bitops.get_bits(inst, 19, 16);
            //var dlo = bitops.get_bits(inst, 15, 12);
            //var m = bitops.get_bits(inst, 11, 8);
            //var n = bitops.get_bits(inst, 3, 0);

            //var n64_n = new Number64(0, this.reg(n));
            //var n64_m = new Number64(0, this.reg(m));
            //var n64_d = new Number64(this.reg(dhi), this.reg(dlo));
            //var ret = n64_n.mul(n64_m).add(n64_d);
            //this.regs[dhi] = ret.high;
            //this.regs[dlo] = ret.low;
            //if (s)
            //{
            //    this.cpsr.n = bitops.get_bit(ret.high, 31);
            //    this.cpsr.z = ret.is_zero() ? 1 : 0;
            //    this.log_apsr();
            //}
            ////this.print_inst_mul(addr, inst, "umlal", s, dhi, dlo, n, m);
        }

        void umull(long inst, long addr)
        {
            //this.print_inst("UMULL", inst, addr);
            var s = inst & 0x00100000;
            var dhi = bitops.get_bits(inst, 19, 16);
            var dlo = bitops.get_bits(inst, 15, 12);
            var m = bitops.get_bits(inst, 11, 8);
            var n = bitops.get_bits(inst, 3, 0);

            //var n64_n = new Number64(0, this.reg(n));
            //var n64_m = new Number64(0, this.reg(m));
            //var ret = n64_n.mul(n64_m);
            //this.regs[dhi] = ret.high;
            //this.regs[dlo] = ret.low;

            var ret = this.reg(n) * this.reg(m);
            this.regs[dhi] = ret >> 32;
            this.regs[dlo] = ret & 0xffffffff;

            if (s != 0)
            {
                //this.cpsr.n = bitops.get_bit(ret.high, 31);
                //this.cpsr.z = ret.is_zero() ? 1 : 0;
                this.cpsr.n = ret >> 63;
                this.cpsr.n = ret == 0 ? 1 : 0;
                //this.log_apsr();
            }
            //this.print_inst_mul(addr, inst, "umull", s, dhi, dlo, n, m);
        }

        long unsigned_satq(long i, long n)
        {
            long ret;
            if (i > (Math.Pow(2, n) - 1))
            {
                ret = (long)Math.Pow(2, n) - 1;
                this.saturated = true;
            }
            else if (i < 0)
            {
                ret = 0;
                this.saturated = true;
            }
            else
            {
                ret = i;
                this.saturated = false;
            }
            return bitops.get_bits64(ret, 31, 0);
        }

        void usat(long inst, long addr)
        {
            //this.print_inst("USAT", inst, addr);
            var saturate_to = bitops.get_bits(inst, 20, 16);
            var d = bitops.get_bits(inst, 15, 12);
            var imm5 = bitops.get_bits(inst, 11, 7);
            var sh = bitops.get_bit(inst, 6);
            var n = bitops.get_bits(inst, 3, 0);
            this.decode_imm_shift(sh << 1, imm5);

            var operand = this.shift(this.reg(n), this.shift_t, this.shift_n, this.cpsr.c);
            var ret = this.unsigned_satq(bitops.sint32(operand), saturate_to);
            this.regs[n] = ret;
            if (this.saturated)
                this.cpsr.q = 1;
            //this.print_inst_unimpl(addr, inst, "usat");
        }

        void uxtab(long inst, long addr)
        {
            //this.print_inst("UXTAB", inst, addr);
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var rotation = ((inst >> 10) & 3) << 3;
            var m = inst & 0xf;

            var rotated = this.ror(this.reg(m), (int)rotation);
            this.regs[d] = this.reg(n) + bitops.get_bits64(rotated, 7, 0);
            //this.print_inst_uxtab(addr, inst, "uxtab", d, n, m, rotation);
        }

        void uxtah(long inst, long addr)
        {
            //this.print_inst("UXTAH", inst, addr);
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var rotation = ((inst >> 10) & 3) << 3;

            var rotated = this.ror(this.reg(m), (int)rotation);
            this.regs[d] = this.reg(n) + bitops.get_bits64(rotated, 15, 0);
            //this.print_inst_uxtab(addr, inst, "uxtah", d, null, m, rotation);
        }

        void uxtb(long inst, long addr)
        {
            //this.print_inst("UXTB", inst, addr);
            var d = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var rotation = ((inst >> 10) & 3) << 3;

            var rotated = this.ror(this.reg(m), (int)rotation);
            this.regs[d] = bitops.get_bits64(rotated, 7, 0);
            //this.print_inst_uxtab(addr, inst, "uxtb", d, null, m, rotation);
        }

        void uxth(long inst, long addr)
        {
            //this.print_inst("UXTH", inst, addr);
            var d = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var rotation = ((inst >> 10) & 3) << 3;

            var rotated = this.ror(this.reg(m), (int)rotation);
            this.regs[d] = bitops.get_bits64(rotated, 15, 0);
            //this.print_inst_uxtab(addr, inst, "uxth", d, null, m, rotation);
        }

        #endregion

        #region Register-shifted Register

        void add_rsr(long inst, long addr)
        {
            //this.print_inst("ADD (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = this.add_with_carry(this.reg(n), shifted, 0);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (sf != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_rsr(addr, inst, "add", sf, d, n, m, shift_t, s);
        }

        void and_rsr(long inst, long addr)
        {
            //this.print_inst("AND (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift_c(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = bitops.and(this.reg(n), shifted);
            this.regs[d] = ret;
            if (sf != 0)
                this.set_apsr(ret, false);
            //this.print_inst_rsr(addr, inst, "and", sf, d, n, m, shift_t, s);
        }

        void bic_rsr(long inst, long addr)
        {
            //this.print_inst("BIC (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift_c(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = bitops.and(this.reg(n), bitops.not(shifted));
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (sf != 0)
                    this.set_apsr(ret, false);
            }
            //this.print_inst_rsr(addr, inst, "bic", sf, d, n, m, shift_t, s);
        }

        void cmp_rsr(long inst, long addr)
        {
            //this.print_inst("CMP (register-shifted register)", inst, addr);
            var n = (inst >> 16) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = this.add_with_carry(this.reg(n), bitops.not(shifted), 1);
            this.set_apsr(ret, true);
            //this.print_inst_rsr(addr, inst, "cmp", null, null, n, m, shift_t, s);
        }

        void eor_rsr(long inst, long addr)
        {
            //this.print_inst("EOR (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift_c(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = bitops.xor(this.reg(n), shifted);
            this.regs[d] = ret;
            if (sf != 0)
                this.set_apsr(ret, false);
            //this.print_inst_rsr(addr, inst, "eor", sf, d, n, m, shift_t, s);
        }

        void mvn_rsr(long inst, long addr)
        {
            //this.print_inst("MVN (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift_c(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = bitops.not(shifted);
            this.regs[d] = ret;
            if (sf != 0)
                this.set_apsr(ret, false);
            //this.print_inst_rsr(addr, inst, "mvn", sf, d, null, m, shift_t, s);
        }

        void orr_rsr(long inst, long addr)
        {
            //this.print_inst("ORR (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift_c(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = bitops.or(this.reg(n), shifted);
            this.regs[d] = ret;
            if (sf != 0)
                this.set_apsr(ret, false);
            //this.print_inst_rsr(addr, inst, "orr", sf, d, n, m, shift_t, s);
        }

        void rsb_rsr(long inst, long addr)
        {
            //this.print_inst("RSB (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = this.add_with_carry(bitops.not(this.reg(n)), shifted, 1);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (sf != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_rsr(addr, inst, "rsb", sf, d, n, m, shift_t, s);
        }

        void sbc_rsr(long inst, long addr)
        {
            //this.print_inst("SBC (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = this.add_with_carry(this.reg(n), bitops.not(shifted), this.cpsr.c);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (sf != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_rsr(addr, inst, "sbc", sf, d, n, m, shift_t, s);
        }

        void sub_rsr(long inst, long addr)
        {
            //this.print_inst("SUB (register-shifted register)", inst, addr);
            var sf = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var d = (inst >> 12) & 0xf;
            var s = (inst >> 8) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = this.add_with_carry(this.reg(n), bitops.not(shifted), 1);
            if (d == 15)
            {
                this.branch_to = ret;
            }
            else
            {
                this.regs[d] = ret;
                if (sf != 0)
                    this.set_apsr(ret, true);
            }
            //this.print_inst_rsr(addr, inst, "sub", sf, d, n, m, shift_t, s);
        }

        void tst_rsr(long inst, long addr)
        {
            //this.print_inst("TST (register-shifted register)", inst, addr);
            var s = inst & 0x00100000;
            var n = (inst >> 16) & 0xf;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;

            var shift_t = this.decode_reg_shift((int)type);
            var shift_n = bitops.get_bits(this.reg(s), 7, 0);
            var shifted = this.shift_c(this.reg(m), shift_t, (int)shift_n, this.cpsr.c);
            var ret = bitops.and(this.reg(n), shifted);
            this.set_apsr(ret, false);
            //this.print_inst_rsr(addr, inst, "tst", null, null, n, m, shift_t, s);
        }

        #endregion

        #region Load Store

        void ldrh_imm(long inst, long addr)
        {
            //this.print_inst("LDRH (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm4h = (inst >> 8) & 0xf;
            var imm4l = inst & 0xf;
            var imm32 = (imm4h << 4) + imm4l;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            // data = MemU[address,2];
            var data = this.ld_halfword(address);
            if (is_wback)
                this.regs[n] = offset_addr;
            this.regs[t] = data;
            //this.log_regs(null);
            //this.print_inst_imm(addr, inst, "ldrh", null, t, n, imm32, true, is_wback, is_add);
        }

        void ldrh_reg(long inst, long addr)
        {
            //this.print_inst("LDRH (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset = this.shift(this.reg(m), SRType_LSL, 0, this.cpsr.c);
            var offset_addr = valn + (is_add ? offset : -offset);
            var address = is_index ? offset_addr : valn;
            // data = MemU[address,2];
            var data = this.ld_halfword(address);
            if (is_wback)
                this.regs[n] = offset_addr;
            this.regs[t] = data;
            //this.print_inst_reg(addr, inst, "ldrh", null, t, n, m, this.SRType_LSL, 0, true, is_wback, is_add);
        }

        void ldrsb_imm(long inst, long addr)
        {
            //this.print_inst("LDRSB (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm4h = (inst >> 8) & 0xf;
            var imm4l = inst & 0xf;
            var imm32 = (imm4h << 4) + imm4l;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            this.regs[t] = bitops.sign_extend(this.ld_byte(address), 8, 32);
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "ldrsb", null, t, n, m, null, null, true, is_wback, is_add);
            //this.print_inst_unimpl(addr, inst, "ldrsb");
        }

        void ldrsb_reg(long inst, long addr)
        {
            //this.print_inst("LDRSB (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var offset = this.shift(this.reg(m), SRType_LSL, 0, this.cpsr.c);
            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? offset : -offset);
            var address = is_index ? offset_addr : valn;
            this.regs[t] = bitops.sign_extend(this.ld_byte(address), 8, 32);
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "ldrsb", null, t, n, m, null, null, true, is_wback, is_add);
            //this.print_inst_unimpl(addr, inst, "ldrsb");
        }

        void str_reg(long inst, long addr)
        {
            //this.print_inst("STR (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            this.decode_imm_shift(type, imm5);
            var valn = this.reg(n);
            var offset = this.shift(this.reg(m), this.shift_t, this.shift_n, this.cpsr.c);
            var offset_addr = valn + (is_add ? offset : -offset);
            var address = is_index ? offset_addr : valn;
            address = bitops.get_bits64(address, 31, 0); // XXX
            var data = this.reg(t);
            this.st_word(address, data);
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "str", null, t, n, m, this.shift_t, this.shift_n, true, is_wback);
        }

        void strbt_a1(long inst, long addr)
        {
            //this.print_inst("STRBT A1", inst, addr);
            var u = inst & (1 << 23);
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm32 = inst & 0xfff;
            var is_add = u == 1;

            var valn = this.reg(n);
            var offset = imm32;
            var offset_addr = valn + (is_add ? offset : -offset);
            this.st_byte(valn, bitops.get_bits(this.reg(t), 7, 0));
            this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "strbt", null, t, n, m, this.shift_t, this.shift_n, true, true);
        }

        void strbt_a2(long inst, long addr)
        {
            //this.print_inst("STRBT A2", inst, addr);
            var u = (inst >> 23) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;
            var is_add = u == 1;
            this.decode_imm_shift(type, imm5);

            var valn = this.reg(n);
            var offset = this.shift(this.reg(m), this.shift_t, this.shift_n, this.cpsr.c);
            var offset_addr = valn + (is_add ? offset : -offset);
            this.st_byte(valn, bitops.get_bits(this.reg(t), 7, 0));
            this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "strbt", null, t, n, m, this.shift_t, this.shift_n, true, true);
        }

        void strb_reg(long inst, long addr)
        {
            //this.print_inst("STRB (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm5 = (inst >> 7) & 0x1f;
            var type = (inst >> 5) & 3;
            var m = inst & 0xf;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            this.decode_imm_shift(type, imm5);
            var valn = this.reg(n);
            var offset = this.shift(this.reg(m), this.shift_t, this.shift_n, this.cpsr.c);
            var offset_addr = (valn + (is_add ? offset : -offset)) & 0xffffffff;
            var address = is_index ? offset_addr : valn;
            this.st_byte(address, bitops.get_bits(this.reg(t), 7, 0));
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "strb", null, t, n, m, this.shift_t, this.shift_n, true, is_wback);
        }

        void strd_reg(long inst, long addr)
        {
            //this.print_inst("STRD (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var t2 = t + 1;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var valm = this.reg(m);
            var offset_addr = valn + (is_add ? valm : -valm);
            var address = is_index ? offset_addr : valn;
            this.st_word(address, this.reg(t));
            this.st_word(address + 4, this.reg(t2));
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "strd", null, t, n, m, null, null, true, is_wback, is_index);
        }

        void strd_imm(long inst, long addr)
        {
            //this.print_inst("STRD (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm4h = (inst >> 8) & 0xf;
            var imm4l = inst & 0xf;
            var t2 = t + 1;
            var imm32 = (imm4h << 4) + imm4l;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            this.st_word(address, this.reg(t));
            this.st_word(address + 4, this.reg(t2));
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_imm(addr, inst, "strd", null, t, n, imm32, true, is_wback, is_add);
        }

        void strh_imm(long inst, long addr)
        {
            //this.print_inst("STRH (immediate)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var imm4h = (inst >> 8) & 0xf;
            var imm4l = inst & 0xf;
            var imm32 = (imm4h << 4) + imm4l;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset_addr = valn + (is_add ? imm32 : -imm32);
            var address = is_index ? offset_addr : valn;
            this.st_halfword(address, bitops.get_bits(this.reg(t), 15, 0));
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_imm(addr, inst, "strh", null, t, n, imm32, true, is_wback, is_add);
        }

        void strh_reg(long inst, long addr)
        {
            //this.print_inst("STRH (register)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var t = (inst >> 12) & 0xf;
            var m = inst & 0xf;
            var is_index = p == 1;
            var is_add = u == 1;
            var is_wback = p == 0 || w == 1;

            var valn = this.reg(n);
            var offset = this.shift(this.reg(m), SRType_LSL, 0, this.cpsr.c);
            var offset_addr = valn + (is_add ? offset : -offset);
            var address = is_index ? offset_addr : valn;
            this.st_halfword(address, bitops.get_bits(this.reg(t), 15, 0));
            if (is_wback)
                this.regs[n] = offset_addr;
            //this.print_inst_reg(addr, inst, "strh", null, t, n, m, this.SRType_LSL, 0, true, is_wback, is_add);
        }

        void ldm(long inst, long addr)
        {
            //this.print_inst("LDM / LDMIA / LDMFD", inst, addr);
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0xffff;
            var n_registers = bitops.bit_count(register_list, 16);
            //var is_pop = false;
            //if (w != 0 && n == 13 && n_registers >= 2)
            //{
            //    is_pop = true;
            //}
            var is_wback = w == 1;

            var valn = this.reg(n);
            var address = valn;
            //var reglist = new List<int>();
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.Add(i);
                    this.regs[i] = this.ld_word(address);
                    address += 4;
                }
            }
            //if ((register_list >>> 15) & 1) {
            if ((register_list & 0x8000) != 0)
            {
                //reglist.Add(15);
                this.branch_to = this.ld_word(address);
            }
            if (is_wback)
                this.regs[n] = this.reg(n) + 4 * n_registers;
            //this.log_regs(null);
            //if (is_pop)
            //    this.print_inst_ldstm(addr, inst, "pop", is_wback, null, reglist);
            //else
            //    this.print_inst_ldstm(addr, inst, "ldm", is_wback, n, reglist);
        }

        void ldm_er(long inst, long addr)
        {
            //this.print_inst("LDM (exception return)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0x7fff;
            var n_registers = bitops.bit_count(register_list, 15);
            var is_wback = w == 1;
            var is_increment = u == 1;
            var is_wordhigher = p == u;

            var valn = this.reg(n);
            if (this.is_user_or_system())
                this.abort_unpredictable_instruction("LDM (exception return)", inst, addr);
            var length = 4 * n_registers + 4;
            var address = valn + (is_increment ? 0 : -length);
            if (is_wordhigher)
                address += 4;
            //var reglist = [];
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    this.regs[i] = this.ld_word(address);
                    address += 4;
                }
            }
            var new_pc = this.ld_word(address);

            if (is_wback)
                this.regs[n] = valn + (is_increment ? length : -length);
            //this.log_regs(null);
            this.cpsr_write_by_instr(this.get_current_spsr(), 15, true);
            this.branch_to = new_pc;
            //this.print_inst_ldstm(addr, inst, "ldm", is_wback, n, reglist);
            //this.print_inst_unimpl(addr, inst, "ldm");
        }

        void ldm_ur(long inst, long addr)
        {
            //this.print_inst("LDM (user registers)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0x7fff;
            var n_registers = bitops.bit_count(register_list, 15);
            var is_increment = u == 1;
            var is_wordhigher = p == u;

            var valn = this.reg(n);
            if (this.is_user_or_system())
                this.abort_unpredictable_instruction("LDM (user registers)", inst, addr);
            var length = 4 * n_registers;
            var address = valn + (is_increment ? 0 : -length);
            if (is_wordhigher)
                address += 4;
            //var reglist = [];
            //this.log_regs(null);
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    // FIXME
                    this.regs_usr[i] = this.ld_word(address);
                    if (this.cpsr.m == FIQ_MODE)
                    {
                        if (!(i >= 8 && i <= 14))
                            this.regs[i] = this.regs_usr[i];
                    }
                    else
                    {
                        if (!(i >= 13 && i <= 14))
                            this.regs[i] = this.regs_usr[i];
                    }
                    address += 4;
                }
            }
            //logger.log(reglist.toString());
            //this.print_inst_unimpl(addr, inst, "ldm");
        }

        void ldmda(long inst, long addr)
        {
            //this.print_inst("LDMDA / LDMFA", inst, addr);
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0xffff;
            var n_registers = bitops.bit_count(register_list, 16);

            var address = this.reg(n) - 4 * n_registers + 4;
            //var reglist = [];
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    this.regs[i] = this.ld_word(address);
                    address += 4;
                }
            }
            if ((register_list & 0x8000) != 0)
            {
                //reglist.push(15);
                this.branch_to = this.ld_word(address);
            }
            if (w != 0)
                this.regs[n] = this.reg(n) - 4 * n_registers;
            //this.log_regs(null);
            //this.print_inst_ldstm(addr, inst, "ldmda", w, n, reglist);
        }

        void ldmdb(long inst, long addr)
        {
            //this.print_inst("LDMDB / LDMEA", inst, addr);
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0xffff;
            var n_registers = bitops.bit_count(register_list, 16);

            var address = this.reg(n) - 4 * n_registers;
            //var reglist = [];
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    this.regs[i] = this.ld_word(address);
                    address += 4;
                }
            }
            if ((register_list & 0x8000) != 0)
            {
                //reglist.push(15);
                this.branch_to = this.ld_word(address);
            }
            if (w != 0)
                this.regs[n] = this.reg(n) - 4 * n_registers;
            //this.log_regs(null);
            //this.print_inst_ldstm(addr, inst, "ldmdb", w, n, reglist);
        }

        void ldmib(long inst, long addr)
        {
            //this.print_inst("LDMIB / LDMED", inst, addr);
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0xffff;
            var n_registers = bitops.bit_count(register_list, 16);

            var address = this.reg(n) + 4;
            //var reglist = [];
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    this.regs[i] = this.ld_word(address);
                    address += 4;
                }
            }
            if ((register_list & 0x8000) != 0)
            {
                //reglist.push(15);
                this.branch_to = this.ld_word(address);
            }
            if (w != 0)
                this.regs[n] = this.reg(n) + 4 * n_registers;
            //this.log_regs(null);
            //this.print_inst_ldstm(addr, inst, "ldmib", w, n, reglist);
        }

        void stm(long inst, long addr)
        {
            //this.print_inst("STM / STMIA / STMEA", inst, addr);
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0xffff;
            var n_registers = bitops.bit_count(register_list, 16);

            //this.log_regs(null);
            var address = this.reg(n);
            //var reglist = [];
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    this.st_word(address, this.regs[i]);
                    address += 4;
                }
            }
            if ((register_list & 0x8000) != 0)
            {
                //reglist.push(15);
                this.st_word(address, this.get_pc());
            }
            if (w != 0)
                this.regs[n] = this.reg(n) + 4 * n_registers;
            //this.print_inst_ldstm(addr, inst, "stm", w, n, reglist);
        }

        void stmdb(long inst, long addr)
        {
            //this.print_inst("STMDB / STMFD", inst, addr);
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0xffff;
            var n_registers = bitops.bit_count(register_list, 16);
            var is_push = false;
            var valn = this.reg(n);
            if (w != 0 && n == 13 && n_registers >= 2)
            {
                is_push = true;
            }

            //this.log_regs(null);
            var address = valn - 4 * n_registers;
            //var reglist = [];
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    this.st_word(address, this.regs[i]);
                    address += 4;
                }
            }
            if ((register_list & 0x8000) != 0)
            {
                //reglist.push(15);
                this.st_word(address, this.get_pc());
            }
            if (w != 0 || is_push)
                this.regs[n] = this.reg(n) - 4 * n_registers;
            //if (is_push)
            //    this.print_inst_ldstm(addr, inst, "push", w, null, reglist);
            //else
            //    this.print_inst_ldstm(addr, inst, "stmdb", w, n, reglist);
        }

        void stmib(long inst, long addr)
        {
            //this.print_inst("STMIB / STMFA", inst, addr);
            var w = (inst >> 21) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0xffff;
            var n_registers = bitops.bit_count(register_list, 16);
            var valn = this.reg(n);
            //this.log_regs(null);
            var address = valn + 4;
            //var reglist = [];
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    this.st_word(address, this.regs[i]);
                    address += 4;
                }
            }
            if ((register_list & 0x8000) != 0)
            {
                //reglist.push(15);
                this.st_word(address, this.get_pc());
            }
            if (w != 0)
                this.regs[n] = this.reg(n) + 4 * n_registers;
            //this.print_inst_ldstm(addr, inst, "stmib", w, n, reglist);
        }

        void stm_ur(long inst, long addr)
        {
            //this.print_inst("STM (user registers)", inst, addr);
            var p = (inst >> 24) & 1;
            var u = (inst >> 23) & 1;
            var n = (inst >> 16) & 0xf;
            var register_list = inst & 0xffff;
            var n_registers = bitops.bit_count(register_list, 16);
            var is_increment = u == 1;
            var is_wordhigher = p == u;
            if (n == 15 || n_registers < 1)
                this.abort_unpredictable_instruction("STM (user registers)", inst, addr);
            if (this.is_user_or_system())
                this.abort_unpredictable("STM (user registers)", 0);

            var length = 4 * n_registers;
            //this.log_regs(null);
            var address = this.reg(n) + (is_increment ? 0 : -length);
            if (is_wordhigher)
                address += 4;
            //var reglist = [];
            for (var i = 0; i < 15; i++)
            {
                if (((register_list >> i) & 1) != 0)
                {
                    //reglist.push(i);
                    // XXX
                    if (this.cpsr.m == FIQ_MODE)
                    {
                        if (i >= 8 && i <= 14)
                            this.st_word(address, this.regs_usr[i]);
                        else
                            this.st_word(address, this.regs[i]);
                    }
                    else
                    {
                        if (i >= 13 && i <= 14)
                            this.st_word(address, this.regs_usr[i]);
                        else
                            this.st_word(address, this.regs[i]);
                    }
                    address += 4;
                }
            }
            if ((register_list & 0x8000) != 0)
            {
                //reglist.push(15);
                //this.st_word(address, this.regs_usr[15] + 8);
                this.st_word(address, this.get_pc());
            }
            //this.print_inst_ldstm(addr, inst, "stm_usr", null, n, reglist); // FIXME
        }

        void cps(long inst, long addr)
        {
            //this.print_inst("CPS", inst, addr);
            var imod = (inst >> 18) & 3;
            var m = inst & (1 << 17);
            var a = inst & (1 << 8);
            var i = inst & (1 << 7);
            var f = inst & (1 << 6);
            var mode = inst & 0xf;
            var enable = imod == 2;
            var disable = imod == 3;

            if (this.is_priviledged())
            {
                var new_cpsr = this.clone_psr(this.cpsr);
                if (enable)
                {
                    if (a != 0) new_cpsr.a = 0;
                    if (i != 0) new_cpsr.i = 0;
                    if (f != 0) new_cpsr.f = 0;
                }
                if (disable)
                {
                    if (a != 0) new_cpsr.a = 1;
                    if (i != 0) new_cpsr.i = 1;
                    if (f != 0) new_cpsr.f = 1;
                }
                if (m != 0)
                    new_cpsr.m = mode;
                this.cpsr_write_by_instr(new_cpsr, 15, true);
            }
            //this.print_inst_unimpl(addr, inst, "cps");
        }

        void clrex(long inst, long addr)
        {
            //this.print_inst("CLREX", inst, addr);
            // Clear Exclusive clears the local record of the executing processor that an address has had a request for an exclusive access.
            // FIXME: Need to do nothing?
            //this.print_inst_unimpl(addr, inst, "clrex");
        }

        void dsb(long inst, long addr)
        {
            //this.print_inst("DSB", inst, addr);
            //var option = bitops.get_bits(inst, 3, 0);
            // Data Synchronization Barrier
            // FIXME: Need to do nothing?
            //this.print_inst_unimpl(addr, inst, "dsb");
        }

        void dmb(long inst, long addr)
        {
            //this.print_inst("DMB", inst, addr);
            //var option = bitops.get_bits(inst, 3, 0);
            // Data Memory Barrier
            // FIXME: Need to do nothing?
            //this.print_inst_unimpl(addr, inst, "dmb");
        }

        void isb(long inst, long addr)
        {
            //this.print_inst("ISB", inst, addr);
            //var option = bitops.get_bits(inst, 3, 0);
            // Instruction Synchronization Barrier
            // FIXME: Need to do nothing?
            //this.print_inst_unimpl(addr, inst, "isb");
        }

        void wfi(long inst, long addr)
        {
            //this.print_inst("WFI", inst, addr);
            //this.is_halted = true;
            this.cpsr.i = 0;
            //this.print_inst_unimpl(addr, inst, "wfi");
        }

        void vmrs(long inst, long addr)
        {
            //this.print_inst("VMRS", inst, addr);
            // XXX: VFP support v0.3: no double precision support                                   
            this.regs[6] = 1 << 20;
            //this.print_inst_unimpl(addr, inst, "vmrs");
        }

        void nop(long inst, long addr)
        {
            //this.print_inst("NOP", inst, addr);
            //this.print_inst_unimpl(addr, inst, "nop");
        }

        public void exec(string inst_name, long inst, long addr)
        {
            this.current = inst_name;
            //FIXME
            switch (inst_name)
            {
                case "vmrs":
                    vmrs(inst, addr);
                    break;
                case "ldrb_reg":
                    ldrb_reg(inst, addr);
                    break;
                case "ldr_reg":
                    ldr_reg(inst, addr);
                    break;
                case "strbt_a2":
                    strbt_a2(inst, addr);
                    break;
                case "strb_reg":
                    strb_reg(inst, addr);
                    break;
                case "str_reg":
                    str_reg(inst, addr);
                    break;
                case "ldrb_imm":
                    ldrb_imm(inst, addr);
                    break;
                case "ldrt_a1":
                    ldrt_a1(inst, addr);
                    break;
                case "ldr_lit":
                    ldr_lit(inst, addr);
                    break;
                case "ldr_imm":
                    ldr_imm(inst, addr);
                    break;
                case "strbt_a1":
                    strbt_a1(inst, addr);
                    break;
                case "strb_imm":
                    strb_imm(inst, addr);
                    break;
                case "str_imm":
                    str_imm(inst, addr);
                    break;
                case "b":
                    b(inst, addr);
                    break;
                case "ldm_er":
                    ldm_er(inst, addr);
                    break;
                case "ldm_ur":
                    ldm_ur(inst, addr);
                    break;
                case "stm_ur":
                    stm_ur(inst, addr);
                    break;
                case "ldmda":
                    ldmda(inst, addr);
                    break;
                case "ldm":
                    ldm(inst, addr);
                    break;
                case "ldmdb":
                    ldmdb(inst, addr);
                    break;
                case "ldmib":
                    ldmib(inst, addr);
                    break;
                case "stm":
                    stm(inst, addr);
                    break;
                case "stmdb":
                    stmdb(inst, addr);
                    break;
                case "stmib":
                    stmib(inst, addr);
                    break;
                //case "svc":
                //    svc(inst, addr);
                //    break;
                case "mrc_a1":
                    mrc_a1(inst, addr);
                    break;
                case "mcr_a1":
                    mcr_a1(inst, addr);
                    break;
                case "cps":
                    cps(inst, addr);
                    break;
                case "pld_imm":
                    pld_imm(inst, addr);
                    break;
                case "clrex":
                    clrex(inst, addr);
                    break;
                case "dsb":
                    dsb(inst, addr);
                    break;
                case "dmb":
                    dmb(inst, addr);
                    break;
                case "isb":
                    isb(inst, addr);
                    break;
                case "swp":
                    swp(inst, addr);
                    break;
                case "strex":
                    strex(inst, addr);
                    break;
                case "ldrex":
                    ldrex(inst, addr);
                    break;
                case "strexd":
                    strexd(inst, addr);
                    break;
                case "ldrexd":
                    ldrexd(inst, addr);
                    break;
                case "and_imm":
                    and_imm(inst, addr);
                    break;
                case "eor_imm":
                    eor_imm(inst, addr);
                    break;
                case "adr_a2":
                    adr_a2(inst, addr);
                    break;
                case "sub_imm":
                    sub_imm(inst, addr);
                    break;
                case "rsb_imm":
                    rsb_imm(inst, addr);
                    break;
                case "adr_a1":
                    adr_a1(inst, addr);
                    break;
                case "add_imm":
                    add_imm(inst, addr);
                    break;
                case "adc_imm":
                    adc_imm(inst, addr);
                    break;
                case "sbc_imm":
                    sbc_imm(inst, addr);
                    break;
                case "rsc_imm":
                    rsc_imm(inst, addr);
                    break;
                case "tst_imm":
                    tst_imm(inst, addr);
                    break;
                case "teq_imm":
                    teq_imm(inst, addr);
                    break;
                case "cmp_imm":
                    cmp_imm(inst, addr);
                    break;
                case "cmn_imm":
                    cmn_imm(inst, addr);
                    break;
                case "orr_imm":
                    orr_imm(inst, addr);
                    break;
                case "mov_imm_a1":
                    mov_imm_a1(inst, addr);
                    break;
                case "bic_imm":
                    bic_imm(inst, addr);
                    break;
                case "mvn_imm":
                    mvn_imm(inst, addr);
                    break;
                case "msr_imm_sys":
                    msr_imm_sys(inst, addr);
                    break;
                case "nop":
                    nop(inst, addr);
                    break;
                case "wfi":
                    wfi(inst, addr);
                    break;
                case "msr_reg_sys":
                    msr_reg_sys(inst, addr);
                    break;
                case "mrs":
                    mrs(inst, addr);
                    break;
                case "bx":
                    bx(inst, addr);
                    break;
                case "clz":
                    clz(inst, addr);
                    break;
                case "blx_reg":
                    blx_reg(inst, addr);
                    break;
                case "and_reg":
                    and_reg(inst, addr);
                    break;
                case "eor_reg":
                    eor_reg(inst, addr);
                    break;
                case "sub_reg":
                    sub_reg(inst, addr);
                    break;
                case "rsb_reg":
                    rsb_reg(inst, addr);
                    break;
                case "add_reg":
                    add_reg(inst, addr);
                    break;
                case "adc_reg":
                    adc_reg(inst, addr);
                    break;
                case "sbc_reg":
                    sbc_reg(inst, addr);
                    break;
                case "tst_reg":
                    tst_reg(inst, addr);
                    break;
                case "teq_reg":
                    teq_reg(inst, addr);
                    break;
                case "cmp_reg":
                    cmp_reg(inst, addr);
                    break;
                case "cmn_reg":
                    cmn_reg(inst, addr);
                    break;
                case "orr_reg":
                    orr_reg(inst, addr);
                    break;
                case "mov_reg":
                    mov_reg(inst, addr);
                    break;
                case "lsl_imm":
                    lsl_imm(inst, addr);
                    break;
                case "lsr_imm":
                    lsr_imm(inst, addr);
                    break;
                case "asr_imm":
                    asr_imm(inst, addr);
                    break;
                case "rrx":
                    rrx(inst, addr);
                    break;
                case "ror_imm":
                    ror_imm(inst, addr);
                    break;
                case "bic_reg":
                    bic_reg(inst, addr);
                    break;
                case "mvn_reg":
                    mvn_reg(inst, addr);
                    break;
                case "and_rsr":
                    and_rsr(inst, addr);
                    break;
                case "eor_rsr":
                    eor_rsr(inst, addr);
                    break;
                case "sub_rsr":
                    sub_rsr(inst, addr);
                    break;
                case "rsb_rsr":
                    rsb_rsr(inst, addr);
                    break;
                case "add_rsr":
                    add_rsr(inst, addr);
                    break;
                case "sbc_rsr":
                    sbc_rsr(inst, addr);
                    break;
                case "tst_rsr":
                    tst_rsr(inst, addr);
                    break;
                case "cmp_rsr":
                    cmp_rsr(inst, addr);
                    break;
                case "orr_rsr":
                    orr_rsr(inst, addr);
                    break;
                case "lsl_reg":
                    lsl_reg(inst, addr);
                    break;
                case "lsr_reg":
                    lsr_reg(inst, addr);
                    break;
                case "asr_reg":
                    asr_reg(inst, addr);
                    break;
                case "bic_rsr":
                    bic_rsr(inst, addr);
                    break;
                case "mvn_rsr":
                    mvn_rsr(inst, addr);
                    break;
                case "ldrh_imm":
                    ldrh_imm(inst, addr);
                    break;
                case "ldrh_reg":
                    ldrh_reg(inst, addr);
                    break;
                case "strh_imm":
                    strh_imm(inst, addr);
                    break;
                case "strh_reg":
                    strh_reg(inst, addr);
                    break;
                case "ldrsh_imm":
                    ldrsh_imm(inst, addr);
                    break;
                case "ldrsh_reg":
                    ldrsh_reg(inst, addr);
                    break;
                case "strd_imm":
                    strd_imm(inst, addr);
                    break;
                case "strd_reg":
                    strd_reg(inst, addr);
                    break;
                case "ldrsb_imm":
                    ldrsb_imm(inst, addr);
                    break;
                case "ldrsb_reg":
                    ldrsb_reg(inst, addr);
                    break;
                case "ldrd_imm":
                    ldrd_imm(inst, addr);
                    break;
                case "ldrd_reg":
                    ldrd_reg(inst, addr);
                    break;
                case "mul":
                    mul(inst, addr);
                    break;
                case "mla":
                    mla(inst, addr);
                    break;
                case "mls":
                    mls(inst, addr);
                    break;
                case "umull":
                    umull(inst, addr);
                    break;
                case "umlal":
                    umlal(inst, addr);
                    break;
                case "smull":
                    smull(inst, addr);
                    break;
                case "smlal":
                    smlal(inst, addr);
                    break;
                case "mov_imm_a2":
                    mov_imm_a2(inst, addr);
                    break;
                case "movt":
                    movt(inst, addr);
                    break;
                case "sxtb":
                    sxtb(inst, addr);
                    break;
                case "rev":
                    rev(inst, addr);
                    break;
                case "sxth":
                    sxth(inst, addr);
                    break;
                case "sxtah":
                    sxtah(inst, addr);
                    break;
                case "rev16":
                    rev16(inst, addr);
                    break;
                case "uxtb":
                    uxtb(inst, addr);
                    break;
                case "uxtab":
                    uxtab(inst, addr);
                    break;
                case "uxth":
                    uxth(inst, addr);
                    break;
                case "uxtah":
                    uxtah(inst, addr);
                    break;
                case "usat":
                    usat(inst, addr);
                    break;
                case "sbfx":
                    sbfx(inst, addr);
                    break;
                case "bfc":
                    bfc(inst, addr);
                    break;
                case "bfi":
                    bfi(inst, addr);
                    break;
                case "ubfx":
                    ubfx(inst, addr);
                    break;
                case "bl_imm":
                    bl_imm(inst, addr);
                    break;
                default:
                    //Console.WriteLine("exec {0}", inst_name);
                    break;
            }
        }

        #endregion

        #region Decoder

        string decode_uncond(long inst, long addr)
        {
            long op = 0;
            long op1 = 0;
            long op2 = 0;
            long tmp = 0;

            op1 = (inst >> 20) & 0xff;
            if ((op1 >> 7) == 0)
            {
                // [31:27]=11110
                // Miscellaneous instructions, memory hints, and Advanced SIMD instructions
                op1 = (inst >> 20) & 0x7f;
                op = (inst >> 16) & 1;
                op2 = (inst >> 4) & 0xf;

                tmp = (op1 >> 5) & 3;
                switch (tmp)
                {
                    case 0:
                        if (op1 == 0x10 && (op2 & 2) == 0)
                        {
                            if (op != 0)
                            {
                                // SETEND
                                this.abort_not_impl("SETEND", inst, addr);
                            }
                            else
                            {
                                // CPS
                                return "cps";
                            }
                            break;
                        }
                        this.abort_unknown_inst(inst, addr);
                        break;
                    case 1:
                        // Advanced SIMD data-processing instructions
                        this.abort_simdvfp_inst(inst, addr);
                        break;
                    case 2:
                        if ((op1 & 1) == 0)
                        {
                            // Advanced SIMD element or structure load/store instructions
                            this.abort_simdvfp_inst(inst, addr);
                        }
                        switch (op1 >> 1 & 3)
                        {
                            case 2:
                                if ((op1 & 0x10) != 0)
                                {
                                    // PLD (immediate, literal)
                                    return "pld_imm";
                                }
                                else
                                {
                                    // PLI (immediate, literal)
                                    this.abort_not_impl("PLI (immediate, literal)", inst, addr);
                                }
                                break;
                            case 3:
                                if ((op1 & 0x18) == 0x10)
                                {
                                    switch (op2)
                                    {
                                        case 1:
                                            // CLREX
                                            return "clrex";
                                            // Clear Exclusive clears the local record of the executing processor that an address has had a request for an exclusive access.
                                            // FIXME: Need to do nothing?
                                        case 4:
                                            // DSB
                                            return "dsb";
                                            //var option = bitops.get_bits(inst, 3, 0);
                                            // Data Synchronization Barrier
                                            // FIXME: Need to do nothing?
                                        case 5:
                                            // DMB
                                            return "dmb";
                                            //var option = bitops.get_bits(inst, 3, 0);
                                            // Data Memory Barrier
                                            // FIXME: Need to do nothing?
                                        case 6:
                                            // ISB
                                            return "isb";
                                            //var option = bitops.get_bits(inst, 3, 0);
                                            // Instruction Synchronization Barrier
                                            // FIXME: Need to do nothing?
                                        default:
                                            // UNPREDICTABLE
                                            this.abort_unpredictable_instruction("Miscellaneous instructions, memory hints, and Advanced SIMD instructions", inst, addr);
                                            break;
                                    }
                                }
                                else
                                {
                                    // UNPREDICTABLE
                                    this.abort_unpredictable_instruction("Miscellaneous instructions, memory hints, and Advanced SIMD instructions", inst, addr);
                                }
                                break;
                            default:
                                this.abort_unknown_inst(inst, addr);
                                break;
                        }
                        break;
                    case 3:
                        if ((op2 & 1) == 0)
                        {
                            switch (op1 & 7)
                            {
                                case 5:
                                    if ((op1 & 0x10) != 0)
                                    {
                                        // PLD (register)
                                        this.abort_not_impl("PLD (register)", inst, addr);
                                    }
                                    else
                                    {
                                        // PLI (register)
                                        this.abort_not_impl("PLI (register)", inst, addr);
                                    }
                                    break;
                                case 7:
                                    // UNPREDICTABLE
                                    this.abort_unpredictable_instruction("Miscellaneous instructions, memory hints, and Advanced SIMD instructions", inst, addr);
                                    break;
                                default:
                                    this.abort_unknown_inst(inst, addr);
                                    break;
                            }
                        }
                        break;
                    default:
                        this.abort_decode_error(inst, addr);
                        break;
                }
            }
            else
            {
                switch (op1)
                {
                    case 0xc4:
                        // MCRR, MCRR2
                        this.abort_not_impl("MCRR, MCRR2", inst, addr);
                        break;
                    case 0xc5:
                        // MRRC, MRRC2
                        this.abort_not_impl("MRRC, MRRC2", inst, addr);
                        break;
                    default:
                        tmp = (op1 >> 5) & 7;
                        switch (tmp)
                        {
                            case 4:
                                if ((op1 & 4) != 0)
                                {
                                    if ((op1 & 1) == 0)
                                    {
                                        // SRS
                                        this.abort_not_impl("SRS", inst, addr);
                                        break;
                                    }
                                }
                                else
                                {
                                    if ((op1 & 1) != 0)
                                    {
                                        // RFE
                                        this.abort_not_impl("RFE", inst, addr);
                                        break;
                                    }
                                }
                                this.abort_unknown_inst(inst, addr);
                                break;
                            case 5:
                                // BL, BLX (immediate)
                                this.abort_not_impl("BL, BLX (immediate)", inst, addr);
                                break;
                            case 6:
                                if ((op1 & 1) != 0)
                                {
                                    // LDC, LDC2 (immediate) & LDC, LDC2 (literal)
                                    throw new Exception("UND");
                                }
                                else
                                {
                                    // STC, STC2
                                    throw new Exception("UND");
                                }
                            case 7:
                                if ((op1 & 1 << 4) == 0)
                                {
                                    if ((op & 1) != 0)
                                    {
                                        if ((op1 & 1) != 0)
                                        {
                                            // MRC, MRC2
                                            // TODO
                                            this.abort_not_impl("MRC, MRC2", inst, addr);
                                        }
                                        else
                                        {
                                            // MCR, MCR2
                                            // TODO
                                            this.abort_not_impl("MCR, MCR2", inst, addr);
                                        }
                                    }
                                    else
                                    {
                                        // CDP, CDP2
                                        throw new Exception("UND");
                                    }
                                    break;
                                }
                                this.abort_unknown_inst(inst, addr);
                                break;
                            // Fall through
                            default:
                                this.abort_unknown_inst(inst, addr);
                                break;
                        }
                        break;
                }
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_sync_prim(long inst, long addr)
        {
            // Synchronization primitives
            // [27:24]=0001 [7:4]=1001
            var op = (inst >> 20) & 0xf;

            if ((op & 8) == 0)
            {
                if ((op & 3) == 0)
                {
                    // SWP, SWPB
                    return "swp";
                }
                else
                {
                    this.abort_unknown_inst(inst, addr);
                }
            }
            else
            {
                switch (op & 7)
                {
                    case 0:
                        // STREX
                        return "strex";
                    case 1:
                        // LDREX
                        return "ldrex";
                    case 2:
                        // STREXD
                        return "strexd";
                    case 3:
                        // LDREXD
                        return "ldrexd";
                    case 4:
                        // STREXB
                        this.abort_not_impl("STREXB", inst, addr);
                        break;
                    case 5:
                        // LDREXB
                        this.abort_not_impl("LDREXB", inst, addr);
                        break;
                    case 6:
                        // STREXH
                        this.abort_not_impl("STREXH", inst, addr);
                        break;
                    case 7:
                        // LDREXH
                        this.abort_not_impl("LDREXH", inst, addr);
                        break;
                    default:
                        break;
                }
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_dataproc_imm(long inst, long addr)
        {
            // [27:25]=001
            // Data-processing (immediate)
            var op = (inst >> 20) & 0x1f;
            long rn;
            switch (op >> 1)
            {
                case 0:
                    // AND (immediate)
                    return "and_imm";
                case 1:
                    // EOR (immediate)
                    return "eor_imm";
                case 2:
                    rn = (inst >> 16) & 0xf;
                    if (rn == 0xf)
                    {
                        // [24:21]=0010
                        // ADR A2
                        return "adr_a2";
                    }
                    else
                    {
                        // SUB (immediate)
                        return "sub_imm";
                    }
                case 3:
                    // RSB (immediate)
                    return "rsb_imm";
                case 4:
                    rn = (inst >> 16) & 0xf;
                    if (rn == 0xf)
                    {
                        // [24:21]=0100
                        // ADR A1
                        return "adr_a1";
                    }
                    else
                    {
                        // ADD (immediate)
                        return "add_imm";
                    }
                case 5:
                    // ADC (immediate)
                    return "adc_imm";
                case 6:
                    // SBC (immediate)
                    return "sbc_imm";
                case 7:
                    // RSC (immediate)
                    return "rsc_imm";
                case 8:
                    if ((op & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // TST (immediate)
                    return "tst_imm";
                case 9:
                    if ((op & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // TEQ (immediate)
                    return "teq_imm";
                case 0xa:
                    if ((op & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // CMP (immediate)
                    return "cmp_imm";
                case 0xb:
                    if ((op & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // CMN (immediate)
                    return "cmn_imm";
                case 0xc:
                    // ORR (immediate)
                    return "orr_imm";
                case 0xd:
                    // MOV (immediate) A1
                    return "mov_imm_a1";
                case 0xe:
                    // BIC (immediate)
                    return "bic_imm";
                case 0xf:
                    // MVN (immediate)
                    return "mvn_imm";
                default:
                    break;
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_msr_imm_and_hints(long inst, long addr)
        {
            // [27:23]=00110 [21:20]=10
            // MSR (immediate), and hints
            var op = inst & (1 << 22);
            var op1 = (inst >> 16) & 0xf;
            var op2 = inst & 0xff;
            if (op != 0)
            {
                // MSR (immediate) (system level)
                return "msr_imm_sys";
            }
            else
            {
                if ((op1 & 2) != 0)
                {
                    // MSR (immediate) (system level)
                    return "msr_imm_sys";
                }
                else
                {
                    if ((op1 & 1) != 0)
                    {
                        // MSR (immediate) (system level)
                        return "msr_imm_sys";
                    }
                    else
                    {
                        if ((op1 & 8) != 0)
                        {
                            // MSR (immediate) (application level)
                            this.abort_not_impl("MSR (immediate) (application level)", inst, addr);
                        }
                        else
                        {
                            if ((op1 & 4) != 0)
                            {
                                // MSR (immediate) (application level)
                                this.abort_not_impl("MSR (immediate) (application level)", inst, addr);
                            }
                            else
                            {
                                if ((op2 & 0xf0) == 0xf0)
                                {
                                    // DBG
                                    this.abort_not_impl("DBG", inst, addr);
                                }
                                else
                                {
                                    switch (op2)
                                    {
                                        case 0:
                                            // NOP
                                            return "nop";
                                        case 1:
                                            // YIELD
                                            this.abort_not_impl("YIELD", inst, addr);
                                            break;
                                        case 2:
                                            // WFE
                                            this.abort_not_impl("WFE", inst, addr);
                                            break;
                                        case 3:
                                            // WFI
                                            return "wfi";
                                        case 4:
                                            // SEV
                                            this.abort_not_impl("SEV", inst, addr);
                                            break;
                                        default:
                                            this.abort_unknown_inst(inst, addr);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        void decode_half_mul(long inst, long addr)
        {
            throw new Exception("decode_half_mul");
        }

        string decode_misc(long inst, long addr)
        {
            // [27:23]=00010 [20]=0 [7]=0
            // Miscellaneous instructions
            var op = (inst >> 21) & 0x3;
            var op1 = (inst >> 16) & 0xf;
            var op2 = (inst >> 4) & 0x7;
            switch (op2)
            {
                case 0:
                    if ((op & 1) != 0)
                    {
                        if (!((op & 2) == 2) && (op1 & 3) == 0)
                        {
                            // MSR (register) (application level)
                            this.abort_not_impl("MSR (register) (application level)", inst, addr);
                        }
                        else
                        {
                            // MSR (register) (system level)
                            return "msr_reg_sys";
                        }
                    }
                    else
                    {
                        // MRS
                        return "mrs";
                    }
                    break;
                case 1:
                    switch (op)
                    {
                        case 1:
                            // BX
                            return "bx";
                        case 3:
                            // CLZ
                            return "clz";
                        default:
                            this.abort_unknown_inst(inst, addr);
                            break;
                    }
                    break;
                case 2:
                    if (op != 1)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // BXJ
                    this.abort_not_impl("BXJ", inst, addr);
                    break;
                case 3:
                    if (op != 1)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // BLX (register)
                    return "blx_reg";
                case 5:
                    // Saturating addition and subtraction
                    this.abort_not_impl("Saturating addition and subtraction", inst, addr);
                    break;
                case 7:
                    switch (op)
                    {
                        case 1:
                            // BKPT
                            this.abort_not_impl("BKPT", inst, addr);
                            break;
                        case 3:
                            // SMC (previously SMI)
                            this.abort_not_impl("SMC (previously SMI)", inst, addr);
                            break;
                        default:
                            this.abort_unknown_inst(inst, addr);
                            break;
                    }
                    break;
                default:
                    this.abort_unknown_inst(inst, addr);
                    break;
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_dataproc_reg(long inst, long addr)
        {
            // [27:25]=000 [4]=0
            // Data-processing (register)
            var op1 = (inst >> 20) & 0x1f;
            var op2 = (inst >> 7) & 0x1f;
            var op3 = (inst >> 5) & 0x3;
            // op1 != 0b10xx0
            switch (op1 >> 1)
            {
                case 0:
                    // AND (register)
                    return "and_reg";
                case 1:
                    // EOR (register)
                    return "eor_reg";
                case 2:
                    // SUB (register)
                    return "sub_reg";
                case 3:
                    // RSB (register)
                    return "rsb_reg";
                case 4:
                    // ADD (register)
                    return "add_reg";
                case 5:
                    // ADC (register)
                    return "adc_reg";
                case 6:
                    // SBC (register)
                    return "sbc_reg";
                case 7:
                    // RSC (register)
                    this.abort_not_impl("RSC (register)", inst, addr);
                    break;
                case 8:
                    if ((op1 & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // TST (register)
                    return "tst_reg";
                case 9:
                    if ((op1 & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // TEQ (register)
                    return "teq_reg";
                case 0xa:
                    if ((op1 & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // CMP (register)
                    return "cmp_reg";
                case 0xb:
                    if ((op1 & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // CMN (register)
                    return "cmn_reg";
                case 0xc:
                    // ORR (register)
                    return "orr_reg";
                case 0xd:
                    switch (op3)
                    {
                        case 0:
                            if (op2 == 0)
                            {
                                // MOV (register)
                                return "mov_reg";
                            }
                            else
                            {
                                // LSL (immediate)
                                return "lsl_imm";
                            }
                        case 1:
                            // LSR (immediate)
                            return "lsr_imm";
                        case 2:
                            // ASR (immediate)
                            return "asr_imm";
                        case 3:
                            if (op2 == 0)
                            {
                                // RRX
                                return "rrx";
                            }
                            else
                            {
                                // ROR (immediate)
                                return "ror_imm";
                            }
                        default:
                            break;
                    }
                    break;
                case 0xe:
                    // BIC (register)
                    return "bic_reg";
                case 0xf:
                    // MVN (register)
                    return "mvn_reg";
                default:
                    break;
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_dataproc_rsr(long inst, long addr)
        {
            // [27:25]=000 [7]=0 [4]=1
            // Data-processing (register-shifted register)
            var op1 = (inst >> 20) & 0x1f;
            var op2 = (inst >> 5) & 0x3;
            // op1 != 0b10xx0
            switch (op1 >> 1)
            {
                case 0:
                    // AND (register-shifted register)
                    return "and_rsr";
                case 1:
                    // EOR (register-shifted register)
                    return "eor_rsr";
                case 2:
                    // SUB (register-shifted register)
                    return "sub_rsr";
                case 3:
                    // RSB (register-shifted register)
                    return "rsb_rsr";
                case 4:
                    // ADD (register-shifted register)
                    return "add_rsr";
                case 5:
                    // ADC (register-shifted register)
                    this.abort_not_impl("ADC (register-shifted register)", inst, addr);
                    break;
                case 6:
                    // SBC (register-shifted register)
                    return "sbc_rsr";
                case 7:
                    // RSC (register-shifted register)
                    this.abort_not_impl("RSC (register-shifted register)", inst, addr);
                    break;
                case 8:
                    if ((op1 & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // TST (register-shifted register)
                    return "tst_rsr";
                case 9:
                    if ((op1 & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // TEQ (register-shifted register)
                    this.abort_not_impl("TEQ (register-shifted register)", inst, addr);
                    break;
                case 0xa:
                    if ((op1 & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // CMP (register-shifted register)
                    return "cmp_rsr";
                case 0xb:
                    if ((op1 & 1) == 0)
                    {
                        this.abort_unknown_inst(inst, addr);
                    }
                    // CMN (register-shifted register)
                    this.abort_not_impl("CMN (register-shifted register)", inst, addr);
                    break;
                case 0xc:
                    // ORR (register-shifted register)
                    return "orr_rsr";
                case 0xd:
                    switch (op2)
                    {
                        case 0:
                            // LSL (register)
                            return "lsl_reg";
                        case 1:
                            // LSR (register)
                            return "lsr_reg";
                        case 2:
                            // ASR (register)
                            return "asr_reg";
                        case 3:
                            // ROR (register)
                            this.abort_not_impl("ROR (register)", inst, addr);
                            break;
                        default:
                            break;
                    }
                    break;
                case 0xe:
                    // BIC (register-shifted register)
                    return "bic_rsr";
                case 0xf:
                    // MVN (register-shifted register)
                    return "mvn_rsr";
                default:
                    break;
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_extra_ldst_unpriv1(long inst, long addr)
        {
            // [27:24]=0000 [21]=1 [7]=1 [4]=1
            // [7:4]=1011
            // Extra load/store instructions (unprivileged) #1
            long op = bitops.get_bit(inst, 20);
            //op2=01
            //if ((op2 & 3) === 0) {
            //    this.abort_unknown_inst(inst, addr);
            //}
            if (op != 0)
            {
                // LDRHT
                this.abort_not_impl("LDRHT", inst, addr);
            }
            else
            {
                // STRHT
                this.abort_not_impl("STRHT", inst, addr);
            }
            return null;
        }

        string decode_extra_ldst_unpriv2(long inst, long addr)
        {
            // [27:24]=0000 [21]=1 [7]=1 [4]=1
            // [7:4]=11x1
            // Extra load/store instructions (unprivileged) #2
            // op2=1x
            var op2 = bitops.get_bits(inst, 6, 5);
            //if ((op2 & 3) === 0) {
            //    this.abort_unknown_inst(inst, addr);
            //}
            if (op2 != 0)
            {
                switch (op2)
                {
                    case 2:
                        // LDRSBT
                        this.abort_not_impl("LDRSBT", inst, addr);
                        break;
                    case 3:
                        // LDRSHT
                        this.abort_not_impl("LDRSHT", inst, addr);
                        break;
                    default:
                        this.abort_unknown_inst(inst, addr);
                        break;
                }
            }
            else
            {
                var rt = bitops.get_bits(inst, 15, 12);
                if ((rt & 1) != 0)
                {
                    // UNDEFINED
                    this.abort_undefined_instruction("Extra load/store instructions (unprivileged) #2", inst, addr);
                }
                else
                {
                    // UNPREDICTABLE
                    this.abort_unpredictable_instruction("Extra load/store instructions (unprivileged) #2", inst, addr);
                }
            }
            return null;
        }

        string decode_extra_ldst1(long inst, long addr)
        {
            // [27:25]=000 [7]=1 [4]=1
            // [7:4]=1011
            // Extra load/store instructions #1
            long op1 = (inst >> 20) & 0x1f;
            //op2 = bitops.get_bits(inst, 6, 5);
            //op2=01
            if ((op1 & 1) != 0)
            {
                if ((op1 & 4) != 0)
                {
                    long rn = (inst >> 16) & 0xf;
                    if (rn == 0xf)
                    {
                        // LDRH (literal)
                        this.abort_not_impl("LDRH (literal)", inst, addr);
                    }
                    else
                    {
                        // LDRH (immediate)
                        return "ldrh_imm";
                    }
                }
                else
                {
                    // LDRH (register)
                    return "ldrh_reg";
                }
            }
            else
            {
                if ((op1 & 4) != 0)
                {
                    // STRH (immediate)
                    return "strh_imm";
                }
                else
                {
                    // STRH (register)
                    return "strh_reg";
                }
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_extra_ldst2(long inst, long addr)
        {
            // [27:25]=000 [7]=1 [4]=1
            // [7:4]=11x1
            // Extra load/store instructions #2
            var op1 = (inst >> 20) & 0x1f;
            var op2 = (inst >> 5) & 0x3;
            //op2=1x
            var rn = (inst >> 16) & 0xf;
            if ((op2 & 1) != 0)
            {
                if ((op1 & 1) != 0)
                {
                    if ((op1 & 4) != 0)
                    {
                        if (rn == 0xf)
                        {
                            // LDRSH (literal)
                            this.abort_not_impl("LDRSH (literal)", inst, addr);
                        }
                        else
                        {
                            // LDRSH (immediate)
                            return "ldrsh_imm";
                        }
                    }
                    else
                    {
                        // LDRSH (register)
                        return "ldrsh_reg";
                    }
                }
                else
                {
                    if ((op1 & 4) != 0)
                    {
                        // STRD (immediate)
                        return "strd_imm";
                    }
                    else
                    {
                        // STRD (register)
                        return "strd_reg";
                    }
                }
            }
            else
            {
                if ((op1 & 1) != 0)
                {
                    if ((op1 & 4) != 0)
                    {
                        if (rn == 0xf)
                        {
                            // LDRSB (literal)
                            this.abort_not_impl("LDRSB (literal)", inst, addr);
                        }
                        else
                        {
                            // LDRSB (immediate)
                            return "ldrsb_imm";
                        }
                    }
                    else
                    {
                        // LDRSB (register)
                        return "ldrsb_reg";
                    }
                }
                else
                {
                    if ((op1 & 4) != 0)
                    {
                        if (rn == 0xf)
                        {
                            // LDRD (literal)
                            this.abort_not_impl("LDRD (literal)", inst, addr);
                        }
                        else
                        {
                            // LDRD (immediate)
                            return "ldrd_imm";
                        }
                    }
                    else
                    {
                        // LDRD (register)
                        return "ldrd_reg";
                    }
                }
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_multi(long inst, long addr)
        {
            // [27:24]=0000 [7:4]=1001
            // Multiply and multiply-accumulate

            var op = (inst >> 20) & 0xf;
            switch (op >> 1)
            {
                case 0:
                    // MUL
                    return "mul";
                case 1:
                    // MLA
                    return "mla";
                case 2:
                    if ((op & 1) != 0)
                    {
                        // UNDEFINED
                        this.abort_undefined_instruction("Multiply and multiply-accumulate", inst, addr);
                    }
                    else
                    {
                        // UMAAL
                        this.abort_not_impl("UMAAL", inst, addr);
                    }
                    break;
                case 3:
                    if ((op & 1) != 0)
                    {
                        // UNDEFINED
                        this.abort_undefined_instruction("Multiply and multiply-accumulate", inst, addr);
                    }
                    else
                    {
                        // MLS
                        return "mls";
                    }
                    break;
                case 4:
                    // UMULL
                    return "umull";
                case 5:
                    // UMLAL
                    return "umlal";
                case 6:
                    // SMULL
                    return "smull";
                case 7:
                    // SMLAL
                    return "smlal";
                default:
                    break;
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_datamisc(long inst, long addr)
        {
            // Data-processing and miscellaneous instructions
            var op = (inst >> 25) & 1;
            var op1 = (inst >> 20) & 0x1f;
            var op2 = (inst >> 4) & 0xf;

            if (op != 0)
            {
                //if ((op1 >> 3) == 2 && (op1 & 3) == 2) { // 10x10
                if (op1 == 0x12 || op1 == 0x16)
                { // 10x10
                    return this.decode_msr_imm_and_hints(inst, addr);
                }
                else
                {
                    switch (op1)
                    {
                        case 0x10:
                            // MOV (immediate) A2?
                            return "mov_imm_a2";
                        case 0x14:
                            // MOVT
                            return "movt";
                        default:
                            if ((op1 >> 3) == 2 && (op1 & 1) == 0)
                            {
                                this.abort_unknown_inst(inst, addr);
                                return null;
                            }
                            else
                            { //if (!(op1 >> 3 == 2 && (op1 & 1) === 0)) {
                                // [27:25]=001
                                // Data-processing (immediate)
                                return this.decode_dataproc_imm(inst, addr);
                            }
                    }
                }
            }
            else
            {
                if ((op2 & 1) != 0)
                {
                    if ((op2 >> 3) != 0)
                    {
                        if ((op2 & 4) == 4)
                        {
                            if ((op1 >> 4) == 0 && (op1 & 2) == 2)
                            { // 0xx1x
                                // Extra load/store instructions (unprivileged) #2
                                return this.decode_extra_ldst_unpriv2(inst, addr);
                            }
                            else
                            {
                                // Extra load/store instructions #2
                                return this.decode_extra_ldst2(inst, addr);
                            }
                        }
                        else
                        {
                            if ((op2 & 2) != 0)
                            {
                                if ((op1 >> 4) == 0 && (op1 & 2) == 2)
                                { // 0xx1x
                                    // Extra load/store instructions (unprivileged) #1
                                    return this.decode_extra_ldst_unpriv1(inst, addr);
                                }
                                else
                                {
                                    // Extra load/store instructions #1
                                    return this.decode_extra_ldst1(inst, addr);
                                }
                            }
                            else
                            {
                                if ((op1 >> 4) != 0)
                                {
                                    // Synchronization primitives
                                    return this.decode_sync_prim(inst, addr);
                                }
                                else
                                {
                                    // Multiply and multiply-accumulate
                                    return this.decode_multi(inst, addr);
                                }
                            }
                        }
                    }
                    else
                    {
                        if ((op1 >> 3) == 2 && (op1 & 1) == 0)
                        { // 10xx0
                            // Miscellaneous instructions
                            return this.decode_misc(inst, addr);
                        }
                        else
                        {
                            // Data-processing (register-shifted register)
                            return this.decode_dataproc_rsr(inst, addr);
                        }
                    }
                }
                else
                {
                    if ((op1 >> 3) == 2 && (op1 & 1) == 0)
                    { // 10xx0
                        if ((op2 >> 3) != 0)
                        {
                            // Halfword multiply and multiply-accumulate
                            this.abort_not_impl("Halfword multiply and multiply-accumulate", inst, addr);
                        }
                        else
                        {
                            // Miscellaneous instructions
                            return this.decode_misc(inst, addr);
                        }
                    }
                    else
                    {
                        // Data-processing (register)
                        return this.decode_dataproc_reg(inst, addr);
                    }
                }
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        string decode_media(long inst, long addr)
        {
            // [27:25]=011 [4]=1
            // Media instructions
            long op1 = (inst >> 20) & 0x1f;
            long op2 = (inst >> 5) & 0x7;
            var tmp = op1 >> 3;
            long rn = 0;
            long a;
            switch (tmp)
            {
                case 0:
                    if ((op1 & 4) != 0)
                    {
                        // [27:22]=011001 [4]=1
                        // Parallel addition and subtraction, unsigned
                        op1 = bitops.get_bits(inst, 21, 20);
                        op2 = bitops.get_bits(inst, 7, 5);
                        switch (op1)
                        {
                            case 1:
                                switch (op2)
                                {
                                    case 0:
                                        // UADD16
                                        this.abort_not_impl("UADD16", inst, addr);
                                        break;
                                    case 1:
                                        // UASX
                                        this.abort_not_impl("UASX", inst, addr);
                                        break;
                                    case 2:
                                        // USAX
                                        this.abort_not_impl("USAX", inst, addr);
                                        break;
                                    case 3:
                                        // USUB16
                                        this.abort_not_impl("USUB16", inst, addr);
                                        break;
                                    case 4:
                                        // UADD8
                                        this.abort_not_impl("UADD8", inst, addr);
                                        break;
                                    case 7:
                                        // USUB8
                                        this.abort_not_impl("USUB8", inst, addr);
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                                break;
                            case 2:
                                switch (op2)
                                {
                                    case 0:
                                        // UQADD16
                                        this.abort_not_impl("UQADD16", inst, addr);
                                        break;
                                    case 1:
                                        // UQASX
                                        this.abort_not_impl("UQASX", inst, addr);
                                        break;
                                    case 2:
                                        // UQSAX
                                        this.abort_not_impl("UQSAX", inst, addr);
                                        break;
                                    case 3:
                                        // UQSUB16
                                        this.abort_not_impl("UQSUB16", inst, addr);
                                        break;
                                    case 4:
                                        // UQADD8
                                        this.abort_not_impl("UQADD8", inst, addr);
                                        break;
                                    case 7:
                                        // UQSUB8
                                        this.abort_not_impl("UQSUB8", inst, addr);
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                                break;
                            case 3:
                                switch (op2)
                                {
                                    case 0:
                                        // UHADD16
                                        this.abort_not_impl("UHADD16", inst, addr);
                                        break;
                                    case 1:
                                        // UHASX
                                        this.abort_not_impl("UHASX", inst, addr);
                                        break;
                                    case 2:
                                        // UHSAX
                                        this.abort_not_impl("UHSAX", inst, addr);
                                        break;
                                    case 3:
                                        // UHSUB16
                                        this.abort_not_impl("UHSUB16", inst, addr);
                                        break;
                                    case 4:
                                        // UHADD8
                                        this.abort_not_impl("UHADD8", inst, addr);
                                        break;
                                    case 7:
                                        // UHSUB8
                                        this.abort_not_impl("UHSUB8", inst, addr);
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                                break;
                            default:
                                this.abort_unknown_inst(inst, addr);
                                break;
                        }
                    }
                    else
                    {
                        // [27:22]=011000 [4]=1
                        // Parallel addition and subtraction, signed
                        op1 = bitops.get_bits(inst, 21, 20);
                        op2 = bitops.get_bits(inst, 7, 5);
                        switch (op1)
                        {
                            case 1:
                                switch (op2)
                                {
                                    case 0:
                                        // SADD16
                                        this.abort_not_impl("SADD16", inst, addr);
                                        break;
                                    case 1:
                                        // SASX
                                        this.abort_not_impl("SASX", inst, addr);
                                        break;
                                    case 2:
                                        // SSAX
                                        this.abort_not_impl("SSAX", inst, addr);
                                        break;
                                    case 3:
                                        // SSUB16
                                        this.abort_not_impl("SSUB16", inst, addr);
                                        break;
                                    case 4:
                                        // SADD8
                                        this.abort_not_impl("SADD8", inst, addr);
                                        break;
                                    case 7:
                                        // SSUB8
                                        this.abort_not_impl("SSUB8", inst, addr);
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                                break;
                            case 2:
                                switch (op2)
                                {
                                    case 0:
                                        // QADD16
                                        this.abort_not_impl("QADD16", inst, addr);
                                        break;
                                    case 1:
                                        // QASX
                                        this.abort_not_impl("QASX", inst, addr);
                                        break;
                                    case 2:
                                        // QSAX
                                        this.abort_not_impl("QSAX", inst, addr);
                                        break;
                                    case 3:
                                        // QSUB16
                                        this.abort_not_impl("QSUB16", inst, addr);
                                        break;
                                    case 4:
                                        // QADD8
                                        this.abort_not_impl("QADD8", inst, addr);
                                        break;
                                    case 7:
                                        // QSUB8
                                        this.abort_not_impl("QSUB8", inst, addr);
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                                break;
                            case 3:
                                switch (op2)
                                {
                                    case 0:
                                        // SHADD16
                                        this.abort_not_impl("SHADD16", inst, addr);
                                        break;
                                    case 1:
                                        // SHASX
                                        this.abort_not_impl("SHASX", inst, addr);
                                        break;
                                    case 2:
                                        // SHSAX
                                        this.abort_not_impl("SHSAX", inst, addr);
                                        break;
                                    case 3:
                                        // SHSUB16
                                        this.abort_not_impl("SHSUB16", inst, addr);
                                        break;
                                    case 4:
                                        // SHADD8
                                        this.abort_not_impl("SHADD8", inst, addr);
                                        break;
                                    case 7:
                                        // SHSUB8
                                        this.abort_not_impl("SHSUB8", inst, addr);
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                                break;
                            default:
                                this.abort_unknown_inst(inst, addr);
                                break;
                        }
                    }
                    break;
                case 1:
                    // [27:23]=01101 [4]=1
                    // Packing, unpacking, saturation, and reversal
                    op1 = (inst >> 20) & 0x7;
                    op2 = (inst >> 5) & 0x7;
                    tmp = op1 >> 1;
                    switch (tmp)
                    {
                        case 0:
                            if (op1 != 0)
                            {
                                this.abort_unknown_inst(inst, addr);
                            }
                            if ((op2 & 1) != 0)
                            {
                                switch (op2 >> 1)
                                {
                                    case 1:
                                        a = bitops.get_bits(inst, 19, 16);
                                        if (a == 0xf)
                                        {
                                            // SXTB16
                                            this.abort_not_impl("SXTB16", inst, addr);
                                        }
                                        else
                                        {
                                            // SXTAB16
                                            this.abort_not_impl("SXTAB16", inst, addr);
                                        }
                                        break;
                                    case 2:
                                        // SEL
                                        this.abort_not_impl("SEL", inst, addr);
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                            }
                            else
                            {
                                throw new Exception("PKH");
                            }
                            break;
                        case 1:
                            if ((op2 & 1) != 0)
                            {
                                switch (op1)
                                {
                                    case 2:
                                        switch (op2)
                                        {
                                            case 1:
                                                // SSAT16
                                                this.abort_not_impl("SSAT16", inst, addr);
                                                break;
                                            case 3:
                                                a = bitops.get_bits(inst, 19, 16);
                                                if (a == 0xf)
                                                {
                                                    // SXTB
                                                    return "sxtb";
                                                }
                                                else
                                                {
                                                    // SXTAB
                                                    this.abort_not_impl("SXTAB", inst, addr);
                                                }
                                                break;
                                            default:
                                                this.abort_unknown_inst(inst, addr);
                                                break;
                                        }
                                        break;
                                    case 3:
                                        switch (op2)
                                        {
                                            case 1:
                                                // REV
                                                return "rev";
                                            case 3:
                                                a = (inst >> 16) & 0xf;
                                                if (a == 0xf)
                                                {
                                                    // SXTH
                                                    return "sxth";
                                                }
                                                else
                                                {
                                                    // SXTAH
                                                    return "sxtah";
                                                }
                                            case 5:
                                                // REV16
                                                return "rev16";
                                            default:
                                                this.abort_unknown_inst(inst, addr);
                                                break;
                                        }
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                            }
                            else
                            {
                                // SSAT
                                this.abort_not_impl("SSAT", inst, addr);
                            }
                            break;
                        case 2:
                            if (op2 != 3)
                            {
                                this.abort_unknown_inst(inst, addr);
                            }
                            a = bitops.get_bits(inst, 19, 16);
                            if (a == 0xf)
                            {
                                // UXTB16
                                this.abort_not_impl("UXTB16", inst, addr);
                            }
                            else
                            {
                                // UXTAB16
                                this.abort_not_impl("UXTAB16", inst, addr);
                            }
                            break;
                        case 3:
                            if ((op2 & 1) != 0)
                            {
                                switch (op1)
                                {
                                    case 6:
                                        switch (op2)
                                        {
                                            case 1:
                                                // USAT16
                                                this.abort_not_impl("USAT16", inst, addr);
                                                break;
                                            case 3:
                                                a = (inst >> 16) & 0xf;
                                                if (a == 0xf)
                                                {
                                                    // UXTB
                                                    return "uxtb";
                                                }
                                                else
                                                {
                                                    // UXTAB
                                                    return "uxtab";
                                                }
                                            default:
                                                this.abort_unknown_inst(inst, addr);
                                                break;
                                        }
                                        break;
                                    case 7:
                                        switch (op2)
                                        {
                                            case 1:
                                                // RBIT
                                                this.abort_not_impl("RBIT", inst, addr);
                                                break;
                                            case 3:
                                                a = (inst >> 16) & 0xf;
                                                if (a == 0xf)
                                                {
                                                    // UXTH
                                                    return "uxth";
                                                }
                                                else
                                                {
                                                    // UXTAH
                                                    return "uxtah";
                                                }
                                            case 5:
                                                // REVSH
                                                this.abort_not_impl("REVSH", inst, addr);
                                                break;
                                            default:
                                                this.abort_unknown_inst(inst, addr);
                                                break;
                                        }
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                            }
                            else
                            {
                                // USAT
                                return "usat";
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case 2:
                    // [27:23]=01110 [4]=1
                    // Signed multiplies
                    op1 = (inst >> 20) & 0x7;
                    op2 = (inst >> 5) & 0x7;
                    a = (inst >> 12) & 0xf;
                    switch (op1)
                    {
                        case 0:
                            switch (op2 >> 1)
                            {
                                case 0:
                                    if (a == 0xf)
                                    {
                                        // SMUAD
                                        this.abort_not_impl("SMUAD", inst, addr);
                                    }
                                    else
                                    {
                                        // SMLAD
                                        this.abort_not_impl("SMLAD", inst, addr);
                                    }
                                    break;
                                case 1:
                                    if (a == 0xf)
                                    {
                                        // SMUSD
                                        this.abort_not_impl("SMUSD", inst, addr);
                                    }
                                    else
                                    {
                                        // SMLSD
                                        this.abort_not_impl("SMLSD", inst, addr);
                                    }
                                    break;
                                default:
                                    this.abort_unknown_inst(inst, addr);
                                    break;
                            }
                            break;
                        case 4:
                            switch (op2 >> 1)
                            {
                                case 0:
                                    // SMLALD
                                    this.abort_not_impl("SMLALD", inst, addr);
                                    break;
                                case 1:
                                    // SMLSLD
                                    this.abort_not_impl("SMLSLD", inst, addr);
                                    break;
                                default:
                                    this.abort_unknown_inst(inst, addr);
                                    break;
                            }
                            break;
                        case 5:
                            switch (op2 >> 1)
                            {
                                case 0:
                                    if (a == 0xf)
                                    {
                                        // SMMUL
                                        this.abort_not_impl("SMMUL", inst, addr);
                                    }
                                    else
                                    {
                                        // SMMLA
                                        this.abort_not_impl("SMMLA", inst, addr);
                                    }
                                    break;
                                case 3:
                                    // SMMLS
                                    this.abort_not_impl("SMMLS", inst, addr);
                                    break;
                                default:
                                    this.abort_unknown_inst(inst, addr);
                                    break;
                            }
                            break;
                        default:
                            this.abort_unknown_inst(inst, addr);
                            break;
                    }
                    break;
                case 3:
                    if (op1 == 0x1f && op2 == 7)
                    {
                        // UNDEFINED
                        this.abort_undefined_instruction("Signed multiplies", inst, addr);
                    }
                    switch (op1 >> 1 & 3)
                    {
                        case 0:
                            if ((op1 & 1) == 0 && op2 == 0)
                            {
                                var rd = bitops.get_bits(inst, 15, 12);
                                if (rd == 0xf)
                                {
                                    // USAD8
                                    this.abort_not_impl("USAD8", inst, addr);
                                }
                                else
                                {
                                    // USADA8
                                    this.abort_not_impl("USADA8", inst, addr);
                                }
                                break;
                            }
                            this.abort_unknown_inst(inst, addr);
                            break;
                        case 1:
                            if ((op2 & 3) == 2)
                            {
                                // SBFX
                                return "sbfx";
                            }
                            this.abort_unknown_inst(inst, addr);
                            break;
                        case 2:
                            if ((op2 & 3) == 0)
                            {
                                rn = inst & 0xf;
                                if (rn == 0xf)
                                {
                                    // BFC
                                    return "bfc";
                                }
                                else
                                {
                                    // BFI
                                    return "bfi";
                                }
                            }
                            this.abort_unknown_inst(inst, addr);
                            break;
                        case 3:
                            if ((op2 & 3) == 2)
                            {
                                // UBFX
                                return "ubfx";
                            }
                            this.abort_unknown_inst(inst, addr);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        public string decode(long inst, long addr)
        {
            /*
     *  bits[31:28]: cond
     *  bits[27:25]: op1
     *  bit[4]: op
     */
            var cond = inst >> 28;
            var op = (inst >> 4) & 1;
            var op1 = (inst >> 25) & 7;
            long rn;
            long coproc;

            this.shift_t = 0;
            this.shift_n = 0;
            this.carry_out = 0;
            this.overflow = 0;

            if (inst == 0xeef06a10)
                return "vmrs";
            
            if (cond == 0xf)
            {
                // Unconditional instructions
                return this.decode_uncond(inst, addr);
            }
            else
            { // cond != 0xf
                switch (op1 >> 1)
                {
                    case 0:
                        // Data-processing and miscellaneous instructions
                        return this.decode_datamisc(inst, addr);
                    case 1:
                        if ((op1 & 1) != 0)
                        {
                            if (op != 0)
                            {
                                // [27:25]=011 [4]=1
                                // Media instructions
                                return this.decode_media(inst, addr);
                            }
                            else
                            {
                                // [27:25]=011 [4]=0
                                // Load/store word and unsigned byte #2
                                op1 = (inst >> 20) & 0x1f;
                                // A=1 B=0
                                if ((op1 & 1) != 0)
                                {
                                    if ((op1 & 4) != 0)
                                    { // xx1x1
                                        if (op1 == 7 || op1 == 15)
                                        { // 0x111
                                            // LDRBT
                                            this.abort_not_impl("LDRBT", inst, addr);
                                        }
                                        else
                                        {
                                            // LDRB (register)
                                            return "ldrb_reg";
                                        }
                                    }
                                    else
                                    { // xx0x1
                                        if (op1 == 3 || op1 == 11)
                                        { // 0x011
                                            // LDRT
                                            this.abort_not_impl("LDRT A2", inst, addr);
                                        }
                                        else
                                        {
                                            // LDR (register)
                                            return "ldr_reg";
                                        }
                                    }
                                }
                                else
                                {
                                    if ((op1 & 4) != 0)
                                    { // xx1x0
                                        if (op1 == 6 || op1 == 14)
                                        { // 0x110
                                            // STRBT A2
                                            return "strbt_a2";
                                        }
                                        else
                                        {
                                            // STRB (register)
                                            return "strb_reg";
                                        }
                                    }
                                    else
                                    { // xx0x0
                                        if (op1 == 2 || op1 == 10)
                                        { // 0x010
                                            // STRT
                                            this.abort_not_impl("STRT", inst, addr);
                                        }
                                        else
                                        {
                                            // STR (register)
                                            return "str_reg";
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // [27:25]=010 [4]=x
                            // Load/store word and unsigned byte #1
                            op1 = (inst >> 20) & 0x1f;
                            // A=0 B=x
                            if ((op1 & 1) != 0)
                            {
                                if ((op1 & 4) != 0)
                                { // xx1x1
                                    if (op1 == 7 || op1 == 15)
                                    { // 0x111
                                        // LDRBT
                                        this.abort_not_impl("LDRBT", inst, addr);
                                    }
                                    else
                                    {
                                        rn = (inst >> 16) & 0xf;
                                        if (rn == 0xf)
                                        {
                                            // LDRB (literal)
                                            this.abort_not_impl("LDRB (literal)", inst, addr);
                                        }
                                        else
                                        {
                                            // LDRB (immediate)
                                            return "ldrb_imm";
                                        }
                                    }
                                    //break;
                                }
                                else
                                { // xx0x1
                                    if (op1 == 3 || op1 == 0xb)
                                    { // 0x011
                                        // LDRT
                                        return "ldrt_a1";
                                    }
                                    else
                                    {
                                        rn = (inst >> 16) & 0xf;
                                        if (rn == 0xf)
                                        {
                                            // LDR (literal)
                                            return "ldr_lit";
                                        }
                                        else
                                        {
                                            // LDR (immediate)
                                            return "ldr_imm";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((op1 & 4) != 0)
                                { // xx1x0
                                    if (op1 == 6 || op1 == 14)
                                    { // 0x110
                                        // STRBT A1
                                        return "strbt_a1";
                                    }
                                    else
                                    {
                                        // STRB (immediate)
                                        return "strb_imm";
                                    }
                                }
                                else
                                { // xx0x0
                                    if (op1 == 2 || op1 == 10)
                                    { // 0x010
                                        // STRT
                                        this.abort_not_impl("STRT", inst, addr);
                                    }
                                    else
                                    {
                                        // STR (immediate)
                                        return "str_imm";
                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        // [27:26]=10
                        // Branch, branch with link, and block data transfer
                        op = (inst >> 20) & 0x3f;
                        if ((op & 0x20) != 0)
                        {
                            if ((op & 0x10) != 0)
                            {
                                // BL, BLX (immediate)
                                return "bl_imm";
                            }
                            else
                            {
                                // [27:24]=1010
                                // B (branch)
                                return "b";
                            }
                        }
                        else
                        {
                            if ((op & 4) != 0)
                            {
                                if ((op & 1) != 0)
                                {
                                    var r = (inst >> 15) & 1;
                                    if (r != 0)
                                    {
                                        // LDM (exception return)
                                        return "ldm_er";
                                    }
                                    else
                                    {
                                        // LDM (user registers)
                                        return "ldm_ur";
                                    }
                                }
                                else
                                {
                                    // STM (user registers)
                                    return "stm_ur";
                                }
                            }
                            else
                            {
                                if ((op & 1) != 0)
                                {
                                    switch (op >> 2 & 7)
                                    { // 0b11100
                                        case 0:
                                            // LDMDA / LDMFA
                                            return "ldmda";
                                        case 2:
                                            // LDM / LDMIA / LDMFD
                                            return "ldm";
                                        case 4:
                                            // LDMDB / LDMEA
                                            return "ldmdb";
                                        case 6:
                                            // LDMIB / LDMED
                                            return "ldmib";
                                        default:
                                            this.abort_unknown_inst(inst, addr);
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (op >> 2 & 7)
                                    { // 0b11100
                                        case 0:
                                            // STMDA / STMED
                                            this.abort_not_impl("STMDA / STMED", inst, addr);
                                            break;
                                        case 2:
                                            // STM / STMIA / STMEA
                                            return "stm";
                                        case 4:
                                            // STMDB / STMFD
                                            return "stmdb";
                                        case 6:
                                            // STMIB / STMFA
                                            return "stmib";
                                        default:
                                            this.abort_unknown_inst(inst, addr);
                                            break;
                                    }
                                }
                            }
                        }
                        break;
                    case 3:
                        // [27:26]=11
                        // System call, and coprocessor instructions
                        op1 = (inst >> 20) & 0x3f;
                        op = (inst >> 4) & 1;
                        if ((op1 & 0x20) != 0)
                        {
                            if ((op1 & 0x10) != 0)
                            {
                                // SVC (previously SWI)
                                return "svc";
                            }
                            else
                            {
                                coproc = (inst >> 8) & 0xf;
                                if (op != 0)
                                {
                                    if ((coproc >> 1) == 5)
                                    { // 0b101x
                                        // Advanced SIMD, VFP
                                        // 8, 16, and 32-bit transfer between ARM core and extension registers
                                        this.abort_simdvfp_inst(inst, addr);
                                    }
                                    else
                                    {
                                        if ((op1 & 1) != 0)
                                        {
                                            // cond != 1111
                                            // MRC, MRC2 A1
                                            return "mrc_a1";
                                        }
                                        else
                                        {
                                            // cond != 1111
                                            // MCR, MCR2 A1
                                            return "mcr_a1";
                                        }
                                    }
                                }
                                else
                                {
                                    if ((coproc >> 1) == 5)
                                    { // 0b101x
                                        // VFP data-processing instructions
                                        this.abort_simdvfp_inst(inst, addr);
                                    }
                                    else
                                    {
                                        // CDP, CDP2
                                        throw new Exception("UND");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if ((op1 >> 3) == 0 && (op1 & 2) == 0)
                            { // 000x0x
                                switch (op1 >> 1)
                                {
                                    case 0:
                                        // UNDEFINED
                                        this.abort_undefined_instruction("System call, and coprocessor instructions", inst, addr);
                                        break;
                                    case 2:
                                        coproc = bitops.get_bits(inst, 11, 8);
                                        if ((coproc >> 1) == 5)
                                        { // 0b101x
                                            // 64-bit transfers between ARM core and extension registers
                                            this.abort_simdvfp_inst(inst, addr);
                                        }
                                        else
                                        {
                                            if ((op1 & 1) != 0)
                                            {
                                                // MRRC, MRRC2
                                                this.abort_not_impl("MRRC, MRRC2", inst, addr);
                                            }
                                            else
                                            {
                                                // MCRR, MCRR2
                                                this.abort_not_impl("MCRR, MCRR2", inst, addr);
                                            }
                                        }
                                        break;
                                    default:
                                        this.abort_unknown_inst(inst, addr);
                                        break;
                                }
                            }
                            else
                            {
                                coproc = bitops.get_bits(inst, 11, 8);
                                if ((coproc >> 1) == 5)
                                { // 0b101x
                                    // Advanced SIMD, VFP
                                    // Extension register load/store instructions
                                    this.abort_simdvfp_inst(inst, addr);
                                }
                                else
                                {
                                    if ((op1 & 1) != 0)
                                    {
                                        rn = bitops.get_bits(inst, 19, 16);
                                        if (rn == 0xf)
                                        {
                                            // LDC, LDC2 (literal)
                                            throw new Exception("UND");
                                        }
                                        else
                                        {
                                            // LDC, LDC2 (immediate)
                                            throw new Exception("UND");
                                        }
                                    }
                                    else
                                    {
                                        // STC, STC2
                                        throw new Exception("UND");
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            this.abort_unknown_inst(inst, addr);
            return null;
        }

        #endregion

        void interrupt(int irq)
        {
            //logger.log("got interrupt");
            this.spsr_irq = this.clone_psr(this.cpsr);
            this.regs_irq[14] = this.get_pc() - 4;

            this.change_mode(IRQ_MODE);
            this.cpsr.i = 1;
            this.cpsr.a = 1;

            //var cp15 = this.coprocs[15];
            //this.regs[15] = cp15.interrupt_vector_address + 0x18;
        }

        void data_abort()
        {
            //logger.log("got data abort");
            this.spsr_abt = this.clone_psr(this.cpsr);
            this.regs_abt[14] = this.get_pc();

            this.change_mode(ABT_MODE);
            this.cpsr.i = 1;

            //var cp15 = this.coprocs[15];
            //this.regs[15] = cp15.interrupt_vector_address + 0x10;
        }

        void prefetch_abort()
        {
            //logger.log("got prefetch abort");
            this.spsr_abt = this.clone_psr(this.cpsr);
            this.regs_abt[14] = this.get_pc() - 4;

            this.change_mode(ABT_MODE);
            this.cpsr.i = 1;

            //var cp15 = this.coprocs[15];
            //this.regs[15] = cp15.interrupt_vector_address + 0x0c;
        }

        void supervisor()
        {
            //logger.log("got svc");
            this.spsr_svc = this.clone_psr(this.cpsr);
            this.regs_svc[14] = this.get_pc() - 4;

            this.change_mode(SVC_MODE);
            this.cpsr.i = 1;

            //var cp15 = this.coprocs[15];
            //this.regs[15] = cp15.interrupt_vector_address + 0x08;
        }

        void undefined_instruction()
        {
            //logger.log("undef instr");
            this.spsr_und = this.clone_psr(this.cpsr);
            this.regs_und[14] = this.get_pc() - 4;

            this.change_mode(UND_MODE);
            this.cpsr.i = 1;

            //var cp15 = this.coprocs[15];
            //this.regs[15] = cp15.interrupt_vector_address + 0x04;
        }
    }
}
