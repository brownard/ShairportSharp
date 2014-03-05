using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Airplay
{
    interface IPlistResponse
    {
        Dictionary<string, object> GetPlist();
    }
}
