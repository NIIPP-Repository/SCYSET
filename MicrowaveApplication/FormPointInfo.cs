using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MicrowaveApplication
{
    public partial class FormPointInfo : Form
    {
        public FormPointInfo()
        {
            InitializeComponent();
        }

        public static FormPointInfo Instance { get; private set; }

        private int MeasureString(string str)
        {
            int res = 0;
            string[] mas = str.Split(' ');
            using (Graphics g = CreateGraphics())
            {
                res += mas.Select(token => g.MeasureString(token, SystemFonts.CaptionFont)).Select(size => (int) (size.Width + 2.5)).Sum();
            }
            return res;
        }

        public void Build(double x, double y, string additionInfo)
        {
            lblX.Text = String.Format("{0} {1}", GraficLibrary.ToStr(x), GraficLibrary.DimOfX);
            lblY.Text = String.Format("{0} {1}", GraficLibrary.ToStr(y), GraficLibrary.DimOfY);

            Text = additionInfo;
            int newWidth = Math.Max(MeasureString(additionInfo) + 10, Width);
            if (newWidth != Width)
                Width = newWidth;
        }


        private void PointInfo_Load(object sender, EventArgs e)
        {
            Instance = this;
            Top = 40;
            Left = 200;
        }

        private void PointInfo_FormClosed(object sender, FormClosedEventArgs e)
        {
            GraficLibrary.RemoveTemproryCross();
            FormPlot.Instance.RefreshPicture();
        }
    }
}
