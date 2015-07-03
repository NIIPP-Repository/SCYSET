using System;
using System.Drawing;

namespace MicrowaveApplication
{
    public static class GraficLibrary
    {
        /// <summary>
        /// Ширина области построения графика
        /// </summary>
        public static int WPlot = 1090;

        /// <summary>
        /// Граница окна области построения графика (право)
        /// </summary>
        public static int HPlot = 650;

        /// <summary>
        /// Граница окна области построения графика (право)
        /// </summary>
        private const int Border = 75;

        /// <summary>
        /// Граница окна области построения графика (право)
        /// </summary>
        private const int BorderAtRight = 75;

        // графические переменные для рисования схемы и графика
        public static Bitmap PicPlot;
        private static Bitmap _pureBitmap;
        private static Graphics _gPlot;

        // набор карандашей для рисования разлиных линий на графике
        private static readonly Pen PenForPlot = new Pen(Color.Black, 1);
        private static readonly Pen PenForCross = new Pen(Color.Black, 2);
        private static readonly Pen PenForMesh = new Pen(Color.LightGray, 1);

        // для эффективной перерисовки окна при перемещении крестика на графике
        private static int _changedDotArea;
        private static int _oldX0, _oldY0;

        private static double _lenXReal;
        private static double _lenYReal;
        private static double _startX;
        private static double _startY;

        private static double[][] _masXg;
        private static double[][] _masYg;

        public static string DimOfX = "";
        public static string DimOfY = "";


        /// <summary>
        /// Подготовка грфических переменных для рисования графика
        /// </summary>
        private static void InitGraphPlot()
        {
            PicPlot = new Bitmap(WPlot, HPlot);
            _gPlot = Graphics.FromImage(PicPlot);
            _gPlot.Clear(Color.White);

            // сбрасываем кэш
            _oldX0 = 0;
            _oldY0 = 0;
        }

        /// <summary>
        /// Рисует линии на графике
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static void DrawLinePlot(int x0, int y0, int x, int y)
        {
            _gPlot.DrawLine(PenForPlot, new Point(x0, HPlot - y0), new Point(x, HPlot - y));
        }

        /// <summary>
        /// рисует линии сетки на графике
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private static void DrawLineMesh(int x0, int y0, int x, int y)
        {
            _gPlot.DrawLine(PenForMesh, new Point(x0, HPlot - y0), new Point(x, HPlot - y));
        }

        /// <summary>
        /// Выводит текст на график с заданным размером шрифта
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="size"></param>
        /// <param name="text"></param>
        private static void PrintTextPlot(int x, int y, float size, String text)
        {
            _gPlot.DrawString(text, new Font("Arial", size), Brushes.Black, new PointF(x, HPlot - y));
        }

        private static void DrawLineCurveS2P(int x0, int y0, int x, int y, Pen nowPen)
        {
            _gPlot.DrawLine(nowPen, new Point(x0, HPlot - y0), new Point(x, HPlot - y));
        }

        public static double ConvertFromPixelToRealX(int pixCountX)
        {
            double frac = (double)(pixCountX - Border) / (WPlot - 2*Border);
            if (frac >= 0 && frac <= 1)
                return _startX + frac * _lenXReal;
            else
                return 0;
        }

        public static double ConvertFromPixelToRealY(int pixCountY)
        {
            double frac = (double)(pixCountY - Border) / (HPlot - 2*Border);
            if (frac >= 0 && frac <= 1)
                return _startY + frac * _lenYReal;
            else
                return 0;
        }

