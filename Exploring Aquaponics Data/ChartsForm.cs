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
    public partial class ChartsForm : Form
    {
        Dictionary<string, Dictionary<string, List<Object>>> Table;
        Dictionary<string, Dictionary<string, object>> Info;
        Dictionary<string, string> timestampdict;
        Dictionary<string, string> IDdict;
        Dictionary<string, Series> Seriesdict = new Dictionary<string, Series>();
        Dictionary<string, Series> Outdict = new Dictionary<string, Series>();
        DateTime ChartStart;
        DateTime ChartEnd;
        DateTime ChartStartOld;
        DateTime ChartEndOld;
        DateTime XMin;
        DateTime XMax;
        Point? prevPosition = null;
        ToolTip tooltip = new ToolTip();
        int maxRes = 5000;

        ChartsSettingsForm ChartSettings = new ChartsSettingsForm();

        double ChartMin = 0;
        double ChartMax = 0;
        bool UseMin = false;
        bool UseMax = false;
        bool ShowOutliers = true;
        bool OutlierLegend = false;



        public ChartsForm(Dictionary<string, Dictionary<string, List<Object>>> data, Dictionary<string, Dictionary<string, object>> info, Dictionary<string, string> Timestamps, Dictionary<string, string> IDs)
        {
            InitializeComponent();
            Table = data;
            Info = info;
            timestampdict = Timestamps;
            IDdict = IDs;
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            this.Width = 1006;

            foreach (string tablekey in Table.Keys)
            {
                if (!timestampdict.ContainsKey(tablekey) && !IDdict.ContainsKey(tablekey))
                {
                    MessageBox.Show("Tabelle " + tablekey + " besitzt keine auswertbare X-Achse und wird geschlossen.");
                    this.Close();
                }
                TreeNode node = treeView1.Nodes.Add(tablekey, tablekey);
                foreach (string key in Table[tablekey].Keys)
                {
                    node.Nodes.Add(key, key);
                    if ((Type)Info[tablekey][key] == typeof(System.Boolean) || (Type)Info[tablekey][key] == typeof(System.String) || (Type)Info[tablekey][key] == typeof(DBNull))
                    {
                        treeView1.Nodes[tablekey].Nodes[key].Remove();
                        //treeView1.Nodes[tablekey].Nodes[key].ForeColor = Color.Red;
                        comboBox1.Items.Add(key);
                        continue;
                    }
                    if (timestampdict.ContainsKey(tablekey) && key == timestampdict[tablekey])
                    {
                        treeView1.Nodes[tablekey].Nodes[key].ForeColor = SystemColors.GrayText;
                        numericUpDown1.Minimum = 1;
                        numericUpDown1.Maximum = Table[tablekey][key].Count();
                        XMin = ((DateTime)Table[tablekey][key][0]);
                        XMax = ((DateTime)Table[tablekey][key][Table[tablekey][key].Count() - 1]);
                        ChartStart = XMin.Date;
                        ChartStartOld = XMin.Date;
                        ChartEnd = XMax.Date;
                        ChartEndOld = XMax.Date;
                        textBox1.Text = XMin.ToShortDateString();
                        textBox2.Text = XMax.ToShortDateString();

                        if (Table[tablekey][key].Count() > maxRes)
                        {
                            numericUpDown1.Enabled = true;
                            button3.Enabled = true;
                        }
                    }
                    else if (timestampdict.ContainsKey(tablekey))
                    {
                        //if (key.Contains("ID")) treeView1.Nodes[tablekey].Nodes[key].Remove();
                        //Series ser1 = new Series(key, Table[tablekey][key].Count);
                        /*
                        var list1 = Table[tablekey][timestampdict[tablekey]];
                        var list2 = Table[tablekey][key];
                        object[] a = new object[list1.Count / 8];
                        object[] b = new object[list2.Count / 8];
                        Array.Copy(list1.ToArray(), 0, a, 0, a.Length);
                        Array.Copy(list2.ToArray(), 0, b, 0, b.Length);
                        */
                        try
                        {
                            //ser1.Points.DataBindXY(a, b);/Table[tablekey][timestampdict[tablekey]], Table[tablekey][key]);
                            CutSeries(key, Table[tablekey][timestampdict[tablekey]], Table[tablekey][key], 1);
                            //Series ser1 = FitSeries(Table[tablekey][timestampdict[tablekey]], Table[tablekey][key], 1);
                            //Seriesdict[key] = ser1;
                        }
                        catch (Exception ex)
                        {
                            treeView1.Nodes[tablekey].Nodes[key].Remove();
                            MessageBox.Show(ex.ToString());
                        }
                    }

                    else if (IDdict.ContainsKey(tablekey) && key == IDdict[tablekey])
                    {
                        treeView1.Nodes[tablekey].Nodes[key].ForeColor = Color.Orange;
                        numericUpDown1.Minimum = 1;
                        numericUpDown1.Maximum = Table[tablekey][key].Count();
                        textBox1.Enabled = false;
                        textBox2.Enabled = false;
                        button3.Enabled = false;
                    }

                    else if (IDdict.ContainsKey(tablekey))
                    {
                        /*
                        var list1 = Table[tablekey][timestampdict[tablekey]];
                        var list2 = Table[tablekey][key];
                        object[] a = new object[list1.Count / 8];
                        object[] b = new object[list2.Count / 8];
                        Array.Copy(list1.ToArray(), 0, a, 0, a.Length);
                        Array.Copy(list2.ToArray(), 0, b, 0, b.Length);
                        */
                        //
                        try
                        {
                            Series ser1 = new Series(key, Table[tablekey][key].Count);
                            ser1.Points.DataBindXY(Table[tablekey][IDdict[tablekey]], Table[tablekey][key]);
                            ser1.ChartType = SeriesChartType.Column;
                            Seriesdict[key] = ser1;
                        }
                        catch (Exception ex)
                        {
                            treeView1.Nodes[tablekey].Nodes[key].Remove();
                            MessageBox.Show(ex.ToString());
                        }
                    }
                }
            }
            treeView1.ExpandAll();
            if (comboBox1.Items.Count == 0) button2.Enabled = false;
            else comboBox1.SelectedIndex = 0;
        }

        //BUTTONS

        private void button1_Click(object sender, EventArgs e)
        {
            chart1.SaveImage(Application.StartupPath + @"\VisevData\Graph.emf", ChartImageFormat.EmfDual);
            MessageBox.Show("Graph gespeichert.");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.Size.Width == 1006) this.Width = 1285;
            else this.Width = 1006;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (DateTime.TryParse(textBox1.Text, out ChartStart))
            {
                if (ChartStart < XMin.Date)
                {
                    ChartStart = XMin.Date;
                }
            }
            else MessageBox.Show("Bitte korrektes Datum eingeben.");

            if (DateTime.TryParse(textBox2.Text, out ChartEnd))
            {
                if (ChartEnd > XMax.Date)
                {
                    ChartEnd = XMax.Date;
                }
            }
            else MessageBox.Show("Bitte korrektes Datum eingeben.");

            if (ChartStart > ChartEnd)
            {
                ChartStart = XMin.Date;
                ChartEnd = XMax.Date;
            }
            textBox1.Text = ChartStart.ToShortDateString();
            textBox2.Text = ChartEnd.ToShortDateString();

            foreach (string tablekey in Table.Keys)
            {
                foreach (string key in Table[tablekey].Keys)
                {
                    if ((Type)Info[tablekey][key] == typeof(System.Boolean) || (Type)Info[tablekey][key] == typeof(System.String) || (Type)Info[tablekey][key] == typeof(DBNull))
                    {
                        continue;
                    }
                    if (timestampdict.ContainsKey(tablekey) && key == timestampdict[tablekey])
                    {
                        continue;
                    }
                    else if (timestampdict.ContainsKey(tablekey))
                    {
                        try
                        {
                            List<Object>[] ShortList = ListBetween(Table[tablekey][timestampdict[tablekey]], Table[tablekey][key], ChartStart, ChartEnd);
                            List<Object> ShortListX = ShortList[0];
                            List<Object> ShortListY = ShortList[1];
                            numericUpDown1.Minimum = 1;
                            if (ChartStart != ChartStartOld.Date || ChartEnd != ChartEndOld.Date) numericUpDown1.Value = numericUpDown1.Minimum;
                            numericUpDown1.Maximum = ShortListX.Count();
                            CutSeries(key, ShortListX, ShortListY, (int)numericUpDown1.Value);
                            //Series ser1 = FitSeries(ShortListX, ShortListY, (int)numericUpDown1.Value);
                            //Seriesdict[key] = ser1;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                    }
                }
            }
            ChartStartOld = ChartStart;
            ChartEndOld = ChartEnd;


            DrawChart();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataGridView();
        }

        private void FillDataGridView()
        {
            string key = (string)comboBox1.SelectedItem;
            dataGridView1.Rows.Clear();

            if (timestampdict.ContainsKey(this.Text) || IDdict.ContainsKey(this.Text))
            {
                dataGridView1.ColumnCount = 2;
                if (timestampdict.ContainsKey(this.Text))
                {
                    dataGridView1.Columns[0].HeaderText = timestampdict[this.Text];
                    dataGridView1.Columns[1].HeaderText = key;
                    for (int i = 0; i < Table[this.Text][key].Count(); i++)
                    {
                        dataGridView1.Rows.Add(Table[this.Text][timestampdict[this.Text]][i], Table[this.Text][key][i]);
                    }
                }
                else if (IDdict.ContainsKey(this.Text))
                {
                    dataGridView1.Columns[0].HeaderText = IDdict[this.Text];
                    dataGridView1.Columns[1].HeaderText = key;
                    for (int i = 0; i < Table[this.Text][key].Count(); i++)
                    {
                        dataGridView1.Rows.Add(Table[this.Text][IDdict[this.Text]][i], Table[this.Text][key][i]);
                    }
                }
            }
            else
            {
                dataGridView1.ColumnCount = 1;
                dataGridView1.Columns[0].HeaderText = key;
                for (int i = 0; i < Table[this.Text][key].Count(); i++)
                {
                    dataGridView1.Rows.Add(Table[this.Text][key][i]);
                }
            }
        }

        //CHART-BEDIENUNG


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


        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            if (prevPosition.HasValue && pos == prevPosition.Value)
                return;
            tooltip.RemoveAll();
            prevPosition = pos;
            var results = chart1.HitTest(pos.X, pos.Y, false,
                                            ChartElementType.DataPoint);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var prop = result.Object as DataPoint;
                    if (prop != null)
                    {
                        var pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        var pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);

                        // check if the cursor is really close to the point (2 pixels around the point)
                        if (Math.Abs(pos.X - pointXPixel) < 2 &&
                            Math.Abs(pos.Y - pointYPixel) < 2)
                        {
                            if (timestampdict.ContainsKey(this.Text)) tooltip.Show((string)timestampdict[this.Text] + "=" + DateTime.FromOADate(prop.XValue) + ", " + result.Series.Name + "=" + prop.YValues[0], this.chart1, pos.X, pos.Y - 15);
                            else tooltip.Show((string)IDdict[this.Text] + prop.XValue + ", " + result.Series.Name + "=" + prop.YValues[0], this.chart1, pos.X, pos.Y - 15);
                        }
                    }
                }
            }
        }

        //TREEVIEW-BEDIENUNG

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.Nodes.Count > 0)
                {
                    this.CheckAllChildNodes(e.Node, e.Node.Checked);
                }
                else
                {
                    List<string> CheckedNodes = CheckedNames(e.Node.Parent.Nodes);
                    if (timestampdict.ContainsKey(e.Node.Parent.Name)) CheckedNodes.Remove(timestampdict[e.Node.Parent.Name]);
                    else if (IDdict.ContainsKey(e.Node.Parent.Name)) CheckedNodes.Remove(IDdict[e.Node.Parent.Name]);
                    if (CheckedNodes.Any() && timestampdict.ContainsKey(e.Node.Parent.Name)) e.Node.Parent.Nodes[timestampdict[e.Node.Parent.Name]].Checked = true;
                    else if (CheckedNodes.Any() && IDdict.ContainsKey(e.Node.Parent.Name)) e.Node.Parent.Nodes[IDdict[e.Node.Parent.Name]].Checked = true;
                    else
                    {
                        if (timestampdict.ContainsKey(e.Node.Parent.Name)) e.Node.Parent.Nodes[timestampdict[e.Node.Parent.Name]].Checked = false;
                        else if (IDdict.ContainsKey(e.Node.Parent.Name)) e.Node.Parent.Nodes[IDdict[e.Node.Parent.Name]].Checked = false;
                        e.Node.Parent.Checked = false;
                    }
                }

                DrawChart();
            }
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                if (Color.Red == node.ForeColor) continue;
                node.Checked = nodeChecked;
                if (node.Nodes.Count > 0)
                {
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        List<String> CheckedNames(System.Windows.Forms.TreeNodeCollection theNodes)
        {
            List<String> aResult = new List<String>();

            if (theNodes != null)
            {
                foreach (System.Windows.Forms.TreeNode aNode in theNodes)
                {
                    if (aNode.Checked)
                    {
                        aResult.Add(aNode.Text);
                    }

                    aResult.AddRange(CheckedNames(aNode.Nodes));
                }
            }
            return aResult;
        }

        private void treeView1_BeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown && SystemColors.GrayText == e.Node.ForeColor)
            {
                MessageBox.Show("Zeitstempel werden automatisch mit anderen Daten geladen.");
                e.Cancel = true;
            }
            else if (e.Action != TreeViewAction.Unknown && Color.Orange == e.Node.ForeColor)
            {
                MessageBox.Show("ID-Stempel werden automatisch mit anderen Daten geladen. (Ersatz für Zeitstempel)");
                e.Cancel = true;
            }
            else if (e.Action != TreeViewAction.Unknown && Color.Red == e.Node.ForeColor)
            {
                MessageBox.Show("Nicht grafisch auswertbare Daten werden abgelehnt.");
                e.Cancel = true;
            }
        }

        //HILFSFUNKTIONEN

        private void label3_DoubleClick(object sender, EventArgs e)
        {
            ChartStart = XMin;
            textBox1.Text = ChartStart.ToShortDateString();
        }

        private void label2_DoubleClick(object sender, EventArgs e)
        {
            ChartEnd = XMax;
            textBox2.Text = ChartEnd.ToShortDateString();
        }

        private List<Object>[] ListBetween(List<Object> OriginalX, List<Object> OriginalY, DateTime Lower, DateTime Upper)
        {
            List<Object>[] List = new List<Object>[2];
            List[0] = new List<Object>();
            List[1] = new List<Object>();
            for (int i = 0; i < OriginalX.Count(); i++)
            {
                if ((DateTime)OriginalX[i] > Lower && (DateTime)OriginalX[i] < Upper)
                {
                    List[0].Add(OriginalX[i]);
                    List[1].Add(OriginalY[i]);
                }
            }
            return List;
        }

        private void CutSeries(string SeriesKey, List<object> XValues, List<object> YValues, int ValuesperPoint)
        {
            Series Original = FitSeries(XValues, YValues, ValuesperPoint);
            Series OutSeries = new Series();
            Series ShortSeries = new Series();

            if (UseMax || UseMin)
            {
                foreach (DataPoint dp in Original.Points)
                {
                    if (UseMin && Convert.ToDouble(dp.YValues[0]) <= ChartMin)
                    {
                        OutSeries.Points.Add(dp);
                    }
                    else if (UseMax && Convert.ToDouble(dp.YValues[0]) >= ChartMax)
                    {
                        OutSeries.Points.Add(dp);
                    }
                    else ShortSeries.Points.Add(dp);
                }
                Outdict[SeriesKey] = OutSeries;
                Outdict[SeriesKey].Name = SeriesKey + "-Outlier";
                Outdict[SeriesKey].XValueType = ChartValueType.DateTime;
                Outdict[SeriesKey].ChartType = SeriesChartType.Point;
                Seriesdict[SeriesKey] = ShortSeries;
                Seriesdict[SeriesKey].Name = SeriesKey;
                Seriesdict[SeriesKey].XValueType = ChartValueType.DateTime;
                Seriesdict[SeriesKey].ChartType = SeriesChartType.Line;
            }

            else
            {
                Seriesdict[SeriesKey] = Original;
                Seriesdict[SeriesKey].Name = SeriesKey;
            }
        }

        private void DrawChart()
        {
            chart1.Series.Clear();
            if (CheckedNames(treeView1.Nodes[this.Text].Nodes).Count != 0)
            {
                foreach (string List in CheckedNames(treeView1.Nodes[this.Text].Nodes))
                {
                    if (timestampdict.ContainsKey(this.Text) && List == timestampdict[this.Text]) continue;
                    else if (IDdict.ContainsKey(this.Text) && List == IDdict[this.Text]) continue;
                    Seriesdict[List].Name = List;
                    chart1.Series.Add(Seriesdict[List]);
                    if (ShowOutliers && Outdict.ContainsKey(List))
                    {
                        chart1.Series.Add(Outdict[List]);
                        if (!OutlierLegend) chart1.Series[List + "-Outlier"].IsVisibleInLegend = false;
                        else chart1.Series[List + "-Outlier"].IsVisibleInLegend = true;
                    }
                }
                if (timestampdict.ContainsKey(this.Text))
                {
                    chart1.ChartAreas[0].AxisX.Minimum = ChartStart.ToOADate();
                    chart1.ChartAreas[0].AxisX.Maximum = ChartEnd.ToOADate();
                }
                if (CheckedNames(treeView1.Nodes[this.Text].Nodes).Count == 2)
                {
                    ChartSettings.OrigTitle = CheckedNames(treeView1.Nodes[this.Text].Nodes)[1];
                    ChartSettings.OrigXAxis = CheckedNames(treeView1.Nodes[this.Text].Nodes)[0] + "[" + "Einheit" + "]";
                    ChartSettings.OrigYAxis = CheckedNames(treeView1.Nodes[this.Text].Nodes)[1] + "[" + "Einheit" + "]";
                }
                else
                {
                    ChartSettings.OrigTitle = "Multiple Values";
                    ChartSettings.OrigXAxis = CheckedNames(treeView1.Nodes[this.Text].Nodes)[0] + "[" + "Einheit" + "]";
                    ChartSettings.OrigYAxis = "Multiple Values";
                }
                chart1.ChartAreas[0].RecalculateAxesScale();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ChartSettings.ShowDialog();

            chart1.Titles.Clear();
            chart1.ChartAreas[0].AxisX.Title = "";
            chart1.ChartAreas[0].AxisY.Title = "";

            if (ChartSettings.TitleActivated)
            {
                Title title = chart1.Titles.Add(ChartSettings.TitleText);
                title.Font = new System.Drawing.Font("Arial", ChartSettings.TitleSize, ChartSettings.TitleStyle);
            }

            if (ChartSettings.XAxisActivated)
            {
                chart1.ChartAreas[0].AxisX.Title = ChartSettings.XAxisText;
                chart1.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Arial", ChartSettings.XAxisSize, ChartSettings.XAxisStyle);
            } 

            if (ChartSettings.YAxisActivated)
            {
                chart1.ChartAreas[0].AxisY.Title = ChartSettings.YAxisText;
                chart1.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Arial", ChartSettings.YAxisSize, ChartSettings.YAxisStyle);
            }

            ChartMax = ChartSettings.Max;
            UseMax = ChartSettings.UseMax;
            ChartMin = ChartSettings.Min;
            UseMin = ChartSettings.UseMin;

            ShowOutliers = ChartSettings.ShowPoints;
            OutlierLegend = ChartSettings.OutlierLegend;

            Outdict = new Dictionary<string, Series>();

            if (!ChartSettings.ShowLegend) chart1.Legends[0].Enabled = false;
            else chart1.Legends[0].Enabled = true;

            foreach (string tablekey in Table.Keys)
            {
                foreach (string key in Table[tablekey].Keys)
                {
                    if ((Type)Info[tablekey][key] == typeof(System.Boolean) || (Type)Info[tablekey][key] == typeof(System.String) || (Type)Info[tablekey][key] == typeof(DBNull))
                    {
                        continue;
                    }
                    if (timestampdict.ContainsKey(tablekey) && key == timestampdict[tablekey])
                    {
                        continue;
                    }
                    CutSeries(key, Table[tablekey][timestampdict[tablekey]], Table[tablekey][key], (int)numericUpDown1.Value);
                }
            }
            DrawChart();
        }
    }
}
