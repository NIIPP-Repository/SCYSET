using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NIIPP.WaferMeasData;

namespace MicrowaveApplication
{
    public partial class FormMain : Form
    {
        public FormMain(string[] args)
        {
            InitializeComponent();
            if (args.Length > 0)
            {
                List<string> res = new List<string>();
                foreach (string path in args)
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    if (di.Exists)
                        LoadWafer(di.GetFiles().Select(fi => fi.FullName).ToArray());
                    else
                        res.Add(path);
                }
                LoadWafer(res.ToArray());
            }
        }

        /// <summary>
        /// Текущий экземпляр открытого окна (для доступа из других классов)
        /// </summary>
        public static FormMain Instance;

        /// <summary>
        /// Нужно ли опускать кривые с фазами которые начинаются с положительных значений
        /// </summary>
        private readonly bool _needToEliminatePositiveStartPhase = false;

        /// <summary>
        /// Нужна ли мгновенная обработка событий
        /// </summary>
        private bool _needAutoUpdate;

        /// <summary>
        /// Текущий экземпляр класса с данными о пластине
        /// </summary>
        private WaferMeasData _currWd;

        public void LoadToFormLimits(double x1, double x2, double y1, double y2)
        {
            textBoxX1.Text = GraficLibrary.DoubleToStr(x1);
            textBoxX2.Text = GraficLibrary.DoubleToStr(x2);
            textBoxY1.Text = GraficLibrary.DoubleToStr(y1);
            textBoxY2.Text = GraficLibrary.DoubleToStr(y2);

            checkBoxAxesFixed.Checked = true;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            Instance = this;
            // уходим в левый верхний угол
            Top = 0;
            Left = SystemInformation.PrimaryMonitorSize.Width - Width;

            // так кажется работает
            TopMost = true;
        }

        private void PrepareCheckboxesByXRange(int n)
        {
            List<Control> controls = gbSXPAxes.Controls.Cast<Control>()
                .Where(control => control.GetType() == typeof (CheckBox))
                .Where(control => control.Name[4] - '0' <= n && control.Name[5] - '0' <= n)
                .ToList();

            foreach (var control in controls)
            {
                ((CheckBox) control).Enabled = true;
            }
        }

        private void PrepareSParSwitchCheckboxes(string typeOfFiles)
        {
            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;

            if (Utils.IsSXPFormat(typeOfFiles))
            {
                radioButtonAmplitude.Enabled = true;
                radioButtonPhase.Enabled = true;
                switch (typeOfFiles)
                {
                    case ".s1p":
                        PrepareCheckboxesByXRange(1);
                        chbS11.Checked = true;
                        break;
                    case ".s2p":
                        PrepareCheckboxesByXRange(2);
                        chbS21.Checked = true;
                        break;
                    case ".s3p":
                        PrepareCheckboxesByXRange(3);
                        chbS21.Checked = true;
                        break;
                    case ".s4p":
                        PrepareCheckboxesByXRange(4);
                        chbS21.Checked = true;
                        break;
                }
            }

            if (Utils.IsLibertyFormat(typeOfFiles))
            {
                cbXAxes.Enabled = true;
                cbYAxes.Enabled = true;
            }

            _needAutoUpdate = copy;
        }

