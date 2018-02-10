using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace golfRando
{
    public partial class Form1 : Form
    {
        bool loading = true;
        byte[] romData;
        byte[] romData2;

        int[] holeAddresses = { 0x27b4, 0x2ba9, 0x33d3, 0x35c4, 0x2aa9, 0x2fdd, 0x31e7, 0x3903, 0x37bc,
                                0x30bf, 0x2ef9, 0x2de9, 0x28cc, 0x2991, 0x34bc, 0x2d06, 0x36b7, 0x32e2 };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtSeed.Text = (DateTime.Now.Ticks % 2147483647).ToString();

            try
            {
                using (TextReader reader = File.OpenText("lastgolf.txt"))
                {
                    txtFlags.Text = reader.ReadLine();
                    txtFileName.Text = reader.ReadLine();

                    determineChecks(null, null);

                    runChecksum();
                    loading = false;
                }
            }
            catch
            {
                // ignore error
                loading = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            using (StreamWriter writer = File.CreateText("lastgolf.txt"))
            {
                writer.WriteLine(txtFlags.Text);
                writer.WriteLine(txtFileName.Text);
            }
        }

        private void runChecksum()
        {
            try
            {
                using (var md5 = SHA1.Create())
                {
                    using (var stream = File.OpenRead(txtFileName.Text))
                    {
                        lblSHAChecksum.Text = BitConverter.ToString(md5.ComputeHash(stream)).ToLower().Replace("-", "");
                    }
                }
            }
            catch
            {
                lblSHAChecksum.Text = "????????????????????????????????????????";
            }
        }

        private bool loadRom(bool extra = false)
        {
            try
            {
                romData = File.ReadAllBytes(txtFileName.Text);
                if (extra)
                    romData2 = File.ReadAllBytes(txtCompare.Text);
            }
            catch
            {
                MessageBox.Show("Empty file name(s) or unable to open files.  Please verify the files exist.");
                return false;
            }
            return true;
        }

        private void saveRom()
        {
            string options = "";
            string finalFile = Path.Combine(Path.GetDirectoryName(txtFileName.Text), "Golf_" + txtSeed.Text + "_" + txtFlags.Text + ".nes");
            File.WriteAllBytes(finalFile, romData);
            lblStatus.Text = "ROM hacking complete!  (" + finalFile + ")";
            txtCompare.Text = finalFile;
        }

        private void btnNewSeed_Click(object sender, EventArgs e)
        {
            txtSeed.Text = (DateTime.Now.Ticks % 2147483647).ToString();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFileName.Text = openFileDialog1.FileName;
                runChecksum();
            }
        }

        private void determineFlags(object sender, EventArgs e)
        {
            if (loading)
                return;

            string flags = "";
            int number = (chkHoleLocations.Checked ? 1 : 0) + (chkObstacles.Checked ? 2 : 0) + (chkTeeBoxes.Checked ? 4 : 0) +
                (chkClubs.Checked ? 8 : 0);
            flags += convertIntToChar(number);

            txtFlags.Text = flags;
        }

        private void determineChecks(object sender, EventArgs e)
        {
            if (txtFlags.Text.Length != 1) return;
            loading = true;
            string flags = txtFlags.Text;
            int number = convertChartoInt(Convert.ToChar(flags.Substring(0, 1)));
            chkHoleLocations.Checked = (number % 2 == 1);
            chkObstacles.Checked = (number % 4 >= 2);
            chkTeeBoxes.Checked = (number % 8 >= 4);
            chkClubs.Checked = (number % 16 >= 8);
            loading = false;
        }

        private string convertIntToChar(int number)
        {
            if (number >= 0 && number <= 9)
                return number.ToString();
            if (number >= 10 && number <= 35)
                return Convert.ToChar(55 + number).ToString();
            if (number >= 36 && number <= 61)
                return Convert.ToChar(61 + number).ToString();
            if (number == 62) return "!";
            if (number == 63) return "@";
            return "";
        }

        private int convertChartoInt(char character)
        {
            if (character >= Convert.ToChar("0") && character <= Convert.ToChar("9"))
                return character - 48;
            if (character >= Convert.ToChar("A") && character <= Convert.ToChar("Z"))
                return character - 55;
            if (character >= Convert.ToChar("a") && character <= Convert.ToChar("z"))
                return character - 61;
            if (character == Convert.ToChar("!")) return 62;
            if (character == Convert.ToChar("@")) return 63;
            return 0;
        }

        private void cmdRandomize_Click(object sender, EventArgs e)
        {
            try
            {
                loadRom();
                Random r1 = new Random(Convert.ToInt32(txtSeed.Text));
                if (chkHoleLocations.Checked) randomizeHoleLocations(r1);
                if (chkObstacles.Checked) randomizeObstacles(r1);
                if (chkTeeBoxes.Checked) randomizeTeeBoxes(r1);
                if (chkClubs.Checked) randomizeClubs(r1);
                saveRom();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:  " + ex.Message);
            }

        }

        private void randomizeHoleLocations(Random r1)
        {
            for (int i = 0; i < 18; i++)
            {
                // Find X/Y difference between hole and green start
                int greenXDiff = 0;
                int greenYDiff = 0;

                greenXDiff = romData[holeAddresses[i] + 0] - romData[holeAddresses[i] + 3];
                greenYDiff = romData[holeAddresses[i] + 1] - romData[holeAddresses[i] + 4];

                bool legal = false;
                while (!legal)
                {
                    romData[holeAddresses[i] + 3] = (byte)(r1.Next() % 208);
                    romData[holeAddresses[i] + 4] = (byte)((r1.Next() % 96) + 128);
                    if (romData[holeAddresses[i] + 3] + greenYDiff < 208 && romData[holeAddresses[i] + 3] + greenYDiff > 0 && romData[holeAddresses[i] + 4] + greenXDiff < 224 && romData[holeAddresses[i] + 4] + greenXDiff > 128)
                    {
                        legal = true;
                        romData[holeAddresses[i] + 0] = (byte)(romData[holeAddresses[i] + 3] + greenXDiff);
                        romData[holeAddresses[i] + 1] = (byte)(romData[holeAddresses[i] + 4] + greenYDiff);
                    }
                }
            }
        }

        private void randomizeObstacles(Random r1)
        {
            for (int i = 0; i < 18; i++)
            {
                bool legal = false;
                int j = 7;
                while (!legal && romData[holeAddresses[i] + j] != 255)
                {
                    int startY = (byte)(r1.Next() % 208);
                    int startX = (byte)((r1.Next() % 96) + 128);

                    if (Math.Abs(startY - romData[holeAddresses[i] + 0]) >= 32 && Math.Abs(startX - romData[holeAddresses[i] + 1]) >= 32)
                    {
                        romData[holeAddresses[i] + j + 1] = (byte)startY;
                        romData[holeAddresses[i] + j + 2] = (byte)startX;
                        j += 3;
                        legal = true;
                    }
                }
            }
        }

        private void randomizeTeeBoxes(Random r1)
        {
            for (int i = 0; i < 18; i++)
            {
                bool legal = false;
                while (!legal)
                {
                    int startY = (byte)(r1.Next() % 208);
                    int startX = (byte)((r1.Next() % 96) + 128);

                    if (Math.Abs(startY - romData[holeAddresses[i] + 0]) >= 32 && Math.Abs(startX - romData[holeAddresses[i] + 1]) >= 32)
                    {
                        romData[holeAddresses[i] + 5] = (byte)startY;
                        romData[holeAddresses[i] + 6] = (byte)startX;
                        legal = true;
                    }
                }
            }
        }

        private void randomizeClubs(Random r1)
        {
            for (int i = 0; i < 13; i++)
            {
                romData[0x160d + i] = (byte)(inverted_power_curve(10, 250, .4, r1));
            }
        }

        private int inverted_power_curve(int min, int max, double powToUse, Random r1)
        {
            int range = max - min;
            double p_range = Math.Pow(range, 1 / powToUse);
            double section = (double)r1.Next() / int.MaxValue;
            int points = (int)Math.Round(max - Math.Pow(section * p_range, powToUse));
            return points;
        }
    }
}
