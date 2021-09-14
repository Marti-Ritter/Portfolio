using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.IO;
using Microsoft.Office.Core;
using Excel = Microsoft.Office.Interop.Excel;

namespace WindowsFormsApplication1
{
    public partial class MainMenuForm : Form
    {
        ServerSettingsForm SQLData;
        AdvancedSettingsForm AdvancedSettings;

        string username, password, serverurl, trustedconnection, database, timeout, location;
        string timestamp;
        DateTime LoadStart, LoadEnd;
        bool LoadCache = false;
        bool timestampReg;
        bool RefuseRed;
        Dictionary<string, string> timestampdict;
        Dictionary<string, string> IDdict;
        MySqlConnection myConnection;
        List<string> tablenames = new List<string>();
        List<string> columnnames = new List<string>();
        String[][] LoadArray;
        string CachePath;
        string ConnectionString;

        Dictionary<string, Dictionary<string, List<Object>>> DataTable;
        Dictionary<string, Dictionary<string, object>> InfoTable;
        Dictionary<string, Dictionary<string, List<object>>> MetaTable;
        Dictionary<int, string> MetaInfo;

        Dictionary<string, DateTime[]> LimitTable;
        DateTime OriginalOldest;
        DateTime OriginalNewest;

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
            }
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                if (RefuseRed && (Color.Red == node.ForeColor)) continue;
                node.Checked = nodeChecked;
                if (node.Nodes.Count > 0)
                {
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBox1.Checked)
            {
                FileInfo ExcelDoc = new FileInfo(CachePath + location + "Cache.xlsx");
                bool Conf = true;
                while (IsFileLocked(ExcelDoc))
                {
                    string[] Text = new String[] { "Die Datei '" + CachePath + location + "Cache.xlsx' wird durch ein anderes Programm vom Zugriff gesperrt.", "Wollen Sie den Zugriff erneut versuchen oder den Prozess abbrechen?" };
                    LockedAccessForm Confirmation = new LockedAccessForm(Text);
                    Confirmation.Text = "Datei wird bereits verwendet.";
                    Confirmation.ShowDialog();
                    Conf = Confirmation.Confirmed;
                    Confirmation.Close();
                    if (Conf) continue;
                    else break;
                }

                if (!Conf)
                {
                    checkBox1.Checked = false;
                    return;
                }

                //button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                LoadCache = true;

                //MessageBox.Show(CachePath);

                Excel.Application oXL = new Excel.Application();
                Excel.Workbooks oWBs = oXL.Workbooks;
                Excel._Workbook oWB = oWBs.Open(CachePath + location + "Cache.xlsx");

                treeView1.Nodes.Clear();
                timestampdict = new Dictionary<string, string>();
                IDdict = new Dictionary<string, string>();
                InfoTable = new Dictionary<string, Dictionary<string, object>>();
                DateTime NewestDate = DateTime.MinValue;

                int columncount = 0;
                foreach (Excel._Worksheet oSheet in oWB.Worksheets)
                {
                    columncount += oSheet.UsedRange.Columns.Count;
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oSheet);
                }

                ProgressBarForm Progress = new ProgressBarForm("ExcelLoad", columncount);
                int tablecount = 0;
                Progress.Show();