        private void LoadWafer(string[] pathToFiles)
        {
            if (pathToFiles.Length == 0)
                return;

            _needAutoUpdate = false;

            _currWd = new WaferMeasData(pathToFiles, ref rtbInfo, ref pbMain);

            // из полученых данных формируем интерфейс формы

            if (Utils.IsSXPFormat(WaferMeasData.TypeOfCurves))
            {
                labelX1.Text = "GHz";
                labelX2.Text = "GHz";
                labelY1.Text = "dB";
                labelY2.Text = "db";
            }

            // выводим сообщения на форму
            PushListOfWarningsToForm(_currWd.Messages);

            // тип файлов и соответствующие контролы
            lblTypeOfFiles.Text = WaferMeasData.TypeOfCurves;
            PrepareSParSwitchCheckboxes(WaferMeasData.TypeOfCurves);

            // если первый раз то загружаем параметры, второй раз не нужно, если они будут разные то получим Exception :)
            if (WaferMeasData.MasOfWaferData.Count == 1)
            {
                PushListOfParametersToForm(_currWd.ListOfParameters);
                PushListOfCalibrParToForm(_currWd.ListOfParameters);
                gbListOfParameters.Text = "Параметры (" + _currWd.CountOfParameters + ")";
            }

            PushListOfChipsToForm(_currWd.ListOfChips);
            gbListOfChips.Text = "Список чипов (" + WaferMeasData.CountOfChipsFromAllWafers + ")";

            PushListOfWafersToForm(_currWd.NameOfWafer);
            gbListOfWafers.Text = "Список пластин (" + WaferMeasData.CountOfWafers + ")";

            // если загружено больше одной то очищаем поля
            if (WaferMeasData.MasOfWaferData.Count > 1)
            {
                ClearListOfCullingFilesInForm();
                ClearListOfPotentialTemplateInForm();
            }

            PushListOfCullingFilesToForm(_currWd.CullingFiles);
            PushListOfPotentialTemplateToForm(_currWd.PotentialTemplateFiles);

            if (!_currWd.UnparsedFileNamesExist)
                PushListOfWarningsToForm(_currWd.DiagnosticOfWaferData());

            PushCurrentWaferToForm(_currWd);

            _currWd.ProcessingOfWaferPhase();
            SetProbablyCalibrPar();

            if (comboBoxCullingFile.Text == "")
                comboBoxCullingFile.Text = "none";
            // если существует один, ранее отбракованный файл, то выбираем его
            if (comboBoxCullingFile.Text != "none")
            {
                _currWd.DoCullingByFile(comboBoxCullingFile.Text);
            }
            // если существует только один map file то выбираем его
            if (comboBoxTemplateOfCullingFile.Text != "none")
            {
                _currWd.CurrentTemplateFile = comboBoxTemplateOfCullingFile.Text;
            }
            PushMainInfoToForm(_currWd);

            // загружаем возможные оси
            FirstLoadNewAxeses(checkedListBoxParameters);

            _needAutoUpdate = true;

            DrawToPlotForm();
        }

        public void PushGroupOfChipsToInfo(List<string> setOfGoodChips)
        {
            rtbInfo.Clear();
            rtbInfo.AppendText("Количество интересующих чипов: " + setOfGoodChips.Count.ToString() + '\n');
            foreach (string str in setOfGoodChips)
            {
                rtbInfo.AppendText(str + '\n');
            }
        }

        private void PushListOfWarningsToForm(List<string> listOfWarnings)
        {
            foreach (string str in listOfWarnings)
            {
                rtbInfo.AppendText(str + " \n");
            }
        }

        private void ClearListOfWarningsInForm()
        {
            rtbInfo.Clear();
        }

        private void PushListOfPotentialTemplateToForm(string[] listOfFiles)
        {
            comboBoxTemplateOfCullingFile.BeginUpdate();
            foreach (string str in listOfFiles)
            {
                comboBoxTemplateOfCullingFile.Items.Add(str);
            }
            comboBoxTemplateOfCullingFile.Text = listOfFiles.Length == 1 ? listOfFiles[0] : "none";
            comboBoxTemplateOfCullingFile.EndUpdate();
        }

        private void PushListOfPotentialTemplateToFormOnline(string[] listOfFiles, string currentFile)
        {
            comboBoxTemplateOfCullingFile.BeginUpdate();

            foreach (string str in listOfFiles)
            {
                comboBoxTemplateOfCullingFile.Items.Add(str);
            }
            comboBoxTemplateOfCullingFile.Text = currentFile;

            comboBoxTemplateOfCullingFile.EndUpdate();
        }

        private void ClearListOfPotentialTemplateInForm()
        {
            comboBoxTemplateOfCullingFile.BeginUpdate();
            comboBoxTemplateOfCullingFile.Items.Clear();
            comboBoxTemplateOfCullingFile.Items.Add("none");
            comboBoxTemplateOfCullingFile.Text = "none";
            comboBoxTemplateOfCullingFile.EndUpdate();
        }

        private void PushListOfCullingFilesToForm(string[] listOfFiles)
        {
            comboBoxCullingFile.BeginUpdate();
            foreach (string str in listOfFiles)
            {
                comboBoxCullingFile.Items.Add(str);
            }
            if (listOfFiles.Length == 1)
                comboBoxCullingFile.Text = listOfFiles[0];

            comboBoxCullingFile.EndUpdate();
        }

        private void PushListOfCullingFilesToFormOnline(string[] listOfFiles, string currentFile)
        {
            comboBoxCullingFile.BeginUpdate();

            foreach (string str in listOfFiles)
            {
                comboBoxCullingFile.Items.Add(str);
            }
            comboBoxCullingFile.Text = currentFile;

            comboBoxCullingFile.EndUpdate();
        }

        private void ClearListOfCullingFilesInForm()
        {
            comboBoxCullingFile.BeginUpdate();
            comboBoxCullingFile.Items.Clear();
            comboBoxCullingFile.Items.Add("none");
            comboBoxCullingFile.Text = "none";
            comboBoxCullingFile.EndUpdate();
        }

