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
        string imageIdentifier;
        byte[] imageData;

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
            int width = GUIGraphicsContext.SkinSize.Width;
            Logger.Instance.Debug("PhotoWindow: Skin width '{0}'", width);
            if (width != 1920 && width != 1280 && width != 960 && width != 720)
            {
                Logger.Instance.Warn("PhotoWindow: Unsupported width '{0}', defaulting to 1280", width);
                width = 1280;
            }
            GUIPropertyManager.SetProperty("#AirPlay.skinwidth", width.ToString());
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
            imageSwapper.SetResource(imageIdentifier, imageData);
        }

        public void SetPhoto(string imageIdentifier, byte[] imageData)
        {
            this.imageIdentifier = imageIdentifier;
            this.imageData = imageData;
            if (imageSwapper != null)
                imageSwapper.SetResource(imageIdentifier, imageData);
        }
    }
}
