using ALGLibNS;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoordinateToolDotNet.Common
{
    public class Point2dCS : BindableBase
    {
        private double _x;
        public double X { get => _x; set => SetProperty(ref _x, value); }

        private double _y;

        public double Y { get => _y; set => SetProperty(ref _y, value); }
    }


    public class Tool
    {
       

        public static List<Point2dCS> LoadPointsFile(string filePath)
        {
            List<Point2dCS> points = new List<Point2dCS>();

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
                            Point2dCS point = new Point2dCS { X = x, Y = y };
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


        public static void WriteText(List<Point2dCS> data, string fileName)
        {
            var sw = File.CreateText(fileName);
            for (int i = 0; i < data.Count; i++)
            {
                sw.WriteLine($"{data[i].X}\t{data[i].Y}");
            }
            sw.Flush();
            sw.Close();
        }

    }

}
