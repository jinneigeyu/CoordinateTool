using ALGLibNS;
using ALGTool.Events;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace ALGTool.ViewModels
{
    public class CoordinateCalibrateViewModel : BindableBase
    {
        public CoordinateCalibrateViewModel()
        {

        }

        public class Point2d : BindableBase
        {
            private double _x;
            public double X { get => _x; set => SetProperty(ref _x, value); }

            private double _y;

            public double Y { get => _y; set => SetProperty(ref _y, value); }
        }


        private bool? _useEigen = true;
        public bool? UseEigen
        {
            get => _useEigen;
            set => SetProperty(ref _useEigen, value);
        }

        private int _selectIndex;
        public int SelectIndex
        {
            get => _selectIndex;
            set => SetProperty(ref _selectIndex, value);
        }

        private Point2d _doPoint = new Point2d();
        public Point2d DoPoint
        {
            get => _doPoint;
            set => SetProperty(ref _doPoint, value);
        }

    
        private ObservableCollection<Point2d> _pixels = new ObservableCollection<Point2d>();
        public ObservableCollection<Point2d> Pixels { get => _pixels; set => SetProperty(ref _pixels, value); }

        private ObservableCollection<Point2d> _worlds = new ObservableCollection<Point2d>();

        public ObservableCollection<Point2d> Worlds { get => _worlds; set => SetProperty(ref _worlds, value); }


        public ICommand LoadPixelsCmd
        {
            get => new DelegateCommand(() =>
            {
                string file = Common.Helper.FileDialog.OpenDialog(null);
                if (file == null)
                    return;
                PixelsFile = file;
                var ps = LoadPixels(file);

                if (ps != null)
                {
                    Pixels = new ObservableCollection<Point2d>(ps);
                }
            });
        }

        public ICommand LoadWorldsCmd
        {
            get => new DelegateCommand<object>((obj) =>
            {
                string file = Common.Helper.FileDialog.OpenDialog(null);
                if (file == null)
                    return;

                WorldsFile = file;
                var ps = LoadPixels(file);

                if (ps != null)
                {
                    Worlds = new ObservableCollection<Point2d>(ps);
                }

            });
        }


        public ICommand LoadMidPointCmd
        {
            get => new DelegateCommand<object>((obj) =>
            {
                string file = Common.Helper.FileDialog.OpenDialog(null);
                if (file == null)
                    return;

                var ps = LoadPixels(file);

                if (ps != null)
                {
                    if (ps.Count > 0)
                    {
                 
                        DoPoint = new Point2d();
                        DoPoint.X = ps[0].X;
                        DoPoint.Y = ps[0].Y;
                    }
                }

            });
        }

        public ICommand SetOutDirCmd
        {
            get => new DelegateCommand(() =>
            {
                var selectedPath = Common.Helper.FileDialog.OpenSelectFolderDialog();

                if (string.IsNullOrEmpty(selectedPath))
                    PublishEvent.BoxMessage(new MessageType("Error", "User cancelled folder selection"));

                OutDir = selectedPath;

            });
        }

        private List<Point2d> LoadPixels(string filePath)
        {
            List<Point2d> points = new List<Point2d>();

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // 假设文件中每行包含两个数字，用空格或逗号分隔
                        string[] parts = line.Split(new string[] { " ", ",", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length >= 2 && double.TryParse(parts[0], out double x) && double.TryParse(parts[1], out double y))
                        {
                            Point2d point = new Point2d { X = x, Y = y };
                            points.Add(point);
                        }
                        else
                        {
                            Console.WriteLine($"Skipping invalid line: {line}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
            }

            return points;
        }

        private void WriteText(List<Point2d> data, string fileName)
        {
            var sw = File.CreateText(fileName);
            for (int i = 0; i < data.Count; i++)
            {
                sw.WriteLine($"{data[i].X}\t{data[i].Y}");
            }
            sw.Flush();
            sw.Close();
        }

        public ICommand DoCalibCmd
        {
            get => new DelegateCommand(() =>
            {
                if (string.IsNullOrEmpty(OutDir))
                {
                    PublishEvent.BoxMessage(new MessageType("Error", "Not set output folder"));
                    return;
                }


                if (string.IsNullOrEmpty(CalibFileName))
                {
                    PublishEvent.BoxMessage(new MessageType("Error", "Not set output file name"));
                    return;
                }

                if (Pixels.Count < 3 || Worlds.Count < 3)
                {
                    PublishEvent.BoxMessage(new MessageType("Error", "The minimum number of points is 3 groups"));
                    return;
                }

                List<ALGLibNS.Point2d> ps = new List<ALGLibNS.Point2d>();
                List<ALGLibNS.Point2d> ws = new List<ALGLibNS.Point2d>();

                for (int i = 0; i < Pixels.Count; i++)
                {
                    ps.Add(new ALGLibNS.Point2d() { x = Pixels[i].X, y = Pixels[i].Y });
                    ws.Add(new ALGLibNS.Point2d() { x = Worlds[i].X, y = Worlds[i].Y });
                }

                Directory.CreateDirectory(OutDir);

                //var midP = new ALGLibNS.Point2d() { x = DoPoint.X, y = DoPoint.Y };
                //WriteText(new List<Point2d>() { DoPoint }, Path.Combine(OutDir, "mid_point.txt"));
                              

                int ret = ALGLib.CalibCoordinateMap(ps, ws,  Path.Combine(OutDir, CalibFileName));

                if (ret != 0)
                {
                    PublishEvent.BoxMessage(new MessageType("Error", "Output directory not set"));
                    return;
                }

                WriteText(Pixels.ToList(), Path.Combine(OutDir, "pixels.txt"));
                WriteText(Worlds.ToList(), Path.Combine(OutDir, "worlds.txt"));
                PublishEvent.BoxMessage(new MessageType("Success", "Calibration successful"));
            });
        }


        public ICommand CmdAddRow
        {
            get => new DelegateCommand<object>((obj) =>
            {
                string type = obj as string;

                if (type == null || type == string.Empty)
                {
                    return;
                }

                if (type.ToLower().StartsWith("worlds"))
                {
                    Worlds.Add(new Point2d() { X = 0, Y = 0 });
                }
                else if (type.ToLower().StartsWith("pixels"))
                {
                    Pixels.Add(new Point2d() { X = 0, Y = 0 });

                }
                else
                {
                    return;
                }


            });
        }

        public ICommand CmdDelRow
        {
            get => new DelegateCommand<object>((obj) =>
            {
                string type = obj as string;

                if (type == null || type == string.Empty)
                {
                    return;
                }

                if (type.ToLower().StartsWith("worlds"))
                {
                    if (SelectIndex < Worlds.Count)
                    {
                        Worlds.RemoveAt(SelectIndex);
                    }
                }
                else if (type.ToLower().StartsWith("pixels"))
                {
                    if (SelectIndex < Worlds.Count)
                    {
                        Pixels.RemoveAt(SelectIndex);
                    }

                }



            });
        }
        public ICommand CmdClear
        {
            get => new DelegateCommand<object>((obj) =>
            {
                string type = obj as string;

                if (type == null || type == string.Empty)
                {
                    return;
                }

                if (type.ToLower().StartsWith("worlds"))
                {

                    Worlds.Clear();
                }
                else if (type.ToLower().StartsWith("pixels"))
                {
                    Pixels.Clear();

                }
                else
                {
                    return;
                }


            });
        }

        private string _pixelsFile;
        public string PixelsFile
        {
            get => _pixelsFile;
            set => SetProperty(ref _pixelsFile, value);
        }

        private string _worldsFile;
        public string WorldsFile
        {
            get => _worldsFile;
            set => SetProperty(ref _worldsFile, value);
        }

        private string _outDir = "calibDir";
        public string OutDir
        {
            get => _outDir;
            set => SetProperty(ref _outDir, value);
        }


        private string _calibFileName = "calib.txt";


        public string CalibFileName
        {
            get => _calibFileName;
            set => SetProperty(ref _calibFileName, value);
        }


    }
}
