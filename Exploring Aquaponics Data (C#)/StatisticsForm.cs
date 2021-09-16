using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace WindowsFormsApplication1
{
    public partial class StatisticsForm : Form
    {
        Dictionary<string, Dictionary<string, List<Object>>> Table;
        Dictionary<string, Dictionary<string, object>> Info;
        Dictionary<string, string> timestampdict;
        Dictionary<string, Series> Seriesdict = new Dictionary<string, Series>();
        Dictionary<string, List<string>> ComboStructure;
        DateTime XMin;
        DateTime XMax;
        int maxRes = 5000;
        double WhiskerPercentile = 0.1;
        int DecimalPositions = 2;
        bool AutomaticDecimals = true;

        ChartsSettingsForm TopSettings = new ChartsSettingsForm();
        ChartsSettingsForm BottomSettings = new ChartsSettingsForm();
        ChartsSettingsForm RightSettings = new ChartsSettingsForm("Statistics");

        Dictionary<string, Series> ChartDict = new Dictionary<string, Series>();
        Dictionary<string, Series> Outdict = new Dictionary<string, Series>();
        Dictionary<string, bool[]> UseDict = new Dictionary<string, bool[]>()
        {
            {"Top", new bool[2] {false, false} },
            {"Bottom", new bool[2] {false, false} },
//            {"Right", new bool[2] {false, false} },    
        }; //MIN=0, MAX=1
        Dictionary<string, double[]> MinMaxDict = new Dictionary<string, double[]>()
        {
            {"Top", new double[2] {0,0} },
            {"Bottom", new double[2] {0,0} },
//            {"Right", new double[2] {0,0} },
        }; //ChartMin=0, ChartMax=1

        bool TopShowOutliers = true;
        bool TopOutlierLegend = false;

        bool BottomShowOutliers = true;
        bool BottomOutlierLegend = false;

        //INITIALIZATION

        public StatisticsForm(Dictionary<string, Dictionary<string, List<Object>>> data, Dictionary<string, Dictionary<string, object>> info, Dictionary<string, string> Timestamps)
        {
            InitializeComponent();
            Table = data;
            Info = info;
            timestampdict = Timestamps;
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            this.Width = 940;
            this.Height = 375;
            ComboStructure = new Dictionary<string, List<string>>();
            foreach (string tablekey in Table.Keys)
            {
                if (!timestampdict.ContainsKey(tablekey))
                {
                    MessageBox.Show("Tabelle " + tablekey + " besitzt keine auswertbare X-Achse und wird geschlossen.");
                    this.Close();
                    //continue?
                }

                ComboStructure[tablekey] = new List<string>();

                foreach (string key in Table[tablekey].Keys)
                {
                    if (key.Contains("ID") || (Type)Info[tablekey][key] == typeof(System.Boolean) || (Type)Info[tablekey][key] == typeof(System.String) || (Type)Info[tablekey][key] == typeof(DBNull))
                    {
                        continue;
                    }

                    if (timestampdict.ContainsKey(tablekey) && key == timestampdict[tablekey])
                    {
                        XMin = (DateTime)Table[tablekey][key][0];
                        XMax = (DateTime)Table[tablekey][key][Table[tablekey][key].Count() - 1];
                    }

                    else if (timestampdict.ContainsKey(tablekey))
                    {
                        try
                        {
                            Series ser1 = FitSeries(Table[tablekey][timestampdict[tablekey]], Table[tablekey][key], 1);
                            ser1.Name = key;
                            Seriesdict[key] = ser1;
                            ComboStructure[tablekey].Add(key);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                    }
                }
            }

            foreach (string tablekey in ComboStructure.Keys)
            {
                comboBox1.Items.Add(tablekey);
                comboBox3.Items.Add(tablekey);
            }

            comboBox1.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox2.Items.AddRange(ComboStructure[(string)comboBox1.SelectedItem].ToArray());
            comboBox4.Items.AddRange(ComboStructure[(string)comboBox3.SelectedItem].ToArray());
            comboBox2.SelectedIndex = 0;
            comboBox4.Items.RemoveAt(0);
            comboBox4.SelectedIndex = 0;

            chart1.Series.Clear();
            chart2.Series.Clear();
            chart3.Series.Clear();

            //Vorbereitung Chart1
            chart1.ChartAreas.Add("Boxplotarea");
            chart1.ChartAreas[0].Name = "Regressionarea";
            chart1.ChartAreas["Boxplotarea"].AlignWithChartArea = "Regressionarea";
            chart1.ChartAreas["Boxplotarea"].AlignmentOrientation = AreaAlignmentOrientations.Horizontal;
            chart1.ChartAreas["Boxplotarea"].Position.X = 79;
            chart1.ChartAreas["Boxplotarea"].Position.Width = 20;
            chart1.ChartAreas["Boxplotarea"].AxisX.MajorGrid.Enabled = false;
            chart1.ChartAreas["Boxplotarea"].AxisX.MajorTickMark.Enabled = false;
            chart1.ChartAreas["Boxplotarea"].AxisX.LabelStyle.Enabled = false;
            chart1.ChartAreas["Boxplotarea"].AxisY.LineColor = Color.Transparent;
            chart1.ChartAreas["Boxplotarea"].AxisY.MinorTickMark.Enabled = true;
            chart1.ChartAreas["Regressionarea"].Position.Height = 90;
            chart1.ChartAreas["Regressionarea"].Position.Width = 80;

            //Vorbereitung Chart2
            chart2.ChartAreas.Add("Boxplotarea");
            chart2.ChartAreas[0].Name = "Regressionarea";
            chart2.ChartAreas["Boxplotarea"].AlignWithChartArea = "Regressionarea";
            chart2.ChartAreas["Boxplotarea"].AlignmentOrientation = AreaAlignmentOrientations.Horizontal;
            chart2.ChartAreas["Boxplotarea"].Position.X = 79;
            chart2.ChartAreas["Boxplotarea"].Position.Width = 20;
            chart2.ChartAreas["Boxplotarea"].AxisX.MajorGrid.Enabled = false;
            chart2.ChartAreas["Boxplotarea"].AxisX.MajorTickMark.Enabled = false;
            chart2.ChartAreas["Boxplotarea"].AxisX.LabelStyle.Enabled = false;
            chart2.ChartAreas["Boxplotarea"].AxisY.LineColor = Color.Transparent;
            chart2.ChartAreas["Boxplotarea"].AxisY.MinorTickMark.Enabled = true;
            chart2.ChartAreas["Regressionarea"].Position.Height = 90;
            chart2.ChartAreas["Regressionarea"].Position.Width = 80;

            //Vorbereitung Chart3
            chart3.ChartAreas[0].AxisX.LabelStyle.Format = "{0:0.###}";
            chart3.ChartAreas[0].AxisY.LabelStyle.Format = "{0:0.###}";

            DrawTop();

        }

        //BUTTONS

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "+")
            {
                this.Width = 1260;
                this.Height = 700;
                button1.Text = "-";
                if (comboBox1.SelectedItem == comboBox3.SelectedItem)
                {
                    comboBox2.Items.Remove(comboBox4.SelectedItem);
                    if (comboBox2.SelectedItem == null) comboBox2.SelectedIndex = 0;
                }
                DrawBottom();
                DrawRight();
            }
            else
            {
                this.Width = 940;
                this.Height = 375;
                button1.Text = "+";
                comboBox2.Items.Clear();
                comboBox2.Items.AddRange(ComboStructure[(string)comboBox1.SelectedItem].ToArray());
                string CurrentChart = chart1.Series[0].Name;
                if (!comboBox2.Items.Contains(CurrentChart))
                    comboBox2.Items.Add(CurrentChart);
                comboBox2.SelectedItem = CurrentChart;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            chart1.SaveImage(Application.StartupPath + @"\VisevData\GraphTop.emf", ChartImageFormat.EmfDual);
            MessageBox.Show("Graph gespeichert.");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            chart2.SaveImage(Application.StartupPath + @"\VisevData\GraphBottom.emf", ChartImageFormat.EmfDual);
            MessageBox.Show("Graph gespeichert.");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            chart3.SaveImage(Application.StartupPath + @"\VisevData\GraphRight.emf", ChartImageFormat.EmfDual);
            MessageBox.Show("Graph gespeichert.");
        }

        //EINSTELLUNGEN
        private void button4_Click(object sender, EventArgs e)
        {
            TopSettings.ShowDialog();

            chart1.Titles.Clear();
            chart1.ChartAreas[0].AxisX.Title = "";
            chart1.ChartAreas[0].AxisY.Title = "";

            if (TopSettings.TitleActivated)
            {
                chart1.ChartAreas["Regressionarea"].Position.Y = 10;
                chart1.ChartAreas["Regressionarea"].Position.Height = 80;
                Title title = chart1.Titles.Add(TopSettings.TitleText);
                title.Font = new System.Drawing.Font("Arial", TopSettings.TitleSize, TopSettings.TitleStyle);
            }
            else
            {
                chart1.ChartAreas["Regressionarea"].Position.Y = 0;
                chart1.ChartAreas["Regressionarea"].Position.Height = 90;
            }

            if (TopSettings.XAxisActivated)
            {
                chart1.ChartAreas[0].AxisX.Title = TopSettings.XAxisText;
                chart1.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Arial", TopSettings.XAxisSize, TopSettings.XAxisStyle);
            }

            if (TopSettings.YAxisActivated)
            {
                chart1.ChartAreas[0].AxisY.Title = TopSettings.YAxisText;
                chart1.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Arial", TopSettings.YAxisSize, TopSettings.YAxisStyle);
            }

            MinMaxDict["Top"][1] = TopSettings.Max;
            UseDict["Top"][1] = TopSettings.UseMax;
            MinMaxDict["Top"][0] = TopSettings.Min;
            UseDict["Top"][0] = TopSettings.UseMin;

            TopShowOutliers = TopSettings.ShowPoints;
            TopOutlierLegend = TopSettings.OutlierLegend;

            Outdict["Top"] = new Series();

            if (!TopSettings.ShowLegend) chart1.Legends[0].Enabled = false;
            else chart1.Legends[0].Enabled = true;

            DrawTop();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            BottomSettings.ShowDialog();

            chart2.Titles.Clear();
            chart2.ChartAreas[0].AxisX.Title = "";
            chart2.ChartAreas[0].AxisY.Title = "";

            if (BottomSettings.TitleActivated)
            {
                chart2.ChartAreas["Regressionarea"].Position.Y = 10;
                chart2.ChartAreas["Regressionarea"].Position.Height = 80;
                Title title = chart2.Titles.Add(BottomSettings.TitleText);
                title.Font = new System.Drawing.Font("Arial", BottomSettings.TitleSize, BottomSettings.TitleStyle);
            }
            else
            {
                chart2.ChartAreas["Regressionarea"].Position.Y = 0;
                chart2.ChartAreas["Regressionarea"].Position.Height = 90;
            }


            if (BottomSettings.XAxisActivated)
            {
                chart2.ChartAreas[0].AxisX.Title = BottomSettings.XAxisText;
                chart2.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Arial", BottomSettings.XAxisSize, BottomSettings.XAxisStyle);
            }

            if (BottomSettings.YAxisActivated)
            {
                chart2.ChartAreas[0].AxisY.Title = BottomSettings.YAxisText;
                chart2.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Arial", BottomSettings.YAxisSize, BottomSettings.YAxisStyle);
            }

            MinMaxDict["Bottom"][1] = BottomSettings.Max;
            UseDict["Bottom"][1] = BottomSettings.UseMax;
            MinMaxDict["Bottom"][0] = BottomSettings.Min;
            UseDict["Bottom"][0] = BottomSettings.UseMin;

            BottomShowOutliers = BottomSettings.ShowPoints;
            BottomOutlierLegend = BottomSettings.OutlierLegend;

            Outdict["Bottom"] = new Series();

            if (!BottomSettings.ShowLegend) chart2.Legends[0].Enabled = false;
            else chart2.Legends[0].Enabled = true;

            DrawBottom();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            RightSettings.ShowDialog();

            chart3.Titles.Clear();
            chart3.ChartAreas[0].AxisX.Title = "";
            chart3.ChartAreas[0].AxisY.Title = "";

            if (RightSettings.TitleActivated)
            {
                Title title = chart3.Titles.Add(RightSettings.TitleText);
                title.Font = new System.Drawing.Font("Arial", RightSettings.TitleSize, RightSettings.TitleStyle);
            }

            if (RightSettings.XAxisActivated)
            {
                chart3.ChartAreas[0].AxisX.Title = RightSettings.XAxisText;
                chart3.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Arial", RightSettings.XAxisSize, RightSettings.XAxisStyle);
            }

            if (RightSettings.YAxisActivated)
            {
                chart3.ChartAreas[0].AxisY.Title = RightSettings.YAxisText;
                chart3.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Arial", RightSettings.YAxisSize, RightSettings.YAxisStyle);
            }

            if (!RightSettings.ShowLegend) chart3.Legends[0].Enabled = false;
            else chart3.Legends[0].Enabled = true;

            DrawRight();

        }

        //CHARTS

        private void DrawTop()
        {
            //Regression-Chart
            chart1.Series.Clear();

            TopSettings.OrigTitle = (string)comboBox2.SelectedItem;
            TopSettings.OrigXAxis = timestampdict[(string)comboBox1.SelectedItem] + "[" + "Einheit" + "]";
            TopSettings.OrigYAxis = (string)comboBox2.SelectedItem + "[" + "Einheit" + "]";

//            Series ShortSeries;
//            Series LongSeries;

            try
            {
                /*
                //TEST
                Series Test = FitSeries2(XList, YList, 27);
                Test.ChartType = SeriesChartType.FastLine;
                Test.Name = "Test";
                chart1.Series.Add(Test);
                */
                /*
                ShortSeries = new Series();
                LongSeries = new Series();
                for (int i = 0; i < YList.Count(); i += maxRes)
                {
                    ShortSeries.Points.DataBindXY(XList.GetRange(i, Math.Min(maxRes, XList.Count - i)), YList.GetRange(i, Math.Min(maxRes, XList.Count - i)));
                    foreach (DataPoint dp in ShortSeries.Points) LongSeries.Points.Add(dp);
                    //MessageBox.Show(chart1.Series[name].Points.Count.ToString());
                }
                */

                CutSeries("Top", (string)comboBox2.SelectedItem, Table[(string)comboBox1.SelectedItem][timestampdict[(string)comboBox1.SelectedItem]], Table[(string)comboBox1.SelectedItem][(string)comboBox2.SelectedItem], 1);

                chart1.Series[(string)comboBox2.SelectedItem] = ChartDict["Top"];
                chart1.Series[(string)comboBox2.SelectedItem].ChartType = SeriesChartType.FastLine;

                if (TopShowOutliers && Outdict.ContainsKey("Top"))
                {
                    chart1.Series.Add(Outdict["Top"]);
                    if (!TopOutlierLegend) chart1.Series["Outlier"].IsVisibleInLegend = false;
                    else chart1.Series["Outlier"].IsVisibleInLegend = true;
                }


                chart1.ChartAreas[0].RecalculateAxesScale();

                List<object> XList = new List<object>();
                List<object> YList = new List<object>();

                foreach (DataPoint dp in chart1.Series[(string)comboBox2.SelectedItem].Points)
                {
                    XList.Add(DateTime.FromOADate(dp.XValue));
                    YList.Add(dp.YValues[0]);
                }

                XMin = (DateTime)XList[0];
                XMax = (DateTime)XList[XList.Count - 1];

                List<object> CopyList = new List<object>(YList);

                CopyList.Sort();

                if (AutomaticDecimals)
                {
                    for (int i = 0; ; i++)
                    {
                        if ((Convert.ToDouble(CopyList[YList.Count() - 1]) - Convert.ToDouble(CopyList[0])) * Math.Pow(10, i) >= 1)
                        {
                            DecimalPositions = i;
                            break;
                        }
                        if (i > 10)
                        {
                            //zu lang
                            i = 9;
                            break;
                        }
                    }
                }

                double BottomWhisker = Math.Round(Quantil(CopyList, WhiskerPercentile), DecimalPositions + 3);
                double FirstQuartil = Math.Round(Quantil(CopyList, 0.25), DecimalPositions + 3);
                double Median = Math.Round(Quantil(CopyList, 0.50), DecimalPositions + 3);
                double ThirdQuartil = Math.Round(Quantil(CopyList, 0.75), DecimalPositions + 3);
                double TopWhisker = Math.Round(Quantil(CopyList, 1 - WhiskerPercentile), DecimalPositions + 3);

                TopFirstQuartil.Text = "Erstes Quartil: " + FirstQuartil;
                TopMedian.Text = "Median: " + Median;
                TopThirdQuartil.Text = "Drittes Quartil: " + ThirdQuartil;
                TopMinimum.Text = "Minimum:" + Math.Round(Convert.ToDouble(CopyList[0]), DecimalPositions + 3);
                TopMaximum.Text = "Maximum:" + Math.Round(Convert.ToDouble(CopyList[YList.Count() - 1]), DecimalPositions + 3);

                //Y-Werte
                double YSum = 0;
                foreach (object E in CopyList)
                {
                    YSum += Convert.ToDouble(E);
                }
                double YArithmeticMiddle = YSum / CopyList.Count();
                TopArithmeticMiddle.Text = "Mittelwert:" + Math.Round(YArithmeticMiddle, DecimalPositions + 3);
                double YVarSum = 0;
                foreach (object E in CopyList)
                {
                   YVarSum += Math.Pow((Convert.ToDouble(E) - YArithmeticMiddle),2);
                }
                double YVariance = 1.0 / CopyList.Count() * YVarSum;
                double YStandardDeviation = Math.Sqrt(YVariance);
                double SEM = Math.Sqrt(YVariance) / Math.Sqrt(CopyList.Count());
                TopSEM.Text = "SEM:" + Math.Round(SEM, DecimalPositions + 3);

                //X-Werte
                double XSum = 0;
                foreach (object E in XList)
                {
                    XSum += ((DateTime)E).ToOADate();
                }
                double XArithmeticMiddle = XSum / XList.Count();
                double XVarSum = 0;
                foreach (object E in XList)
                {
                    XVarSum += Math.Pow((((DateTime)E).ToOADate() - XArithmeticMiddle), 2);
                }
                double XVariance = 1.0 / XList.Count() * XVarSum;
                double XStandardDeviation = Math.Sqrt(XVariance);

                //Funktion
                double CoVarSum = 0;
                for (int i = 0; i < XList.Count(); i++)
                {
                    CoVarSum += (((DateTime)XList[i]).ToOADate() - XArithmeticMiddle) * (Convert.ToDouble(YList[i]) - YArithmeticMiddle);
                }
                double Covariance = 1.0 / XList.Count() * CoVarSum;
                double Correlation = Math.Pow(Covariance / (XStandardDeviation * YStandardDeviation), 2);
                TopCorrelation.Text = "R^2:" + Math.Round(Correlation, 4);
                double b = Covariance / XVariance;
                double a = YArithmeticMiddle - (b * XArithmeticMiddle);
                TopFunction.Text = "Funktion:" + Math.Round(a, DecimalPositions + 3) + "+" + Math.Round(b, 4) + "x";

                chart1.Series.Add("Boxplot");
                chart1.Series["Boxplot"].ChartArea = "Boxplotarea";
                chart1.Series["Boxplot"].IsVisibleInLegend = false;
                chart1.Series["Boxplot"].Points.Add(new DataPoint(0, 0));
                chart1.Series["Boxplot"].BorderColor = Color.Black;

                chart1.Series["Boxplot"].ChartType = SeriesChartType.BoxPlot;

                chart1.Series["Boxplot"].Points[0]["BoxPlotSeries"] = (string)comboBox2.SelectedItem;

                chart1.Series["Boxplot"]["BoxPlotWhiskerPercentile"] = (WhiskerPercentile*100).ToString();
                chart1.Series["Boxplot"]["BoxPlotPercentile"] = "25";
                chart1.Series["Boxplot"]["BoxPlotShowAverage"] = "true";
                chart1.Series["Boxplot"]["BoxPlotShowMedian"] = "true";
                //chart1.Series["Boxplot"]["BoxPlotShowUnusualValues"] = "true";
                chart1.Series["Boxplot"]["MinPixelPointWidth"] = "20";
                chart1.Series["Boxplot"]["MaxPixelPointWidth"] = "20";
                chart1.Series["Boxplot"].ToolTip = string.Format("1.Whisker: {0}\n3.Quartil: {1}\nMedian: {2}\n1.Quartil: {3}\n2.Whisker: {4}", TopWhisker, ThirdQuartil, Median, FirstQuartil, BottomWhisker).Replace("\n", Environment.NewLine);

                chart1.ChartAreas[0].AxisX.Interval = 0;
                chart1.ChartAreas[0].AxisX.Minimum = XMin.ToOADate();
                chart1.ChartAreas[0].AxisX.Maximum = XMax.ToOADate();
                chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Days;

                //chart1.ChartAreas["Boxplotarea"].AxisY.Minimum = Math.Round(Quantil(CopyList, 0.05), 2);
                //chart1.ChartAreas["Boxplotarea"].AxisY.Maximum = Math.Round(Quantil(CopyList, 1-0.05), 2);
                chart1.ChartAreas[1].AxisY.Interval = 0;
                chart1.ChartAreas[1].AxisY.LabelStyle.Format = "{0.000}";

                chart1.Series.Add("Regressionsgerade");
                chart1.Series["Regressionsgerade"].ChartType = SeriesChartType.FastLine;
                chart1.Series["Regressionsgerade"].Points.AddXY(XMin, a + (b * ((DateTime)XMin).ToOADate()));
                chart1.Series["Regressionsgerade"].Points.AddXY(XMax, a + (b * ((DateTime)XMax).ToOADate()));
                //MessageBox.Show(chart1.Series[name].Points.Count.ToString());
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.ToString());
            }

        }

        private void DrawBottom()
        {
            //Regression-Chart
            chart2.Series.Clear();

            BottomSettings.OrigTitle = (string)comboBox4.SelectedItem;
            BottomSettings.OrigXAxis = timestampdict[(string)comboBox3.SelectedItem] + "[" + "Einheit" + "]";
            BottomSettings.OrigYAxis = (string)comboBox4.SelectedItem + "[" + "Einheit" + "]";

            try
            {
                CutSeries("Bottom", (string)comboBox4.SelectedItem, Table[(string)comboBox3.SelectedItem][timestampdict[(string)comboBox3.SelectedItem]], Table[(string)comboBox3.SelectedItem][(string)comboBox4.SelectedItem], 1);

                chart2.Series[(string)comboBox4.SelectedItem] = ChartDict["Bottom"];
                chart2.Series[(string)comboBox4.SelectedItem].ChartType = SeriesChartType.FastLine;

                if (BottomShowOutliers && Outdict.ContainsKey("Bottom"))
                {
                    chart2.Series.Add(Outdict["Bottom"]);
                    if (!BottomOutlierLegend) chart2.Series["Outlier"].IsVisibleInLegend = false;
                    else chart2.Series["Outlier"].IsVisibleInLegend = true;
                }

                chart2.ChartAreas[0].RecalculateAxesScale();

                List<object> XList = new List<object>();
                List<object> YList = new List<object>();

                foreach (DataPoint dp in chart2.Series[(string)comboBox4.SelectedItem].Points)
                {
                    XList.Add(DateTime.FromOADate(dp.XValue));
                    YList.Add(dp.YValues[0]);
                }

                List<object> CopyList = new List<object>(YList);

                CopyList.Sort();

                if (AutomaticDecimals)
                {
                    for (int i = 0; ; i++)
                    {
                        if ((Convert.ToDouble(CopyList[YList.Count() - 1]) - Convert.ToDouble(CopyList[0]) * Math.Pow(10, i)) >= 1)
                        {
                            DecimalPositions = i;
                            break;
                        }
                        if (i > 10)
                        {
                            //zu lang
                            i = 9;
                            break;
                        }
                    }
                }

                double BottomWhisker = Math.Round(Quantil(CopyList, WhiskerPercentile), DecimalPositions + 3);
                double FirstQuartil = Math.Round(Quantil(CopyList, 0.25), DecimalPositions + 3);
                double Median = Math.Round(Quantil(CopyList, 0.50), DecimalPositions + 3);
                double ThirdQuartil = Math.Round(Quantil(CopyList, 0.75), DecimalPositions + 3);
                double TopWhisker = Math.Round(Quantil(CopyList, 1 - WhiskerPercentile), DecimalPositions + 3);

                /*
                double BottomWhisker = Quantil(CopyList, WhiskerPercentile);
                double FirstQuartil = Quantil(CopyList, 0.25);
                double Median = Quantil(CopyList, 0.50);
                double ThirdQuartil = Quantil(CopyList, 0.75);
                double TopWhisker = Quantil(CopyList, 1 - WhiskerPercentile);
                */
                BottomFirstQuartil.Text = "Erstes Quartil: " + FirstQuartil;
                BottomMedian.Text = "Median: " + Median;
                BottomThirdQuartil.Text = "Drittes Quartil: " + ThirdQuartil;
                //CopyList.Sort();

                BottomMinimum.Text = "Minimum:" + Math.Round(Convert.ToDouble(CopyList[0]), DecimalPositions + 3);
                BottomMaximum.Text = "Maximum:" + Math.Round(Convert.ToDouble(CopyList[YList.Count() - 1]), DecimalPositions + 3);

                //Y-Werte
                double YSum = 0;
                foreach (object E in CopyList)
                {
                    YSum += Convert.ToDouble(E);
                }
                double YArithmeticMiddle = YSum / CopyList.Count();
                BottomArithmeticMiddle.Text = "Mittelwert:" + Math.Round(YArithmeticMiddle, DecimalPositions + 3);
                double YVarSum = 0;
                foreach (object E in CopyList)
                {
                    YVarSum += Math.Pow((Convert.ToDouble(E) - YArithmeticMiddle), 2);
                }
                double YVariance = 1.0 / CopyList.Count() * YVarSum;
                double YStandardDeviation = Math.Sqrt(YVariance);
                double SEM = Math.Sqrt(YVariance) / Math.Sqrt(CopyList.Count());
                BottomSEM.Text = "SEM:" + Math.Round(SEM, DecimalPositions + 3);

                //X-Werte
                double XSum = 0;
                foreach (object E in XList)
                {
                    XSum += ((DateTime)E).ToOADate();
                }
                double XArithmeticMiddle = XSum / XList.Count();
                double XVarSum = 0;
                foreach (object E in XList)
                {
                    XVarSum += Math.Pow((((DateTime)E).ToOADate() - XArithmeticMiddle), 2);
                }
                double XVariance = 1.0 / XList.Count() * XVarSum;
                double XStandardDeviation = Math.Sqrt(XVariance);

                //Funktion
                double CoVarSum = 0;
                for (int i = 0; i < XList.Count(); i++)
                {
                    CoVarSum += (((DateTime)XList[i]).ToOADate() - XArithmeticMiddle) * (Convert.ToDouble(YList[i]) - YArithmeticMiddle);
                }
                double Covariance = 1.0 / XList.Count() * CoVarSum;
                double Correlation = Math.Pow(Covariance / (XStandardDeviation * YStandardDeviation), 2);
                BottomCorrelation.Text = "R^2:" + Math.Round(Correlation, 4);
                double b = Covariance / XVariance;
                double a = YArithmeticMiddle - (b * XArithmeticMiddle);
                BottomFunction.Text = "Funktion:" + Math.Round(a, DecimalPositions + 3) + "+" + Math.Round(b, 4) + "x";

                chart2.Series.Add("Boxplot");
                chart2.Series["Boxplot"].ChartArea = "Boxplotarea";
                chart2.Series["Boxplot"].IsVisibleInLegend = false;
                chart2.Series["Boxplot"].Points.Add(new DataPoint(0, 0));
                chart2.Series["Boxplot"].BorderColor = Color.Black;

                chart2.Series["Boxplot"].ChartType = SeriesChartType.BoxPlot;

                chart2.Series["Boxplot"].Points[0]["BoxPlotSeries"] = (string)comboBox4.SelectedItem;

                chart2.Series["Boxplot"]["BoxPlotWhiskerPercentile"] = (WhiskerPercentile*100).ToString();
                chart2.Series["Boxplot"]["BoxPlotPercentile"] = "25";
                chart2.Series["Boxplot"]["BoxPlotShowAverage"] = "true";
                chart2.Series["Boxplot"]["BoxPlotShowMedian"] = "true";
                //chart2.Series["Boxplot"]["BoxPlotShowUnusualValues"] = "true";
                chart2.Series["Boxplot"]["MinPixelPointWidth"] = "20";
                chart2.Series["Boxplot"]["MaxPixelPointWidth"] = "20";
                chart2.Series["Boxplot"].ToolTip = string.Format("1.Whisker: {0}\n3.Quartil: {1}\nMedian: {2}\n1.Quartil: {3}\n2.Whisker: {4}", TopWhisker, ThirdQuartil, Median, FirstQuartil, BottomWhisker).Replace("\n", Environment.NewLine);

                chart2.ChartAreas[0].AxisX.Interval = 0;
                chart2.ChartAreas[0].AxisX.Minimum = XMin.ToOADate();
                chart2.ChartAreas[0].AxisX.Maximum = XMax.ToOADate();
                chart2.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Days;

                //chart2.ChartAreas["Boxplotarea"].AxisY.Minimum = Math.Round(Quantil(CopyList, 0.05), 2);
                //chart2.ChartAreas["Boxplotarea"].AxisY.Maximum = Math.Round(Quantil(CopyList, 1 - 0.05), 2);
                chart2.ChartAreas[1].AxisY.Interval = 0;
                chart2.ChartAreas[1].AxisY.LabelStyle.Format = "{0.00}";

                chart2.Series.Add("Regressionsgerade");
                chart2.Series["Regressionsgerade"].ChartType = SeriesChartType.FastLine;
                chart2.Series["Regressionsgerade"].Points.AddXY(XMin, a + (b * ((DateTime)XMin).ToOADate()));
                chart2.Series["Regressionsgerade"].Points.AddXY(XMax, a + (b * ((DateTime)XMax).ToOADate()));
                //MessageBox.Show(chart2.Series[name].Points.Count.ToString());
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.ToString());
            }

        }

        private void DrawRight()
        {
            chart3.Series.Clear();

            int UsedPointsChart1 = 0;
            int AllPointsChart1 = 0;
            int UsedPointsChart2 = 0;
            int AllPointsChart2 = 0;
            int ComparisonPoints = 0;

            RightSettings.OrigTitle = "Korrelation " + (string)comboBox2.SelectedItem + "/" + (string)comboBox4.SelectedItem;
            RightSettings.OrigXAxis = (string)comboBox4.SelectedItem + "[" + "Einheit" + "]";
            RightSettings.OrigYAxis = (string)comboBox2.SelectedItem + "[" + "Einheit" + "]";

            string name = "Korrelationspunkte";
            chart3.Series.Add(name);
            chart3.Series[name].ChartType = SeriesChartType.Point;

            //           List<object> Y1 = Table[(string)comboBox1.SelectedItem][(string)comboBox2.SelectedItem];
            //           List<object> X1 = Table[(string)comboBox1.SelectedItem][timestampdict[(string)comboBox1.SelectedItem]];

            List<object> Y1 = new List<object>();
            List<object> X1 = new List<object>();
            foreach (DataPoint dp in chart1.Series[0].Points)
            {
                X1.Add(dp.XValue);
                Y1.Add(dp.YValues[0]);
            }

            //            List<object> Y2 = Table[(string)comboBox3.SelectedItem][(string)comboBox4.SelectedItem];
            //            List<object> X2 = Table[(string)comboBox3.SelectedItem][timestampdict[(string)comboBox3.SelectedItem]];

            List<object> Y2 = new List<object>();
            List<object> X2 = new List<object>();
            foreach (DataPoint dp in chart2.Series[0].Points)
            {
                X2.Add(dp.XValue);
                Y2.Add(dp.YValues[0]);
            }

            AllPointsChart1 = X1.Count();
            AllPointsChart2 = X2.Count();

            Series ShortSeries;

            try
            {
                ShortSeries = new Series();
                //                Series Original = new Series();

                if (Y1.Count() != Y2.Count())
                {
                    int x1 = 0;
                    int x2 = 0;
                    int x1Length = X1.Count()-1;
                    int x2Length = X2.Count()-1;

                    //ZURECHTSCHNEIDEN
                    if ((double)X1[0] < (double)X2[0])
                    {
                        while ((double)X1[x1] < (double)X2[0])
                        {
                            x1++;
                        }
                    }

                    else if ((double)X2[0] < (double)X1[0])
                    {
                        while ((double)X2[x2] < (double)X1[0])
                        {
                            x2++;
                        }
                    }

                    if ((double)X1[X1.Count() - 1] > (double)X2[X2.Count() - 1])
                    {
                        while ((double)X1[x1Length] > (double)X2[X2.Count() - 1])
                        {
                            x1Length--;
                        }
                    }

                    else if ((double)X2[X2.Count() - 1] > (double)X1[X1.Count() - 1])
                    {
                        while ((double)X2[x2Length] > (double)X1[X1.Count() - 1])
                        {
                            x2Length--;
                        }
                    }

                    //FITTEN

                    if (x1Length + 1 - x1 < x2Length + 1 - x2)
                    {
                        if (x2 != 0) x1++;
                        if (x1Length != X1.Count() - 1) x1Length++;
                        Y1 = Y1.GetRange(x1, x1Length + 1 - x1);
                        Y2 = Y2.GetRange(x2, x2Length + 1 - x2);
                        X1 = X1.GetRange(x1, x1Length + 1 - x1);
                        X2 = X2.GetRange(x2, x2Length + 1 - x2);

                        UsedPointsChart1 = X1.Count();
                        UsedPointsChart2 = X2.Count();

                        List<object> FittedY2 = new List<object>();
                        List<int> DelList = new List<int>();
                        x2 = 0;
                        foreach (object x in X1)
                        {
                            double YVal = 0;
                            int j = 0;
                            while (x2 < X2.Count() && (double)X2[x2] <= (double)x)
                            {
                                YVal += (double)Y2[x2];
                                x2++;
                                j++;
                            }
                            if (YVal == 0 && j == 0)
                            {
                                DelList.Add(X2.IndexOf(x));
                                continue;
                            }
                            FittedY2.Add(YVal / j);
                        }
                        for (int i = DelList.Count() - 1; i >= 0; i--) Y1.RemoveAt(DelList[i]);
                        Y2 = FittedY2;
                    }

                    else if (x2Length + 1 - x2 < x1Length + 1 - x1)
                    {
                        if (x1 != 0) x2++;
                        if (x2Length != X2.Count() - 1) x2Length++;
                        Y1 = Y1.GetRange(x1, x1Length + 1 - x1);
                        Y2 = Y2.GetRange(x2, x2Length + 1 - x2);
                        X1 = X1.GetRange(x1, x1Length + 1 - x1);
                        X2 = X2.GetRange(x2, x2Length + 1 - x2);

                        UsedPointsChart1 = X1.Count();
                        UsedPointsChart2 = X2.Count();

                        List<object> FittedY1 = new List<object>();
                        List<int> DelList = new List<int>();
                        x1 = 0;
                        foreach (object x in X2)
                        {
                            double YVal = 0;
                            int j = 0;
                            while (x1 < X1.Count()-1 && (double)X1[x1] <= (double)x)
                            {
                                YVal += (double)Y1[x1];
                                x1++;
                                j++;
                            }
                            if (YVal == 0 && j == 0)
                            {
                                DelList.Add(X2.IndexOf(x));
                                continue;
                            }
                            FittedY1.Add(YVal / j);
                        }
                        for (int i = DelList.Count() - 1; i >= 0; i--) Y2.RemoveAt(DelList[i]);
                        Y1 = FittedY1;
                    }



                    /*
                    if ((double)X1[0]<(double)X2[0])
                    {
                        while((double)X1[x1]<(double)X2[0])
                        {
                            x1++;
                        }
                        if (((double)X2[1] - (double)X2[0]) > ((double)X1[1] - (double)X1[0])) x2++;
                    }

                    else if ((double)X2[0] < (double)X1[0])
                    {
                        while ((double)X2[x2] < (double)X1[0])
                        {
                            x2++;
                        }
                        if (((double)X1[1] - (double)X1[0]) > ((double)X2[1] - (double)X2[0])) x1++;
                    }

                    if ((double)X1[X1.Count()-1] < (double)X2[X2.Count() - 1])
                    {
                        while ((double)X1[x1Length] < (double)X2[X2.Count() - 1 ])
                        {
                            x1++;
                        }
                    }

                    if (((double)X1[1] - (double)X1[0]) > ((double)X2[1] - (double)X2[0]))
                    {
                        List<object> FittedY2 = new List<object>();
                        foreach (object x in X1)
                        {
                            double Y2Val = 0;
                            int j = 0;
                            while (x2 < X2.Count() && (double)X2[x2] <= (double)x)
                            {
                                Y2Val += (double)Y2[x2];
                                x2++;
                                j++;
                            }
                            if (Y2Val == 0 || j == 0) continue; //KLÄREN!
                            FittedY2.Add(Y2Val / j);
                            if (x2 == X2.Count()) break;
                            x1++;
                        }
                        Y2 = FittedY2;
                        Y1 = Y1.GetRange(0, x1);
                    }
                    else
                    {
                        List<object> FittedY1 = new List<object>();
                        foreach (object x in X2)
                        {
                            double Y1Val = 0;
                            int j = 0;
                            while (x1 < X1.Count() && (double)X1[x1] <= (double)x)
                            {
                                Y1Val += (double)Y1[x1];
                                x1++;
                                j++;
                            }
                            if (Y1Val == 0 || j == 0) continue; //KLÄREN!
                            FittedY1.Add(Y1Val / j);
                            if (x1 == X1.Count()) break;
                            x2++;
                        }
                        Y1 = FittedY1;
                        Y2 = Y2.GetRange(0, x1);
                    }
                    */
                }
                else
                {
                    UsedPointsChart1 = X1.Count();
                    UsedPointsChart2 = X2.Count();
                }


                for (int i = 0; i < Y2.Count(); i += maxRes)
                {
                    ShortSeries.Points.DataBindXY(Y2.GetRange(i, Math.Min(maxRes, Y2.Count - i)), Y1.GetRange(i, Math.Min(maxRes, Y1.Count - i)));
                    foreach (DataPoint dp in ShortSeries.Points) chart3.Series[name].Points.Add(dp);
                }

                ComparisonPoints = chart3.Series[name].Points.Count();

                List<object> Copy1 = new List<object>(Y1);
                List<object> Copy2 = new List<object>(Y2);

                Copy1.Sort();
                Copy2.Sort();

                double A;
                A = Convert.ToDouble(Copy1[0]);
                double Min1 = A - 0.1 * A;
                A = Convert.ToDouble(Copy1[Copy1.Count - 1]);
                double Max1 = A + 0.1 * A;
                A = Convert.ToDouble(Copy2[0]);
                double Min2 = A - 0.1 * A;
                A = Convert.ToDouble(Copy2[Copy2.Count - 1]);
                double Max2 = A + 0.1 * A;
                /*
                //BAUSTELLE
                bool UseLimit1 = true;
                bool UseLimit2 = true;

                if (UseLimit1 || UseLimit2)
                {
                    double MinLimit1 = Quantil(Copy1, 0.05);
                    double MaxLimit1 = Quantil(Copy1, 1 - 0.05);
                    double MinLimit2 = Quantil(Copy2, 0.05);
                    double MaxLimit2 = Quantil(Copy2, 1 - 0.05);

                    chart3.Series.Add("Top-Outlier");
                    chart3.Series.Add("Bottom-Outlier");
                    chart3.Series.Add("Both-Outlier");


                    foreach (DataPoint dp in Original.Points)
                    {
                        if (UseLimit1 && (Convert.ToDouble(dp.YValues[0]) <= MinLimit1 || Convert.ToDouble(dp.YValues[0]) >= MaxLimit1))
                        {
                            if (UseLimit2 && (Convert.ToDouble(dp.XValue) <= MinLimit2 || Convert.ToDouble(dp.XValue) >= MaxLimit2))
                            {
                                chart3.Series["Both-Outlier"].Points.Add(dp);
                            }
                            else chart3.Series["Top-Outlier"].Points.Add(dp);
                        }
                        else if (UseLimit2 && (Convert.ToDouble(dp.XValue) <= MinLimit2 || Convert.ToDouble(dp.XValue) >= MaxLimit2))
                        {
                            chart3.Series["Bottom-Outlier"].Points.Add(dp);
                        }
                        else chart3.Series[name].Points.Add(dp);
                    }
                }

                else
                {
                    chart3.Series[name] = Original;
                }

                foreach (Series S in chart3.Series)
                {
                    if (S.Points.Count() == 0) chart3.Series.Remove(S);
                    else S.ChartType = SeriesChartType.Point;
                }


                //BAUSTELLE ENDE
                */

                if (AutomaticDecimals)
                {
                    for (int i = 0; ; i++)
                    {
                        if ((Convert.ToDouble(Copy1[Y1.Count() - 1]) - Convert.ToDouble(Copy1[0]) * Math.Pow(10, i)) >= 1)
                        {
                            DecimalPositions = i;
                            break;
                        }
                        if (i > 10)
                        {
                            //zu lang
                            i = 9;
                            break;
                        }
                    }
                }

                //Top-Werte
                double Sum1 = 0;
                Copy1 = new List<object>();
                Copy2 = new List<object>();
                foreach (DataPoint dp in chart3.Series[name].Points)
                {
                    Copy1.Add(dp.YValues[0]);
                    Copy2.Add(dp.XValue);
                }
                foreach (double E in Copy1)
                {
                    Sum1 += E;
                }
                double ArithmeticMiddle1 = Sum1 / Copy1.Count();
                double VarSum1 = 0;
                foreach (object E in Copy1)
                {
                    VarSum1 += Math.Pow((Convert.ToDouble(E) - ArithmeticMiddle1), 2);
                }
                double Variance1 = 1.0 / Copy1.Count() * VarSum1;
                double StandardDeviation1 = Math.Sqrt(Variance1);

                //Bottom-Werte
                double Sum2 = 0;
                foreach (object E in Copy2)
                {
                    Sum2 += Convert.ToDouble(E);
                }
                double ArithmeticMiddle2 = Sum2 / Copy2.Count();
                double VarSum2 = 0;
                foreach (object E in Copy2)
                {
                    VarSum2 += Math.Pow((Convert.ToDouble(E) - ArithmeticMiddle2), 2);
                }
                double Variance2 = 1.0 / Copy2.Count() * VarSum2;
                double StandardDeviation2 = Math.Sqrt(Variance2);

                //Funktion
                double CoVarSum = 0;
                for (int i = 0; i < Y2.Count(); i++)
                {
                    CoVarSum += ((Convert.ToDouble(Y2[i]) - ArithmeticMiddle2) * (Convert.ToDouble(Y1[i]) - ArithmeticMiddle1));
                }
                double Covariance = 1.0 / Y2.Count() * CoVarSum;
                double Correlation = Math.Pow(Covariance / (StandardDeviation2 * StandardDeviation1), 2);
                RightCorrelation.Text = "R^2:" + Math.Round(Correlation, 4);
                double b = Covariance / Variance2;
                double a = ArithmeticMiddle1 - (b * ArithmeticMiddle2);
                RightFunction.Text = "Funktion:" + Math.Round(a, DecimalPositions + 3) + "+" + Math.Round(b, 4) + "x";

                chart3.ChartAreas[0].AxisX.Interval = 0;
                chart3.ChartAreas[0].AxisX.Minimum = Min2;
                chart3.ChartAreas[0].AxisX.Maximum = Max2;
                chart3.ChartAreas[0].AxisY.Minimum = Min1;
                //chart3.ChartAreas[0].AxisY.Maximum = Max1;
                
                chart3.Series.Add("Regressionsgerade");
                chart3.Series["Regressionsgerade"].ChartType = SeriesChartType.FastLine;
                chart3.Series["Regressionsgerade"].Points.AddXY(Min2, a + (b * Min2));
                chart3.Series["Regressionsgerade"].Points.AddXY(Max2, a + (b * Max2));
                chart3.ChartAreas[0].AxisX.LabelStyle.Format = "{0.000}";

                RightComparison.Text = "Für den Vergleich wurden" + Environment.NewLine + UsedPointsChart1 + "/" + AllPointsChart1 + " Punkte aus Chart 1 und" + Environment.NewLine + UsedPointsChart2 + "/" + AllPointsChart2 + " Punkte aus Chart 2 genutzt um" + Environment.NewLine + ComparisonPoints + " Vergleichspunkte zu erstellen.";
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.ToString());
            }

        }

        /*
                private Series FitSeries(List<object> XValues, List<object> YValues, int ValuesperPoint)
                {
                    Series FittedSeries = new Series();
                    List<object> _xvals = new List<object>();
                    List<object> _yvals = new List<object>();
                    if (XValues.Count() < maxRes)
                    {
                        FittedSeries.Points.DataBindXY(XValues, YValues);
                        FittedSeries.ChartType = SeriesChartType.Line;
                        return FittedSeries;
                    }
                    else
                    {
                        int JoinedValues = (int)Math.Ceiling((double)XValues.Count() / maxRes);
                        JoinedValues = Math.Max(JoinedValues, ValuesperPoint);
                        List<object> YList = new List<object>();
                        for (int i = 0; i < XValues.Count(); i++)
                        {
                            //                    int xv = 0;
                            double XVal = 0;
                            for (int j = 0; j < JoinedValues && i < XValues.Count(); j++)
                            {
                                XVal += ((DateTime)XValues[i]).ToTimestamp() / JoinedValues;
                                // XVal += ((DateTime)XValues[i]).Ticks/JoinedValues;
                                YList.Add(YValues[i]);
                                i++;
                            }
                            _xvals.Add(((int)XVal).ToDateTime());
                            if (YValues[0].GetType() == typeof(double) || YValues[0].GetType() == typeof(int) || YValues[0].GetType() == typeof(float))
                            {
                                double YVal = 0;
                                foreach (object Value in YList)
                                {
                                    YVal += System.Convert.ToDouble(Value) / JoinedValues;
                                }
                                YList.Clear();
                                // FittedSeries.Points.Add(new DataPoint(XVal, YVal));
                                _yvals.Add(YVal);
                            }
                            else
                            {
                                var YVal = YList[0];
                                //    FittedSeries.Points.Add(new DataPoint(XVal, YVal));
                                _yvals.Add(YVal);
                            }
                        }
                        if ((DateTime)_xvals[_xvals.Count() - 1] < (DateTime)_xvals[_xvals.Count() - 2])
                        {
                            _xvals.RemoveAt(_xvals.Count() - 1);
                            _yvals.RemoveAt(_yvals.Count() - 1);
                        }
                        FittedSeries.Points.DataBindXY(_xvals, _yvals);
                        FittedSeries.ChartType = SeriesChartType.Line;
                        return FittedSeries;
                    }
                }
        */
        private Series FitSeries(List<object> XValues, List<object> YValues, int ValuesperPoint)
        {
            Series ShortSeries = new Series();
            Series LongSeries = new Series();
            LongSeries.XValueType = ChartValueType.DateTime;
            LongSeries.ChartType = SeriesChartType.Line;
            Series FittedSeries = new Series();
            FittedSeries.XValueType = ChartValueType.DateTime;
            FittedSeries.ChartType = SeriesChartType.Line;
            for (int i = 0; i < YValues.Count(); i += maxRes)
            {
                ShortSeries.Points.DataBindXY(XValues.GetRange(i, Math.Min(maxRes, XValues.Count - i)), YValues.GetRange(i, Math.Min(maxRes, YValues.Count - i)));
                foreach (DataPoint dp in ShortSeries.Points) LongSeries.Points.Add(dp);
                //MessageBox.Show(chart1.Series[name].Points.Count.ToString());
            }
            if (ValuesperPoint != 1)
            {
                for (int i = 0; i < XValues.Count();)
                {
                    double XVal = 0;
                    double YVal = 0;
                    int j = 0;
                    for (; j < ValuesperPoint && i < XValues.Count(); j++)
                    {
                        XVal += LongSeries.Points[i].XValue;
                        YVal += LongSeries.Points[i].YValues[0];
                        i++;
                    }
                    FittedSeries.Points.AddXY(XVal / j, YVal / j);
                }
                return FittedSeries;
            }
            else return (FittedSeries = LongSeries);
        }

        private void CutSeries(string chartname, string SeriesKey, List<object> XValues, List<object> YValues, int ValuesperPoint)
        {
            Series Original = Seriesdict[SeriesKey];
            Series OutSeries = new Series();
            Series ShortSeries = new Series();

            if (UseDict[chartname][1] || UseDict[chartname][0])
            {
                foreach (DataPoint dp in Original.Points)
                {
                    if (UseDict[chartname][0] && Convert.ToDouble(dp.YValues[0]) <= MinMaxDict[chartname][0])
                    {
                        OutSeries.Points.Add(dp);
                    }
                    else if (UseDict[chartname][1] && Convert.ToDouble(dp.YValues[0]) >= MinMaxDict[chartname][1])
                    {
                        OutSeries.Points.Add(dp);
                    }
                    else ShortSeries.Points.Add(dp);
                }
                Outdict[chartname] = OutSeries;
                Outdict[chartname].Name = "Outlier";
                Outdict[chartname].XValueType = ChartValueType.DateTime;
                Outdict[chartname].ChartType = SeriesChartType.Point;
                ChartDict[chartname] = ShortSeries;
                ChartDict[chartname].Name = SeriesKey;
                ChartDict[chartname].XValueType = ChartValueType.DateTime;
                ChartDict[chartname].ChartType = SeriesChartType.Line;
            }

            else
            {
                ChartDict[chartname] = Original;
                ChartDict[chartname].Name = SeriesKey;
                Outdict[chartname] = OutSeries;
                Outdict[chartname].Name = "Outlier";
                Outdict[chartname].XValueType = ChartValueType.DateTime;
                Outdict[chartname].ChartType = SeriesChartType.Point;
            }
        }


        //COMBOBOX-STEUERUNG

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (button1.Text == "-")
            {
                comboBox2.Items.Clear();
                comboBox2.Items.AddRange(ComboStructure[(string)comboBox1.SelectedItem].ToArray());
                if (comboBox4.SelectedItem != null) comboBox2.Items.Remove(comboBox4.SelectedItem);
                //comboBox2.SelectedIndex = 0;
                if (comboBox2.SelectedItem != null) comboBox4.Items.Remove(comboBox2.SelectedItem);
            }
            else
            {
                comboBox2.Items.Clear();
                comboBox2.Items.AddRange(ComboStructure[(string)comboBox1.SelectedItem].ToArray());
                comboBox2.SelectedIndex = 0;
            }
            string CurrentChart = (string)chart1.Series[0].Name;
            comboBox2.Items.Add(CurrentChart);
            comboBox2.Text = CurrentChart;
        }

        private void comboBox2_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == comboBox3.SelectedItem)
            {
                var Memory = comboBox4.SelectedItem;
                comboBox4.Items.Clear();
                comboBox4.Items.AddRange(ComboStructure[(string)comboBox3.SelectedItem].ToArray());
                comboBox4.Items.Remove(comboBox2.SelectedItem);
                comboBox4.SelectedItem = Memory;
                if (comboBox4.SelectedItem == null) comboBox4.SelectedIndex = 0;
            }
            DrawTop();
            if (button1.Text == "-") DrawRight();
        }

        private void comboBox3_SelectionChangeCommitted(object sender, EventArgs e)
        {
            comboBox4.Items.Clear();
            comboBox4.Items.AddRange(ComboStructure[(string)comboBox3.SelectedItem].ToArray());
            if (comboBox2.SelectedItem != null) comboBox4.Items.Remove(comboBox2.SelectedItem);
            //comboBox4.SelectedIndex = 0;
            if (comboBox4.SelectedItem != null)comboBox2.Items.Remove(comboBox4.SelectedItem);
            string CurrentChart = (string)chart2.Series[0].Name;
            comboBox4.Items.Add(CurrentChart);
            comboBox4.Text = CurrentChart;
        }

        private void comboBox4_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == comboBox3.SelectedItem)
            {
                var Memory = comboBox2.SelectedItem;
                comboBox2.Items.Clear();
                comboBox2.Items.AddRange(ComboStructure[(string)comboBox1.SelectedItem].ToArray());
                comboBox2.Items.Remove(comboBox4.SelectedItem);
                comboBox2.SelectedItem = Memory;
            }
            DrawBottom();
            if (button1.Text == "-") DrawRight();
        }

        private void comboBox2_DropDown(object sender, EventArgs e)
        {
            string CurrentChart = (string)chart1.Series[0].Name;
            if (comboBox2.Items.Contains(CurrentChart) && !ComboStructure[(string)comboBox1.SelectedItem].Contains(CurrentChart))
                comboBox2.Items.Remove(CurrentChart);
            else if (comboBox2.Items.Count > ComboStructure[(string)comboBox1.SelectedItem].Count || (string)comboBox2.Items[comboBox2.Items.Count - 1] == CurrentChart && button1.Text == "-" && comboBox2.Items.Count == ComboStructure[(string)comboBox1.SelectedItem].Count)
                comboBox2.Items.RemoveAt(comboBox2.Items.Count - 1);
        }

        private void comboBox2_DropDownClosed(object sender, EventArgs e)
        {
            string CurrentChart = (string)chart1.Series[0].Name;
            if (!comboBox2.Items.Contains(CurrentChart))
            {
                comboBox2.Items.Add(CurrentChart);
            }
            comboBox2.Text = CurrentChart;
        }

        private void comboBox4_DropDown(object sender, EventArgs e)
        {
            string CurrentChart = (string)chart2.Series[0].Name;
            if (comboBox4.Items.Contains(CurrentChart) && !ComboStructure[(string)comboBox3.SelectedItem].Contains(CurrentChart))
                comboBox4.Items.Remove(CurrentChart);
            else if ((string)comboBox4.Items[comboBox4.Items.Count - 1] == CurrentChart && comboBox4.Items.Count == ComboStructure[(string)comboBox3.SelectedItem].Count)
                comboBox4.Items.RemoveAt(comboBox4.Items.Count - 1);
        }

        private void comboBox4_DropDownClosed(object sender, EventArgs e)
        {
            string CurrentChart = (string)chart2.Series[0].Name;
            if (!comboBox4.Items.Contains(CurrentChart))
            {
                comboBox4.Items.Add(CurrentChart);
            }
            comboBox4.Text = CurrentChart;
        }

        //MATHEMATIK

        private double Quantil(List<object> list, double p)
        {
            int n = list.Count();
            list.Sort();
            if ((n * p) % 1 == 0)
            {
                return (Convert.ToDouble(list[(int)(n * p)]) + Convert.ToDouble(list[(int)(n * p) + 1])) / 2;
            }
            return Convert.ToDouble(list[(int)Math.Ceiling(n * p)]);
        }
    }
}