        private void PushCurrentWaferToForm(WaferMeasData wd)
        {
            comboBoxWafer.Items.Add(wd.NameOfWafer);
            comboBoxWafer.Text = wd.NameOfWafer;
        }

        private void PushMainInfoToForm(WaferMeasData wd)
        {
            labelNumOfChips.Text = wd.CountOfChips.ToString();
            labelNumOfCulling.Text = wd.CountOfCulling.ToString();
            labelNumOfUnculling.Text = wd.CountOfUnculling.ToString();
            labelProcent.Text = wd.ProcentOfGood;
            Text = wd.PathToFolder;
        }

        public void PushMainInfoToForm()
        {
            // вычисляем текущую пластину
            var wd = WaferMeasData.MasOfWaferData[comboBoxWafer.Text];

            labelNumOfChips.Text = wd.CountOfChips.ToString();
            labelNumOfCulling.Text = wd.CountOfCulling.ToString();
            labelNumOfUnculling.Text = wd.CountOfUnculling.ToString();
            labelProcent.Text = wd.ProcentOfGood;
            Text = wd.PathToFolder;
        }

        private void ClearMainInfoInForm()
        {
            comboBoxWafer.BeginUpdate();
            comboBoxWafer.Items.Clear();
            comboBoxWafer.Items.Add("none");
            comboBoxWafer.Items.Add("all wafers");
            comboBoxWafer.Text = "none";
            comboBoxWafer.EndUpdate();

            labelNumOfChips.Text = "";
            labelNumOfCulling.Text = "";
            labelNumOfUnculling.Text = "";
            labelProcent.Text = "";
            Text = "SCySET (Scattering parameters selection tools)";
        }

        private void CreateCheckBoxItem(CheckedListBox clb, string str)
        {
            clb.Items.Add(str);
        }

        private void CheckAllItemsInCheckedListBox(CheckedListBox clb)
        {
            for (int i = 0; i < clb.Items.Count; i++)
            {
                clb.SetItemChecked(i, true);
            }
        }

        private void UncheckAllItemsInCheckedListBox(CheckedListBox clb)
        {
            for (int i = 0; i < clb.Items.Count; i++)
            {
                clb.SetItemChecked(i, false);
            }
        }

        private void PushListOfCalibrParToForm(string[] listOfParameters)
        {
            int i = 0;
            cbChooseCalibrPar.BeginUpdate();
            foreach (string str in listOfParameters)
            {
                cbChooseCalibrPar.Items.Add(str);
                i++;
            }
            cbChooseCalibrPar.EndUpdate();
        }

        private void ClearListOfCalibrParInForm()
        {
            cbChooseCalibrPar.BeginUpdate();
            cbChooseCalibrPar.Items.Clear();
            cbChooseCalibrPar.Items.Add("none");
            cbChooseCalibrPar.Text = "none";
            cbChooseCalibrPar.EndUpdate();
        }

        public void SetAllWaferResume()
        {
            comboBoxWafer.Text = "all wafers";
        }

        public void SetNewCullingFileSingle()
        {
            DateTime time = DateTime.Now;

            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;

            // необходимо удалить из списка файл предыдущей отбраковки
            foreach (Object next in comboBoxCullingFile.Items)
            {
                if (((string) next).IndexOf("Int ") == 0)
                {
                    comboBoxCullingFile.Items.Remove(next);
                    break;
                }
            }

            comboBoxCullingFile.Items.Add("Int Culling File ( " + time.ToString().Replace(':', '-') + " )");
            comboBoxCullingFile.Text = String.Format("Int Culling File ( {0} )", time.ToString().Replace(':', '-'));

            _needAutoUpdate = copy;
        }

        public void SetNewCullingFileMulty()
        {
            DateTime time = DateTime.Now;

            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;

            comboBoxCullingFile.Items.Clear();
            comboBoxCullingFile.Items.Add("none");
            comboBoxCullingFile.Items.Add("Int Culling File ( " + time.ToString().Replace(':', '-') + " )");
            comboBoxCullingFile.Text = "Int Culling File ( " + time.ToString().Replace(':', '-') + " )";

            _needAutoUpdate = copy;
        }

        private void PushListOfParametersToForm(string[] listOfParameters)
        {
            int i = 0;
            checkedListBoxParameters.BeginUpdate();
            foreach (string str in listOfParameters)
            {
                CreateCheckBoxItem(checkedListBoxParameters, str);
                i++;
            }
            CheckAllItemsInCheckedListBox(checkedListBoxParameters);
            checkedListBoxParameters.EndUpdate();
        }

        private void ClearListOfParametersInForm()
        {
            checkedListBoxParameters.BeginUpdate();
            checkedListBoxParameters.Items.Clear();
            checkedListBoxParameters.EndUpdate();
        }

