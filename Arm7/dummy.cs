using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arm7
{
    class dummy
    {

        void dummy(string inst_name, long inst, long addr)
        {
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
                case "svc":
                    svc(inst, addr);
                    break;
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
                    ldrb_reg(inst, addr);
                    break;
                case "strh_reg":
                    ldrb_reg(inst, addr);
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
    }
}
