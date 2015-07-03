using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using NIIPP.WaferMeasData;

namespace MicrowaveApplication
{
    public partial class FormPlot : Form
    {
        public FormPlot()
        {
            InitializeComponent();
        }

        public static FormPlot Instance { get; private set; }

        // переменные для отбраковки
        private string _currResume = "measure";

        private int 
            _numOfCullingClick,
            _numOfZoomClick,
            _x0Zoom,
            _y0Zoom,
            _x1Zoom,
            _y1Zoom,
            _currCurve;

        // координаты выбраных точек, для отбраковки
        private int
            _x0PixCull,
            _y0PixCull,
            _x1PixCull,
            _y1PixCull;

        // храним информацию о кривых которые на экране
        int[][] _curveCoordX;
        int[][] _curveCoordY;
        List<string>[] _curveInfo;

        readonly List<string> _setOfCurves = new List<string>();




        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            const int wmKeydown = 0x100;
            const int wmSyskeydown = 0x104;

            if ((msg.Msg == wmKeydown) || (msg.Msg == wmSyskeydown))
            {
                if (keyData == Keys.Space)
                {
                    // в зависимости от режима один из методов сработает
                    DoGateCulling(false);
                    DoZoom();
                }

                if (keyData == Keys.C)
                {
                    SwitchToCulling();
                }
                if (keyData == Keys.Z)
                {
                    SwitchToZoom();
                }
                if (keyData == Keys.M)
                {
                    SwitchToMeasure();
                }
                if (keyData == Keys.A)
                {
                    DrawWithAutoSize();
                }
                if (keyData == Keys.NumPad1)
                {
                    DoQuadCulling(_x0PixCull, _y0PixCull, "left-down");
                }
                if (keyData == Keys.NumPad3)
                {
                    DoQuadCulling(_x0PixCull, _y0PixCull, "right-down");
                }
                if (keyData == Keys.NumPad7)
                {
                    DoQuadCulling(_x0PixCull, _y0PixCull, "left-up");
                }
                if (keyData == Keys.NumPad9)
                {
                    DoQuadCulling(_x0PixCull, _y0PixCull, "right-up");
                }
                if (keyData == Keys.I)
                {
                    DoGateCulling(true);
                }

                if (FormIsOpen("FormWaferMap"))
                    FormWaferMap.Instance.RefreshPicture();
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        void plot_Load(object sender, EventArgs e)
        {
            Instance = this;
            Text = "График [ measure ]";
            CalibratePlotWindow();
        }

        public void RefreshPicture()
        {
            pictureBox_plot.Image = GraficLibrary.PicPlot;
        }

        public void LoadBitmap(Bitmap bmp)
        {
            pictureBox_plot.Image = bmp;
        }

        public void LoadCurveInfo(List<string>[] curveInfoPar, int n)
        {
            _setOfCurves.Clear();
            for (int i = 1; i <= n; i++)
            {
                string res = "";
                int k = 0;
                foreach (string item in curveInfoPar[i])
                {
                    if (k != 0)
                        res = res + item + " \n";
                    k++;
                }
                _setOfCurves.Add(res);
            }

            _curveInfo = curveInfoPar;
        }

        public void LoadCurveCoord(int[][] masX, int[][] masY)
        {
            _curveCoordX = masX;
            _curveCoordY = masY;
        }

        void CalibratePlotWindow()
        {
            GraficLibrary.WPlot = pictureBox_plot.Width;
            GraficLibrary.HPlot = pictureBox_plot.Height;
        }

        void ZoomClick(MouseEventArgs e)
        {
            _numOfZoomClick++;

            int x = e.X,
                y = GraficLibrary.HPlot - e.Y;

            if (_numOfZoomClick == 1)
            {
                if (GraficLibrary.ConvertFromPixelToRealX(x) != 0 && GraficLibrary.ConvertFromPixelToRealY(y) != 0)
                {
                    GraficLibrary.DrawCross(x, y);
                    _x0Zoom = x;
                    _y0Zoom = y;
                }
                else
                    _numOfZoomClick--;
            }
            if (_numOfZoomClick == 2)
            {
                if (GraficLibrary.ConvertFromPixelToRealX(x) != 0 && GraficLibrary.ConvertFromPixelToRealY(y) != 0)
                {
                    GraficLibrary.DrawCross(x, y);
                    _x1Zoom = x;
                    _y1Zoom = y;
                }
                else
                    _numOfZoomClick--;
            }
            if (_numOfZoomClick == 3)
            // предыдущий зум не выбран
            {
                GraficLibrary.RemoveFigure(_x0Zoom, _y0Zoom);
                GraficLibrary.RemoveFigure(_x1Zoom, _y1Zoom);
                _numOfZoomClick = 0;
            }

            pictureBox_plot.Image = GraficLibrary.PicPlot;
        }

        void отбраковкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchToCulling();
        }