        private void PushListOfChipsToForm(string[] listOfChips)
        {
            int i = 0;
            checkedListBoxChips.BeginUpdate();
            foreach (string str in listOfChips)
            {
                CreateCheckBoxItem(checkedListBoxChips, str);
                i++;
            }
            CheckAllItemsInCheckedListBox(checkedListBoxChips);
            checkedListBoxChips.EndUpdate();
        }

        private void ClearListOfChipsInForm()
        {
            checkedListBoxChips.BeginUpdate();
            checkedListBoxChips.Items.Clear();
            checkedListBoxChips.EndUpdate();
        }

        private void PushListOfWafersToForm(string nameOfWafers)
        {
            checkedListBoxWafers.BeginUpdate();
            CreateCheckBoxItem(checkedListBoxWafers, nameOfWafers);
            CheckAllItemsInCheckedListBox(checkedListBoxWafers);
            checkedListBoxWafers.EndUpdate();
        }

        private void ClearListOfWafersInForm()
        {
            checkedListBoxWafers.BeginUpdate();
            checkedListBoxWafers.Items.Clear();
            checkedListBoxWafers.EndUpdate();
        }

        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private string[] GetListOfChecked(CheckedListBox clb)
        {
            string[] res = new string[clb.CheckedItems.Count];
            int i = 0;
            foreach (object item in clb.CheckedItems)
            {
                res[i] = item.ToString();
                i++;
            }

            return res;
        }

        private List<int> GetListCheckedSandA()
        {
            List<string> sxpAxises = Utils.GetSXPAxes(WaferMeasData.TypeOfCurves);
            List<int> res =
                gbSXPAxes.Controls.Cast<Control>()
                    .Where(control => control.GetType() == typeof (CheckBox))
                    .Where(control => ((CheckBox) control).Checked)
                    .Select(
                        control =>
                            radioButtonAmplitude.Checked
                                ? String.Format("|{0}|", control.Name.Substring(3))
                                : String.Format("angle ({0})", control.Name.Substring(3)))
                    .Select(str => sxpAxises.IndexOf(str))
                    .ToList();
            return res;
        }

        private void DrawToPlotForm(double? minX = null, double? maxX = null, double? minY = null, double? maxY = null)
        {
            if (_currWd == null)
            {
                MessageBox.Show("Не загружено ни одной пластины!");
                return;
            }

            string[] wafers = GetListOfChecked(checkedListBoxWafers);
            string[] parameters = GetListOfChecked(checkedListBoxParameters);
            string[] chips = GetListOfChecked(checkedListBoxChips);

            List<int> indexesOfGraphs = new List<int>();
            int indexOfX = 0;
            string nameOfYAxes = "",
                nameOfXAxes = "",
                header = "";

            if (Utils.IsSXPFormat(WaferMeasData.TypeOfCurves))
            {
                indexesOfGraphs = GetListCheckedSandA();
                indexOfX = 0;
                nameOfXAxes = "Frequency, GHz";
                nameOfYAxes = NameOfYAxes;
            }

            if (Utils.IsLibertyFormat(WaferMeasData.TypeOfCurves))
            {
                int indexOfY = cbYAxes.Items.IndexOf(cbYAxes.Text);
                if (cbYAxes.Text != "")
                    indexesOfGraphs.Add(indexOfY);
                indexOfX = cbXAxes.Items.IndexOf(cbXAxes.Text);

                nameOfYAxes = cbYAxes.Text;
                nameOfXAxes = cbXAxes.Text;
            }

            // калибровка
            if (cbChooseCalibrPar.Text != "none")
            {
                WaferMeasData.SetCalibrationParameter(cbChooseCalibrPar.Text);
            }
            else
            {
                WaferMeasData.CancelCalibration();
            }
            // только после калибровки устраняем положительные фазы
            if (_needToEliminatePositiveStartPhase)
                WaferMeasData.EliminatePositiveStartPhase();
            if (!FormIsOpen("FormPlot"))
            {
                Form winPlot = new FormPlot();
                winPlot.Show();
            }

            double[][] masX,
                masY;
            List<string>[] curveInfo;
            int countOfCurves;
            WaferMeasData.ReturnDefinedCurves(indexesOfGraphs, indexOfX, parameters, wafers, chips,
                out masX, out masY, out curveInfo, out countOfCurves);

            // Формируем заголовок графика
            if (Utils.IsSXPFormat(WaferMeasData.TypeOfCurves))
                header = GetPlotHeader(curveInfo, countOfCurves);

            Bitmap pictureWithGraph;
            int[][] masPixX, masPixY;
            GraficLibrary.DrawSetOfCurves(masX, masY, countOfCurves, nameOfXAxes, nameOfYAxes, header, minX, maxX, minY,
                maxY,
                out pictureWithGraph, out masPixX, out masPixY);

            FormPlot.Instance.LoadBitmap(pictureWithGraph);
            FormPlot.Instance.LoadCurveCoord(masPixX, masPixY);
            FormPlot.Instance.LoadCurveInfo(curveInfo, countOfCurves);
        }

