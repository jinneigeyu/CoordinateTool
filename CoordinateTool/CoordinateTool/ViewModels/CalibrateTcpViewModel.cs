using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace CoordinateToolUI.ViewModels
{
    public class CalibrateTcpViewModel : BindableBase
    {

        public class Point6d : BindableBase
        {
            private double _x;
            public double X
            {
                get => _x;
                set => SetProperty(ref _x, value);
            }

            private double _y;
            public double Y
            {
                get => _y;
                set => SetProperty(ref _y, value);
            }

            private double _z;
            public double Z
            {
                get => _z;
                set => SetProperty(ref _z, value);
            }



            private double _rx;
            public double RX
            {
                get => _rx;
                set => SetProperty(ref _rx, value);
            }

            private double _ry;
            public double RY
            {
                get => _ry;
                set => SetProperty(ref _ry, value);
            }

            private double _rz;
            public double RZ
            {
                get => _rz;
                set => SetProperty(ref _rz, value);
            }

        }

        public CalibrateTcpViewModel()
        {
        }

        private ObservableCollection<Point6d> _postures = new ObservableCollection<Point6d>();
        public ObservableCollection<Point6d> Postures { get => _postures; set => SetProperty(ref _postures, value); }


        private List<Point6d> LoadPostures(string filePath)
        {
            List<Point6d> points = new List<Point6d>();

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // 假设文件中每行包含两个数字，用空格或逗号分隔
                        string[] parts = line.Split(new string[] { " ", ",", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length >= 6 && double.TryParse(parts[0], out double x) 
                            && double.TryParse(parts[1], out double y)
                            && double.TryParse(parts[2], out double z)
                            && double.TryParse(parts[3], out double rx)
                            && double.TryParse(parts[4], out double ry)
                            && double.TryParse(parts[1], out double zy)
                            )
                        {
                            Point6d point = new Point6d { X = x, Y = y,Z=z,RX=rx,RY=ry,RZ=ry};
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

        private string _posturesFile;
        public string PosturesFile
        {
            get => _posturesFile;
            set => SetProperty(ref _posturesFile, value);
        }


        public ICommand LoadPosturesCmd
        {
            get => new DelegateCommand(() =>
            {
                string file = Common.Helper.FileDialog.OpenDialog(null);
                if (file == null)
                    return;
                PosturesFile = file;
                var ps = LoadPostures(file);

                if (ps != null)
                {
                    Postures = new ObservableCollection<Point6d>(ps);
                }
            });
        }



        public ICommand DoCalibTcp
        {
            get => new DelegateCommand(() => 
            {

            });

        }
    }
}