        void SwitchToMeasure()
        {
            _currResume = "measure";
            this.Text = "График [ " + _currResume + " ] ";
        }

        void SwitchToZoom()
        {
            _currResume = "zoom";
            this.Text = "График [ " + _currResume + " ] ";
        }

        void SwitchToCulling()
        {
            _currResume = "culling";
            this.Text = "График [ " + _currResume + " ] ";
        }

        void зумToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchToZoom();
        }

        //void ViewGroupOfChips(int x0Pix, int y0Pix, int y1Pix)
        //{
        //    List<String> setOfGoodChips = new List<String>();

        //    // делаем так что всегда y1Pix >= y0Pix
        //    if (y1Pix < y0Pix)
        //    {
        //        int copy = y0Pix;
        //        y0Pix = y1Pix;
        //        y1Pix = copy;
        //    }
        //    int x0 = GetCoordXbyPix(x0Pix);
        //    for (int i = 1; i < curveCoordY.Length; i++)
        //    {
        //        if (curveCoordY[i][x0] < y1Pix && curveCoordY[i][x0] > y0Pix)
        //            setOfGoodChips.Add(curveInfo[i].ToArray()[1]);
        //    }

        //    S2PReader.instance.PushGroupOfChipsToInfo(setOfGoodChips);
        //}

        bool IsPointOnSection(double x0, double y0, Point p1, Point p2)
        {
            return ((x0 >= p1.X && x0 <= p2.X) || (x0 >= p2.X && x0 <= p1.X)) &&
                   ((y0 >= p1.Y && y0 <= p2.Y) || (y0 >= p2.Y && y0 <= p1.Y));
        }

        bool IsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double 
                a1 = p1.Y - p2.Y,
                b1 = p2.X - p1.X,
                c1 = -a1 * p1.X - b1 * p1.Y;
            double 
                a2 = p3.Y - p4.Y,
                b2 = p4.X - p3.X,
                c2 = -a2 * p3.X - b2 * p3.Y;
            double
                del = a1 * b2 - a2 * b1;

            bool res;
            if (del == 0)
            // отрезки параллельны
            {
                res = false;
            }
            else
            // отрезки не параллельны
            {
                double x0 = (- c1 * b2 + c2 * b1) / del,
                       y0 = (- a1 * c2 + a2 * c1) / del;
                res = IsPointOnSection(x0, y0, p1, p2) && IsPointOnSection(x0, y0, p3, p4);
            }

