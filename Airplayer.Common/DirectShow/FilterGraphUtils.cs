using DirectShow;
using DirectShow.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AirPlayer.Common.DirectShow
{
    public static class FilterGraphUtils
    {
        public static IBaseFilter AddFilterByName(IGraphBuilder graphBuilder, Guid deviceCategory, string friendlyName)
        {
            IBaseFilter filter = null;

            if (graphBuilder == null)
                return null; // throw new ArgumentNullException("graphBuilder");

            DSDevice dsDevice;
            using (DSCategory dsCategory = new DSCategory(deviceCategory))
                dsDevice = dsCategory.FirstOrDefault(d => String.Compare(d.Name, friendlyName, StringComparison.OrdinalIgnoreCase) == 0);

            if (dsDevice == null)
                return null;

            int hr = ((IFilterGraph2)graphBuilder).AddSourceFilterForMoniker(dsDevice.Value, null, friendlyName, out filter);
            if (hr != 0 || filter == null)
                return null; //new HRESULT(hr).Throw();

            return filter;
        }

        public static void RemoveUnusedFilters(IGraphBuilder graphBuilder)
        {
            IEnumFilters pEnum;
            if (graphBuilder.EnumFilters(out pEnum) >= 0)
            {
                try
                {
                    IBaseFilter[] aFilters = new IBaseFilter[1];
                    while (pEnum.Next(1, aFilters, IntPtr.Zero) == 0)
                    {
                        using (var filter = new DSFilter(aFilters[0]))
                        {
                            if (filter.InputPin != null && !filter.InputPin.IsConnected && filter.OutputPin != null && !filter.OutputPin.IsConnected)
                                graphBuilder.RemoveFilter(filter.Value);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(pEnum);
                }
            }
        }

        public static void SetQualityControl(IGraphBuilder graphBuilder, IQualityControl iqc)
        {
            IEnumFilters pEnum;
            if (graphBuilder.EnumFilters(out pEnum) >= 0)
            {
                try
                {
                    IBaseFilter[] aFilters = new IBaseFilter[1];
                    while (pEnum.Next(1, aFilters, IntPtr.Zero) == 0)
                    {
                        using (var filter = new DSFilter(aFilters[0]))
                        {
                            if (isVideoRenderer(filter))
                            {
                                IntPtr piqc = Marshal.GetComInterfaceForObject(iqc, typeof(IQualityControl));
                                try
                                {
                                    ((IQualityControl)filter.InputPin.Value).SetSink(piqc);
                                }
                                finally
                                {
                                    Marshal.Release(piqc);
                                }
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(pEnum);
                }
            }
        }

        static bool isVideoRenderer(DSFilter filter)
        {
            return filter.OutputPin == null && filter.InputPin != null && filter.InputPin.ConnectionMediaType != null && filter.InputPin.ConnectionMediaType.majorType == MediaType.Video;
        }
    }
}