        static Color SwitchColor(double a, double b, double c)
        {
            Color res = Color.Black;
            int x = CalcHash(a, b, c);
            switch (x % 13)
            {
                case 0:
                    // темено-синий
                    res = Color.FromArgb(20, 68, 194);
                    break;
                case 1:
                    // светло-зеленый
                    res = Color.FromArgb(56, 241, 66);
                    break;
                case 2:
                    // коричневый
                    res = Color.FromArgb(217, 108, 0);
                    break;
                case 3:
                    // темно-синий
                    res = Color.FromArgb(96, 43, 234);
                    break;
                case 4:
                    // светло-зеленый
                    res = Color.FromArgb(100, 200, 10);
                    break;
                case 5:
                    // темно-голубой
                    res = Color.FromArgb(28, 156, 159);
                    break;
                case 6:
                    // темно-фиолетовый
                    res = Color.FromArgb(134, 91, 142);
                    break;
                case 7:
                    // зеленый
                    res = Color.FromArgb(122, 197, 73);
                    break;
                case 8:
                    // светло-синий
                    res = Color.FromArgb(86, 175, 235);
                    break;
                case 9:
                    // темно-зеленый
                    res = Color.FromArgb(10, 135, 16);
                    break;
                case 10:
                    // темно-коричневый
                    res = Color.FromArgb(100, 113, 6);
                    break;
                case 11:
                    // фиолетовый
                    res = Color.FromArgb(170, 0, 170);
                    break;
                case 12:
                    // зеленый
                    res = Color.FromArgb(10, 165, 40);
                    break;
            }

            return res;
        }

        private static void FindMinMax(double[][] masX, double[][] masY, int countOfCurves, out double minX, out double maxX, out double minY, out double maxY)
        {
            try
            {
                minX = masX[1][1];
                maxX = masX[1][1];
                minY = masY[1][1];
                maxY = masY[1][1];

                for (int i = 1; i <= countOfCurves; i++)
                    for (int j = 1; j <= masX[i][0]; j++)
                    {
                        if (masY[i][j] > maxY)
                            maxY = masY[i][j];
                        if (masY[i][j] < minY)
                            minY = masY[i][j];
                        if (masX[i][j] > maxX)
                            maxX = masX[i][j];
                        if (masX[i][j] < minX)
                            minX = masX[i][j];
                    }
            }
            catch
            {
                minX = -1;
                maxX = 1;
                minY = -1;
                maxY = 1;
            }
        }

        private static double ChooseLimit(double? num1, double num2)
        {
            if (num1 == null)
                return num2;
            return (double) num1;
        }

        public static void DrawSetOfCurves(double[][] masX, double[][] masY, int countOfCurves, string nameOfXAxes, string nameOfYAxes, String header, double? minX, double? maxX, double? minY, double? maxY, out Bitmap pictureWithGraph, out int[][] masPixX, out int[][] masPixY)
        {
            // ищем минимум и максимум в массивах
            double minXAuto, maxXAuto, minYAuto, maxYAuto;
            FindMinMax(masX, masY, countOfCurves, out minXAuto, out maxXAuto, out minYAuto, out maxYAuto);

            // инициализация Bitmap и Grafics
            InitGraphPlot();

            // расчет наиболее подходящей сетки
            OutFromIntelMeshing outClass = IntelligentMeshing(
                ChooseLimit(minX, minXAuto),
                ChooseLimit(maxX, maxXAuto),
                ChooseLimit(minY, minYAuto),
                ChooseLimit(maxY, maxYAuto));

            // прорисовка сетки и надписей
            DrawMesh(outClass);

            // прорисовка всех кривых на созданной структуре
            Tuple<int[][], int[][]> tupleDrawSetOfCurves = DrawSetOfCurves(masX, masY, countOfCurves, outClass);
            masPixX = tupleDrawSetOfCurves.Item1;
            masPixY = tupleDrawSetOfCurves.Item2;

            // название оси Y
            PrintTextPlot(60, HPlot - 30, 12, nameOfYAxes);
            DimOfY = nameOfYAxes.Substring(nameOfYAxes.IndexOf(',') + 2);

            // название оси X
            PrintTextPlot(Border + 2 * WPlot / 5, Border / 2, 12, nameOfXAxes);
            DimOfX = nameOfXAxes.Substring(nameOfXAxes.IndexOf(',') + 2);

            // заголовок графика
            PrintTextPlot(WPlot / 3, HPlot - 20, 12, header);

            // сохраняем оригинал изображения с графиками
            _pureBitmap = (Bitmap) PicPlot.Clone();

            pictureWithGraph = PicPlot;
        }

        public static PointF GetRealCoord(int numOfCurve, int numOfPoint)
        {
            PointF res = new PointF
            {
                X = (float) (_masXg[numOfCurve][numOfPoint]),
                Y = (float) (_masYg[numOfCurve][numOfPoint])
            };
            return res;
        }

