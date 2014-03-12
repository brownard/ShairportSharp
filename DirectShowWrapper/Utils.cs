using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DirectShow.Helper
{
    public static class Utils
    {
        public static IBaseFilter AddFilterByName(IGraphBuilder graphBuilder, Guid deviceCategory, string friendlyName)
        {
            IBaseFilter filter = null;

            if (graphBuilder == null)
            {
                return null;// throw new ArgumentNullException("graphBuilder");
            }

            foreach (DSDevice t in new DSCategory(deviceCategory))
            {
                if (String.Compare(t.Name, friendlyName, true) != 0)
                {
                    continue;
                }

                int hr = ((IFilterGraph2)graphBuilder).AddSourceFilterForMoniker(t.Value, null, friendlyName, out filter);
                if (hr != 0 || filter == null)
                    return null; //new HRESULT(hr).Throw();

                break;
            }

            return filter;
        }
    }
}