            return res;
        }

        bool CurveIsGood(int numOfCurve, Point p1Gate, Point p2Gate)
        {
            // проходим по всем соседним парам точек
            int maxX = Math.Max(p1Gate.X, p2Gate.X),
                minX = Math.Min(p1Gate.X, p2Gate.X),
                maxY = Math.Max(p1Gate.Y, p2Gate.Y),
                minY = Math.Min(p1Gate.Y, p2Gate.Y);

            bool res = false;
            for (int i = 1; i < _curveCoordX[numOfCurve].Length - 1; i++)
            {
                var p1 = new Point(_curveCoordX[numOfCurve][i], _curveCoordY[numOfCurve][i]);
                var p2 = new Point(_curveCoordX[numOfCurve][i + 1], _curveCoordY[numOfCurve][i + 1]);

                if ((Math.Max(p1.X, p2.X) < minX) || (Math.Min(p1.X, p2.X) > maxX) ||
                     (Math.Max(p1.Y, p2.Y) < minY) || (Math.Min(p1.Y, p2.Y) > maxY))
                    continue;

                if (IsIntersect(p1Gate, p2Gate, p1, p2))
                {
                    res = true;
                    break;
                }
            }

            return res;
        }

        void MakeGateCulling(int x0Pix, int y0Pix, int x1Pix, int y1Pix)
        {
            List<string> setOfBadChips = new List<string>();

            Point p1 = new Point(x0Pix, y0Pix),
                  p2 = new Point(x1Pix, y1Pix);

            for (int i = 1; i < _curveCoordY.Length; i++)
            {
                if ( !CurveIsGood(i, p1, p2) )
                    setOfBadChips.Add(_curveInfo[i].ToArray()[1]);
            }

            WaferMeasData.CullingAllWafers(setOfBadChips);
        }

        void MakeInverseGateCulling(int x0Pix, int y0Pix, int x1Pix, int y1Pix)
        {
            List<string> setOfBadChips = new List<string>();

            Point p1 = new Point(x0Pix, y0Pix),
                  p2 = new Point(x1Pix, y1Pix);

            for (int i = 1; i < _curveCoordY.Length; i++)
            {
                if ( CurveIsGood(i, p1, p2) )
                    setOfBadChips.Add(_curveInfo[i].ToArray()[1]);
            }

            WaferMeasData.CullingAllWafers(setOfBadChips);
        }

        void QuadCullingLeftUp(int x0Pix, int y0Pix)
        {
            List<string> setOfBadChips = new List<string>();

            for (int i = 1; i < _curveCoordY.Length; i++)
            // цикл по все кривым
            {
                for (int j = 1; j < _curveCoordX[i].Length; j++)
                    if (_curveCoordX[i][j] <= x0Pix && _curveCoordY[i][j] >= y0Pix)
                    {
                        setOfBadChips.Add(_curveInfo[i].ToArray()[1]);
                        break;
                    }
            }

            WaferMeasData.CullingAllWafers(setOfBadChips);
            if (FormIsOpen("FormWaferMap"))
                FormWaferMap.Instance.RefreshPicture();
        }

        void QuadCullingLeftDown(int x0Pix, int y0Pix)
        {
            List<string> setOfBadChips = new List<string>();

            for (int i = 1; i < _curveCoordY.Length; i++)
            // цикл по все кривым
            {
                for (int j = 1; j < _curveCoordX[i].Length; j++)
                    if (_curveCoordX[i][j] <= x0Pix && _curveCoordY[i][j] <= y0Pix)
                    {
                        setOfBadChips.Add(_curveInfo[i].ToArray()[1]);
                        break;
                    }
            }

            WaferMeasData.CullingAllWafers(setOfBadChips);
        }

        void QuadCullingRightUp(int x0Pix, int y0Pix)
        {
            List<string> setOfBadChips = new List<string>();

            for (int i = 1; i < _curveCoordY.Length; i++)
            // цикл по все кривым
            {
                for (int j = 1; j < _curveCoordX[i].Length; j++)
                    if (_curveCoordX[i][j] >= x0Pix && _curveCoordY[i][j] >= y0Pix)
                    {
                        setOfBadChips.Add(_curveInfo[i].ToArray()[1]);
                        break;
                    }
            }

            WaferMeasData.CullingAllWafers(setOfBadChips);
        }

        void QuadCullingRightDown(int x0Pix, int y0Pix)
        {
            List<string> setOfBadChips = new List<string>();

            for (int i = 1; i < _curveCoordY.Length; i++)
            // цикл по все кривым
            {
                for (int j = 1; j < _curveCoordX[i].Length; j++)
                    if (_curveCoordX[i][j] >= x0Pix && _curveCoordY[i][j] <= y0Pix)
                    {
                        setOfBadChips.Add(_curveInfo[i].ToArray()[1]);
                        break;
                    }
            }

            WaferMeasData.CullingAllWafers(setOfBadChips);
        }

        private void печатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            printDocument1.PrinterSettings.DefaultPageSettings.Landscape = false;
            printDocument1.Print();
        }

        private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            WindowState = FormWindowState.Normal;
            Width = 970;
            Height = 700;

            e.Graphics.DrawImage(GraficLibrary.PicPlot, 0, 0);
        }


        bool FormIsOpen(string name)
        {
            return Application.OpenForms.Cast<Form>().Any(f => f.Name == name);
        }

        int FindXCoord(int numOfCurve, int x0, int y0)
        {

            int res = 0;
            int deltaMin = Int32.MaxValue;
            for (int i = 1; i < _curveCoordX[numOfCurve].Length; i++)
            {
                int delta = 2 * Math.Abs(_curveCoordX[numOfCurve][i] - x0) + Math.Abs(_curveCoordY[numOfCurve][i] - y0);
                if (delta < deltaMin)
                {
                    deltaMin = delta;
                    res = i;
                }
            }

            return res;
        }

        void FindNearestCurve(int x0, int y0, out int numOfCurve, out int numOfPoint)
        {
            numOfCurve = 0;
            numOfPoint = 0;
            int minDist = Int32.MaxValue;
            for (int i = 1; i < _curveCoordY.Length; i++)
            {
                for (int j = 1; j < _curveCoordX[i].Length; j++)
                {
                    int curDist =  Math.Abs(x0 - _curveCoordX[i][j]) + Math.Abs(y0 - _curveCoordY[i][j]);
                    if (curDist < minDist)
                    {
                        minDist = curDist;
                        numOfCurve = i;
                        numOfPoint = j;
                    }
                }
            }
        }

        void ShowSinglePointInfo(MouseEventArgs e)
        {
            int x = e.X, 
                y = GraficLibrary.HPlot - e.Y;
            // находим ближайшую точку
            int numOfCurve, numOfPoint;
            FindNearestCurve(x, y, out numOfCurve, out numOfPoint);
            if (numOfPoint == 0 || numOfCurve == 0)
                return;
            if (_curveCoordY[numOfCurve][numOfPoint] == 0)
                return;

            // установка текущей на данный момент кривой  по которой будет бегать маркер
            _currCurve = numOfCurve;

            PointF point = GraficLibrary.GetRealCoord(numOfCurve, numOfPoint);
            GraficLibrary.DrawTemproryCross(_curveCoordX[numOfCurve][numOfPoint], _curveCoordY[numOfCurve][numOfPoint]);
            if (!FormIsOpen("FormPointInfo"))
            {
                FormPointInfo pi = new FormPointInfo();
                pi.Show();
            }
            FormPointInfo.Instance.Build(point.X, point.Y, _setOfCurves[numOfCurve - 1]);
            pictureBox_plot.Image = GraficLibrary.PicPlot;

        }

        void ShowPointsOfCurveInfo(MouseEventArgs e)
        {
            int numOfCurve = _currCurve,
                x = e.X,
                y = GraficLibrary.HPlot - e.Y;
            int numOfPoint = FindXCoord(numOfCurve, x, y);
            if (_curveCoordX[numOfCurve][numOfPoint] == 0 || _curveCoordY[numOfCurve][numOfPoint] == 0)
                return;

            PointF point = GraficLibrary.GetRealCoord(numOfCurve, numOfPoint);
            GraficLibrary.DrawTemproryCross(_curveCoordX[numOfCurve][numOfPoint], _curveCoordY[numOfCurve][numOfPoint]);

            if (!FormIsOpen("FormPointInfo"))
            {
                FormPointInfo pi = new FormPointInfo();
                pi.Show();
            }
            FormPointInfo.Instance.Build(point.X, point.Y, _setOfCurves[numOfCurve - 1]);
            pictureBox_plot.Image = GraficLibrary.PicPlot;
        }

        void CullingClick(MouseEventArgs e)
        {
            _numOfCullingClick++;

            int x = e.X,
                y = GraficLibrary.HPlot - e.Y;

            switch (_numOfCullingClick)
            {
                case 1:
                    if ( GraficLibrary.ConvertFromPixelToRealX(x) != 0 && GraficLibrary.ConvertFromPixelToRealY(y) != 0 )
                    {
                        GraficLibrary.DrawCross(x, y);
                        _x0PixCull = x;
                        _y0PixCull = y;
                    }
                    else
                        _numOfCullingClick--;
                    break;

                case 2:
                    if (GraficLibrary.ConvertFromPixelToRealY(y) != 0)
                    {
                        GraficLibrary.DrawCross(x, y);
                        _y1PixCull = y;
                        _x1PixCull = x;
                    }
                    else
                        _numOfCullingClick--;
                    break;

                case 3:
                    // удаляем все отметки
                    GraficLibrary.RemoveFigure(_x0PixCull, _y0PixCull);
                    GraficLibrary.RemoveFigure(_x1PixCull, _y1PixCull);
                    _numOfCullingClick = 0;
                    break;
            }

            pictureBox_plot.Image = GraficLibrary.PicPlot;
        }

        void SwitchingByResume(MouseEventArgs e)
        {
            switch (_currResume)
            {
                case "measure":
                    ShowSinglePointInfo(e);
                    break;
                case "culling":
                    CullingClick(e);
                    break;
                case "zoom":
                    ZoomClick(e);
                    break;
            }
        }

        private void pictureBox_plot_MouseClick(object sender, MouseEventArgs e)
        {
            SwitchingByResume(e);
        }

        void DoGateCulling(bool inverse)
        {
            if (_currResume == "culling" && _numOfCullingClick == 2)
            {
                if (!inverse) 
                    MakeGateCulling(_x0PixCull, _y0PixCull, _x1PixCull, _y1PixCull);
                else
                    MakeInverseGateCulling(_x0PixCull, _y0PixCull, _x1PixCull, _y1PixCull);

                if (WaferMeasData.CountOfWafers > 1)
                {
                    FormMain.Instance.SetAllWaferResume();
                    FormMain.Instance.PushMainInfoAllWafer();
                    FormMain.Instance.SetNewCullingFileMulty();
                }
                else
                {
                    FormMain.Instance.SetNewCullingFileSingle();
                    FormMain.Instance.PushMainInfoToForm();
                }

                _numOfCullingClick = 0;
            }
            FormMain.Instance.RedrawWithLimits();
        }

        void DoGroupView()
        {
            if (_currResume == "culling" && _numOfCullingClick == 2)
            {
                //ViewGroupOfChips(x0_pix, y0_pix, y1_pix);
                _numOfCullingClick = 0;
            }
            FormMain.Instance.RedrawWithLimits();
        }

        void DoZoom()
        {
            if (_currResume == "zoom" && _numOfZoomClick == 2)
            {
                int minX = Math.Min(_x0Zoom, _x1Zoom),
                maxX = Math.Max(_x0Zoom, _x1Zoom),
                minY = Math.Min(_y0Zoom, _y1Zoom),
                maxY = Math.Max(_y0Zoom, _y1Zoom);
                FormMain.Instance.LoadToFormLimits(
                    GraficLibrary.ConvertFromPixelToRealX(minX),
                    GraficLibrary.ConvertFromPixelToRealX(maxX),
                    GraficLibrary.ConvertFromPixelToRealY(minY),
                    GraficLibrary.ConvertFromPixelToRealY(maxY));
                _numOfZoomClick = 0;
            }
        }

        private void зумироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoZoom();
        }

        private void pictureBox_plot_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currResume == "measure" && e.Button == MouseButtons.Left && _currCurve != 0)
            {
                ShowPointsOfCurveInfo(e);
            }
        }

        void DrawWithAutoSize()
        {
            FormMain.Instance.UncheckFrozenAxes();
        }

        private void автомасштабToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DrawWithAutoSize();
        }

        private void измерениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchToMeasure();
        }

        private void просмотрГруппыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: DoGroupView();
        }

        private void методGCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoGateCulling(false);
        }

        void DoQuadCulling(int x0, int y0, string quad)
        {
            //TODO: здесь грамотно поставить высоту / ширину битмапа в пикселях
            int inf = 2500;
            if (_currResume == "culling" && _numOfCullingClick == 1)
            {
                switch (quad)
                {
                    case "left-up":
                        QuadCullingLeftUp(x0, y0);
                        MakeInverseGateCulling(x0, y0, x0, inf);
                        MakeInverseGateCulling(x0, y0, 0, y0);
                        break;
                    case "right-up":
                        QuadCullingRightUp(x0, y0);
                        MakeInverseGateCulling(x0, y0, x0, inf);
                        MakeInverseGateCulling(x0, y0, inf, y0);
                        break;
                    case "left-down":
                        QuadCullingLeftDown(x0, y0);
                        MakeInverseGateCulling(x0, y0, x0, 0);
                        MakeInverseGateCulling(x0, y0, 0, y0);
                        break;
                    case "right-down":
                        QuadCullingRightDown(x0, y0);
                        MakeInverseGateCulling(x0, y0, x0, 0);
                        MakeInverseGateCulling(x0, y0, inf, y0);
                        break;
                }

                if (WaferMeasData.CountOfWafers > 1)
                {
                    FormMain.Instance.SetAllWaferResume();
                    FormMain.Instance.PushMainInfoAllWafer();
                    FormMain.Instance.SetNewCullingFileMulty();
                }
                else
                {
                    FormMain.Instance.SetNewCullingFileSingle();
                    FormMain.Instance.PushMainInfoToForm();
                }
                
                _numOfCullingClick = 0;
            }
            FormMain.Instance.RedrawWithLimits();
        }

        private void методIGCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoGateCulling(true);
        }

        private void методQCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoQuadCulling(_x0PixCull, _y0PixCull, "left-up");
        }

        private void методQCleftDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoQuadCulling(_x0PixCull, _y0PixCull, "left-down");
        }

        private void методQCrightUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoQuadCulling(_x0PixCull, _y0PixCull, "right-up");
        }

        private void методQCrightDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoQuadCulling(_x0PixCull, _y0PixCull, "right-down");
        }

        private void отбраковатьToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