        private static int CalcHash(double a, double b, double c)
        {
			a = 1e-14 + Math.Abs(a);
			b = 1e-14 + Math.Abs(b);
			c = 1e-14 + Math.Abs(c);
            while (a < 100)
                a *= 11;
            while (b < 100)
                b *= 11;
            while (c < 100)
                c *= 11;

            double res = ((a * 7) + b) * 17 + c;
            return (int) res;
        }

        private static Tuple<int[][], int[][]> DrawSetOfCurves(double[][] masX, double[][] masY, int countOfCurves, OutFromIntelMeshing inClass)
        {
            int[][] curveCoordY = new int[countOfCurves + 1][];
            int[][] curveCoordX = new int[countOfCurves + 1][];
            int lenBetweenYLines = (HPlot - 2 * Border) / inClass.NumOfYLines;
            int lenBetweenXLines = (WPlot - Border - BorderAtRight) / inClass.NumOfXLines;

            // сохраняем информацию, чтобы выполнять запросы (координата - реальная координата)
            _masXg = masX;
            _masYg = masY;

            for (int n = 1; n <= countOfCurves; n++)
            {
                int countOfPoints = (int) masX[n][0];
                curveCoordY[n] = new int[countOfPoints + 1];
                curveCoordX[n] = new int[countOfPoints + 1];
                var x = new int[countOfPoints + 1];
                var y = new int[countOfPoints + 1];

                // выбираем цвет кривой
                var pen = countOfPoints >= 3 ? new Pen(SwitchColor(masY[n][1], masY[n][countOfPoints / 2], masY[n][countOfPoints]), 1) : new Pen(SwitchColor(n, n, n), 1);

                for (int i = 1; i <= countOfPoints; i++)
                {
                    double fracX = (masX[n][i] - inClass.StartX) / inClass.LenX,
                           fracY = (masY[n][i] - inClass.StartY) / inClass.LenY;
                    if (fracX > 1 || fracX < 0 || fracY > 1 || fracY < 0)
                        continue;

                    x[i] = (int)(Border + fracX * (inClass.NumOfXLines * lenBetweenXLines));
                    y[i] = (int)(Border + fracY * (inClass.NumOfYLines * lenBetweenYLines));
                    if (y[i - 1] != 0)
                        DrawLineCurveS2P(x[i], y[i], x[i - 1], y[i - 1], pen);
                    // сохраняем координаты точек, где рисовали
                    curveCoordY[n][i] = y[i];
                    curveCoordX[n][i] = x[i];
                }
            }

            return Tuple.Create(curveCoordX, curveCoordY);
        }

        /// <summary>
        /// Получаем хороший шаг для сетки графика (округленная дробь)
        /// </summary>
        /// <param name="step">Необработанный шаг</param>
        /// <returns>Хороший шаг</returns>
        private static double GetGoodStep(double step)
        {
            step = TruncOfDouble(step);
            double res = 0;
            int pow = 0;
            // приводим к виду step*10^pow, где 0 < step < 10
            while (step >= 10) 
            {
                pow++;
                step = step / 10;
            }
            while (step < 1)
            {
                pow--;
                step = step * 10;
            }

            int integer = (int) step;
            double frac = step - integer;

            if (integer <= 2)
            {
                if (frac == 0)
                    res = integer;
                if (frac <= 0.5 && frac > 0)
                    res = integer + 0.5;
                if (frac > 0.5)
                    res = integer + 1;
            }

            if (integer == 3)
            {
                if (frac == 0)
                    res = integer;
                else
                    res = integer + 1;
            }

            if (integer == 4)
            {
                if (frac > 0)
                    res = integer + 1;
                else
                    res = integer;
            }

            if (integer >= 5)
            {
                if (frac == 0 && integer == 5)
                    res = integer;
                else
                    res = 10;
            }

            if (pow < 0)
            {
                while (pow < 0)
                {
                    pow++;
                    res = res / 10;
                }
            }
            if (pow > 0)
            {
                while (pow > 0)
                {
                    pow--;
                    res = res * 10;
                }
            }

            return res;
        }

        public static string DoubleToStr(double x)
        {
            bool neg = false;
            if (x < 0)
            {
                neg = true;
                x = -x;
            }

            if ((x < 0.01 || x > 1000) && x != 0)
                return neg ? (-x).ToString("#.#E+0") : x.ToString("#.#E+0");
            else
            {
                x = Math.Floor(x * 1000 + 0.5) / 1000;
                return (neg ? -x : x).ToString();
            }
        }

