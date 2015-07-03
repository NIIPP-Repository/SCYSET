using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NIIPP.WaferMeasData
{
    /// <summary>
    /// Класс для работы с измеренными электрофизическими параметрами пластины
    /// </summary>
    public class WaferMeasData
    {
        /// <summary>
        /// структура хранит пары: название пластины (выбирается по имени папки) - соответствующий объект пластины
        /// в данной структуре хранятся объекты всех пластин
        /// </summary>
        public static readonly Dictionary<string, WaferMeasData> MasOfWaferData = new Dictionary<string, WaferMeasData>();

        /// <summary>
        /// Количество загруженных пластин
        /// </summary>
        public static int CountOfWafers
        {
            get { return MasOfWaferData.Count; }
        }

        /// <summary>
        /// Тип кривых (.s1p .s2p, .s3p, .s4p или .txt) которые содержат все чипы всех пластин
        /// </summary>
        public static string TypeOfCurves { get; private set; }

        /// <summary>
        /// Абсолютный путь к текущему файлу отбраковки
        /// </summary>
        private string _currentCullingFile = "none";

        /// <summary>
        /// Абсолютный путь к текущему файлу - шаблону для файла отбраковки
        /// </summary>
        private string _currentWaferMapTemplateFile = "none";

        /// <summary>
        /// Название папки, в которой находятся экспериментальные данные пластины
        /// </summary>
        private string NameOfWaferFolder { get; set; }

        /// <summary>
        /// Текущий параметр, по которому произведена калибровка
        /// </summary>
        private string CurrentCalibrParameter { get; set; }

        /// <summary>
        /// Структура хранит пары: названия чипа (выбирается по названию файла *.s2p - объект соответствующего чипа)
        /// </summary>
        private readonly Dictionary<string, ChipData> _setOfChips = new Dictionary<string, ChipData>();

        /// <summary>
        /// Структура хранит пары: названия параметров измерения чипов - уникальный идентификатор этого параметра
        /// </summary>
        private readonly Dictionary<string, int> _setOfParameters = new Dictionary<string, int>();

        /// <summary>
        /// Количество измеренных параметров чипа данной пластины
        /// </summary>
        public int CountOfParameters
        {
            get { return _setOfParameters.Count; }
        }

        /// <summary>
        /// Лист хранит массив абсолютных путей к файлам отбраковки (файлы где хранится информация какой чип годный, а какой - нет)
        /// </summary>
        private readonly List<string> _setOfCullingFiles = new List<string>();

        /// <summary>
        /// Лист хранит массив абсолютных путей к файлам - шаблонам для файлов отбраковки (Алексей Безрук знает где их найти)
        /// </summary>
        private readonly List<string> _setOfCullingFilesTemplate = new List<string>();

        /// <summary>
        /// Массив с картой раскроя, где помечены статусы чипов (годный, негодный, тестовый и т. д.)
        /// </summary>
        private int[,] _waferMap;

        // минимумы и максимумы координат X и Y чипов
        private int _mapMinX, _mapMaxX, _mapMinY, _mapMaxY;

        /// <summary>
        /// Количество файлов в папке этой (this) пластины
        /// </summary>
        private int _countOfFiles;

        /// <summary>
        /// Количество негодных чипов пластины
        /// </summary>
        public int CountOfCulling;

        /// <summary>
        /// Количество годных чипов
        /// </summary>
        public int CountOfUnculling;

        /// <summary>
        /// Количество параметров по которым измерялись чипы (вытаскивается из названия чипа)
        /// </summary>
        private int CountOfChipParameters { get; set; }

        /// <summary>
        ///  Процент выхода годных
        /// </summary>
        public double ProcentOfOut { get; private set; }

        /// <summary>
        /// Путь к папке в которой лежат измерения
        /// </summary>
        private string _pathToFolder;

        // контролы для вывода прогресса загрузки на форму
        private readonly RichTextBox _rtbOut;
        private readonly ProgressBar _pbOut;
        public int CurrentXAxis,
                    CurrentYAxis;

        private string[] _nameOfFiles;

        /// <summary>
        /// Ошибки, предупреждения и так далее
        /// </summary>
        public List<string> Messages = new List<string>();

        /// <summary>
        /// Существуют ли файлы названия которых не были распарсены (несоответствие формату названий)
        /// </summary>
        public bool UnparsedFileNamesExist { get; private set; }


        /// <summary>
        /// Конструктор, загружает экспериментальные данные пластины
        /// </summary>
        /// <param name="pathToFolder">Пути к файлам с измерениями</param>
        public WaferMeasData(string[] pathToFolder)
        {
            LoadWaferData(pathToFolder);
        }

        /// <summary>
        /// Конструктор, загружает экспериментальные данные пластины
        /// </summary>
        /// <param name="pathToFiles">Пути к файлам с измерениями</param>
        /// <param name="rtbOut">richTextBox, в который будет записываться текущий загружаемый файл</param>
        /// <param name="pbOut">progressBar, отражающий прогресс загрузки файлов</param>
        public WaferMeasData(string[] pathToFiles, ref RichTextBox rtbOut, ref ProgressBar pbOut)
        {
            _rtbOut = rtbOut;
            _pbOut = pbOut;
            LoadWaferData(pathToFiles);
        }

        /// <summary>
        /// Внутренний метод, загружающий экспериментальные данные
        /// </summary>
        /// <param name="pathes">Пути к файлам с данными</param>
        private void LoadWaferData(string[] pathes)
        {
            CurrentCalibrParameter = "none";

            _pathToFolder = (new FileInfo(pathes[0])).DirectoryName;
            NameOfWaferFolder = Path.GetFileName(_pathToFolder);
            MasOfWaferData.Add(NameOfWaferFolder, this);

            LoadFolder(pathes);

            CountOfUnculling = CountOfChips;
            CountOfCulling = 0;

            CurrentXAxis = 0;
            CurrentYAxis = 1;
        }

        /// <summary>
        /// Метод удаляет экспериментальные данные всех пластин
        /// </summary>
        public static void ClearAllWafers()
        {
            MasOfWaferData.Clear();
        }

        /// <summary>
        /// Метод отбраковывает указанные чипы с любых пластин
        /// </summary>
        /// <param name="setOfBadChips">Лист, в котором хранятся негодные чипы, при этом чипы могут принадлежать любой пластине</param>
        public static void CullingAllWafers(List<string> setOfBadChips)
        {
            foreach (string nameOfChip in setOfBadChips)
            {
                string waferPossession = nameOfChip.Substring(nameOfChip.IndexOf(" in ") + 4), // получаем имя пластины
                    chipName = nameOfChip.Substring(0, nameOfChip.IndexOf(" in "));  // получаем имя чипа
                MasOfWaferData[waferPossession].CullingOfChipByBad(chipName);
            }
        }

        /// <summary>
        /// Метод обрабатывает фазы измеренных параметров данной пластины (устраняет разрывы фаз)
        /// </summary>
        public void ProcessingOfWaferPhase()
        {
            if (TypeOfCurves == ".txt") return;

            foreach (ChipData cd in _setOfChips.Values)
            {
                cd.ProcessingOfChipPhase();
            }
        }

        /// <summary>
        /// Возвращает массив названия кривых для выбранного параметра
        /// </summary>
        /// <param name="nameOfParameter">Запрашиваемый параметр</param>
        /// <returns>Массив названий кривых</returns>
        public string[] GetAxisesOfParameter(string nameOfParameter)
        {
            List<string> res = new List<string>();

            foreach (ChipData cd in _setOfChips.Values)
            {
                res.AddRange(cd.GetAxisesOfParameter(nameOfParameter));
                break;
            }

            return res.ToArray();
        }

        /// <summary>
        /// Возвращает массив размерностей осей кривых для выбранного параметра
        /// </summary>
        /// <param name="nameOfParameter">Запрашиваемый параметр</param>
        /// <returns>Массив размерностей кривых</returns>
        public string[] GetDimensionsOfParameter(string nameOfParameter)
        {
            List<string> res = new List<string>();

            foreach (ChipData cd in _setOfChips.Values)
            {
                res.AddRange(cd.GetDimensionsOfParameter(nameOfParameter));
                break;
            }

            return res.ToArray();
        }

        /// <summary>
        /// Метод опускает фазы измеренных параметров данной пластины на 360 градусов, если начальная точка положительна
        /// </summary>
        private void EliminatePositiveStartWaferPhase()
        {
            foreach (ChipData cd in _setOfChips.Values)
            {
                cd.EliminatePositiveStartChipPhase();
            }
        }

        /// <summary>
        /// Метод запускает диагностику на целостность экспериментальных данных (параметров чипов) пластины
        /// </summary>
        /// <returns> На выходе лист, содержащий собщения о найденных отсутствующих параметрах</returns>
        public List<string> DiagnosticOfWaferData()
        {
            List<string> res = new List<string>();

            foreach (ChipData cd in _setOfChips.Values)
            {
                foreach (string index in _setOfParameters.Keys)
                {
                    try
                    {
                        cd.GetMasOfCurve(_setOfParameters[index], 0);
                    }
                    catch
                    {
                        res.Add(String.Format("{0}) У чипа '{1}' отсутствует параметр '{2}' !", res.Count + 1, cd.NameOfChip, index));
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Метод сбрасывает текущую отбраковку для всех пластин, то есть все чипы всех пластин становятся годными
        /// </summary>
        public static void ResetCullingForAll()
        {
            foreach (WaferMeasData wd in MasOfWaferData.Values)
            {
                wd.ResetCulling();
            }
        }

        /// <summary>
        /// Метод возвращает строку, отображающий параметр, по коду этого параметра. Например 1 - |S11|, 2 - alpha(S11), 3 - |S21|, и т. д.
        /// </summary>
        /// <param name="index">Код параметра</param>
        /// <returns>Строка, представляющая параметр</returns>
        private static string GetStringByIndexOfSandA(int index)
        {
            if (TypeOfCurves == ".s2p")
                return S2PConfig.AxisesOfCurves[index];

            return S3PConfig.AxisesOfCurves[index];
        }

        /// <summary>
        /// Метод, устанавливающий калибровочный параметр для всех пластин
        /// </summary>
        /// <param name="calibrPar">Название калибровочного параметра</param>
        public static void SetCalibrationParameter(string calibrPar)
        {
            foreach (WaferMeasData wd in MasOfWaferData.Values)
            {
                if (wd.CurrentCalibrParameter != calibrPar)
                {
                    wd.CalibrateAllChips(calibrPar);
                    wd.CurrentCalibrParameter = calibrPar;
                }
            }
        }

        /// <summary>
        /// Метод, отменяющий калибровку для всех пластин
        /// </summary>
        public static void CancelCalibration()
        {
            foreach (WaferMeasData wd in MasOfWaferData.Values)
                wd.UncalibrateAllChips();
        }

        /// <summary>
        /// Метод обрабатывает фазы измеренных параметров всех пластин (устраняет разрывы фаз)
        /// </summary>
        public static void ProcessingOfAllWafersPhase()
        {
            foreach (WaferMeasData wd in MasOfWaferData.Values)
            {
                wd.ProcessingOfWaferPhase();
            }
        }

        /// <summary>
        /// Суммарное количество чипов со всех загруженных пластин
        /// </summary>
        public static int CountOfChipsFromAllWafers
        {
            get { return MasOfWaferData.Values.Sum(wd => wd.CountOfChips); }
        }

        /// <summary>
        /// Суммарное количество отбракованных чипов со всех пластин
        /// </summary>
        public static int CountOfCulledChipsFromAllWafers
        {
            get { return MasOfWaferData.Values.Sum(wd => wd.CountOfCulling); }
        }

        /// <summary>
        /// Суммарное количество годных чипов со всех пластин
        /// </summary>
        public static int CountOfGoodChipsFromAllWafers
        {
            get { return MasOfWaferData.Values.Sum(wd => wd.CountOfUnculling); }
        }

        /// <summary>
        /// Процент выхода годных чипов для всех пластин
        /// </summary>
        public static string ProcentOfGoodFromAllWafers
        {
            get
            {
                int count = CountOfChipsFromAllWafers,
                good = CountOfGoodChipsFromAllWafers;
                string res = ( (double) good / count).ToString("0.0 %");
                return res; 
            }
        }

        /// <summary>
        /// Метод опускает фазы измеренных параметров всех пластин на 360 градусов, если начальная точка положительна
        /// </summary>
        public static void EliminatePositiveStartPhase()
        {
            foreach (WaferMeasData wd in MasOfWaferData.Values)
            {
                wd.EliminatePositiveStartWaferPhase();
            }
        }

        /// <summary>
        /// Метод формирует массив точек выбранных кривых и информацию о кривых для последующей отрисовки
        /// </summary>
        /// <param name="indexOfCurves">Лист, содержащий набор идентификаторов интересующих параметров. Например идентификаторы {1, 2, 4} соответствуют {|S11|, alpha(S11), alpha(S21)}</param>
        /// <param name="indexOfX">Индекс кривой точек оси X</param>
        /// <param name="listOfParameters">Массив строк, содержащий названия интересующих параметров</param>
        /// <param name="wafers">Массив строк, содержащий названия интересующих пластин (названия пластин == названия папок с экспериментальными данными)</param>
        /// <param name="chips">Массив строк, содержащий названия интересующих чипов</param>
        /// <param name="masX">Двумерный массив в котором будут записаны Х координаты точек кривых, первое измерение - порядковый номер кривой, второе измерение - порядковый номер точки, при этом masX[N][0] - содержит количество точек кривой с номером N</param>
        /// <param name="masY">Двумерный массив в котором будут записаны Y координаты точек кривых, первое измерение - порядковый номер кривой, второе измерение - порядковый номер точки</param>
        /// <param name="curveInfo">Лист строк, в которых будет записана информация о выбранных кривых (curveInfo[N][0] - название пластины, curveInfo[N][1] - название чипа, curveInfo[N][2] - название параметра, curveInfo[N][3] - название S параметра</param>
        /// <param name="countOfCurves">Переменная в которую будет записано количество возвращенных кривых</param>
        public static void ReturnDefinedCurves(List<int> indexOfCurves, int indexOfX, string[] listOfParameters, string[] wafers, string[] chips, out double[][] masX, out double[][] masY, out List<string>[] curveInfo, out int countOfCurves)
        {
            // рассчитываем сколько кривых максимально может быть
            int maxCountOfCurves = 1 + wafers.Sum(nextWafer => MasOfWaferData[nextWafer].CountOfUnculling * MasOfWaferData[nextWafer].CountOfChipParameters * indexOfCurves.Count);

            masX = new double[maxCountOfCurves][];
            masY = new double[maxCountOfCurves][];
            curveInfo = new List<string>[maxCountOfCurves];
            countOfCurves = 0;

            foreach (string wafer in wafers)
            {
                WaferMeasData wd = MasOfWaferData[wafer];
                foreach (string chip in chips)
                {
                    // если этот чип не принадлежит этой пластине то пропускаем
                    string waferPossession = chip.Substring(chip.IndexOf(" in ") + 4);
                    if (waferPossession != wd.NameOfWaferFolder)
                        continue;
                    // получаем имя чипа
                    string chipName = chip.Substring(0, chip.IndexOf(" in "));
                    ChipData cd = wd._setOfChips[chipName];

                    // если отбраковован, то пропускаем
                    if (cd.IsCulling)
                        continue;
                    foreach (string currPar in listOfParameters)
                    {
                        int indexOfParameter = wd._setOfParameters[currPar];
                        foreach (int index in indexOfCurves.ToArray())
                        {
                            // кривой может не быть
                            double[] x;
                            double[] y;
                            try
                            {
                                x = cd.GetMasOfCurve(indexOfParameter, indexOfX);
                                y = cd.GetMasOfCurve(indexOfParameter, index);
                            }
                            catch
                            {
                                continue;
                            }
                            
                            countOfCurves++;
                            masX[countOfCurves] = x;
                            masY[countOfCurves] = y;
                            masX[countOfCurves][0] = cd.GetCountOfPoints(indexOfParameter);

                            curveInfo[countOfCurves] = new List<string>
                            {
                                wd.NameOfWafer,
                                String.Format("{0} in {1}", cd.NameOfChip, waferPossession),
                                currPar,
                                Utils.IsSXPFormat(TypeOfCurves) ? GetStringByIndexOfSandA(index) : ""
                            };

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Метод производит отбраковку чипов пластины с помощью массива с негодными чипами
        /// </summary>
        /// <param name="namesOfBadChips">Лист, содержащий названия чипов, которые необходимо отбраковать</param>
        public void CullingOfChipsByBad(List<string> namesOfBadChips)
        {
            foreach (string str in namesOfBadChips)
            {
                ChipData cd = _setOfChips[str];
                if (!cd.IsCulling)
                {
                    cd.IsCulling = true;
                    CountOfCulling++;
                    CountOfUnculling--;
                }
            }
        }
        
        /// <summary>
        /// Метод производит отбраковку одного чипа пластины
        /// </summary>
        /// <param name="nameOfChip">Название чипа, которого необходимо отбраковать</param>
        private void CullingOfChipByBad(string nameOfChip)
        {
            ChipData cd = _setOfChips[nameOfChip];
            if (!cd.IsCulling)
            {
                cd.IsCulling = true;
                CountOfCulling++;
                CountOfUnculling--;
            }
        }

        /// <summary>
        /// Название пластины (выбирается по названию папки в которой лежат экспериментальные данные)
        /// </summary>
        public string NameOfWafer
        {
            get { return NameOfWaferFolder; }
        }

        /// <summary>
        /// Полный путь к папке с эспериментальными данными пластины
        /// </summary>
        public string PathToFolder
        {
            get { return _pathToFolder; }
        }

        /// <summary>
        /// Массив в котором содержаться названия файлов - возможных файлов-шаблонов отбраковки (карты раскроя) в формате 'myfile.ext'
        /// </summary>
        public string[] PotentialTemplateFiles
        {
            get
            {
                string[] res = new string[_setOfCullingFilesTemplate.Count];
                int i = 0;
                foreach (string str in _setOfCullingFilesTemplate)
                {
                    res[i] = Path.GetFileName(str);
                    i++;
                }

                return res;
            }
        }

        private void SaveMinMaxAvgTrackFile(string parameter, string resume, string nameOfFile)
        {
            // количетво кривых + 1
            int countOfCurves = Utils.GetCountOfCurves(TypeOfCurves) + 1;

            double[][] masOfArtPoints = new double[countOfCurves][];
            int num = 1,
                index = _setOfParameters[parameter],
                countOfPoints = 0;

            foreach (ChipData cd in _setOfChips.Values)
            {
                if (cd.IsCulling)
                    continue;

                if (num == 1)
                {
                    countOfPoints = cd.GetCountOfPoints(index);
                    masOfArtPoints[0] = (double[]) cd.GetMasOfCurve(index, 0).Clone();
                    for (int i = 1; i < countOfCurves; i++)
                    {
                        masOfArtPoints[i] = (double[]) cd.GetMasOfCurve(index, i).Clone();
                    }
                    for (int p = 1; p <= countOfPoints; p++)
                    {
                        masOfArtPoints[0][p] *= 1E9;
                    }
                }
                else
                {
                    if (resume == "min")
                    {
                        for (int k = 1; k < countOfCurves; k++)
                        {
                            double[] masOfY = cd.GetMasOfCurve(index, k);
                            for (int p = 1; p <= countOfPoints; p++)
                            {
                                if (masOfY[p] < masOfArtPoints[k][p])
                                    masOfArtPoints[k][p] = masOfY[p];
                            }
                        }
                    }
                    if (resume == "max")
                    {
                        for (int k = 1; k < countOfCurves; k++)
                        {
                            double[] masOfY = cd.GetMasOfCurve(index, k);
                            for (int p = 1; p <= countOfPoints; p++)
                            {
                                if (masOfY[p] > masOfArtPoints[k][p])
                                    masOfArtPoints[k][p] = masOfY[p];
                            }
                        }
                    }
                    if (resume == "avg")
                    {
                        for (int k = 1; k < countOfCurves; k++)
                        {
                            double[] masOfY = cd.GetMasOfCurve(index, k);
                            for (int p = 1; p <= countOfPoints; p++)
                            {
                                masOfArtPoints[k][p] += masOfY[p];
                            }
                        }
                    }
                }
                num++;
            }
            if (resume == "avg")
            {
                for (int k = 1; k < countOfCurves; k++)
                {
                    for (int p = 1; p <= countOfPoints; p++)
                    {
                        masOfArtPoints[k][p] /= CountOfUnculling;
                    }
                }
            }

            // сохранеие файла
            FileStream outStream = new FileStream(_pathToFolder + "\\.Samson\\MinMaxTracks" + "\\" + nameOfFile, FileMode.Create);
            StreamWriter sw = new StreamWriter(outStream);
            sw.WriteLine("! This is artificial s2p file which generated using {0} ( {1} )", NameOfWaferFolder,  _pathToFolder);
            sw.WriteLine("! This is MINIMAL track of {0}", parameter);
            sw.WriteLine("# Hz S DB R 50");
            for (int p = 1; p <= countOfPoints; p++)
            {
                for (int k = 0; k < countOfCurves; k++)
                    sw.Write(masOfArtPoints[k][p].ToString("E", CultureInfo.InvariantCulture) + " ");
                sw.WriteLine();
            }
            sw.Flush();
            outStream.Flush();
            sw.Close();
            outStream.Close();
        }

        public void SaveMinMaxTracksOfParameters()
        {
            // если этой папки нет - то создаем
            DirectoryInfo diSamson = new DirectoryInfo(_pathToFolder + "\\.Samson");
            if (!diSamson.Exists)
                diSamson.Create();
            DirectoryInfo diMinMaxTracks = new DirectoryInfo(_pathToFolder + "\\.Samson\\MinMaxTracks");
            if (diMinMaxTracks.Exists)
            {
                diMinMaxTracks.Delete(true);
                Thread.Sleep(200);
            }
            diMinMaxTracks.Create();

            foreach (string parameter in _setOfParameters.Keys)
            {
                string nameOfFileMin = "chip_000001_" + parameter + ".s2p";
                SaveMinMaxAvgTrackFile(parameter, "min", nameOfFileMin);

                string nameOfFileAvg = "chip_000005_" + parameter + ".s2p";
                SaveMinMaxAvgTrackFile(parameter, "avg", nameOfFileAvg);

                string nameOfFileMax = "chip_000010_" + parameter + ".s2p";
                SaveMinMaxAvgTrackFile(parameter, "max", nameOfFileMax);
            }
        }

        /// <summary>
        /// Текущее название файла шаблона для отбраковки (карты раскроя) в формате "myfile.ext"
        /// Установке нового файла шаблона производиться только при его наличии во множестве потенциальных файлов-шаблонов
        /// </summary>
        public string CurrentTemplateFile
        {
            get { return Path.GetFileName(_currentWaferMapTemplateFile); }
            set
            {
                if (value != "none")
                {
                    foreach (string path in _setOfCullingFilesTemplate.Where(path => Path.GetFileName(path) == value))
                    {
                        _currentWaferMapTemplateFile = path;
                        break;
                    }
                }
                else
                {
                    _currentWaferMapTemplateFile = "none";
                }
            }
        }

        /// <summary>
        /// Метод сохраняет текущую отбраковку в файл для всех загруженных пластин (у каждой пластины появиться данный файл отбраковки)
        /// </summary>
        /// <param name="fileName">Название файла в который будет сохранена карта раскроя после отбраковки</param>
        public static void SaveAllWafersCullingChipsInfo(string fileName)
        {
            foreach (WaferMeasData wd in MasOfWaferData.Values)
            {
                wd.SaveCullingChipsInfo(fileName);
            }
        }

        /// <summary>
        /// Метод проводит инициализацию карты раскроя (загрузка файла-отбраковки либо файла-шаблона отбраковки 
        /// и нахождение минимальных и максимальных координат чипов на пластине)
        /// </summary>
        private void InitWaferMap()
        {
            int width, height;
            if (_currentWaferMapTemplateFile != "none")
                LoadTemplateFile(_currentWaferMapTemplateFile, out width, out height);
            else
                InitWaferMapWithoutFile(_nameOfFiles);

            _mapMaxX = 1;
            _mapMaxY = 1;
            _mapMinX = 400;
            _mapMinY = 400;

            for (int i = 1; i <= 400; i++)
                for (int j = 1; j <= 400; j++)
                {
                    if (_waferMap[i, j] != 0)
                    {
                        if (i > _mapMaxX)
                            _mapMaxX = i;
                        if (i < _mapMinX)
                            _mapMinX = i;
                        if (j > _mapMaxY)
                            _mapMaxY = j;
                        if (j < _mapMinY)
                            _mapMinY = j;
                    }
                }

            foreach (ChipData cd in _setOfChips.Values)
            {
                Point p = cd.CoordOfChip;
                if (cd.IsCulling)
                {
                    _waferMap[p.X + 1, p.Y + 1] = 2;
                }
            }
        }

        private void InitWaferMapWithoutFile(String[] namesOfChips)
        {
            _waferMap = new int[401, 401];

            foreach (string name in namesOfChips)
            {
                Point? point = GetCoordFromChipName(name);
                if (point != null)
                {
                    int x = ((Point) point).X + 1;
                    int y = ((Point) point).Y + 1;
                    _waferMap[x, y] = 1;
                }
            }
        }

        private Point? GetCoordFromChipName(string chipName)
        {
            Point res;
            try
            {
                res = new Point
                {
                    X = Int32.Parse(chipName.Substring(5, 3)),
                    Y = Int32.Parse(chipName.Substring(8, 3))
                };
            }
            catch
            {
                return null;
            }

            return res;
        }

        /// <summary>
        /// Метод возвращает изображение текущей карты раскроя пластины (с учетом отброковок)
        /// </summary>
        /// <param name="widthPix">Ширина изображения в пикселях</param>
        /// <param name="heightPix">Высота изображения в пикселях</param>
        /// <returns>Изображение карты раскроя</returns>
        public Bitmap GetBmpWaferMap(int widthPix, int heightPix)
        {
            InitWaferMap();

            Bitmap res = new Bitmap(widthPix, heightPix);
            Graphics g = Graphics.FromImage(res);

            int hCount = _mapMaxY - _mapMinY + 1,
                wCount = _mapMaxX - _mapMinX + 1;

            int h0 = heightPix / hCount,
                w0 = widthPix / wCount;

            for (int i = _mapMinX; i <= _mapMaxX; i++)
            {
                for (int j = _mapMinY; j <= _mapMaxY; j++)
                {
                    Color col;
                    switch (_waferMap[i, j])
                    {
                        case 0:
                            col = Color.FromArgb(220, 220, 220);
                            break;
                        case 1:
                            col = Color.FromArgb(0, 255, 0);
                            break;
                        case 2:
                            col = Color.FromArgb(255, 0, 0);
                            break;
                        case 3:
                            col = Color.FromArgb(255, 255, 255);
                            break;
                        case 4:
                            col = Color.FromArgb(255, 255, 0);
                            break;
                        case 5:
                            col = Color.FromArgb(255, 192, 203);
                            break;
                        case 6:
                            col = Color.FromArgb(255, 0, 255);
                            break;
                        case 7:
                            col = Color.FromArgb(65, 105, 225);
                            break;
                        case 8:
                            col = Color.FromArgb(0, 255, 225);
                            break;
                        default:
                            col = Color.Black;
                            break;
                    }

                    SolidBrush brCol = new SolidBrush(col);
                    g.FillRectangle(brCol, (i - _mapMinX) * w0, (j - _mapMinY) * h0, w0, h0);
                }
            }

            for (int i = _mapMinX; i <= _mapMaxX; i++)
            {
                for (int j = _mapMinY; j <= _mapMaxY; j++)
                    g.DrawRectangle(Pens.Gray, (i - _mapMinX) * w0, (j - _mapMinY) * h0, w0, h0);
            }

            return res;
        }

        /// <summary>
        /// Метод загружает файл содержащий шаблон файла отбраковки (начальная карта раскроя)
        /// </summary>
        /// <param name="pathToTemplateFile">Полный путь к файлу - шаблону (карта раскроя)</param>
        /// <param name="culledMas">Массив (401, 401) в который будет записана начальная карта раскроя</param>
        /// <param name="width">В переменную будет записана ширина чипа в мкм</param>
        /// <param name="height">В переменную будет записана высота чипа в мкм</param>
        void LoadTemplateFile(string pathToTemplateFile, out int width, out int height)
        {
            _waferMap = new int[401, 401];
            width = 0;
            height = 0;

            if (_currentWaferMapTemplateFile == "none")
            {
                MessageBox.Show(String.Format("Для пластины {0} не выбран шаблон файла отбраковки", NameOfWaferFolder));
                return;
            }

            FileStream inStream = new FileStream(pathToTemplateFile, FileMode.Open);
            BinaryReader inFile = new BinaryReader(inStream);
            width = inFile.ReadInt32();
            height = inFile.ReadInt32();
            for (int i = 1; i <= 400; i++)
                for (int j = 1; j <= 400; j++)
                {
                    _waferMap[i, j] = inFile.ReadInt32();
                }
            inFile.Close();
            inStream.Close();
        }

        /// <summary>
        /// Метод позволяет сохранить на жесткий диск файл текущей отбраковки (файл с картой раскроя после отбраковки)
        /// </summary>
        /// <param name="nameOfNewFile">Название файла отбраковки в формате "myfile.ext"</param>
        private void SaveCullingChipsInfo(string nameOfNewFile)
        {
            int width, height;
            if (_currentWaferMapTemplateFile != "none")
                LoadTemplateFile(_currentWaferMapTemplateFile, out width, out height);
            else
            {
                InitWaferMapWithoutFile(_nameOfFiles);
                width = 50;
                height = 50;
            }

            foreach (ChipData cd in _setOfChips.Values)
            {
                Point p = cd.CoordOfChip;
                if (cd.IsCulling)
                {
                    _waferMap[p.X + 1, p.Y + 1] = 2;
                }
            }

            // если этой папки нет - то создаем
            DirectoryInfo diSamson = new DirectoryInfo(_pathToFolder + "\\.Samson");
            if (!diSamson.Exists)
                diSamson.Create();

            FileStream outStream = new FileStream(_pathToFolder + "\\.Samson" + "\\" + nameOfNewFile, FileMode.Create);
            BinaryWriter outFile = new BinaryWriter(outStream);
            outFile.Write(width);
            outFile.Write(height);
            for (int i = 1; i <= 400; i++)
                for (int j = 1; j <= 400; j++)
                {
                    outFile.Write(_waferMap[i, j]);
                }
            outFile.Flush();
            outFile.Close();
            outStream.Close();

            // сохраняем пути к новому файлу с отбраковкой
            _setOfCullingFiles.Add(_pathToFolder + "\\.Samson" + "\\" + nameOfNewFile);
            _currentCullingFile = _pathToFolder + "\\.Samson" + "\\" + nameOfNewFile;
        }

        /// <summary>
        /// Массив содержит отсортированный список параметров чипов данной пластины
        /// </summary>
        public string[] ListOfParameters
        {
            get
            {
                string[] res = new string[_setOfParameters.Count];
                int i = 0;
                foreach (string str in _setOfParameters.Keys)
                {
                    res[i] = str;
                    i++;
                }
                Array.Sort(res);
                return res;
            }
        }

        /// <summary>
        /// Массив содержащий названия чипов в формате "НАЗВАНИЕ_ЧИПА in НАЗВАНИЕ_ПЛАСТИНЫ"
        /// </summary>
        public string[] ListOfChips
        {
            get
            {
                string[] res = new string[_setOfChips.Count];
                int i = 0;
                foreach (string str in _setOfChips.Keys)
                {
                    res[i] = str + " in " + NameOfWaferFolder;
                    i++;
                }
                Array.Sort(res);
                return res;
            }
        }

        /// <summary>
        /// Количество чипов пластины
        /// </summary>
        public int CountOfChips
        {
            get { return _setOfChips.Count; }
        }

        /// <summary>
        /// Процент выхода годных чипов пластины
        /// </summary>
        public string ProcentOfGood
        {
            get {
                string res = ((double)CountOfUnculling / CountOfChips).ToString("0.0 %");
                return res; 
            }
        }

        /// <summary>
        /// Метод загружает экспериментальные данные пластины
        /// </summary>
        /// <param name="pathToFiles">Путь к файлам с данными</param>
        private void LoadFolder(string[] pathToFiles)
        {
            _nameOfFiles = pathToFiles.Select(Path.GetFileName).ToArray();

            // загружаем и обрабатываем все файлы
            FileInfo[] fiList = pathToFiles.Select(path => new FileInfo(path)).ToArray();
            _countOfFiles = fiList.Length;
            int div = (_countOfFiles / 200) + 1;

            int currFile = 0;
            double proc = 0;
            if (_pbOut != null)
            {
                _pbOut.Visible = true;
                _pbOut.Value = 0;
            }

            // определяем тип кривых который будем использовать .sXp или .txt
            TypeOfCurves = DetectTypeOfCurves(fiList);

            foreach (FileInfo fi in fiList)
            {
                currFile++;
                var currName = fi.FullName;

                if (Path.GetExtension(currName).ToLower() == TypeOfCurves)
                    ProcessFile(currName);

                if (Path.GetExtension(currName).ToLower() == ".map")
                    _setOfCullingFilesTemplate.Add(currName);

                // отправляем прогресс загрузки
                if (currFile % div == 0 || currFile == _countOfFiles)
                {
                    if (_rtbOut != null)
                    {
                        _rtbOut.Clear();
                        proc = (double)(currFile) / _countOfFiles;
                        _rtbOut.AppendText("Прогресс: " + proc.ToString("P") + " процентов \n" +
                            "В директории " + _countOfFiles + " файлов. \n" +
                            "Чтение " + currFile + " файла из " + _countOfFiles + " ( " + Path.GetFileName(currName) + " )");
                    }
                    if (_pbOut != null)
                    {
                        int procInt = (int)(proc * 100) + 1;
                        if (procInt > 100)
                            procInt = 100;
                        _pbOut.Value = procInt;
                    }
                }
                // форма не замораживается
                Application.DoEvents();
            }
            if (_rtbOut != null)
            {
                _rtbOut.Clear();
            }
            if (_pbOut != null)
            {
                _pbOut.Visible = false;
                _pbOut.Value = 0;
            }

            DirectoryInfo di = new DirectoryInfo(_pathToFolder);
            // смотрим наличие уже готовых файлов с отбраковками
            foreach (DirectoryInfo inDi in di.GetDirectories())
            {
                if (inDi.Name == ".Samson")
                {
                    LoadCullingFiles(inDi);
                    break;
                }
            }
        }

        /// <summary>
        /// Определяет тип кривых файлы которых находятся в указанной папке
        /// </summary>
        /// <param name="fiList"></param>
        /// <returns></returns>
        private string DetectTypeOfCurves(FileInfo[] fiList)
        {
            string[] types = {".txt", ".s1p", ".s2p", ".s3p", ".s4p"};
            string res = fiList.Select(fi => Path.GetExtension(fi.FullName.ToLower())).FirstOrDefault(ext => types.Contains(ext));
            return res ?? "";
        }

        /// <summary>
        /// Метод позволяет добавить новый потенциальный файл-шаблон для отбраковки (карта раскроя)
        /// </summary>
        /// <param name="path">Полный путь к файлу-шаблону</param>
        public void AddNewCullingFileTemplate(string path)
        {
            _setOfCullingFilesTemplate.Add(path);
        }

        void LoadCullingFiles(DirectoryInfo di)
        {
            foreach (FileInfo fi in di.GetFiles())
            {
                if (Path.GetExtension(fi.FullName).ToLower() == ".map")
                {
                    _setOfCullingFiles.Add(fi.FullName);
                }
            }
        }

        /// <summary>
        /// Массив названий файлов отбраковок в формате "myfile.ext" пластины
        /// </summary>
        public string[] CullingFiles
        {
            get
            {
                string[] res = new string[_setOfCullingFiles.Count];
                int i = 0;
                foreach (string path in _setOfCullingFiles)
                {
                    res[i] = Path.GetFileName(path);
                    i++;
                }
                return res;
            }
        }

        /// <summary>
        /// Метод осуществляет отбраковку пластины по заданному файлу
        /// </summary>
        /// <param name="mapFileName">Название файла отбраковки в формате "myfile.ext" (полный путь к файлу должен содержаться в структуре файлов отбраковок данной пластины)</param>
        public void DoCullingByFile(string mapFileName)
        {
            // определяем путь к файлу по имени файла
            string pathToMapFile = (from path in _setOfCullingFiles 
                                    let temp = Path.GetFileName(path) 
                                    where temp == mapFileName 
                                    select path).FirstOrDefault();
            if (pathToMapFile == null)
            {
                MessageBox.Show("Данный файл не существует!");
                return;
            }

            // текущий файл отбраковки изменился
            _currentCullingFile = pathToMapFile;

            // считываем данные файла
            FileStream inStream = new FileStream(pathToMapFile, FileMode.Open);
            BinaryReader inFile = new BinaryReader(inStream);
            int width = inFile.ReadInt32(),
                height = inFile.ReadInt32();
            int[,] culledMas = new int[400, 400];
            for (int i = 0; i < 400; i++)
                for (int j = 0; j < 400; j++)
                {
                    culledMas[i, j] = inFile.ReadInt32();
                }
            inFile.Close();
            inStream.Close();

            // выполняем отбраковку
            for (int i = 0; i < 400; i++)
                for (int j = 0; j < 400; j++)
                {
                    if (culledMas[i, j] == 2)
                    {
                        string chipName = string.Format("chip_{0:000}{1:000}", i, j);
                        if (!_setOfChips.Keys.Contains(chipName))
                            continue;

                        ChipData cd = _setOfChips[chipName];
                        if (!cd.IsCulling)
                        {
                            cd.IsCulling = true;
                            CountOfCulling++;
                            CountOfUnculling--;
                        }
                    }
                }
        }

        string AddFirstZero(int x)
        {
            string res = x.ToString();
            while (res.Length < 3)
                res = "0" + res;
            return res;
        }
        
        /// <summary>
        /// Сброс текущей отбраковки пластины (все чипы пластины становятся годными)
        /// </summary>
        public void ResetCulling()
        {
            // текущий файл отбраковки изменился
            _currentCullingFile = "none";

            foreach (ChipData cd in _setOfChips.Values)
            {
                cd.IsCulling = false;
            }
            CountOfCulling = 0;
            CountOfUnculling = CountOfChips;
        }

        string GetNameOfChip(string name)
        {
            Int32.Parse(name.Substring(5, 6));
            return ChipData.GetChipName(name);
        }

        string GetNameOfParameter(string path)
        {
            return path.Substring(12);
        }

        public string GetCurrentCullingFile()
        {
            return Path.GetFileName(_currentCullingFile);
        }

        void ProcessFile(string pathToFile)
        {
            string nameOfChip, nameOfParameter;
            try
            {
                string shortName = Path.GetFileNameWithoutExtension(pathToFile);
                nameOfChip = GetNameOfChip(shortName);
                nameOfParameter = GetNameOfParameter(shortName);
            }
            catch
            {
                Messages.Add(String.Format("Обнаружено неверное название файла {0}", Path.GetFileName(pathToFile)));
                nameOfChip = Path.GetFileNameWithoutExtension(pathToFile);
                nameOfParameter = "UNPARSED";
                UnparsedFileNamesExist = true;
            }

            // если нет такого параметра, то регистрируем его - кладем в map
            if (!_setOfParameters.ContainsKey(nameOfParameter))
            {
                CountOfChipParameters++;
                _setOfParameters.Add(nameOfParameter, _setOfParameters.Count + 1);
            }
            int indexOfParameter = _setOfParameters[nameOfParameter];

            // если этот чип встретился в первый раз, то регистрируем его - кладем его в map
            if (!_setOfChips.ContainsKey(nameOfChip))
            {
                _setOfChips.Add(nameOfChip, new ChipData(nameOfChip));
            }

            _setOfChips[nameOfChip].PushCurve(pathToFile, TypeOfCurves, nameOfParameter, indexOfParameter);
        }

        /// <summary>
        /// Отменяет калибровку всех чипов
        /// </summary>
        void UncalibrateAllChips()
        {
            if (CurrentCalibrParameter != "none")
            {
                int index = _setOfParameters[CurrentCalibrParameter];
                foreach (ChipData chipData in _setOfChips.Values)
                {
                    chipData.UncalibrateAllCurves(index, CountOfChipParameters);
                }
                CurrentCalibrParameter = "none";
            }
        }

        /// <summary>
        /// Калибровка всех измерений чипов
        /// </summary>
        /// <param name="calibrParameter">Название параметра по которому производится калибровка</param>
        void CalibrateAllChips(string calibrParameter)
        {
            int index = _setOfParameters[calibrParameter];
            // отменяем последнюю калибровку
            UncalibrateAllChips();

            foreach (ChipData chipData in _setOfChips.Values)
            {
                chipData.CalibrateAllCurves(index, CountOfChipParameters);
            }
            CurrentCalibrParameter = calibrParameter;
        }
    }

    /// <summary>
    /// Класс содержит все электрофизические измерения одного чипа
    /// </summary>
    class ChipData
    {
        /// <summary>
        /// Эта структура связывает индекс параметра чипа и класс Curve
        /// тут конечно логично использовать List но это почему то это приводит к ошибкам при чтении из сетевого хранилища
        /// </summary>
        readonly Dictionary<int, ICurveDataFile> _masOfCurves = new Dictionary<int, ICurveDataFile>();

        /// <summary>
        /// Название чипа
        /// </summary>
        public string NameOfChip { get; private set; }

        /// <summary>
        /// Является ли данный чип отбракованным (true - отбракован, false - годен)
        /// </summary>
        public bool IsCulling { get; set; }

        /// <summary>
        /// Координата чипа
        /// </summary>
        public Point CoordOfChip
        {
            get
            {
                try
                {
                    return new Point(Int32.Parse(NameOfChip.Substring(5, 3)), Int32.Parse(NameOfChip.Substring(8, 3)));
                }
                catch
                {
                    MessageBox.Show(String.Format("Обнаружен файл с неверным форматом названия: {0}", NameOfChip));
                    return new Point(0, 0);
                }
            }
        }

        /// <summary>
        /// Устраняем разрывы фаз для всех измерений данного чипа
        /// </summary>
        public void ProcessingOfChipPhase()
        {
            foreach (var curveDataFile in _masOfCurves.Values)
            {
                var crv = (SXPFile) curveDataFile;
                if (crv != null)
                    crv.ProcessingOfCurvePhase();
            }
        }

        /// <summary>
        /// Сдвигаем положительные фазы измерений на 360 градусов вниз
        /// </summary>
        public void EliminatePositiveStartChipPhase()
        {
            foreach (var curveDataFile in _masOfCurves.Values)
            {
                var crv = (SXPFile) curveDataFile;
                if (crv != null)
                    crv.EliminatePositiveStartCurveOfPhase();
            }
        }

        /// <summary>
        /// Инициализация объекта с установкой названия чипа
        /// </summary>
        /// <param name="nameOfChip">Название чипа</param>
        public ChipData(string nameOfChip)
        {
            NameOfChip = nameOfChip;
        }

        /// <summary>
        /// Добавляет новые данные измерений к этому чипу
        /// </summary>
        /// <param name="pathToFile">Путь к файлу содержащему данные измерений</param>
        /// <param name="typeOfCurves">Тип данных</param>
        /// <param name="nameOfParameter">Название измереного параметра</param>
        /// <param name="indexOfParameter">Индекс измереного параметра</param>
        public void PushCurve(string pathToFile, string typeOfCurves, string nameOfParameter, int indexOfParameter)
        {
            if (Utils.IsSXPFormat(typeOfCurves))
                _masOfCurves.Add(indexOfParameter, new SXPFile(typeOfCurves, pathToFile, nameOfParameter, indexOfParameter));
            if (Utils.IsLibertyFormat(typeOfCurves))
                _masOfCurves.Add(indexOfParameter, new LibertyFile(pathToFile, nameOfParameter, indexOfParameter));
        }

        /// <summary>
        /// Возвращает интересующую кривую заданного измеренного параметра
        /// </summary>
        /// <param name="indexOfParameter">Индекс параметра</param>
        /// <param name="indexOfCurve">Индекс кривой</param>
        /// <returns>Массив точек данной кривой</returns>
        public double[] GetMasOfCurve(int indexOfParameter, int indexOfCurve)
        {
            return _masOfCurves[indexOfParameter].GetPointsOfCurve(indexOfCurve);
        }

        public string[] GetAxisesOfParameter(string nameOfParameter)
        {
            List<string> res = new List<string>();

            foreach (ICurveDataFile file in _masOfCurves.Values)
            {
                res.AddRange(file.AxisesOfCurves);
                break;
            }

            return res.ToArray();
        }

        public string[] GetDimensionsOfParameter(string nameOfParameter)
        {
            List<string> res = new List<string>();

            foreach (ICurveDataFile file in _masOfCurves.Values)
            {
                res.AddRange(file.DimOfCurves);
                break;
            }

            return res.ToArray();
        }

        public int GetCountOfPoints(int index)
        {
            return _masOfCurves[index].CountOfPoints;
        }

        public void CalibrateAllCurves(int index, int countOfParameters)
        {
            if (!_masOfCurves.Keys.Contains(index) || _masOfCurves[index] == null)
                return;
            for (int i = 1; i <= countOfParameters; i++)
            {
                if (i == index || !_masOfCurves.Keys.Contains(i) || _masOfCurves[i] == null)
                    continue;
                ((SXPFile)_masOfCurves[i]).SubstrateCurve((SXPFile)_masOfCurves[index]);
            }
        }

        public void UncalibrateAllCurves(int index, int countOfParameters)
        {
            if (!_masOfCurves.Keys.Contains(index) || _masOfCurves[index] == null)
                return;
            for (int i = 1; i <= countOfParameters; i++)
            {
                if (i == index || !_masOfCurves.Keys.Contains(i) || _masOfCurves[i] == null)
                    continue;
                ((SXPFile)_masOfCurves[i]).AddCurve((SXPFile)_masOfCurves[index]);
            }
        }

        public static string GetChipName(string chip)
        {
            if (WaferMeasData.MasOfWaferData.FirstOrDefault().Value.UnparsedFileNamesExist)
                return chip;
            else
                return chip.Substring(0, 11);
        }
    }

    /// <summary>
    /// Класс для всех файлов вида .sXp
    /// </summary>
    public class SXPFile : ICurveDataFile
    {
        /// <summary>
        /// Путь к файлу с точками
        /// </summary>
        private string PathToFile { get; set; }

        /// <summary>
        /// Количество точек кривой
        /// </summary>
        public int CountOfPoints { get; private set; }

        /// <summary>
        /// Количество кривых в файле
        /// </summary>
        private int CountOfCurves { get; set; }

        /// <summary>
        /// Массив индексов кривых для которых нужна калибровка 
        /// </summary>
        private readonly List<int> _calibratedCurves = new List<int>();

        /// <summary>
        /// Размерность точек с частотами в файле
        /// </summary>
        private string _strFreqDim;

        /// <summary>
        /// Названия кривых которые содержаться в этом файле
        /// </summary>
        private readonly List<string> _axisesOfCurves = new List<string>();

        /// <summary>
        /// Названия кривых которые содержаться в этом файле
        /// </summary>
        public string[] AxisesOfCurves
        {
            get { return (string[]) _axisesOfCurves.ToArray().Clone(); }
        }

        /// <summary>
        /// Размерности осей кривых которые содержаться в этом файле
        /// </summary>
        private readonly string[] _dimOfCurves;

        /// <summary>
        /// Размерности осей кривых которые содержаться в этом файле
        /// </summary>
        public string[] DimOfCurves
        {
            get { return (string[])_dimOfCurves.Clone(); }
        }

        /// <summary>
        /// Массив значений координат X точек ( _massOfPoints[0] ) и Y точек в порядке по возраствнию индекса : |S11| - _massOfPoints[1], alpha(S11) - _massOfPoints[2] (и так далее), |S21|, alpha(S21), |S12|, alpha(S12), |S22|, alpha(S22)
        /// </summary>
        private readonly double[][] _masOfPoints;

        /// <summary>
        /// Лист содержит набор строк - комментариев к файлу
        /// </summary>
        private readonly List<string> _comments = new List<string>();

        /// <summary>
        /// Строка с конфигурацией s2p файла. Пример: # Hz S DB R 50
        /// </summary>
        public string SettingsLine
        {
            get { return String.Format("# {0} {1} {2} R {3}", _strFreqDim, TypeOfFile, _formatOfPoints, _strLoadImpedance); }
        }

        private string _formatOfPoints;

        private double _loadImpedance;

        private string TypeOfFile { get; set; }

        private readonly char[] _splitChar = {' ', '\t', '\n', '\r'};

        private string _strLoadImpedance;

        public string NameOfFile
        {
            get { return Path.GetFileName(PathToFile); }
        }

        public SXPFile(string typeOfFile, string pathToFile, string nameOfParameter, int indexOfParameter)
        {
            // настраиваем объект в зависимости от представляемого типа файла
            TypeOfFile = typeOfFile;
            int x = typeOfFile[typeOfFile.Length - 2] - '0';
            CountOfCurves = (int)(2 * Math.Pow(x, 2) + 1);
            _masOfPoints = new double[CountOfCurves][];
            _dimOfCurves = new string[CountOfCurves];
            switch (x)
            {
                case 1:
                    _calibratedCurves = S1PConfig.CalibratedCurves;
                    _axisesOfCurves = S1PConfig.AxisesOfCurves;
                    break;
                case 2:
                    _calibratedCurves = S2PConfig.CalibratedCurves;
                    _axisesOfCurves = S2PConfig.AxisesOfCurves;
                    break;
                case 3:
                    _calibratedCurves = S3PConfig.CalibratedCurves;
                    _axisesOfCurves = S3PConfig.AxisesOfCurves;
                    break;
            }

            PathToFile = pathToFile;
            LoadFile(pathToFile);

            // выставляем желаемые размерности и формат (конвертируем)
            SetFrequencyDimension("GHz");
            SetFormatOfPoints("dB");
        }

        // небезопасно
        public double[] GetPointsOfCurve(int indexOfCurve)
        {
            return _masOfPoints[indexOfCurve];
        }

        public double[] GetPoints(int numberOfFrequency)
        {
            double[] res = new double[CountOfCurves];
            for (int i = 0; i < CountOfCurves; i++)
                res[i] = _masOfPoints[i][numberOfFrequency];
            return res;
        }

        public void AddNewComment(string line)
        {
            _comments.Insert(0, "! " + line);
        }

        public void SetFrequencyDimension(string desireFormat)
        {
            double freqMult = 1;
            string desireFormatUp = desireFormat.ToUpper();
            string currentFormat = _strFreqDim.ToUpper();

            if (desireFormatUp != "HZ" && desireFormatUp != "KHZ" && desireFormatUp != "MHZ" && desireFormatUp != "GHZ")
            {
                return;
            }

            if (currentFormat == "HZ")
            {
                switch (desireFormatUp)
                {
                    case "HZ": freqMult = 1; break;
                    case "KHZ": freqMult = 1e-3; break;
                    case "MHZ": freqMult = 1e-6; break;
                    case "GHZ": freqMult = 1e-9; break;
                }
            }
            if (currentFormat == "KHZ")
            {
                switch (desireFormatUp)
                {
                    case "HZ": freqMult = 1e3; break;
                    case "KHZ": freqMult = 1; break;
                    case "MHZ": freqMult = 1e-3; break;
                    case "GHZ": freqMult = 1e-6; break;
                }
            }
            if (currentFormat == "MHZ")
            {
                switch (desireFormatUp)
                {
                    case "HZ": freqMult = 1e6; break;
                    case "KHZ": freqMult = 1e3; break;
                    case "MHZ": freqMult = 1; break;
                    case "GHZ": freqMult = 1e-3; break;
                }
            }
            if (currentFormat == "GHZ")
            {
                switch (desireFormatUp)
                {
                    case "HZ": freqMult = 1e9; break;
                    case "KHZ": freqMult = 1e6; break;
                    case "MHZ": freqMult = 1e3; break;
                    case "GHZ": freqMult = 1; break;
                }
            }

            for (int i = 1; i <= CountOfPoints; i++)
            {
                _masOfPoints[0][i] *= freqMult;
            }

            _strFreqDim = desireFormat;
            _dimOfCurves[0] = desireFormat;
        }

        public void SetFormatOfPoints(string desireFormat)
        {
            string currentFormat = _formatOfPoints.ToUpper();
            string desireFormatUp = desireFormat.ToUpper();

            if (desireFormatUp != "MA" && desireFormatUp != "RI" && desireFormatUp != "DB")
            {
                return;
            }

            if (currentFormat == "MA")
            {
                switch (desireFormatUp)
                {
                    case "RI": ConvertFromMaToRi(); break;
                    case "DB": ConvertFromMaToDb(); break;
                }
            }

            if (currentFormat == "RI")
            {
                switch (desireFormatUp)
                {
                    case "MA": ConvertFromRiToMa(); break;
                    case "DB": ConvertFromRiToDb(); break;
                }
            }

            if (currentFormat == "DB")
            {
                switch (desireFormatUp)
                {
                    case "MA": ConvertFromDbtoMa(); break;
                    case "RI": ConvertFromDbToRi(); break;
                }
            }

            if (desireFormatUp == "DB")
            {
                for (int i = 1; i < CountOfCurves; i += 2)
                    _dimOfCurves[i] = "dB";
                for (int i = 2; i < CountOfCurves; i += 2)
                    _dimOfCurves[i] = "degree";
            }

            if (desireFormat == "MA")
            {
                for (int i = 1; i < CountOfCurves; i += 2)
                    _dimOfCurves[i] = "times";
                for (int i = 2; i < CountOfCurves; i += 2)
                    _dimOfCurves[i] = "degree";
            }



            if (desireFormat == "RI")
            {
                for (int i = 1; i < CountOfCurves; i++)
                    if (i % 2 == 1)
                        _dimOfCurves[i] = "real";
                    else
                        _dimOfCurves[i] = "imag";
            }

            _formatOfPoints = desireFormat;
        }

        private double ToRad(double x)
        {
            return Math.PI * x / 180;
        }

        private double ToDegree(double x)
        {
            return 180 * x / Math.PI;
        }

        void PointFromRiToMa(ref double x, ref double y)
        {
            double
                re = x,
                im = y;
            x = Math.Sqrt(re * re + im * im);
            y = ToDegree(Math.Atan(im / re));
        }

        private void PointFromRiToDb(ref double x, ref double y)
        {
            double
                re = x,
                im = y;
            x = 20 * Math.Log10(Math.Sqrt(re * re + im * im));
            y = ToDegree(Math.Atan(im / re));
        }

        private void PointFromMaToRi(ref double x, ref double y)
        {
            double
                m = x,
                a = y;
            x = m * Math.Cos(ToRad(a));
            y = m * Math.Sin(ToRad(a));
        }

        private double PointFromMaToDb(double x)
        {
            return 20 * Math.Log10(x);
        }

        private double PointFromDBToMA(double x)
        {
            return Math.Pow(10, x / 20);
        }

        private void PointFromDbToRi(ref double x, ref double y)
        {
            double
                m = Math.Pow(10, x / 20),
                a = y;
            x = m * Math.Cos(ToRad(a));
            y = m * Math.Sin(ToRad(a));
        }

        private void ConvertFromRiToMa()
        {
            for (int i = 1; i <= CountOfPoints; i++)
            {
                for (int n = 2; n < CountOfCurves; n += 2)
                {
                    PointFromRiToMa(ref _masOfPoints[n - 1][i], ref _masOfPoints[n][i]);
                }
            }
        }

        private void ConvertFromRiToDb()
        {
            for (int i = 1; i <= CountOfPoints; i++)
            {
                for (int n = 2; n < CountOfCurves; n += 2)
                {
                    PointFromRiToDb(ref _masOfPoints[n - 1][i], ref _masOfPoints[n][i]);
                }
            }
        }

        private void ConvertFromMaToRi()
        {
            for (int i = 1; i <= CountOfPoints; i++)
            {
                for (int n = 2; n < CountOfCurves; n += 2)
                {
                    PointFromMaToRi(ref _masOfPoints[n - 1][i], ref _masOfPoints[n][i]);
                }
            }
        }

        private void ConvertFromMaToDb()
        {
            for (int i = 1; i <= CountOfPoints; i++)
            {
                for (int n = 1; n < CountOfCurves; n += 2)
                {
                    _masOfPoints[n][i] = PointFromMaToDb(_masOfPoints[n][i]);
                }
            }
        }

        private void ConvertFromDbToRi()
        {
            for (int i = 1; i <= CountOfPoints; i++)
            {
                for (int n = 2; n < CountOfCurves; n += 2)
                {
                    PointFromDbToRi(ref _masOfPoints[n - 1][i], ref _masOfPoints[n][i]);
                }
            }
        }

        private void ConvertFromDbtoMa()
        {
            for (int i = 1; i <= CountOfPoints; i++)
            {
                for (int n = 1; n < CountOfCurves; n += 2)
                {
                    _masOfPoints[n][i] = PointFromDBToMA(_masOfPoints[n][i]);
                }
            }
        }

        private void LoadFile(string pathToFile)
        {
            // считываем метаданные файла
            string[] fileDump = File.ReadAllLines(pathToFile);
            List<string> pointLines = new List<string>();
            foreach (string line in fileDump)
            {
                if (line == "")
                    continue;

                if (line[0] == '!')
                {
                    _comments.Add(line);
                    continue;
                }
                if (line[0] == '#')
                {
                    ParseSettingsLine(line);
                    continue;
                }
                pointLines.Add(line);
            }

            // разбираем строки с данными (точки кривых)
            var posDataLine = new List<List<int>>();
            int n = -1, pos = 0;
            while (pos < pointLines.Count)
            {
                n++;
                posDataLine.Add(new List<int>());

                int tokens = 0;
                while (pos < pointLines.Count && tokens < CountOfCurves)
                {
                    tokens += pointLines[pos].Split(_splitChar, StringSplitOptions.RemoveEmptyEntries).Count();
                    posDataLine[n].Add(pos);
                    pos++;
                }
            }

            // сохраняем количество точек
            CountOfPoints = n + 1;

            // выделяем память
            for (int i = 0; i < CountOfCurves; i++)
            {
                _masOfPoints[i] = new double[CountOfPoints + 1];
            }

            // парсинг
            // желательно исправить на ноль - начало
            for (int i = 0; i < CountOfPoints; i++)
            {
                string pointsToken = posDataLine[i].Aggregate("", (current, x) => current + pointLines[x]);
                ParseDataLine(pointsToken, i + 1);
            }
        }

        private string PointToStr(double x, double y)
        {
            return x.ToString("0.##########E+000", CultureInfo.InvariantCulture) + " "
                + y.ToString("0.##########E+000", CultureInfo.InvariantCulture);
        }

        private string FreqPointToStr(double x)
        {
            return x.ToString("F3", CultureInfo.InvariantCulture);
        }

        private void ParseDataLine(string s, int numOfPoints)
        {
            var temp = s.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries).Select(str => Double.Parse(str, CultureInfo.InvariantCulture));
            int pos = 0;
            foreach (double next in temp)
            {
                _masOfPoints[pos++][numOfPoints] = next;
            }
        }

        private void ParseSettingsLine(string s)
        {
            string[] temp = s.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries).ToArray();
            for (int i = 0; i < temp.Length; i++)
                SetParameter(temp[i], i + 1);
        }

        private void SetParameter(string s, int n)
        {
            switch (n)
            {
                case 2: // частота
                    {
                        _strFreqDim = s;
                        switch (s.ToUpper())
                        {
                            case "GHZ":
                                break;
                            case "MHZ":
                                break;
                            case "KHZ":
                                break;
                            case "HZ":
                                break;
                        }
                    }
                    break;
                case 3:  // параметр, скорее всего S
                    {
                        TypeOfFile = s;
                    }
                    break;
                case 4:  // размерность DB либо MA либо RI
                    {
                        _formatOfPoints = s;
                        s = s.ToUpper();
                        if (s != "DB" && s != "MA" && s != "RI")
                            MessageBox.Show("Неверная строка конфигурации в s2p файле (размерность точек)!");
                    }
                    break;
                case 5:  // R
                    {

                    }
                    break;
                case 6: // импеданс нагрузки
                    {
                        _strLoadImpedance = s;
                        try
                        {
                            _loadImpedance = Double.Parse(s);
                        }
                        catch
                        {
                            MessageBox.Show("Неверная строка конфигурации в s2p файле (импеданс)!");
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Устраняем разрывы фаз (сырые данные векторника)
        /// </summary>
        public void ProcessingOfCurvePhase()
        {
            // выравниваем выбранные кривые
            for (int i = 1; i <= CountOfPoints; i++)
            {
                for (int k = 2; k < CountOfCurves; k += 2)
                {
                    while (_masOfPoints[k][i] - _masOfPoints[k][i - 1] > 180)
                        _masOfPoints[k][i] -= 360;
                }
            }
        }

        /// <summary>
        /// Устраняет разрывы фаз после конвертирования из RI в DB или MA
        /// </summary>
        public void EliminateJumpOfPhaseAftConv()
        {
            // выравниваем выбранные кривые
            for (int i = 1; i <= CountOfPoints; i++)
            {
                for (int k = 2; k < CountOfCurves; k += 2)
                {
                    while (_masOfPoints[k][i] - _masOfPoints[k][i - 1] > 170)
                        _masOfPoints[k][i] -= 180;
                }
            }
        }

        /// <summary>
        /// Если кривые фаз положительны в начальной точке то сдвигаем их на 360 вниз
        /// </summary>
        public void EliminatePositiveStartCurveOfPhase()
        {
            for (int k = 2; k < CountOfCurves; k += 2)
            {
                int add = 0;
                if (_masOfPoints[k][1] > 0)
                    add = -360;
                for (int i = 1; i <= CountOfPoints; i++)
                    _masOfPoints[k][i] += add;
            }
        }

        /// <summary>
        /// Вычитает кривую Y0 - Y1 для каждой координаты X
        /// </summary>
        /// <param name="calibrSxpFile">Кривая Y1</param>
        public void SubstrateCurve(SXPFile calibrSxpFile)
        {
            for (int i = 1; i <= CountOfPoints; i++)
            {
                foreach (int k in _calibratedCurves)
                    _masOfPoints[k][i] -= calibrSxpFile._masOfPoints[k][i];
            }
        }

        /// <summary>
        /// Суммирует кривую Y0 + Y1 для каждой координаты X
        /// </summary>
        /// <param name="calibrSxpFile">Кривая Y1</param>
        public void AddCurve(SXPFile calibrSxpFile)
        {
            for (int i = 1; i <= CountOfPoints; i++)
            {
                foreach (int k in _calibratedCurves)
                    _masOfPoints[k][i] += calibrSxpFile._masOfPoints[k][i];
            }
        }
    }

    public static class S1PConfig
    {
        public static readonly List<string> AxisesOfCurves = new List<string>
        {
            "Frequency", "|S11|"
        };

        public static readonly List<int> CalibratedCurves = new List<int>
        {
        };
    }

    public static class S2PConfig
    {
        public static readonly List<string> AxisesOfCurves = new List<string>
        {
            "Frequency", "|S11|", "angle (S11)", "|S21|", "angle (S21)", "|S12|", "angle (S12)", "|S22|", "angle (S22)"
        };

        public static readonly List<int> CalibratedCurves = new List<int>
        {
            3, 4, 5, 6
        };
    }

    public static class S3PConfig
    {
        public static readonly List<string> AxisesOfCurves = new List<string>
        {
            "Frequency", "|S11|", "angle (S11)", "|S12|", "angle (S12)", "|S13|", "angle (S13)", "|S21|", "angle (S21)", "|S22|", "angle (S22)", "|S23|", "angle (S23)", "|S31|", "angle (S31)", "|S32|", "angle (S32)", "|S33|", "angle (S33)"
        };

        public static readonly List<int> CalibratedCurves = new List<int>
        {
            3, 4, 5, 6, 7, 8, 11, 12, 13, 14, 15, 16
        };
    }

    /// <summary>
    /// Данный класс - оболочка для файлов с любыми измерениями
    /// </summary>
    public class LibertyFile : ICurveDataFile
    {
        /// <summary>
        /// Путь к файлу с координатами кривых
        /// </summary>
        private readonly string _pathToFile;

        /// <summary>
        /// Путь к файлу с координатами кривых
        /// </summary>
        public string PathToFile
        {
            get { return _pathToFile; }
        }

        /// <summary>
        /// Количество кривых в файле
        /// </summary>
        private int _countOfCurves = 0;

        /// <summary>
        /// Количество кривых в файле
        /// </summary>
        public int CountOfCurves
        {
            get { return _countOfCurves; }
        }

        private int _countOfPoints = 0;
        /// <summary>
        /// Количество точек кривой
        /// </summary>
        public int CountOfPoints
        {
            get { return _countOfPoints; }
        }

        /// <summary>
        /// Названия кривых которые содержаться в этом файле
        /// </summary>
        public string[] AxisesOfCurves
        {
            get { return _curvesNames.Values.ToArray(); }
        }

        /// <summary>
        /// Размерности осей кривых которые содержаться в этом файле
        /// </summary>
        public string[] DimOfCurves
        {
            get { return _curvesDimensions.Values.ToArray(); }
        }

        /// <summary>
        /// Структура связывает индекс кривых (ключ) и название кривых (значение)
        /// </summary>
        private readonly Dictionary<int, string> _curvesNames = new Dictionary<int, string>();
        /// <summary>
        /// Структура связывает индекс кривых (ключ) и размерность осей кривых (значение)
        /// </summary>
        private readonly Dictionary<int, string> _curvesDimensions = new Dictionary<int, string>();
        /// <summary>
        /// Структрура содержит множество кривых, каждая из которых состоит из множества чисел
        /// </summary>
        private readonly List<List<double>> _curves = new List<List<double>>();
        /// <summary>
        /// Массив строк - комментариев данного файла
        /// </summary>
        private readonly List<string> _comments = new List<string>();

        private string _nameOfParameter;
        private int _indexOfParameter;

        /// <summary>
        /// Возвращает имя и расширение данного файла
        /// </summary>
        public string NameOfFile
        {
            get { return Path.GetFileName(_pathToFile); }
        }

        /// <summary>
        /// Создает очередной экземпляр класса файла с измерениями
        /// </summary>
        /// <param name="pathToFile">Полный путь к файлу</param>
        /// <param name="nameOfParameter">Название измеряемого параметра</param>
        /// <param name="indexOfParameter">Индекс измеряемого параметра</param>
        public LibertyFile(string pathToFile, string nameOfParameter, int indexOfParameter)
        {
            _nameOfParameter = nameOfParameter;
            _indexOfParameter = indexOfParameter;
            _pathToFile = pathToFile;

            LoadFile(pathToFile);
        }

        // небезопасно
        public double[] GetPointsOfCurve(int indexOfCurve)
        {
            return _curves[indexOfCurve].ToArray();
        }

        public int[] IndexesOfCurves
        {
            get { return _curvesNames.Keys.ToArray(); }
        }

        public string GetDimensionOfCurve(int indexOfCurve)
        {
            return _curvesDimensions[indexOfCurve];
        }

        private void LoadFile(string pathToFile)
        {
            string[] fileDump = File.ReadAllLines(pathToFile);

            List<int> posDataLine = new List<int>(fileDump.Length);
            int pos = -1;
            foreach (string line in fileDump)
            {
                pos++;
                if (line == "")
                    continue;

                if (line[0] == '!')
                {
                    _comments.Add(line);
                    continue;
                }
                if (line[0] == '#')
                {
                    ParseSettingsLine(line);
                    continue;
                }
                posDataLine.Add(pos);
            }

            // сохраняем количество точек
            _countOfPoints = posDataLine.Count;

            // выделяем память
            for (int i = 0; i < _countOfCurves; i++)
            {
                _curves.Add(new List<double>(_countOfPoints));

                // индексация с 1
                _curves[i].Add(0);
            }

            // парсинг
            foreach (int posLine in posDataLine)
                ParseDataLine(fileDump[posLine]);
        }

        private void ParseDataLine(string line)
        {
            char[] separators = { ' ', '\t'};
            double[] data = line.Split(separators, StringSplitOptions.RemoveEmptyEntries).Select(temp => Double.Parse(temp.Replace(',', '.'), CultureInfo.InvariantCulture)).ToArray();
            for (int i = 0; i < data.Length; i++)
                _curves[i].Add(data[i]);
        }

        private void ParseSettingsLine(string line)
        {
            int posOfSharp = line.IndexOf('#'),
                posOfComma = line.IndexOf(',');
            string nameOfCurve = line.Substring(posOfSharp + 1, posOfComma - posOfSharp - 1),
                   dimension = line.Substring(posOfComma + 2);

            _curvesNames.Add(_countOfCurves, nameOfCurve);
            _curvesDimensions.Add(_countOfCurves, dimension);
            _countOfCurves++;
        }
    }

    /// <summary>
    /// Класс со вспомогательными методами
    /// </summary>
    public static class Utils
    {
        public static bool IsLibertyFormat(string typeOfCurves)
        {
            return typeOfCurves == ".txt";
        }

        public static bool IsSXPFormat(string typeOfCurves)
        {
            if (typeOfCurves.Length != 4) return false;
            return typeOfCurves[0] == '.' && typeOfCurves[1] == 's' && typeOfCurves[3] == 'p' && typeOfCurves[2] >= '0' && typeOfCurves[2] <= '9';
        }

        public static int GetCountOfCurves(string typeOfCurves)
        {
            int count = 0;
            if (IsSXPFormat(typeOfCurves))
                count = GetSXPAxes(typeOfCurves).Count - 1;
            return count;
        }

        public static List<string> GetSXPAxes(string typeOfCurves)
        {
            switch (typeOfCurves)
            {
                case ".s1p":
                    return S1PConfig.AxisesOfCurves;
                case ".s2p":
                    return S2PConfig.AxisesOfCurves;
                case ".s3p":
                    return S3PConfig.AxisesOfCurves;
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Интерфейс для всех файлов содержащих точки кривых
    /// </summary>
    public interface ICurveDataFile
    {
        /// <summary>
        /// Метод возвращает массив с точками выбранной кривой
        /// </summary>
        /// <param name="indexOfCurve">Индекс выбранной кривой</param>
        /// <returns>Массив с точками</returns>
        double[] GetPointsOfCurve(int indexOfCurve);
        /// <summary>
        /// Названия кривых которые содержаться в этом файле
        /// </summary>
        string[] AxisesOfCurves { get; }
        /// <summary>
        /// Размерности осей кривых которые содержаться в этом файле
        /// </summary>
        string[] DimOfCurves { get; }
        /// <summary>
        /// Количество точек кривой
        /// </summary>
        int CountOfPoints { get; }
    }
}