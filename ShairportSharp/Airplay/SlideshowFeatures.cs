using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShairportSharp.Airplay
{
    public class SlideshowFeatures : IPlistResponse
    {
        public SlideshowFeatures()
        {
            Themes = new List<SlideshowTheme>();
        }

        public List<SlideshowTheme> Themes { get; protected set; }

        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();

            List<object> themeList = new List<object>();
            foreach (SlideshowTheme theme in Themes)
                themeList.Add(theme.GetPlist());

            plist["themes"] = themeList;
            return plist;
        }
    }

    public class SlideshowTheme : IPlistResponse
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public Dictionary<string, object> GetPlist()
        {
            Dictionary<string, object> plist = new Dictionary<string, object>();
            plist["key"] = Key;
            plist["name"] = Name;
            return plist;
        }
    }
}
