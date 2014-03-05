using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShairportSharp_Test
{
    public partial class PhotoForm : Form
    {
        public PhotoForm()
        {
            InitializeComponent();
        }

        public void SetPhoto(Image image)
        {
            if (panelPhoto.BackgroundImage != null)
                panelPhoto.BackgroundImage.Dispose();
            panelPhoto.BackgroundImage = image;
        }
    }
}
