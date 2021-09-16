using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class ChartsSettingsForm : Form
    {
        public bool TitleActivated;
        public bool XAxisActivated;
        public bool YAxisActivated;
        public string TitleText;
        public string OrigTitle;
        public string XAxisText;
        public string OrigXAxis;
        public string YAxisText;
        public string OrigYAxis;
        public int TitleSize;
        public int XAxisSize;
        public int YAxisSize;
        public FontStyle TitleStyle;
        public FontStyle XAxisStyle;
        public FontStyle YAxisStyle;
        public bool UseMin;
        public bool UseMax;
        public double Min;
        public double Max;
        public bool ShowPoints;
        public bool OutlierLegend;
        public bool ShowLegend;

        public ChartsSettingsForm()
        {
            InitializeComponent();

            var items = new[] {
                new { Text = "Normal", Value = FontStyle.Regular},
                new { Text = "Fett", Value = FontStyle.Bold },
                new { Text = "Kursiv", Value = FontStyle.Italic },
                new { Text = "Unterstrichen", Value = FontStyle.Underline },
                };

            var c1 = items.Clone();
            var c2 = items.Clone();
            var c3 = items.Clone();


            comboBox1.DataSource = c1;
            comboBox2.DataSource = c2;
            comboBox3.DataSource = c3;

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;

            comboBox1.DisplayMember = "Text";
            comboBox1.ValueMember = "Value";
            comboBox2.DisplayMember = "Text";
            comboBox2.ValueMember = "Value";
            comboBox3.DisplayMember = "Text";
            comboBox3.ValueMember = "Value";

            numericUpDown1.Minimum = 1;
            numericUpDown2.Minimum = 1;
            numericUpDown3.Minimum = 1;
            numericUpDown1.Maximum = 24;
            numericUpDown2.Maximum = 24;
            numericUpDown3.Maximum = 24;

            OutlierLegend = false;
        }

        public ChartsSettingsForm(string Statistics)
        {
            InitializeComponent();

            var items = new[] {
                new { Text = "Normal", Value = FontStyle.Regular},
                new { Text = "Fett", Value = FontStyle.Bold },
                new { Text = "Kursiv", Value = FontStyle.Italic },
                new { Text = "Unterstrichen", Value = FontStyle.Underline },
                };

            var c1 = items.Clone();
            var c2 = items.Clone();
            var c3 = items.Clone();


            comboBox1.DataSource = c1;
            comboBox2.DataSource = c2;
            comboBox3.DataSource = c3;

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;

            comboBox1.DisplayMember = "Text";
            comboBox1.ValueMember = "Value";
            comboBox2.DisplayMember = "Text";
            comboBox2.ValueMember = "Value";
            comboBox3.DisplayMember = "Text";
            comboBox3.ValueMember = "Value";

            numericUpDown1.Minimum = 1;
            numericUpDown2.Minimum = 1;
            numericUpDown3.Minimum = 1;
            numericUpDown1.Maximum = 24;
            numericUpDown2.Maximum = 24;
            numericUpDown3.Maximum = 24;

            OutlierLegend = false;

            checkBox4.Enabled = false;
            checkBox5.Enabled = false;
            checkBox6.Enabled = false;
            checkBox6.Checked = false;
            checkBox7.Enabled = false;
            textBox4.Enabled = false;
            textBox5.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TitleActivated = checkBox1.Checked;
            XAxisActivated = checkBox2.Checked;
            YAxisActivated = checkBox3.Checked;
            TitleText = textBox1.Text;
            XAxisText = textBox2.Text;
            YAxisText = textBox3.Text;
            TitleSize = (int)numericUpDown1.Value;
            XAxisSize = (int)numericUpDown2.Value;
            YAxisSize = (int)numericUpDown3.Value;
            TitleStyle = (FontStyle)comboBox1.SelectedValue;
            XAxisStyle = (FontStyle)comboBox2.SelectedValue;
            YAxisStyle = (FontStyle)comboBox3.SelectedValue;
            UseMin = checkBox4.Checked;
            UseMax = checkBox5.Checked;
            Min = Convert.ToDouble(textBox4.Text);
            Max = Convert.ToDouble(textBox5.Text);
            ShowPoints = checkBox6.Checked;
            OutlierLegend = checkBox7.Checked;
            ShowLegend = checkBox8.Checked;
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            textBox1.Text = OrigTitle;
            textBox2.Text = OrigXAxis;
            textBox3.Text = OrigYAxis;
            numericUpDown1.Value = 16;
            numericUpDown2.Value = 12;
            numericUpDown3.Value = 12;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            checkBox6.Checked = true;
            checkBox4.Checked = false;
            textBox4.Text = 0.ToString();
            textBox5.Text = 0.ToString();
            checkBox5.Checked = false;
            checkBox7.Checked = false;
        }

        private void Form8_Load(object sender, EventArgs e)
        {
            textBox1.Text = OrigTitle;
            textBox2.Text = OrigXAxis;
            textBox3.Text = OrigYAxis;
        }
    }
}