                try
                {
                    foreach (Excel._Worksheet oSheet in oWB.Worksheets)
                    {
                        treeView1.Nodes.Add(oSheet.Name, /*oSheet.Cells[2, 1].Value + "_" + */oSheet.Cells[2, 2].Value);
                        InfoTable[oSheet.Name] = new Dictionary<string, object>();

                        columncount = 1;
                        tablecount++;

                        try
                        {
//                            MessageBox.Show(oSheet.Cells[2, 1].Value.ToString("dd/MM/yyyy"));
                            for (int i = 1; i < oSheet.UsedRange.Columns.Count + 1; i++)
                            {
                                treeView1.Nodes[oSheet.Name].Nodes.Add(oSheet.Cells[3, i].Value, oSheet.Cells[3, i].Value);
                                if (oSheet.Cells[4, i].Value == null) InfoTable[oSheet.Name][oSheet.Cells[3, i].Value] = typeof(System.DBNull);
                                else InfoTable[oSheet.Name][oSheet.Cells[3, i].Value] = oSheet.Cells[4, i].Value.GetType();

                                if (!timestampdict.ContainsKey(oSheet.Name) && InfoTable[oSheet.Name][oSheet.Cells[3, i].Value] == typeof(DateTime))
                                {
                                    timestampdict[oSheet.Name] = oSheet.Cells[3, i].Value.ToString();
                                    treeView1.Nodes[oSheet.Name].Nodes[oSheet.Cells[3, i].Value].ForeColor = SystemColors.GrayText;
                                    if (((DateTime)oSheet.Cells[oSheet.UsedRange.Rows.Count, i].Value).Ticks > NewestDate.Ticks)
                                    {
                                        NewestDate = ((DateTime)oSheet.Cells[oSheet.UsedRange.Rows.Count, i].Value);
                                    }
                                }

                                else if ((Type)InfoTable[oSheet.Name][oSheet.Cells[3, i].Value] == typeof(System.Boolean) || (Type)InfoTable[oSheet.Name][oSheet.Cells[3, i].Value] == typeof(System.String) || (Type)InfoTable[oSheet.Name][oSheet.Cells[3, i].Value] == typeof(System.DBNull))
                                {
                                    treeView1.Nodes[oSheet.Name].Nodes[oSheet.Cells[3, i].Value].ForeColor = Color.Red;
                                }

                                else if (!timestampdict.ContainsKey(oSheet.Name) && !IDdict.ContainsKey(oSheet.Name))
                                {
                                    if (oSheet.Cells[3, i].Value.Contains("ID"))
                                    {
                                        IDdict[oSheet.Name] = oSheet.Cells[3, i].Value;
                                        treeView1.Nodes[oSheet.Name].Nodes[oSheet.Cells[3, i].Value].ForeColor = Color.Orange;
                                    }
                                }
                                Progress.progressBar1.Increment(1);
                                Progress.label1.Text = "Seite: " + tablecount + "/" + oWB.Worksheets.Count + "         Spalte: " + columncount + "/" + oSheet.UsedRange.Columns.Count;
                                columncount++;
                            }
                        }
                        catch (Exception E)
                        {
                            MessageBox.Show(E.ToString());
                        }

                        if ((!timestampdict.ContainsKey(oSheet.Name) && !IDdict.ContainsKey(oSheet.Name)) || treeView1.Nodes[oSheet.Name].Nodes.Count == 1) treeView1.Nodes[oSheet.Name].ForeColor = Color.Red;
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oSheet);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString());
                }

                finally
                {
                    oWB.Close(false);
                    oWBs.Close();
                    Progress.Close();
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWB);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWBs);
                    if (oXL != null)
                    {
                        oXL.Quit();
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oXL);
                    }
                }

                if (NewestDate == DateTime.MinValue)
                {
                    label3.Text = "Stand: Keine Zeitangaben gefunden.";
                    textBox1.Text = DateTime.Today.AddDays(-7).ToShortDateString();
                    textBox2.Text = DateTime.Today.ToShortDateString();
                }
                else
                {
                    label3.Text = "Stand: " + NewestDate.ToShortDateString();
                    textBox1.Text = NewestDate.AddDays(-7).ToShortDateString();
                    textBox2.Text = NewestDate.ToShortDateString();
                }
            }

            else if (checkBox1.Checked == false)
            {
                //button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                LoadCache = false;
                treeView1.Nodes.Clear();
                DataBaseLoad();
            }
        }

        public MainMenuForm()
        {
            InitializeComponent();
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

            else if (RefuseRed && e.Action != TreeViewAction.Unknown && Color.Red == e.Node.ForeColor)
            {
                MessageBox.Show("Nicht grafisch auswertbare Daten werden abgelehnt.");
                e.Cancel = true;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {

            //aus Textboxen
            /*
            DateTime StartDate;
            DateTime EndDate;

            if (DateTime.TryParse(textBox1.Text, out StartDate))
            {
                if (StartDate < OriginalOldest)
                {
                    textBox1.Text = OriginalOldest.ToShortDateString();
                    MessageBox.Show("Startdatum kann nicht vor ältestem vorhandenen Datum liegen.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Bitte korrektes Datum eingeben.");
                return;
            }

            if (DateTime.TryParse(textBox2.Text, out EndDate))
            {
                if (EndDate > OriginalNewest)
                {
                    textBox2.Text = OriginalNewest.ToShortDateString();
                    MessageBox.Show("Enddatum kann nicht nach jüngstem vorhandenen Datum liegen.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Bitte korrektes Datum eingeben.");
                return;
            }

            if (StartDate > EndDate)
            {
                MessageBox.Show("Startdatum kann nicht später sein als Enddatum.");
                textBox1.Text = OriginalOldest.ToShortDateString();
                textBox2.Text = OriginalNewest.ToShortDateString();
                return;
            }

            LoadStart = DateTime.Parse(textBox1.Text);
            LoadEnd = DateTime.Parse(textBox2.Text);
            LoadEnd = LoadEnd.Date.AddDays(1).AddTicks(-1);
            */
            //Alle Daten
            LoadStart = DateTime.Parse("1.1.1753");
            LoadEnd = DateTime.MaxValue;

            //LOAD
            DataLoad();
            //LOAD

            if (DataTable.Count == 0) return;

            int nodecount = 0;
            foreach (TreeNode ParentNode in treeView1.Nodes)
            {
                if (CheckedNames(ParentNode.Nodes).Count != 0) nodecount++;
            }

            ProgressBarForm Progress = new ProgressBarForm("Graph", nodecount);
            Progress.Show();


            foreach (TreeNode ParentNode in treeView1.Nodes)
            {
                if (CheckedNames(ParentNode.Nodes).Count != 0)
                {
                    Dictionary<string, Dictionary<string, List<Object>>> ShortDataTable = new Dictionary<string, Dictionary<string, List<Object>>> { { ParentNode.Name, DataTable[ParentNode.Name] } };
                    Dictionary<string, Dictionary<string, object>> ShortInfoTable = new Dictionary<string, Dictionary<string, object>> { { ParentNode.Name, InfoTable[ParentNode.Name] } };
                    ChartsForm Chart = new ChartsForm(ShortDataTable, ShortInfoTable, timestampdict, IDdict);
                    Chart.Text = ParentNode.Name;
                    Chart.Show();
                    Progress.progressBar1.Increment(1);
                    Progress.label1.Text = "Tabelle " + ParentNode.Name + " geladen.";
                    Progress.Activate();
                    Application.DoEvents();
                }
            }
            Progress.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //IMMER neues directory erstellen
            Directory.CreateDirectory(@".\VisevData");
            //Check for .ini
            if (!File.Exists(Application.StartupPath + @".\VisevData\Visev.ini"))
            {
                //.ini existiert nicht
                MessageBox.Show("Es wurde keine INI für dieses Programm im Standardpfad gefunden." + Environment.NewLine + "Bitte geben Sie die benötigten Serverdaten im folgenden Fenster ein.");
                ServerSettingsForm INICreation = new ServerSettingsForm(true);
                INICreation.ShowDialog();
                ConnectionString = INICreation.ConnectionString;
                INICreation.Close();
                if (String.IsNullOrEmpty(ConnectionString)) System.Environment.Exit(0);
            }
            SQLData = new ServerSettingsForm();

            AdvancedSettings = new AdvancedSettingsForm();

            username = SQLData.username;
            password = SQLData.password;
            serverurl = SQLData.serverurl;
            trustedconnection = SQLData.trustedconnection;
            database = SQLData.database;
            timeout = SQLData.timeout;
            timestamp = AdvancedSettings.timestamp;
            timestampReg = AdvancedSettings.timestampReg;
            CachePath = AdvancedSettings.CachePath;
            RefuseRed = AdvancedSettings.RefuseRed;
            location = SQLData.location;

            ConnectionString = SQLData.ConnectionString;
            DataBaseLoad();

            if (!File.Exists(CachePath + location + "Cache.xlsx"))
            {
                MessageBox.Show("Es wurde kein Cache für diesen Server im Standardpfad gefunden." + Environment.NewLine + "Cache laden deaktiviert.");
                //CacheCreation();
                checkBox1.Enabled = false;
            }

            /*tablenames = new List<string> { "1", "2", "3", "4" };
            foreach (string table in tablenames)
            {
                columnnames = new List<string> { "1", "2", "3", "4" };
                TreeNode node = treeView1.Nodes.Add(table);
                foreach (string column in columnnames)
                {
                    node.Nodes.Add(column);
                }
            } */

            //LoadStart = AdvancedSettings.LoadStart;
            //LoadEnd = AdvancedSettings.LoadEnd;
            //CacheUpdate();
        }

/*        private void button1_Click(object sender, EventArgs e)
        {
            //myConnection = new SqlConnection(@"Server=server\instance;Database=Northwind;Integrated Security=True");
            timestampdict = new Dictionary<string, string>();
            IDdict = new Dictionary<string, string>();
            InfoTable = new Dictionary<string, Dictionary<string, object>>();
            using (myConnection = new SqlConnection(ConnectionString))
            {
                try
                {
                    myConnection.Open();
                    MessageBox.Show("is open");
                    SqlCommand TableCom = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND NOT TABLE_NAME LIKE 'sys_%'", myConnection);
                    treeView1.Nodes.Clear();
                    using (SqlDataReader reader = TableCom.ExecuteReader())
                    {
                        tablenames = new List<string>();
                        while (reader.Read())
                        {
                            tablenames.Add((string)reader["TABLE_NAME"]);
                        }
                    }

                    foreach (string table in tablenames)
                    {
                        InfoTable[table] = new Dictionary<string, object>();
                        SqlCommand ColumnCom = new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + table + "'", myConnection);
                        using (SqlDataReader reader = ColumnCom.ExecuteReader())
                        {
                            columnnames = new List<string>();
                            while (reader.Read())
                            {
                                columnnames.Add((string)reader["COLUMN_NAME"]);
                            }
                        }
                        TreeNode node = treeView1.Nodes.Add(table, table);
                        var timestampfound = false;
                        foreach (string column in columnnames)
                        {
                            node.Nodes.Add(column, column);

                            SqlCommand FindCom = new SqlCommand(string.Format("Select {1} from {0}", table, column), myConnection);
                            using (SqlDataReader reader = FindCom.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    InfoTable[table][column] = reader[0].GetType();
                                    if ((Type)InfoTable[table][column] == typeof(System.Boolean) || (Type)InfoTable[table][column] == typeof(System.String) || (Type)InfoTable[table][column] == typeof(System.DBNull))
                                    {
                                        treeView1.Nodes[table].Nodes[column].ForeColor = Color.Red;
                                    }
                                }
                                else treeView1.Nodes[table].Nodes[column].ForeColor = Color.Red;

                                if (!timestampReg)
                                {
                                    if (column == timestamp)
                                    {
                                        timestampdict[table] = timestamp;
                                        treeView1.Nodes[table].Nodes[column].ForeColor = SystemColors.GrayText;
                                    }
                                }
                                else if (!timestampfound)
                                {
                                    //DateTime datetime;
                                    if (reader.Read() && reader[0].GetType() == typeof(DateTime))
                                    {
                                        //System.Diagnostics.Debug.WriteLine(reader[0]);
                                        timestampdict[table] = column;
                                        treeView1.Nodes[table].Nodes[column].ForeColor = SystemColors.GrayText;
                                        timestampfound = true;
                                        //System.Diagnostics.Debug.WriteLine("FOUND");
                                    }
                                }
                            }                      
                        }

                        if (!timestampdict.ContainsKey(table))
                        {
                            foreach (string column in columnnames)
                            {
                                if (column.Contains("ID") && Color.Red != treeView1.Nodes[table].Nodes[column].ForeColor)
                                {
                                    IDdict[table] = column;
                                    treeView1.Nodes[table].Nodes[column].ForeColor = Color.Orange;
                                    break;
                                }
                            }
                        }
                        if (!timestampdict.ContainsKey(table) && !IDdict.ContainsKey(table)) treeView1.Nodes[table].ForeColor = Color.Red;
                    }
                    button3.Enabled = true;
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;
                    myConnection.Close();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString());
                }
            } 
        }
 */           

        private void button2_Click(object sender, EventArgs e)
        {
            string oldlocation = SQLData.location;
            SQLData.ShowDialog();
            serverurl = SQLData.serverurl;
            location = SQLData.location;
            database = SQLData.database;
            ConnectionString = SQLData.ConnectionString;
            DataBaseLoad();
            if (oldlocation != location && !File.Exists(CachePath + location + "Cache.xlsx"))
            {
                //CacheCreation();
                MessageBox.Show("Kein Cache gefunden. Cache laden deaktiviert.");
                checkBox1.Enabled = false;
            }
//            MessageBox.Show(ConnectionString);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DateTime StartDate;
            DateTime EndDate;

            if (DateTime.TryParse(textBox1.Text, out StartDate))
            {
                if (StartDate < OriginalOldest)
                {
                    textBox1.Text = OriginalOldest.ToShortDateString();
                    MessageBox.Show("Startdatum kann nicht vor ältestem vorhandenen Datum liegen.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Bitte korrektes Datum eingeben.");
                return;
            }

            if (DateTime.TryParse(textBox2.Text, out EndDate))
            {
                if (EndDate > OriginalNewest)
                {
                    textBox2.Text = OriginalNewest.ToShortDateString();
                    MessageBox.Show("Enddatum kann nicht nach jüngstem vorhandenen Datum liegen.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Bitte korrektes Datum eingeben.");
                return;
            }

            if (StartDate > EndDate)
            {
                MessageBox.Show("Startdatum kann nicht später sein als Enddatum.");
                textBox1.Text = OriginalOldest.ToShortDateString();
                textBox2.Text = OriginalNewest.ToShortDateString();
                return;
            }

            FileInfo ExcelDoc = new FileInfo(CachePath + "TempCache.xlsx");

            if (ExcelDoc.Exists)
            {
                bool Conf = true;
                while (IsFileLocked(ExcelDoc))
                {
                    string[] Text = new String[] { "Die Datei 'TempCache.xlsx' wird durch ein anderes Programm vom Zugriff gesperrt.", "Wollen Sie den Zugriff erneut versuchen oder den Prozess abbrechen?" };
                    LockedAccessForm Confirmation = new LockedAccessForm(Text);
                    Confirmation.Text = "Datei wird bereits verwendet.";
                    Confirmation.ShowDialog();
                    Conf = Confirmation.Confirmed;
                    Confirmation.Close();
                    if (Conf) continue;
                    else break;
                }

                if (!Conf) return;
            }

            LoadStart = DateTime.Parse(textBox1.Text);
            LoadEnd = DateTime.Parse(textBox2.Text);
            LoadEnd = LoadEnd.Date.AddDays(1).AddTicks(-1);
            LoadStart = LoadEnd.AddDays(-7); //behelfsmäßig
            //LOAD
            DataLoad();
            //LOAD
            if (DataTable.Count == 0) return;

            Excel.Application oXL = new Excel.Application();
            oXL.SheetsInNewWorkbook = 1;
            Excel.Workbooks oWBs = oXL.Workbooks;
            Excel._Workbook oWB;

            try
            {

                oWB = oWBs.Open(CachePath + "TempCache.xlsx");

            }
            catch (System.Runtime.InteropServices.COMException)
            {

                oWB = oWBs.Add();
                oWB.SaveAs(CachePath + "TempCache.xlsx");

            }

            int columncount = 0;
            foreach (string tablekey in DataTable.Keys)
            {
                columncount += DataTable[tablekey].Keys.Count();
            }

            ProgressBarForm Progress = new ProgressBarForm("ExcelWrite", columncount);
            int tablecount = 0;
            Progress.Show();

            try
            {
                foreach (string tablekey in DataTable.Keys)
                {
                    Excel._Worksheet oSheet;

                    columncount = 1;
                    tablecount++;

                    try
                    {

                        oSheet = oWB.Worksheets[Truncator(database + "_" + tablekey, 31)];

                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {

                        oSheet = (Excel._Worksheet)oWB.Sheets.Add();
                        oSheet.Name = Truncator(database + "_" + tablekey, 31);
                        oSheet.Cells[1, 1].Value = "Datenbankname";
                        oSheet.Cells[2, 1].Value = database;
                        oSheet.Cells[1, 2].Value = "Tabellenname";
                        oSheet.Cells[2, 2].Value = tablekey;

                    }

                    int column = 1; // Initialize for keys.

                    if (timestampdict.ContainsKey(tablekey)) oSheet.Cells[3, column++].Value = timestampdict[tablekey];

                    foreach (string key in DataTable[tablekey].Keys)
                    {
                        if (timestampdict.ContainsKey(tablekey) && key == timestampdict[tablekey]) continue;
                        oSheet.Cells[3, column].Value = key;
                        string Information = "Keine Informationen zu dieser Spalte vorhanden.";
                        if (MetaTable.ContainsKey(tablekey) && MetaTable[tablekey].ContainsKey(key))
                        {
                            Information = "";
                            int ID = 1;
                            foreach (object property in MetaTable[tablekey][key])
                            {
                                Information += MetaInfo[ID] + " = " + (string)property + Environment.NewLine;
                                ID++;
                            }
                        }
                        oSheet.Cells[3, column].Validation.Delete();
                        oSheet.Cells[3, column].Validation.Add(Excel.XlDVType.xlValidateDecimal, Excel.XlDVAlertStyle.xlValidAlertStop,Excel.XlFormatConditionOperator.xlBetween, decimal.MinValue, decimal.MaxValue);
                        oSheet.Cells[3, column].Validation.InputMessage = Information;
                        column++;
                    }

                    Object[,] dArray = new Object[DataTable[tablekey][DataTable[tablekey].Keys.First()].Count, DataTable[tablekey].Count];

                    int row = 0; // Initialize for values in key.
                    column = 0; // Initialize for keys.

                    if (timestampdict.ContainsKey(tablekey))
                    {
                        foreach (DateTime value in DataTable[tablekey][timestampdict[tablekey]])
                        {
                            dArray[row, column] = value;
                            row++;
                        }

                        column++; // increment for next key.

                        Progress.progressBar1.Increment(1);
                        Progress.label1.Text = "Seite: " + tablecount + "/" + DataTable.Keys.Count() + "         Spalte: " + columncount + "/" + DataTable[tablekey].Keys.Count();
                        columncount++;
                        Progress.Activate();
                        Application.DoEvents();
                    }



                    foreach (string key in DataTable[tablekey].Keys)
                    {
                        if (timestampdict.ContainsKey(tablekey) && key == timestampdict[tablekey]) continue;
                        row = 0; // Initialize for values in key.

                        foreach (Object value in DataTable[tablekey][key])
                        {
                            dArray[row, column] = value;
                            row++;
                        }

                        column++; // increment for next key.

                        Progress.progressBar1.Increment(1);
                        Progress.label1.Text = "Seite: " + tablecount + "/" + DataTable.Keys.Count() + "         Spalte: " + columncount + "/" + DataTable[tablekey].Keys.Count();
                        columncount++;
                        Progress.Activate();
                        Application.DoEvents();
                    }


                    Excel.Range oXR1 = (Excel.Range)oSheet.Cells[4, 1];
                    Excel.Range oXR2 = (Excel.Range)oSheet.Cells[4 + dArray.GetLength(0) - 1, column];
                    Excel.Range oXR = oSheet.get_Range(oXR1, oXR2);
                    oXR.Value2 = dArray;
                    if (timestampdict.ContainsKey(tablekey))
                    {
                        oXR = oSheet.Range[oSheet.Cells[4, 1], oSheet.Cells[oSheet.UsedRange.Rows.Count, 1]];
                        oXR.EntireColumn.NumberFormat = "dd.mm.yyyy ss:mm:hh";
                    }

                    dynamic allDataRange = oSheet.Range[oSheet.Cells[4, 1], oSheet.Cells[oSheet.UsedRange.Rows.Count, oSheet.UsedRange.Columns.Count]];
                    allDataRange.Sort(allDataRange.Columns[1], Excel.XlSortOrder.xlAscending);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(allDataRange);

                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oSheet);
                }
                oXL.DisplayAlerts = false;
                oWB.Save();
                Progress.Close();
                //MessageBox.Show("Cache in Excel gespeichert");
                try
                {

                    ((Excel.Worksheet)oXL.ActiveWorkbook.Sheets["Tabelle1"]).Delete();
                }

                catch (System.Runtime.InteropServices.COMException /*Error*/)
                {
                    //                    MessageBox.Show(Error.ToString());
                }
                oXL.DisplayAlerts = true;
            }
            catch (Exception Error)
            {
                MessageBox.Show(Error.ToString());
            }
            finally
            {
                oWB.Close(true);
                oWBs.Close();
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWB);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWBs);
                if (oXL != null)
                {
                    oXL.Quit();
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oXL);
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //aus Textboxen
            /*
            DateTime StartDate;
            DateTime EndDate;

            if (DateTime.TryParse(textBox1.Text, out StartDate))
            {
                if (StartDate < OriginalOldest)
                {
                    textBox1.Text = OriginalOldest.ToShortDateString();
                    MessageBox.Show("Startdatum kann nicht vor ältestem vorhandenen Datum liegen.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Bitte korrektes Datum eingeben.");
                return;
            }

            if (DateTime.TryParse(textBox2.Text, out EndDate))
            {
                if (EndDate > OriginalNewest)
                {
                    textBox2.Text = OriginalNewest.ToShortDateString();
                    MessageBox.Show("Enddatum kann nicht nach jüngstem vorhandenen Datum liegen.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Bitte korrektes Datum eingeben.");
                return;
            }

            if (StartDate > EndDate)
            {
                MessageBox.Show("Startdatum kann nicht später sein als Enddatum.");
                textBox1.Text = OriginalOldest.ToShortDateString();
                textBox2.Text = OriginalNewest.ToShortDateString();
                return;
            }

            LoadStart = DateTime.Parse(textBox1.Text);
            LoadEnd = DateTime.Parse(textBox2.Text);
            LoadEnd = LoadEnd.Date.AddDays(1).AddTicks(-1);
            */

            //alle Daten
            LoadStart = DateTime.Parse("1.1.1753");
            LoadEnd = DateTime.MaxValue;

            //LOAD
            DataLoad();
            //LOAD

            if (DataTable.Count == 0) return;

            StatisticsForm Correlation = new StatisticsForm(DataTable, InfoTable, timestampdict);
            Correlation.Text = "Mathematische Auswertung";
            Correlation.ShowDialog();
            //Correlation.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CacheCreation();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            AdvancedSettings.ShowDialog();
            timestamp = AdvancedSettings.timestamp;
            timestampReg = AdvancedSettings.timestampReg;
            RefuseRed = AdvancedSettings.RefuseRed;
            CachePath = AdvancedSettings.CachePath;

            if (!File.Exists(CachePath + location + "Cache.xlsx"))
            {
                CacheCreation();
            }
        }

        private void label1_DoubleClick(object sender, EventArgs e)
        {
            textBox1.Text = OriginalOldest.ToShortDateString();
        }

        private void label2_DoubleClick(object sender, EventArgs e)
        {
            textBox2.Text = OriginalNewest.ToShortDateString();
        }

        private string Truncator(string Original, int length)
        {
            string[] Splitarray = Original.Split('_');
            if (Original.Length < length) return Original;
//            if (String.Join("", Splitarray).Length < length) return String.Join("", Splitarray);
            string TruncatedString = "";
            foreach (string Split in Splitarray)
            {
                TruncatedString += Split.Substring(0, Math.Min(Split.Length, length / Splitarray.Length - 1)) + "_";
//                if (Split.Length < length / Splitarray.Length - 1) length += length / Splitarray.Length - 1 - Split.Length;
            }
            return TruncatedString.Substring(0,TruncatedString.Length-1);
        }

        private bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private void CheckUncheckTreeNode(TreeNodeCollection trNodeCollection, bool isCheck)
        {
            foreach (TreeNode trNode in trNodeCollection)
            {
                trNode.Checked = isCheck;
                if (trNode.Nodes.Count > 0)
                    CheckUncheckTreeNode(trNode.Nodes, isCheck);
            }
        }

        //DataStuff

        private void DataBaseLoad()
        {
            timestampdict = new Dictionary<string, string>();
            IDdict = new Dictionary<string, string>();
            InfoTable = new Dictionary<string, Dictionary<string, object>>();
            LimitTable = new Dictionary<string, DateTime[]>();
            MetaTable = new Dictionary<string, Dictionary<string, List<object>>>();
            MetaInfo = new Dictionary<int, string>();
            Dictionary<int, string> TableLookup = new Dictionary<int, string>();
            using (myConnection = new MySqlConnection(ConnectionString/*"user id=" + username + ";" +
                                       "password=" + password + ";" +
                                       "server=" + serverurl + ";" +
                                       "Trusted_Connection=" + trustedconnection + ";" +
                                       "database=" + database + ";Integrated Security=false;" +
                                       "connection timeout=" + timeout*/))
            {
                try
                {
                    myConnection.Open();
                    MySqlCommand TableCom = new MySqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='" + database + "'", myConnection);
                    treeView1.Nodes.Clear();
                    using (MySqlDataReader reader = TableCom.ExecuteReader())
                    {
                        tablenames = new List<string>();
                        while (reader.Read())
                        {
                            if ((string)reader["TABLE_NAME"] == "G_Tabellen" || (string)reader["TABLE_NAME"] == "G_Metadaten" || (string)reader["TABLE_NAME"] == "G_MetadatenMerkmalstypen") continue;
                            tablenames.Add((string)reader["TABLE_NAME"]);
                        }
                    }

                    MySqlCommand TableRead = new MySqlCommand("SELECT * FROM G_Tabellen", myConnection);
                    using (MySqlDataReader reader = TableRead.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TableLookup[(int)reader["ID"]] = (string)reader["Name"];
                        }
                    }

                    MySqlCommand PropertyRead = new MySqlCommand("SELECT * FROM G_MetadatenMerkmalstypen", myConnection);
                    using (MySqlDataReader reader = PropertyRead.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            MetaInfo[(int)reader["ID"]] = (string)reader["Name"];
                        }
                    }

                    MySqlCommand MetaRead = new MySqlCommand("SELECT * FROM G_Metadaten", myConnection);
                    using (MySqlDataReader reader = MetaRead.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string TableName = TableLookup[(int)reader["TabelleID"]];
                            if (!MetaTable.ContainsKey(TableName)) MetaTable[TableName] = new Dictionary<string, List<object>>();
                            string ColumnName = (string)reader["Spaltenname"];
                            if (!MetaTable[TableName].ContainsKey(ColumnName)) MetaTable[TableName][ColumnName] = new List<object>();
                            object PropertyValue = (object)reader["Merkmal"];
                            MetaTable[TableName][ColumnName].Add(PropertyValue);
                        }
                    }

                    foreach (string table in tablenames)
                        {
                            InfoTable[table] = new Dictionary<string, object>();
                            MySqlCommand ColumnCom = new MySqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + table + "'", myConnection);
                            using (MySqlDataReader reader = ColumnCom.ExecuteReader())
                            {
                                columnnames = new List<string>();
                                while (reader.Read())
                                {
                                    columnnames.Add((string)reader["COLUMN_NAME"]);
                                }
                            }
                            TreeNode node = treeView1.Nodes.Add(table, table);
                            var timestampfound = false;
                            foreach (string column in columnnames)
                            {
                                node.Nodes.Add(column, column);

                                MySqlCommand FindCom = new MySqlCommand(string.Format("Select {1} from {0}", table, column), myConnection);
                                using (MySqlDataReader reader = FindCom.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        InfoTable[table][column] = reader[0].GetType();
                                        if ((Type)InfoTable[table][column] == typeof(System.Boolean) || (Type)InfoTable[table][column] == typeof(System.String) || (Type)InfoTable[table][column] == typeof(System.DBNull))
                                        {
                                            treeView1.Nodes[table].Nodes[column].ForeColor = Color.Red;
                                        }
                                    }
                                    else treeView1.Nodes[table].Nodes[column].ForeColor = Color.Red;

                                    if (!timestampReg)
                                    {
                                        if (column == timestamp)
                                        {
                                            timestampdict[table] = timestamp;
                                            treeView1.Nodes[table].Nodes[column].ForeColor = SystemColors.GrayText;
                                        }
                                    }
                                    else if (!timestampfound)
                                    {
                                        //DateTime datetime;
                                        if (reader.Read() && reader[0].GetType() == typeof(DateTime))
                                        {
                                            //System.Diagnostics.Debug.WriteLine(reader[0]);
                                            timestampdict[table] = column;
                                            treeView1.Nodes[table].Nodes[column].ForeColor = SystemColors.GrayText;
                                            timestampfound = true;
                                            //System.Diagnostics.Debug.WriteLine("FOUND");
                                        }
                                    }
                                }
                            }
                            if (timestampfound)
                            {
                                LimitTable[table] = new DateTime[2];

                                MySqlCommand MinDate = new MySqlCommand(string.Format("select min({1}) from {0}", table, timestampdict[table]), myConnection);
                                using (MySqlDataReader TimeReader = MinDate.ExecuteReader())
                                {
                                    if (TimeReader.Read())
                                    {
                                        LimitTable[table][0] = (DateTime)TimeReader[0];
                                    }
                                }

                                MySqlCommand MaxDate = new MySqlCommand(string.Format("select max({1}) from {0}", table, timestampdict[table]), myConnection);
                                using (MySqlDataReader TimeReader = MaxDate.ExecuteReader())
                                {
                                    if (TimeReader.Read())
                                    {
                                        LimitTable[table][1] = (DateTime)TimeReader[0];
                                    }
                                }
                            }

                            if (!timestampdict.ContainsKey(table))
                            {
                                foreach (string column in columnnames)
                                {
                                    if (column.Contains("ID") && Color.Red != treeView1.Nodes[table].Nodes[column].ForeColor)
                                    {
                                        IDdict[table] = column;
                                        treeView1.Nodes[table].Nodes[column].ForeColor = Color.Orange;
                                        break;
                                    }
                                }
                            }
                            if (!timestampdict.ContainsKey(table) && !IDdict.ContainsKey(table)) treeView1.Nodes[table].ForeColor = Color.Red;
                        }
                    myConnection.Close();

                    button3.Enabled = true;
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;

                    DateTime NewestDate = DateTime.MinValue;
                    DateTime OldestDate = DateTime.MaxValue;

                    foreach (string key in LimitTable.Keys)
                    {
                        if (LimitTable[key][0].Ticks < OldestDate.Ticks) OldestDate = LimitTable[key][0];
                        if (LimitTable[key][1].Ticks > NewestDate.Ticks) NewestDate = LimitTable[key][1];
                    }

                    if (NewestDate == DateTime.MinValue || OldestDate == DateTime.MaxValue)
                    {
                        label3.Text = "Stand: Keine Zeitangaben gefunden.";

                        button3.Enabled = false;
                        textBox1.Enabled = false;
                        textBox2.Enabled = false;

//                        textBox1.Text = NewestDate.AddDays(-7).ToShortDateString();
//                        textBox2.Text = NewestDate.ToShortDateString();

                    }
                    else
                    {
                        label3.Text = "Stand: " + NewestDate.ToShortDateString();

                        button3.Enabled = true;
                        textBox1.Enabled = true;
                        textBox2.Enabled = true;

                        textBox1.Text = OldestDate.ToShortDateString();
                        OriginalOldest = OldestDate;
                        textBox2.Text = NewestDate.ToShortDateString();
                        OriginalNewest = NewestDate;
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 53)
                    {
                        MessageBox.Show("Der Server konnte nicht gefunden werden. Bitte überprüfen Sie die Angaben in der .ini und korrigieren Sie diese nötigenfalls.");
                    }
                    if (ex.Number == -2)
                    {
                        MessageBox.Show("Es trat ein Verbindungstimeout auf.");
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString());
                }
            }

        }

        private void DataLoad()
        {
            LoadArray = new String[treeView1.GetNodeCount(false)][];
            int x = 0;
            //MessageBox.Show(treeView1.GetNodeCount(false).ToString());
            foreach (System.Windows.Forms.TreeNode Node in treeView1.Nodes)
            {
                List<String> CheckedList = CheckedNames(Node.Nodes);
                if (CheckedList.Count != 0)
                {
                    LoadArray[x] = new String[CheckedList.Count + 1];
                    LoadArray[x][0] = Node.Name;
                    CheckedList.ToArray().CopyTo(LoadArray[x], 1);
                }
                else LoadArray[x] = new string[] { Node.Name };
                x += 1;
            }

            DataTable = new Dictionary<string, Dictionary<string, List<object>>>();
            //InfoTable = new Dictionary<string, Dictionary<string, object[]>>();

            if (LoadCache)
            {

                FileInfo ExcelDoc = new FileInfo(CachePath + location + "Cache.xlsx");
                bool Conf = true;
                while (IsFileLocked(ExcelDoc))
                {
                    string[] Text = new String[] { "Die Datei '" + CachePath + location + "Cache.xlsx' wird durch ein anderes Programm vom Zugriff gesperrt.", "Wollen Sie den Zugriff erneut versuchen oder den Prozess abbrechen?" };
                    LockedAccessForm Confirmation = new LockedAccessForm(Text);
                    Confirmation.Text = "Datei wird bereits verwendet.";
                    Confirmation.ShowDialog();
                    Conf = Confirmation.Confirmed;
                    Confirmation.Close();
                    if (Conf) continue;
                    else break;
                }

                if (!Conf) return;

                Excel.Application oXL = new Excel.Application();
                Excel.Workbooks oWBs = oXL.Workbooks;
                Excel._Workbook oWB = oWBs.Open(CachePath + location + "Cache.xlsx");

                try
                {
                    for (x = 0; x < LoadArray.Length; x++)
                    {
                        if (LoadArray[x].Length == 1) continue;
                        DataTable[LoadArray[x][0]] = new Dictionary<string, List<Object>>();
                        //InfoTable[LoadArray[x][0]] = new Dictionary<string, Object[]>();
                        Excel._Worksheet oSheet = oWB.Sheets[LoadArray[x][0]];
                        for (int y = 1; y < LoadArray[x].Length; y++)
                        {
                            for (int z = 1; z < oSheet.UsedRange.Columns.Count + 1; z++)
                            {
                                if (oSheet.Cells[3, z].Value == LoadArray[x][y])
                                {
                                    if (!DataTable[LoadArray[x][0]].ContainsKey(LoadArray[x][y]))
                                    {
                                        DataTable[LoadArray[x][0]].Add(LoadArray[x][y], new List<Object>());
                                        //InfoTable[LoadArray[x][0]].Add(LoadArray[x][y], new object[2]);
                                    }
                                    /*
                                    for (int j = 2; j < oSheet.UsedRange.Rows.Count + 1; j++)
                                    {
                                        DataTable[LoadArray[x][0]][LoadArray[x][y]].Add(oSheet.Cells[j, y].Value.ToString());
                                    }
                                    */
                                    DataTable[LoadArray[x][0]][LoadArray[x][y]] = new List<object>();   //vielleicht überflüssig?
                                    Excel.Range oXR1 = (Excel.Range)oSheet.Cells[4, z];
                                    Excel.Range oXR2 = (Excel.Range)oSheet.Cells[oSheet.UsedRange.Rows.Count, z];
                                    Excel.Range oXR = oSheet.get_Range(oXR1, oXR2);
                                    System.Array myvalues = (System.Array)oXR.Cells.Value;
                                    object[] ValueArray = myvalues.OfType<object>().Select(o => o).ToArray();
                                    DataTable[LoadArray[x][0]][LoadArray[x][y]].AddRange(ValueArray);
                                    //InfoTable[LoadArray[x][0]][LoadArray[x][y]][1] = oSheet.Cells[2, z].GetType().ToString();
                                }
                            }
                            //if (timestampdict.ContainsKey(oSheet.Name)) InfoTable[LoadArray[x][0]][LoadArray[x][y]][0] = true;
                            //else InfoTable[LoadArray[x][0]][LoadArray[x][y]][0] = false;
                        }
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oSheet);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.ToString());
                }
                finally
                {
                    oWB.Close(false);
                    oWBs.Close();
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWB);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWBs);
                    if (oXL != null)
                    {
                        oXL.Quit();
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oXL);
                    }
                }

            }

            else
            {// Laden aus SQL
                //MessageBox.Show("von: " + LoadStart + " bis: " + LoadEnd);

                //var mc = @"Server=EU-RITTER\SQLEXPRESS;Database=KreislaufDaten;User Id=sa;Password=igb;";
                using (/*myConnection = new SqlConnection(ConnectionString)*/ myConnection = new MySqlConnection(ConnectionString))
                {
                    try
                    {
                        myConnection.Open();
                        for (x = 0; x < LoadArray.Length; x++)
                        {
                            if (LoadArray[x].Length == 1) continue;
                            DataTable[LoadArray[x][0]] = new Dictionary<string, List<Object>>();
                            //InfoTable[LoadArray[x][0]] = new Dictionary<string, object[]>();
                            for (int y = 1; y < LoadArray[x].Length; y++)
                            {
                                //System.Diagnostics.Debug.WriteLine(LoadEnd.ToString("yyyy-MM-dd hh:mm:ss.fff"));
                                MySqlCommand LoadCom;
                                if (!DataTable[LoadArray[x][0]].ContainsKey(LoadArray[x][y]))
                                {
                                    DataTable[LoadArray[x][0]].Add(LoadArray[x][y], new List<Object>());
                                    //InfoTable[LoadArray[x][0]].Add(LoadArray[x][y], new object[2]);
                                }
                                if (timestampdict.ContainsKey(LoadArray[x][0]))
                                {
                                    //LoadCom = new MySqlCommand(string.Format("Select {0} from {1} where {2} between convert(DateTime,'{3}',104) and convert(DateTime,'{4}',104)", LoadArray[x][y], LoadArray[x][0], timestampdict[LoadArray[x][0]], LoadStart.ToString("dd.MM.yyyy"), LoadEnd.ToString("dd.MM.yyyy")), myConnection);
                                    LoadCom = new MySqlCommand(string.Format("Select {0} from {1} where {2} between cast('{3}' as date) and cast('{4}' as date)", LoadArray[x][y], LoadArray[x][0], timestampdict[LoadArray[x][0]], LoadStart.ToString("dd.MM.yyyy"), LoadEnd.ToString("dd.MM.yyyy")), myConnection);
                                    //InfoTable[LoadArray[x][0]][LoadArray[x][y]][0] = true;
                                }
                                else
                                {
                                    LoadCom = new MySqlCommand(string.Format("Select {0} from {1}", LoadArray[x][y], LoadArray[x][0]), myConnection);
                                    //InfoTable[LoadArray[x][0]][LoadArray[x][y]][0] = false;
                                }
                                using (MySqlDataReader reader = LoadCom.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        //System.Diagnostics.Debug.WriteLine(reader.GetString(0));
                                        DataTable[LoadArray[x][0]][LoadArray[x][y]].Add(reader[0]);
                                    }
                                }
                                //if (DataTable[LoadArray[x][0]][LoadArray[x][y]].Count() != 0) InfoTable[LoadArray[x][0]][LoadArray[x][y]][1] = DataTable[LoadArray[x][0]][LoadArray[x][y]][0].GetType();
                                //else InfoTable[LoadArray[x][0]][LoadArray[x][y]][1] = null;
                            }
                        }
                        myConnection.Close();
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 53)
                        {
                            MessageBox.Show("Der Server konnte nicht gefunden werden. Bitte überprüfen Sie die Angaben in der .ini und korrigieren Sie diese nötigenfalls.");
                        }
                        if (ex.Number == -2)
                        {
                            MessageBox.Show("Es trat ein Verbindungstimeout auf.");
                        }
                    }
                    catch (Exception error)
                    {
                        MessageBox.Show(error.ToString());
                    }
                }
            }
        }


        //CacheStuff

        private void CacheCreation()
        {
            FileInfo ExcelDoc = new FileInfo(CachePath + location + "Cache.xlsx");

            if (ExcelDoc.Exists)
            {
                bool Conf = true;
                while (IsFileLocked(ExcelDoc))
                {
                    string[] Text = new String[] { "Die Datei '" + CachePath + location + "Cache.xlsx' wird durch ein anderes Programm vom Zugriff gesperrt.", "Wollen Sie den Zugriff erneut versuchen oder den Prozess abbrechen?" };
                    LockedAccessForm Confirmation = new LockedAccessForm(Text);
                    Confirmation.Text = "Datei wird bereits verwendet.";
                    Confirmation.ShowDialog();
                    Conf = Confirmation.Confirmed;
                    Confirmation.Close();
                    if (Conf) continue;
                    else break;
                }

                if (!Conf) return;
            }

            LoadStart = DateTime.Parse("1.1.1753"); //Lang lebe Philip Stanhope
            LoadEnd = DateTime.MaxValue;
            //LOAD
            bool[][] CheckedMemory = new bool[treeView1.GetNodeCount(false)+1][];
            CheckedMemory[0] = new bool[treeView1.GetNodeCount(false)];
            int x = 0;
            int y = 0;

            foreach (System.Windows.Forms.TreeNode Node in treeView1.Nodes)
            {
                CheckedMemory[0][x] = Node.Checked;
                CheckedMemory[x + 1] = new bool[Node.Nodes.Count];
                foreach (System.Windows.Forms.TreeNode node in Node.Nodes)
                {
                    CheckedMemory[x + 1][y] = node.Checked;
                    y++;
                }
                x++;
                y = 0;
            }
            x = 0;

            CheckUncheckTreeNode(treeView1.Nodes, true);
            DataLoad();
            CheckUncheckTreeNode(treeView1.Nodes, false);

            foreach (System.Windows.Forms.TreeNode Node in treeView1.Nodes)
            {
                Node.Checked = CheckedMemory[0][x];
                foreach (System.Windows.Forms.TreeNode node in Node.Nodes)
                {
                    node.Checked = CheckedMemory[x + 1][y];
                    y++;
                }
                x++;
                y = 0;
            }

            Excel.Application oXL = new Excel.Application();
            oXL.SheetsInNewWorkbook = 1;
            Excel.Workbooks oWBs = oXL.Workbooks;
            Excel._Workbook oWB;

            try
            {

                oWB = oWBs.Open(CachePath + location + "Cache.xlsx");

            }
            catch (System.Runtime.InteropServices.COMException)
            {

                oWB = oWBs.Add();
                oWB.SaveAs(CachePath + location + "Cache.xlsx");

            }

            int columncount = 0;
            foreach (string tablekey in DataTable.Keys)
            {
                columncount += DataTable[tablekey].Keys.Count();
            }

            ProgressBarForm Progress = new ProgressBarForm("ExcelWrite", columncount);
            int tablecount = 0;
            Progress.Show();

            try
            {
                foreach (string tablekey in DataTable.Keys)
                {
                    Excel._Worksheet oSheet;
                    columncount = 1;
                    tablecount++;

                    try
                    {

                        oSheet = oWB.Worksheets[Truncator(database + "_" + tablekey, 31)];

                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {

                        oSheet = (Excel._Worksheet)oWB.Sheets.Add();
                        oSheet.Name = Truncator(database + "_" + tablekey, 31);
                        oSheet.Cells[1, 1].Value = "Datenbankname";
                        oSheet.Cells[2, 1].Value = database;
                        oSheet.Cells[1, 2].Value = "Tabellenname";
                        oSheet.Cells[2, 2].Value = tablekey;

                    }

                    int column = 1; // Initialize for keys.

                    if (timestampdict.ContainsKey(tablekey)) oSheet.Cells[3, column++].Value = timestampdict[tablekey];

                    foreach (string key in DataTable[tablekey].Keys)
                    {
                        if (timestampdict.ContainsKey(tablekey) && key == timestampdict[tablekey]) continue;
                        oSheet.Cells[3, column].Value = key;
                        column++;
                    }

                    Object[,] dArray = new Object[DataTable[tablekey][DataTable[tablekey].Keys.First()].Count, DataTable[tablekey].Count];

                    int row = 0; // Initialize for values in key.
                    column = 0; // Initialize for keys.

                    if (timestampdict.ContainsKey(tablekey))
                    {
                        foreach (DateTime value in DataTable[tablekey][timestampdict[tablekey]])
                        {
                            dArray[row, column] = value;
                            row++;
                        }

                        column++; // increment for next key.

                        Progress.progressBar1.Increment(1);
                        Progress.label1.Text = "Seite: " + tablecount + "/" + DataTable.Keys.Count() + "         Spalte: " + columncount + "/" + DataTable[tablekey].Keys.Count();
                        columncount++;
                        Progress.Activate();
                        Application.DoEvents();
                    }



                    foreach (string key in DataTable[tablekey].Keys)
                    {
                        if (timestampdict.ContainsKey(tablekey) && key == timestampdict[tablekey]) continue;
                        row = 0; // Initialize for values in key.

                        foreach (Object value in DataTable[tablekey][key])
                        {
                            dArray[row, column] = value;
                            row++;
                        }

                        column++; // increment for next key.

                        Progress.progressBar1.Increment(1);
                        Progress.label1.Text = "Seite: " + tablecount + "/" + DataTable.Keys.Count() + "         Spalte: " + columncount + "/" + DataTable[tablekey].Keys.Count();
                        columncount++;
                        Progress.Activate();
                        Application.DoEvents();
                    }


                    Excel.Range oXR1 = (Excel.Range)oSheet.Cells[4, 1];
                    Excel.Range oXR2 = (Excel.Range)oSheet.Cells[4 + dArray.GetLength(0) - 1, column];
                    Excel.Range oXR = oSheet.get_Range(oXR1, oXR2);
                    oXR.Value2 = dArray;
                    if (timestampdict.ContainsKey(tablekey))
                    {
                        oXR = oSheet.Range[oSheet.Cells[4, 1], oSheet.Cells[oSheet.UsedRange.Rows.Count, 1]];
                        oXR.EntireColumn.NumberFormat = "dd.mm.yyyy ss:mm:hh";
                    }

                    dynamic allDataRange = oSheet.Range[oSheet.Cells[4, 1], oSheet.Cells[oSheet.UsedRange.Rows.Count, oSheet.UsedRange.Columns.Count]];
                    allDataRange.Sort(allDataRange.Columns[1], Excel.XlSortOrder.xlAscending);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(allDataRange);

                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oSheet);
                }
                oXL.DisplayAlerts = false;
                oWB.Save();
                Progress.Close();
                //MessageBox.Show("Cache in Excel gespeichert");
                try
                {

                    ((Excel.Worksheet)oXL.ActiveWorkbook.Sheets["Tabelle1"]).Delete();
                    //MessageBox.Show("Tabelle1 gelöscht");

                }

                catch (System.Runtime.InteropServices.COMException /*Error*/)
                {
                    //                    MessageBox.Show(Error.ToString());
                }
                oXL.DisplayAlerts = true;
            }
            catch (Exception Error)
            {
                MessageBox.Show(Error.ToString());
            }
            finally
            {
                oWB.Close(true);
                oWBs.Close();
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWB);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWBs);
                if (oXL != null)
                {
                    oXL.Quit();
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oXL);
                }
            }
        }

        private void CacheUpdate()
        {
            List<string> MissingDocs = new List<string>();
            foreach (string location in SQLData.comboBox2.Items)
            {
                try
                {

                    FileInfo ExcelDoc = new FileInfo(CachePath + location + "Cache.xlsx");
                    if (ExcelDoc.Exists)
                    {
                        bool Conf = true;
                        while (IsFileLocked(ExcelDoc))
                        {
                            string[] Text = new String[] { "Die Datei '" + CachePath + location + "Cache.xlsx' wird durch ein anderes Programm vom Zugriff gesperrt.", "Wollen Sie den Zugriff erneut versuchen oder den Prozess abbrechen?" };
                            LockedAccessForm Confirmation = new LockedAccessForm(Text);
                            Confirmation.Text = "Datei wird bereits verwendet.";
                            Confirmation.ShowDialog();
                            Conf = Confirmation.Confirmed;
                            Confirmation.Close();
                            if (Conf) continue;
                            else break;
                        }

                        if (!Conf) return;
                    }
                    else
                    {
                        MissingDocs.Add(location);
                        MessageBox.Show("Für den Standort " + location + " existiert kein Cache. Update wird abgebrochen.");
                        continue;
                    }


                    DateTime UpdateStart = DateTime.Today.AddDays(-7);
                    DateTime UpdateEnd = DateTime.Today;


                    Excel.Application oXL = new Excel.Application();
                    Excel.Workbooks oWBs = oXL.Workbooks;
                    Excel._Workbook oWB = oWBs.Open(CachePath + location + "Cache.xlsx");

                    try
                    {
                        LoadArray = new String[oWB.Worksheets.Count][];
                        int x = 0;
                        foreach (Excel.Worksheet oSheet in oWB.Worksheets)
                        {
                            int ColumnCount = oSheet.UsedRange.Columns.Count;
                            if (ColumnCount != 0)
                            {
                                LoadArray[x] = new String[ColumnCount + 1];
                                LoadArray[x][0] = oSheet.Cells[2, 2].value;

                                for (int y = 1; y < ColumnCount + 1; y++)
                                {
                                    LoadArray[x][y] = oSheet.Cells[3, y].Value;
                                }

                                string toDisplay = string.Join(Environment.NewLine, LoadArray[x]);
                                MessageBox.Show(toDisplay);
                            }
                            x += 1;
                        }

                        //var mc = @"Server=EU-RITTER\SQLEXPRESS;Database=FFM;User Id=sa;Password=igb;";

                        DataTable = new Dictionary<string, Dictionary<string, List<object>>>();

                        using (myConnection = new MySqlConnection(ConnectionString))
                        {
                            myConnection.Open();
                            for (x = 0; x < LoadArray.Length; x++)
                            {
                                DataTable[LoadArray[x][0]] = new Dictionary<string, List<Object>>();
                                for (int y = 1; y < LoadArray[x].Length; y++)
                                {
                                    //System.Diagnostics.Debug.WriteLine(LoadEnd.ToString("yyyy-MM-dd hh:mm:ss.fff"));
                                    MySqlCommand LoadCom = new MySqlCommand(string.Format("Select {0} from {1} where {2} between convert(DateTime,'{3}',104) and convert(DateTime,'{4}',104)", LoadArray[x][y], LoadArray[x][0], timestamp, UpdateStart.ToString("dd.MM.yyyy"), UpdateEnd.ToString("dd.MM.yyyy")), myConnection);
                                    if (!DataTable[LoadArray[x][0]].ContainsKey(LoadArray[x][y]))
                                        DataTable[LoadArray[x][0]].Add(LoadArray[x][y], new List<Object>());
                                    using (MySqlDataReader reader = LoadCom.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            //System.Diagnostics.Debug.WriteLine(reader.GetString(0));
                                            DataTable[LoadArray[x][0]][LoadArray[x][y]].Add(reader[0]);
                                        }
                                    }
                                }
                            }
                            myConnection.Close();
                        }

                        foreach (string tablekey in DataTable.Keys)
                        {
                            Excel._Worksheet oSheet = (Excel._Worksheet)oWB.Sheets[database + tablekey];

                            int timecol = 1;
                            /*
                            for (int i = 1; i < oSheet.UsedRange.Columns.Count+1; i++)
                            {
                                if ((string)oSheet.Cells[1, i].Value == timestampdict[tablekey])
                                {
                                    timecol = i;
                                    break;
                                }
                            }
                            //MessageBox.Show(timecol.ToString());
                            */

                            //MessageBox.Show(oSheet.UsedRange.Rows.Count.ToString());
                            int startheight = oSheet.UsedRange.Rows.Count + 1;
                            for (int j = 2; j < oSheet.UsedRange.Rows.Count + 1; j++)
                            {
                                //MessageBox.Show(((double)oSheet.Cells[j, timecol].Value).ToString());
                                if (oSheet.Cells[j, timecol].Value > UpdateStart)
                                {
                                    startheight = j;
                                    break;
                                }
                            }

                            Object[,] dArray = new Object[DataTable[tablekey][timestampdict[tablekey]].Count, DataTable[tablekey].Count];
                            int column = 0; // Initialize for keys.

                            foreach (string key in DataTable[tablekey].Keys)
                            {
                                if (key == timestampdict[tablekey]) continue;
                                int row = 0; // Initialize for values in key.

                                foreach (Object value in DataTable[tablekey][key])
                                {
                                    dArray[row, column] = value;
                                    row++;
                                }

                                column++; // increment for next key.
                            }

                            /*
                            foreach (string key in DataTable[tablekey].Keys)
                            {
                                int row = startheight; // Initialize for values in key.
                                foreach (string value in DataTable[tablekey][key])
                                {
                                    row++;
                                    oSheet.Cells[row, column].Value = value;
                                }
                                column++; // increment for next key.
                            }
                            */

                            Excel.Range oXR1 = (Excel.Range)oSheet.Cells[startheight, 1];
                            Excel.Range oXR2 = (Excel.Range)oSheet.Cells[startheight + dArray.GetLength(0) - 1, column];
                            Excel.Range oXR = oSheet.get_Range(oXR1, oXR2);
                            oXR.Value2 = dArray;

                            System.Runtime.InteropServices.Marshal.ReleaseComObject(oSheet);
                        }
                        MessageBox.Show("Cache in Excel gespeichert");

                    }

                    catch (SqlException ex)
                    {
                        if (ex.Number == -2)
                        {
                            MessageBox.Show("Es trat ein Verbindungstimeout auf.");
                        }
                    }

                    catch (Exception error)
                    {
                        MessageBox.Show(error.ToString());
                    }

                    finally
                    {
                        oXL.DisplayAlerts = false;
                        oWB.SaveAs(CachePath + location + "Cache.xlsx");
                        oWB.Close(true);
                        oWBs.Close();
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWB);
                        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oWBs);
                        oXL.DisplayAlerts = true;
                        if (oXL != null)
                        {
                            oXL.Quit();
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(oXL);
                        }
                    }
                }

                catch (Exception E)
                {
                    MessageBox.Show(E.ToString());
                }
            }

            if (MissingDocs.Count > 0)
            {
                string Missing = "Für die Standorte ";
                foreach (string Doc in MissingDocs)
                {
                    Missing += Doc + " ";
                }
                Missing += "existierten keine Caches zum Updaten. Bitte neue Caches anlegen.";
                MessageBox.Show(Missing);
                //ANLEGEN ENABLEN
            }
        }

    }
}