        private bool FormIsOpen(string name)
        {
            return Application.OpenForms.Cast<Form>().Any(form => form.Name == name);
        }

        public string NameOfYAxes
        {
            get
            {
                if (radioButtonAmplitude.Checked)
                    return "Magnitude, dB";
                else
                    return "Phase, degree";
            }
        }

        private string GetPlotHeader(List<string>[] curveInfoPar, int n)
        {
            if (n == 0)
                return "";
            string[] header = new string[4];
            header[0] = curveInfoPar[1].ToArray()[0];
            header[1] = curveInfoPar[1].ToArray()[1];
            header[2] = curveInfoPar[1].ToArray()[2];
            header[3] = curveInfoPar[1].ToArray()[3];

            // проверяем на единственность параметров кривых, которые на графике
            for (int i = 2; i <= n; i++)
            {
                int k = 0;
                foreach (string item in curveInfoPar[i])
                {
                    if (header[k] != item)
                        header[k] = "";
                    k++;
                }
            }

            if (header[0] == "")
                header[0] = "Lots of wafers";
            if (header[1] == "")
                header[1] = "Lots of chips";
            if (header[2] == "")
                header[2] = "Lots of chip's parameters";
            if (header[3] == "")
                header[3] = "Lots of S parameters";

            return header[0] + "\n" + header[1] + "       " + header[2] + "       " + header[3];
        }

