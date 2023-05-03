using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ExifStat
{
    public partial class MainForm : Form
    {
        public static string FOCAL_LENGTH = "Focal Length";

        public MainForm() {
            InitializeComponent();
            this.Load += new EventHandler(Form_Load);
        }

        private async void Form_Load(object sender, EventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Multiselect = true,
                Title = "Select Photos to Analysis"
            };

            int count_18 = 0;
            int count_28_35 = 0;
            int count_35_50 = 0;
            int count_50_75 = 0;
            int count_75_100 = 0;
            int count_100_150 = 0;
            int count_150_200 = 0;

            if (dialog.ShowDialog() == DialogResult.OK) {
                await Task.Run(() => {
                    foreach (var file in dialog.FileNames) {
                        var directories = ImageMetadataReader.ReadMetadata(file);
                        int f = GetFocalLength(directories);
                        if (f > 0) {
                            Console.WriteLine($"{file} {f}");
                            if (f <= 18) {
                                count_18 += 1;
                            } else {
                                if (f <= 35) {
                                    count_28_35 += 1;
                                } else {
                                    if (f <= 50) {
                                        count_35_50 += 1;
                                    } else {
                                        if (f <= 75) {
                                            count_50_75 += 1;
                                        } else {
                                            if (f <= 100) {
                                                count_75_100 += 1;
                                            } else {
                                                if (f <= 150) {
                                                    count_100_150 += 1;
                                                } else {
                                                    count_150_200 += 1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                BarChart.Titles[0].Text = this.Text;
                DataPoint[] dp = Enumerable.Range(0, 7).Select(i => new DataPoint()).ToArray();
                dp[0].SetValueXY("<=18mm", count_18);
                dp[1].SetValueXY("28-35mm", count_28_35);
                dp[2].SetValueXY("35-50mm", count_35_50);
                dp[3].SetValueXY("50-75mm", count_50_75);
                dp[4].SetValueXY("75-100mm", count_75_100);
                dp[5].SetValueXY("100-150mm", count_100_150);
                dp[6].SetValueXY("150-200mm", count_150_200);

                foreach (var p in dp) {
                    BarChart.Series[0].Points.Add(p);
                }
            } else {
                Application.Exit();
            }
        }

        public int GetFocalLength(IReadOnlyList<Directory> directories) {
            foreach (Directory directory in directories) {
                foreach (Tag tag in directory.Tags) {
                    if (FOCAL_LENGTH == tag.Name) {
                        string text = tag.Description;
                        int f = int.Parse(text.Substring(0, text.Length - 3));
                        return f;
                    }
                }
            }
            return 0;
        }
    }
}