        /// <summary>
        /// Возвращает число без дроби (коэффициент перед 10 в степени x)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        static double GetCeilMark(double x)
        {
            double res = x,
                   mark = 0;
            while (res > 10)
                res /= 10;
            while (res < 1)
                res *= 10;

            if (res == 1)
                mark = 10;
            if (res == 5)
                mark = 7;
            if (res == 2 || res == 4 || res == 3)
                mark = 5;
            if (res == 1.5 || res == 2.5)
                mark = 3;
            return mark;
        }

        private class OutFromIntelMeshing
        {
            public double
               StepX = 0,
               StartX = 0,
               StepY = 0,
               StartY = 0,
               LenX = 0,
               LenY = 0;
            public int
                NumOfXLines = 0,
                NumOfYLines = 0;
        }

        static OutFromIntelMeshing IntelligentMeshing(double minX, double maxX, double minY, double maxY)
        {
            double
                bestXRating = 0,
                bestStepX = 0,
                bestStartX = 0,
                bestYRating = 0,
                bestStepY = 0,
                bestStartY = 0;
            int
                bestNumOfXLines = 0,
                bestNumOfYLines = 0;

            minX = TruncOfDouble(minX);
            maxX = TruncOfDouble(maxX);
            minY = TruncOfDouble(minY);
            maxY = TruncOfDouble(maxY);

            for (int n = 5; n <= 14; n++)
            {
                double stepX = (maxX - minX) / n; // ориентировочный дробный шаг
                stepX = GetGoodStep(stepX); // теперь шаги более менее целые с функцией getStep
                double sttX = stepX * Math.Floor(minX / stepX); // новое начальное значение
                if (sttX + n * stepX >= maxX)
                {
                    double curRating = 1; // рейтинговая система оценки "хорошего" подбора сетки графика
                    curRating += 1000 * (1 - (Math.Abs(sttX + n * stepX - maxX) / (n * stepX) ));
                    curRating += 1000 * (1 - (Math.Abs(minX - sttX) / (n * stepX) ));
                    curRating += 5.5 - Math.Abs(n - 10);
                    curRating += 2 * GetCeilMark(stepX); // насколько хорош шаг

                    if (curRating > bestXRating)
                    {
                        bestXRating = curRating;
                        bestStartX = sttX;
                        bestStepX = stepX;
                        bestNumOfXLines = n;
                    }
                }
            }

            for (int n = 5; n <= 14; n++)
            {
                double stepY = (maxY - minY) / n; // ориентирвочный дробный шаг
                stepY = GetGoodStep(stepY); // теперь шаги более менее целые с функцией getStep
                double sttY = stepY * Math.Floor(minY / stepY); // новое начальное значение
                if (sttY + n * stepY >= maxY)
                {
                    double curRating = 1; // рейтинговая система оценки "хорошего" подбора сетки графика
                    curRating += 1000 * (1 - (Math.Abs(sttY + n * stepY - maxY) / (n * stepY)) );
                    curRating += 1000 * (1 - (Math.Abs(minY - sttY) / (n * stepY)) );

                    curRating += 5.5 - Math.Abs(n - 10);
                    curRating += 2 * GetCeilMark(stepY); // насколько хорош шаг

                    if (curRating > bestYRating)
                    {
                        bestYRating = curRating;
                        bestStartY = sttY;
                        bestStepY = stepY;
                        bestNumOfYLines = n;
                    }
                }
            }
            
            // сохраняем полученные результаты в класс и отдаем
            OutFromIntelMeshing outClass = new OutFromIntelMeshing();
            if (bestNumOfXLines != 0)
            {
                outClass.StartX = bestStartX;
                outClass.LenX = bestStepX * bestNumOfXLines;
                outClass.NumOfXLines = bestNumOfXLines;
                outClass.StepX = bestStepX;
                // сохраняем глобальные переменные для обработки запросов координат
                _lenXReal = outClass.LenX;
                _startX = outClass.StartX;
            }

            if (bestNumOfYLines != 0)
            {
                outClass.StartY = bestStartY;
                outClass.LenY = bestStepY * bestNumOfYLines;
                outClass.NumOfYLines = bestNumOfYLines;
                outClass.StepY = bestStepY;
                // сохраняем глобальные переменные для обработки запросов координат
                _lenYReal = outClass.LenY;
                _startY = outClass.StartY;
            }

            return outClass;
        }

