using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cornerstone.MP;
using MediaPortal.GUI.Library;

namespace AirPlayer
{
    public class PhotoWindow : GUIWindow
    {
        public const int WINDOW_ID = 9421;
        const string SKIN_FILE = "AirplayPhotoWindow.xml";
        ImageSwapper imageSwapper;
        string filename;

        [SkinControlAttribute(1230)]
        protected GUIImage photoControl1 = null;
        [SkinControlAttribute(1231)]
        protected GUIImage photoControl2 = null;

        public PhotoWindow()
        {
            GetID = WINDOW_ID;
        }

        public override bool Init()
        {
            return Load(GUIGraphicsContext.Skin + "\\" + SKIN_FILE);
        }

        bool firstLoad = true;
        protected override void OnPageLoad()
        {
            base.OnPageLoad();
            if (firstLoad)
            {
                firstLoad = false;
                imageSwapper = new ImageSwapper();
                imageSwapper.PropertyOne = "#AirPlay.Photo1";
                imageSwapper.PropertyTwo = "#AirPlay.Photo2";
            }
            imageSwapper.GUIImageOne = photoControl1;
            imageSwapper.GUIImageTwo = photoControl2;
            imageSwapper.Filename = filename;
        }

        public void SetPhoto(string filename)
        {
            this.filename = filename;
            if (imageSwapper != null)
                imageSwapper.Filename = filename;
        }
    }
}
