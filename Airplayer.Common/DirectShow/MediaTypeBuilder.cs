using AirPlayer.Common.H264;
using DirectShow;
using ShairportSharp.Mirroring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AirPlayer.Common.DirectShow
{
    class MediaTypeBuilder
    {
        const int FOURCC_H264 = 0x34363248;
        const int FOURCC_AVC1 = 0x31435641;
        static readonly Guid SUBTYPE_AVC1 = new Guid(0x31435641, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
        
        public static AMMediaType H264(H264CodecData codecData)
        {
            SPSUnit spsUnit = new SPSUnit(codecData.SPS);
            int width = spsUnit.Width();
            int height = spsUnit.Height();

            VideoInfoHeader2 vi = new VideoInfoHeader2();
            vi.SrcRect.right = width;
            vi.SrcRect.bottom = height;
            vi.TargetRect.right = width;
            vi.TargetRect.bottom = height;

            int hcf = HCF(width, height);
            vi.PictAspectRatioX = width / hcf;
            vi.PictAspectRatioY = height / hcf;

            vi.BmiHeader.Width = width;
            vi.BmiHeader.Height = height;
            vi.BmiHeader.Planes = 1;
            vi.BmiHeader.Compression = FOURCC_H264;

            AMMediaType amt = new AMMediaType();
            amt.majorType = MediaType.Video;
            amt.subType = MediaSubType.H264;
            amt.temporalCompression = true;
            amt.fixedSizeSamples = false;
            amt.sampleSize = 1;
            amt.SetFormat(vi);
            return amt;
        }

        public static AMMediaType AVC1(H264CodecData codecData)
        {
            SPSUnit spsUnit = new SPSUnit(codecData.SPS);
            int width = spsUnit.Width();
            int height = spsUnit.Height();

            Mpeg2VideoInfo vi = new Mpeg2VideoInfo();
            vi.hdr.SrcRect.right = width;
            vi.hdr.SrcRect.bottom = height;
            vi.hdr.TargetRect.right = width;
            vi.hdr.TargetRect.bottom = height;

            int hcf = HCF(width, height);
            vi.hdr.PictAspectRatioX = width / hcf;
            vi.hdr.PictAspectRatioY = height / hcf;

            vi.hdr.BmiHeader.Width = width;
            vi.hdr.BmiHeader.Height = height;
            vi.hdr.BmiHeader.Planes = 1;
            vi.hdr.BmiHeader.Compression = FOURCC_AVC1;

            vi.dwProfile = (uint)codecData.Profile;
            vi.dwLevel = (uint)codecData.Level;
            vi.dwFlags = (uint)codecData.NALSizeMinusOne + 1;

            byte[] extraData = NaluParser.CreateAVC1ParameterSet(codecData.SPS, codecData.PPS, 2);
            vi.cbSequenceHeader = (uint)extraData.Length;

            AMMediaType amt = new AMMediaType();
            amt.majorType = MediaType.Video;
            amt.subType = SUBTYPE_AVC1;
            amt.temporalCompression = true;
            amt.fixedSizeSamples = false;
            amt.sampleSize = 1;
            setFormat(vi, extraData, amt);
            return amt;
        }

        static void setFormat(Mpeg2VideoInfo vi, byte[] extraData, AMMediaType amt)
        {
            int cb = Marshal.SizeOf(vi);
            int add = extraData == null || extraData.Length < 4 ? 0 : extraData.Length - 4;
            IntPtr _ptr = Marshal.AllocCoTaskMem(cb + add);
            try
            {
                Marshal.StructureToPtr(vi, _ptr, false);
                if (extraData != null)
                    Marshal.Copy(extraData, 0, _ptr + cb - 4, extraData.Length);
                amt.SetFormat(_ptr, cb + add);
                amt.formatType = FormatType.Mpeg2Video;
            }
            finally
            {
                Marshal.FreeCoTaskMem(_ptr);
            }
        }

        static int HCF(int x, int y)
        {
            return y == 0 ? x : HCF(y, x % y);
        }
    }
}