        static double TruncOfDouble(double x)
        {
            bool plus = (x >= 0);
            const int r = 1000000000;

            double res = Math.Floor((Math.Abs(x) * r + 0.5)) / r;
            if (!plus)
                return -res;
            return res;
        }

        public static string ToStr(double x)
        {
            return String.Format(Math.Abs(x) < 1e-2 ? "{0:E}" : "{0:0.######}", x);
        }

        private static void DrawMesh(OutFromIntelMeshing inClass)
        {
            // подгонка: растягиваем либо сужаем в зависимости от размера окна
            int lenBetweenYLines = (HPlot - 2 * Border) / inClass.NumOfYLines;
            int lenBetweenXLines = (WPlot - Border - BorderAtRight) / inClass.NumOfXLines;
            for (int i = 0; i <= inClass.NumOfXLines; i++) // вертикальные линии
            {
                PrintTextPlot(Border + i * lenBetweenXLines - 10, Border - 10, 10, DoubleToStr(inClass.StartX + i * inClass.StepX));
                if (i > 0 && i < inClass.NumOfXLines)
                    DrawLineMesh(Border + i * lenBetweenXLines, Border, Border + i * lenBetweenXLines, Border + inClass.NumOfYLines * lenBetweenYLines);
                else
                    DrawLinePlot(Border + i * lenBetweenXLines, Border, Border + i * lenBetweenXLines, Border + inClass.NumOfYLines * lenBetweenYLines);
            }

            for (int i = 0; i <= inClass.NumOfYLines; i++) // горионтальные линии
            {
                PrintTextPlot(Border - 45, Border + i * lenBetweenYLines + 10, 10, DoubleToStr(inClass.StartY + i * inClass.StepY));
                if (i > 0 && i < inClass.NumOfYLines)
                    DrawLineMesh(Border, Border + i * lenBetweenYLines, Border + inClass.NumOfXLines * lenBetweenXLines, Border + i * lenBetweenYLines);
                else
                    DrawLinePlot(Border, Border + i * lenBetweenYLines, Border + inClass.NumOfXLines * lenBetweenXLines, Border + i * lenBetweenYLines);
            }
        }

        public static void RemoveFigure(int x0, int y0)
        {
            y0 = HPlot - y0;
            for (int x = x0 - _changedDotArea; x <= x0 + _changedDotArea; x++)
                for (int y = y0 - _changedDotArea; y <= y0 + _changedDotArea; y++)
                    if (x >= 0 && y >= 0 && x < WPlot && y < HPlot)
                        PicPlot.SetPixel(x, y, _pureBitmap.GetPixel(x, y));
        }

        public static void RemoveTemproryCross()
        {
            if (_oldX0 != 0 || _oldY0 != 0)
            {
                RemoveFigure(_oldX0, _oldY0);
                _oldX0 = 0;
                _oldY0 = 0;
            }
        }

        public static void DrawTemproryCross(int x0, int y0)
        // cтавим крестик, удаляя предыдущий
        {
            RemoveTemproryCross();
            DrawCross(x0, y0);
            _oldX0 = x0;
            _oldY0 = y0;
        }

        /// <summary>
        /// Ставим крестик маркера на кривой
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        public static void DrawCross(int x0, int y0)
        {
            int d = HPlot / 80;
            y0 = HPlot - y0;
            _gPlot.DrawLine(new Pen(Color.White, 6), new Point(x0 - d - 2, y0), new Point(x0 + d + 2, y0));
            _gPlot.DrawLine(new Pen(Color.White, 6), new Point(x0, y0 - d - 2), new Point(x0, y0 + d + 2));
            _gPlot.DrawLine(PenForCross, new Point(x0 - d, y0), new Point(x0 + d, y0));
            _gPlot.DrawLine(PenForCross, new Point(x0, y0 - d), new Point(x0, y0 + d));
            _changedDotArea = d + 5;
        }

    }
}
