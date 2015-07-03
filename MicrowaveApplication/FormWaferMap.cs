using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using NIIPP.WaferMeasData;

namespace MicrowaveApplication
{
    public partial class FormWaferMap : Form
    {
        public FormWaferMap()
        {
            InitializeComponent();
        }

        public static FormWaferMap Instance { get; private set; }

        private void LoadWaferMap(Bitmap bmp)
        {
            pbWaferMap.Image = bmp;
            Refresh();
        }

        public void RefreshPicture()
        {
            LoadWaferMap(WaferMeasData.MasOfWaferData.Values.First().GetBmpWaferMap(pbWaferMap.Width, pbWaferMap.Height));
        }

        private void FormWaferMap_Load(object sender, EventArgs e)
        {
            Instance = this;
            LoadWaferMap(WaferMeasData.MasOfWaferData.Values.First().GetBmpWaferMap(pbWaferMap.Width, pbWaferMap.Height));
        }

        private void FormWaferMap_Resize(object sender, EventArgs e)
        {
            RefreshPicture();
        }
    }
}
