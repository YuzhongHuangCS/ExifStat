﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MetadataExtractor;
using Aspose.Imaging.ImageOptions;

namespace ExifStat
{
    public partial class MainForm : Form
    {
        public static string FOCAL_LENGTH = "Focal Length";
        public static string FOCAL_LENGTH_35 = "Focal Length 35";
        public static string CAMERA_MODEL = "Model";
        public static HashSet<string> APSC_MODEL = new HashSet<string> { "NIKON D3400", "X-S10" };

        public MainForm() {
            InitializeComponent();
            this.Load += new EventHandler(Form_Load);
            this.KeyDown += new KeyEventHandler(Form_KeyDown);
        }

        private async void Form_Load(object sender, EventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Multiselect = true,
                Title = "Select Photos to Analysis"
            };

            int count_18 = 0;
            int count_18_35 = 0;
            int count_35_50 = 0;
            int count_50_75 = 0;
            int count_75_100 = 0;
            int count_100_150 = 0;
            int count_150 = 0;

            if (dialog.ShowDialog() == DialogResult.OK) {
                await Task.Run(() => {
                    foreach (var file in dialog.FileNames) {
                        double f = 0;
                        try {
                            var directories = ImageMetadataReader.ReadMetadata(file);
                            f = GetFocalLength(directories);
                        } catch (Exception) {
                            continue;
                        }

                        if (f > 0) {
                            Console.WriteLine($"{file} {f}");
                            if (f <= 18) {
                                count_18 += 1;
                            } else {
                                if (f <= 35) {
                                    count_18_35 += 1;
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
                                                    count_150 += 1;
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
                double total = (count_18 + count_18_35 + count_35_50 + count_50_75 + count_75_100 + count_100_150 + count_150) / 100.0;
                total = Math.Max(total, 0.01);

                DataPoint[] dp = Enumerable.Range(0, 7).Select(i => new DataPoint()).ToArray();
                dp[0].SetValueXY("<=18mm", count_18);
                dp[0].Label = string.Format("{0:0.00}%", count_18 / total);
                dp[1].SetValueXY("18-35mm", count_18_35);
                dp[1].Label = string.Format("{0:0.00}%", count_18_35 / total);
                dp[2].SetValueXY("35-50mm", count_35_50);
                dp[2].Label = string.Format("{0:0.00}%", count_35_50 / total);
                dp[3].SetValueXY("50-75mm", count_50_75);
                dp[3].Label = string.Format("{0:0.00}%", count_50_75 / total);
                dp[4].SetValueXY("75-100mm", count_75_100);
                dp[4].Label = string.Format("{0:0.00}%", count_75_100 / total);
                dp[5].SetValueXY("100-150mm", count_100_150);
                dp[5].Label = string.Format("{0:0.00}%", count_100_150 / total);
                dp[6].SetValueXY(">=150mm", count_150);
                dp[6].Label = string.Format("{0:0.00}%", count_150 / total);

                foreach (var p in dp) {
                    BarChart.Series[0].Points.Add(p);
                }
            } else {
                Application.Exit();
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.S) {      // Ctrl-S Save
                // Stops other controls on the form receiving event.
                e.SuppressKeyPress = true;

                // Do what you want here
                SaveFileDialog dialog = new SaveFileDialog {
                    Filter = "PDF Document|*.pdf|PNG Image (4x)|*.png|PNG Image (2x)|*.png|PNG Image (1x)|*.png|EMF Vector|*.emf"
                };
                if (dialog.ShowDialog() == DialogResult.OK) {
                    switch (dialog.FilterIndex) {
                        case 1:
                        case 2:
                        case 3:
                            System.IO.MemoryStream buffer = new System.IO.MemoryStream();
                            BarChart.SaveImage(buffer, ChartImageFormat.EmfPlus);
                            buffer.Position = 0;

                            if (dialog.FilterIndex == 1) {
                                var emf = Aspose.Imaging.Image.Load(buffer);
                                var options = new EmfRasterizationOptions {
                                    PageWidth = emf.Width,
                                    PageHeight = emf.Height
                                };

                                emf.Save(dialog.FileName, new PdfOptions() { VectorRasterizationOptions = options });
                            } else {
                                using (Metafile emf = new Metafile(buffer)) {
                                    MetafileHeader emf_header = emf.GetMetafileHeader();
                                    int scale = 2 * (4 - dialog.FilterIndex);
                                    using (Bitmap bitmap = new Bitmap(emf.Width * scale, emf.Height * scale)) {
                                        using (Graphics g = Graphics.FromImage(bitmap)) {
                                            g.ScaleTransform(scale * emf_header.DpiX / g.DpiX, scale * emf_header.DpiY / g.DpiY);
                                            g.DrawImage(emf, 0, 0);
                                        }
                                        bitmap.Save(dialog.FileName, ImageFormat.Png);
                                    }
                                }
                            }
                            break;

                        case 4:
                            BarChart.SaveImage(dialog.FileName, ChartImageFormat.Png);
                            break;

                        case 5:
                            BarChart.SaveImage(dialog.FileName, ChartImageFormat.EmfPlus);
                            break;
                    }
                }
            }
        }

        public double GetFocalLength(IReadOnlyList<Directory> directories) {
            bool apsc = false;
            foreach (Directory directory in directories) {
                foreach (Tag tag in directory.Tags) {
                    // Console.WriteLine($"{tag.Name} - {tag.Description}");
                    if (FOCAL_LENGTH_35 == tag.Name) {
                        string text = tag.Description;
                        double f = double.Parse(text.Substring(0, text.Length - 3));
                        return f;
                    }
                    if (CAMERA_MODEL == tag.Name && APSC_MODEL.Contains(tag.Description)) {
                        apsc = true;
                    }
                }
            }

            foreach (Directory directory in directories) {
                foreach (Tag tag in directory.Tags) {
                    if (FOCAL_LENGTH == tag.Name) {
                        string text = tag.Description;
                        double f = double.Parse(text.Substring(0, text.Length - 3));
                        if (apsc) {
                            f *= 1.5;
                        }
                        return f;
                    }
                }
            }
            return 0;
        }
    }
}