        private void выделитьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var parent = ((ContextMenuStrip) ((ToolStripMenuItem) sender).GetCurrentParent()).SourceControl;
            CheckAllItemsInCheckedListBox((CheckedListBox) parent);
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void снятьВыделенияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var parent = ((ContextMenuStrip) ((ToolStripMenuItem) sender).GetCurrentParent()).SourceControl;
            UncheckAllItemsInCheckedListBox((CheckedListBox) parent);
        }

        private void автоПерерисовкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _needAutoUpdate = true;
            ((ToolStripMenuItem) sender).Checked = true;

            RedrawWithLimits();
        }

        private void ActionOnSelectedIndexChanged(object sender)
        {
            CheckedListBox currClb = (CheckedListBox) sender;

            int index = currClb.SelectedIndex;
            if (ChooseToolStripItem("Выбор одного").Checked)
                UncheckAllItemsInCheckedListBox(currClb);
            if (ChooseToolStripItem("Выбор одного").Checked)
                currClb.SetItemChecked(index, true);
            else
            {
                currClb.SetItemChecked(index, !currClb.GetItemChecked(index));
            }
            LoadNewAxeses(currClb);

            // если включена автоперерисока - делаем
            if (_needAutoUpdate)
            {
                RedrawWithLimits();
            }
        }

        private void LoadNewAxeses(CheckedListBox currClb)
        {
            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;

            string[] axiseOfParameter = _currWd.GetAxisesOfParameter(currClb.Text);
            string[] dimensionsOfParameter = _currWd.GetDimensionsOfParameter(currClb.Text);
            string[] namesForShow = new string[axiseOfParameter.Length];
            for (int i = 0; i < namesForShow.Length; i++)
                namesForShow[i] = axiseOfParameter[i] + ", " + dimensionsOfParameter[i];

            cbXAxes.Items.Clear();
            cbXAxes.Items.AddRange(namesForShow);
            cbYAxes.Items.Clear();
            cbYAxes.Items.AddRange(namesForShow);
            cbXAxes.Text = (string) cbXAxes.Items[_currWd.CurrentXAxis];
            cbYAxes.Text = (string) cbYAxes.Items[_currWd.CurrentYAxis];

            _needAutoUpdate = copy;
        }

        private void FirstLoadNewAxeses(CheckedListBox currClb)
        {
            if (currClb.Items.Count == 0)
                return;

            string[] axiseOfParameter = _currWd.GetAxisesOfParameter((string) currClb.Items[0]);
            string[] dimensionsOfParameter = _currWd.GetDimensionsOfParameter((string) currClb.Items[0]);
            string[] namesForShow = new string[axiseOfParameter.Length];
            for (int i = 0; i < namesForShow.Length; i++)
                namesForShow[i] = axiseOfParameter[i] + ", " + dimensionsOfParameter[i];
            cbXAxes.Items.Clear();
            cbXAxes.Items.AddRange(namesForShow);
            cbYAxes.Items.Clear();
            cbYAxes.Items.AddRange(namesForShow);

            cbXAxes.Text = (string) cbXAxes.Items[_currWd.CurrentXAxis];
            cbYAxes.Text = (string) cbYAxes.Items[_currWd.CurrentYAxis];
        }

        private void checkedListBoxParameters_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionOnSelectedIndexChanged(sender);
        }

        private ToolStripMenuItem ChooseToolStripItem(string name)
        {
            foreach (Object next in contextMenuStripParameters.Items)
            {
                ToolStripMenuItem item;
                try
                {
                    item = (ToolStripMenuItem) next;
                }
                catch
                {
                    continue;
                }

                if (item.Text == name)
                {
                    return item;
                }
            }
            return null;
        }

        private void выборНесколькихToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ((ToolStripMenuItem) sender).Checked = true;
            ToolStrip parent = ((ToolStripMenuItem) sender).GetCurrentParent();
            ToolStripMenuItem single = ChooseToolStripItem("Выбор одного");
            single.Checked = false;
        }

        private void выборОдногоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ((ToolStripMenuItem) sender).Checked = true;
            ToolStrip parent = ((ToolStripMenuItem) sender).GetCurrentParent();
            ToolStripMenuItem single = ChooseToolStripItem("Выбор нескольких");
            single.Checked = false;
        }

        private void checkedListBoxChips_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionOnSelectedIndexChanged(sender);
        }

        private void checkedListBoxWafers_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionOnSelectedIndexChanged(sender);
        }

        private void отрисовкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RedrawWithLimits();
        }

        public void UncheckFrozenAxes()
        {
            checkBoxAxesFixed.Checked = false;
        }

        private void SetProbablyCalibrPar()
        {
            if (cbChooseCalibrPar.Text == "")
                cbChooseCalibrPar.Text = "none";
        }

        private void загрузитьПластинуToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog {Description = "Выбирите папку c файлами измерений"};

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                LoadWafer((new DirectoryInfo(fbd.SelectedPath).GetFiles().Select(fi => fi.FullName).ToArray()));
            }
        }

        private void radioButtonAmplitude_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonAmplitude.Checked)
            {
                labelY1.Text = "dB";
                labelY2.Text = "dB";
            }
            else
            {
                labelY1.Text = "degree";
                labelY2.Text = "degree";
            }
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void checkBoxS11_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void checkBoxS21_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void checkBoxS12_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void checkBoxS22_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        public void RedrawWithLimits()
        {
            double? minX, maxX, minY, maxY;
            try
            {
                if (textBoxX1.Text != "")
                    minX = Double.Parse(textBoxX1.Text.Replace('.', ','));
                else
                    minX = null;
                if (textBoxX2.Text.Replace('.', '.') != "")
                    maxX = Double.Parse(textBoxX2.Text.Replace('.', ','));
                else
                    maxX = null;
                if (textBoxY1.Text.Replace('.', '.') != "")
                    minY = Double.Parse(textBoxY1.Text.Replace('.', ','));
                else
                    minY = null;
                if (textBoxY2.Text.Replace('.', '.') != "")
                    maxY = Double.Parse(textBoxY2.Text.Replace('.', ','));
                else
                    maxY = null;
            }
            catch
            {
                MessageBox.Show("Проверьте правильность введеных данных фиксации осей!");
                return;
            }

            // проверка больше-меньше
            if (minX != null && maxX != null)
            {
                if (minX > maxX)
                {
                    double? copy = minX;
                    minX = maxX;
                    maxX = copy;

                }
            }

            if (minY != null && maxY != null)
            {
                if (minY > maxY)
                {
                    double? copy = minY;
                    minY = maxY;
                    maxY = copy;
                }
            }

            if (checkBoxAxesFixed.Checked)
            {
                DrawToPlotForm(minX, maxX, minY, maxY);
            }
            else
                DrawToPlotForm();

            // закрываем неактуальные окна
            if (FormIsOpen("FormPointInfo"))
                FormPointInfo.Instance.Close();
        }

        private void yAxesFixed_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void textBoxY1_TextChanged(object sender, EventArgs e)
        {
            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;
            checkBoxAxesFixed.Checked = false;
            _needAutoUpdate = copy;

        }

        private void textBoxY2_TextChanged(object sender, EventArgs e)
        {
            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;
            checkBoxAxesFixed.Checked = false;
            _needAutoUpdate = copy;
        }

        private void textBoxX1_TextChanged(object sender, EventArgs e)
        {
            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;
            checkBoxAxesFixed.Checked = false;
            _needAutoUpdate = copy;
        }

        private void textBoxX2_TextChanged(object sender, EventArgs e)
        {
            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;
            checkBoxAxesFixed.Checked = false;
            _needAutoUpdate = copy;
        }

        private void comboBoxChooseCalibrPar_TextChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void comboBoxCullingFile_TextChanged(object sender, EventArgs e)
        {
            if (!_needAutoUpdate)
                return;

            // удаляем промежуточные виртуальные файлы отбраковки
            string toRemove = null;
            if (comboBoxCullingFile.Text.IndexOf("Int Culling File") == -1)
            {
                foreach (string nextItem in comboBoxCullingFile.Items)
                {
                    if (nextItem.IndexOf("Int Culling File") != -1)
                        toRemove = nextItem;
                }
            }
            comboBoxCullingFile.Items.Remove(toRemove);

            if (comboBoxWafer.Text != "all wafers")
            {
                // вычисляем текущую пластину
                WaferMeasData wd = WaferMeasData.MasOfWaferData[comboBoxWafer.Text];

                wd.ResetCulling();
                if (comboBoxCullingFile.Text != "none")
                {
                    wd.DoCullingByFile(comboBoxCullingFile.Text);
                }
                PushMainInfoToForm(wd);
            }
            else
            {
                if (comboBoxCullingFile.Text == "none")
                {
                    WaferMeasData.ResetCullingForAll();
                }
                PushMainInfoAllWafer();
            }

            RedrawWithLimits();
            if (FormIsOpen("FormWaferMap"))
                FormWaferMap.Instance.RefreshPicture();
        }

        private void buttonSaveCullingFile_Click(object sender, EventArgs e)
        {
            SaveCurrentCullingFile();
        }

        private void SaveCurrentCullingFile()
        {
            if (comboBoxCullingFile.Text == "none")
            {
                MessageBox.Show("Отсутствуют данные для сохранения!");
                return;
            }

            bool copy = _needAutoUpdate;
            _needAutoUpdate = false;
            string fileName = comboBoxCullingFile.Text;
            // удаляем "Int " в начале
            comboBoxCullingFile.Items.Remove(fileName);
            fileName = fileName.Substring(4);
            if (Path.GetExtension(fileName).ToLower() != ".map")
            {
                fileName += ".map";
            }
            comboBoxCullingFile.Items.Add(fileName);
            comboBoxCullingFile.Text = fileName;

            WaferMeasData.SaveAllWafersCullingChipsInfo(fileName);

            // доработать
            //comboBoxCullingFile.Items.Add(fileName);
            //comboBoxCullingFile.Text = fileName;

            _needAutoUpdate = copy;
            MessageBox.Show("Сохранено успешно", "Подтверждение");
        }

        private void оПрограммеToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FormAboutProgram formAbout = new FormAboutProgram();
            formAbout.Show();
        }

        private void ClearAllWafersAndInfoOnForm()
        {
            _needAutoUpdate = false;

            // удаляем данные
            WaferMeasData.ClearAllWafers();

            // удаляем все данные на форме
            ClearListOfParametersInForm();
            ClearListOfChipsInForm();
            ClearListOfWafersInForm();
            ClearListOfCalibrParInForm();
            ClearListOfCullingFilesInForm();
            ClearListOfPotentialTemplateInForm();
            ClearListOfWarningsInForm();
            ClearMainInfoInForm();
            gbListOfWafers.Text = "Список пластин";
            gbListOfChips.Text = "Список чипов";
            gbListOfParameters.Text = "Список параметров";

            GC.Collect();

            _needAutoUpdate = true;
        }

        private void выгрузитьПластиныToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ClearAllWafersAndInfoOnForm();
            //if (FormIsOpen("Plot"))
            //{
            //    FormPlot.Instance.Close();
            //}
            Close();
        }

        private void groupBoxListOfWafers_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.All : DragDropEffects.None;
        }

        private void S2PReader_DragDrop(object sender, DragEventArgs e)
        {
            string[] masOfPathes = (string[]) e.Data.GetData(DataFormats.FileDrop, false);

            List<string> res = new List<string>();
            foreach (string path in masOfPathes)
            {
                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists)
                    LoadWafer(di.GetFiles().Select(fi => fi.FullName).ToArray());
                else
                    res.Add(path);
            }

            LoadWafer(res.ToArray());
        }

        private void S2PReader_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void btnSearchForTemplateFile_Click(object sender, EventArgs e)
        {
            OpenCullingFile();
        }

        private void OpenCullingFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Map files|*.map";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _currWd.AddNewCullingFileTemplate(ofd.FileName);

                ClearListOfPotentialTemplateInForm();
                PushListOfPotentialTemplateToFormOnline(_currWd.PotentialTemplateFiles, Path.GetFileName(ofd.FileName));
            }
        }

        private void основныеToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void сохранитьМинмаксТрекиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _currWd.SaveMinMaxTracksOfParameters();
        }

        private void загрузитьТестToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // LoadWafer("D:\\Эксперименты\\2014.12.05_MFR_pl9sl168_13");
        }

        private int startWidth = 0;

        private void свернутьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = (ToolStripMenuItem) sender;
            if (tsmi.Text == "Свернуть")
            {
                startWidth = this.Width;
                this.Width = 220;
                this.Top = 0;
                this.Left = SystemInformation.PrimaryMonitorSize.Width - this.Width;
                tsmi.Text = "Развернуть";
            }
            else
            {
                this.Width = startWidth;
                this.Top = 0;
                this.Left = SystemInformation.PrimaryMonitorSize.Width - this.Width;
                tsmi.Text = "Свернуть";
            }
        }

        public void PushMainInfoAllWafer()
        {
            if (_needAutoUpdate)
            {
                // здесь временно отключаем реакцию на textChanged
                _needAutoUpdate = false;

                // очищаем поля
                ClearListOfCullingFilesInForm();
                ClearListOfPotentialTemplateInForm();

                labelNumOfChips.Text = WaferMeasData.CountOfChipsFromAllWafers.ToString();
                labelNumOfCulling.Text = WaferMeasData.CountOfCulledChipsFromAllWafers.ToString();
                labelNumOfUnculling.Text = WaferMeasData.CountOfGoodChipsFromAllWafers.ToString();
                labelProcent.Text = WaferMeasData.ProcentOfGoodFromAllWafers.ToString();
                Text = "all wafers";

                _needAutoUpdate = true;

            }
        }

        private void comboBoxWafer_TextChanged(object sender, EventArgs e)
        {
            // если выбран режим абстрактной сгруппированной пластины, это не классический случай
            if (comboBoxWafer.Text == "all wafers")
            {
                PushMainInfoAllWafer();
                return;
            }

            WaferMeasData wd;
            try
            {
                wd = WaferMeasData.MasOfWaferData[comboBoxWafer.Text];

            }
            catch
            {
                return;
            }

            if (_needAutoUpdate)
            {
                // здесь временно отключаем реакцию на textChanged
                _needAutoUpdate = false;

                // очищаем поля
                ClearListOfCullingFilesInForm();
                ClearListOfPotentialTemplateInForm();

                PushListOfCullingFilesToFormOnline(wd.CullingFiles, wd.GetCurrentCullingFile());
                PushListOfPotentialTemplateToFormOnline(wd.PotentialTemplateFiles, wd.CurrentTemplateFile);
                PushMainInfoToForm(wd);

                _needAutoUpdate = true;

            }
        }

        private void comboBoxTemplateOfCullingFile_TextChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
            {
                WaferMeasData wd;
                try
                {
                    wd = WaferMeasData.MasOfWaferData[comboBoxWafer.Text];
                }
                catch
                {
                    return;
                }
                wd.CurrentTemplateFile = comboBoxTemplateOfCullingFile.Text;
            }

            if (FormIsOpen("FormWaferMap"))
                FormWaferMap.Instance.RefreshPicture();
        }

        private void открытьКартуРаскрояToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currWd == null)
            {
                return;
            }

            FormWaferMap waferMapForm = new FormWaferMap();
            waferMapForm.Show();
            FormWaferMap.Instance.TopMost = true;
        }

        private void cbXAxes_TextChanged(object sender, EventArgs e)
        {
            _currWd.CurrentXAxis = cbXAxes.Items.IndexOf(cbXAxes.Text);
            if (_needAutoUpdate)
            {
                RedrawWithLimits();
            }
        }

        private void cbYAxes_TextChanged(object sender, EventArgs e)
        {
            _currWd.CurrentYAxis = cbYAxes.Items.IndexOf(cbYAxes.Text);
            if (_needAutoUpdate)
            {
                RedrawWithLimits();
            }
        }

        private void сохранитьТекущийФайлОтбраковкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentCullingFile();
            _currWd.SaveMinMaxTracksOfParameters();
        }

        private void открытьФайлОтбраковкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenCullingFile();
        }

        private void chbS13_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS23_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS33_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS43_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS42_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS41_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS31_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS32_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS14_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS24_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS34_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void chbS44_CheckedChanged(object sender, EventArgs e)
        {
            if (_needAutoUpdate)
                RedrawWithLimits();
        }

        private void загрузитьФайлыСДаннымиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog {Multiselect = true};
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                LoadWafer(ofd.FileNames);
            }
        }

        private void ассоциироватьФайлыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (FileAssociation.IsAssociated)
            //    FileAssociation.Remove();
        }
    }
}
