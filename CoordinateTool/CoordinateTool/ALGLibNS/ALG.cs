using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ALGLibNS
{

    #region 类型定义
    public class Common
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

    }


    [StructLayout(LayoutKind.Sequential)]
    public class ImgType
    {

        public ImgType()
        {

        }

        public ImgType(byte[] buffer, int w, int h)
        {
            data = Marshal.AllocHGlobal(buffer.Length);

            Marshal.Copy(buffer, 0, data, buffer.Length);
            this.w = w;
            this.h = h;
        }


        public ImgType(int w, int h, int c = 1)
        {

            data = Marshal.AllocHGlobal(w * h * c);

            //Marshal.Copy(buffer, 0, data, buffer.Length);
            this.w = w;
            this.h = h;
        }


        public ImgType(int w, int h, IntPtr data)
        {
            this.w = w;
            this.h = h;
            this.data = Marshal.AllocHGlobal(w * h);

            Common.CopyMemory(this.data, data, (uint)(w * h));
        }

        ~ImgType()
        {
            if (data != IntPtr.Zero)
            {
                //if (this.data != null)
                //{
                    Marshal.FreeHGlobal(data);
                //}
            }
        }

        public int w;
        public int h;
        public IntPtr data;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point6d
    {
        public double x;
        public double y;
        public double z;
        public double rx;//degree
        public double ry;
        public double rz;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Point3d
    {
        public double x;
        public double y;
        public double z;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Point2d
    {
        public double x;
        public double y;
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public Point2d tl;
        public double w;
        public double h;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Circle
    {
        public Point2d center;
        public double radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Ellipse
    {
        public Point2d center;
        public Point2d size; // x= 2*r1 , y = 2*r2
        double theta;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct TemplateParam
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string metric;       // char metric[256] = "ignore_global_polarity";

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string contrast;     // char contrast[256] = "auto";

        public double min_contrast; // double min_contrast = 15;

        [MarshalAs(UnmanagedType.I1)]
        public bool is_shapemodel;  // bool is_shapemodel = true;

        [MarshalAs(UnmanagedType.I1)]
        public bool is_grayImg;     // bool is_grayImg = true;
    }

    #endregion

    public class ALGLib
    {
        const string LIBRARY_ALG = "CoordinateAlg.dll";

        [DllImport(LIBRARY_ALG, CallingConvention = CallingConvention.Cdecl)]
        private extern static int CalibCoordinateMap(IntPtr pixels, IntPtr worlds, int n,  string calibFile);

        /// <summary>
        /// 标定坐标系通用接口
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="worlds"></param>
        /// <param name="midpoint"></param>
        /// <param name="calibFile"></param>
        /// <returns></returns>
        public static int CalibCoordinateMap(List<Point2d> pixels, List<Point2d> worlds, string calibFile)
        {
            int n = pixels.Count;
            IntPtr ptrPixels = Marshal.AllocHGlobal(Marshal.SizeOf<Point2d>() * n);
            IntPtr ptrWorlds = Marshal.AllocHGlobal(Marshal.SizeOf<Point2d>() * n);
            //IntPtr ptrMidPoint = Marshal.AllocHGlobal(Marshal.SizeOf<Point2d>() * 1);

            int ret = 0;
            try
            {
                // 将数据复制到非托管内存中
                for (int i = 0; i < n; i++)
                {
                    IntPtr elementPtr1 = IntPtr.Add(ptrPixels, i * Marshal.SizeOf<Point2d>());
                    Marshal.StructureToPtr(pixels[i], elementPtr1, false);

                    IntPtr elementPtr2 = IntPtr.Add(ptrWorlds, i * Marshal.SizeOf<Point2d>());
                    Marshal.StructureToPtr(worlds[i], elementPtr2, false);
                }

                // 现在 ptr 包含了指向 Point2d 数据的指针
                // 可以将 ptr 传递给需要原始指针的外部函数

                //Marshal.StructureToPtr(midpoint, ptrMidPoint, false);
                ret = CalibCoordinateMap(ptrPixels, ptrWorlds, n,  calibFile);
            }
            finally
            {
                // 释放非托管内存
                Marshal.FreeHGlobal(ptrPixels);
                Marshal.FreeHGlobal(ptrWorlds);
                //Marshal.FreeHGlobal(ptrMidPoint);
            }

            return ret;
        }

        [DllImport(LIBRARY_ALG, CallingConvention = CallingConvention.Cdecl)]
        private extern static int CalculateDstPoints(string calibFile, IntPtr pixels, int n, IntPtr dstPoints, bool inverse = false);

        public static List<Point2d> CalculateDstPoints(string calibFile, List<Point2d> pixels, bool inverse = false)
        {
            List<Point2d> dstPoints =null;

            int n = pixels.Count;
            IntPtr ptrPixels = Marshal.AllocHGlobal(Marshal.SizeOf<Point2d>() * n);
            IntPtr ptrWorlds = Marshal.AllocHGlobal(Marshal.SizeOf<Point2d>() * n);
            int ret = 0;
            try
            {
                // 将数据复制到非托管内存中
                for (int i = 0; i < n; i++)
                {
                    IntPtr elementPtr1 = IntPtr.Add(ptrPixels, i * Marshal.SizeOf<Point2d>());
                    Marshal.StructureToPtr(pixels[i], elementPtr1, false);
                }

                // 现在 ptr 包含了指向 Point2d 数据的指针
                // 可以将 ptr 传递给需要原始指针的外部函数

                ret = CalculateDstPoints(calibFile, ptrPixels, n, ptrWorlds, inverse);
                if (ret == 0)  // alg success
                {
                    dstPoints = new List<Point2d>();

                    for (int i = 0; i < n; i++)
                    {
                        IntPtr elementPtr2 = IntPtr.Add(ptrWorlds, i * Marshal.SizeOf<Point2d>());
                        var p = Marshal.PtrToStructure<Point2d>(elementPtr2);
                        dstPoints.Add(p);
                    }
                }
            }
            finally
            {
                // 释放非托管内存
                Marshal.FreeHGlobal(ptrPixels);
                Marshal.FreeHGlobal(ptrWorlds);
            }

            return dstPoints;
        }


    }
}
