using System;
using System.Collections.Generic;
using System.Text;
//using HalconDotNet;
using Microsoft.Win32;
// using Prism.Dialogs;
// using Prism.Services.Dialogs; for donet
using Prism.Services.Dialogs;


namespace Common.Helper
{
    public static class FileDialog
    {

        /// <summary>
        /// 弹出保存文件对话框，返回文件名。取消返回null
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string SaveDialog(string title, string filter)
        {
            var pSaveFileDialog = new SaveFileDialog();
            pSaveFileDialog.Filter = string.IsNullOrEmpty(filter) ? "全部文件|*.*" : filter;
            pSaveFileDialog.Title = string.IsNullOrEmpty(title) ? "保存为:" : title;
            pSaveFileDialog.RestoreDirectory = true;

            //同打开文件，也可指定任意类型的文件
            if (pSaveFileDialog.ShowDialog() == true)
            {
                return pSaveFileDialog.FileName;

            }

            return null;
        }
        public static string SaveDialog(string filter)
        {
            var pSaveFileDialog = new SaveFileDialog();
            pSaveFileDialog.Filter = string.IsNullOrEmpty(filter) ? "全部文件|*.*" : filter;
            pSaveFileDialog.Title = "保存为:";
            pSaveFileDialog.RestoreDirectory = true;

            //同打开文件，也可指定任意类型的文件
            if (pSaveFileDialog.ShowDialog() == true)
            {
                return pSaveFileDialog.FileName;

            }

            return null;
        }
        // ReSharper disable once MethodOverloadWithOptionalParameter
        /// <summary>
        /// 弹出另存为对话框，返回文件名。取消返回null
        /// </summary>
        /// <param name="defaultFileName"></param>
        /// <param name="filter"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string SaveDialogEx(string defaultFileName = null, string filter = "全部文件|*.*", string title = "另存为")
        {
            var pSaveFileDialog = new SaveFileDialog();

            pSaveFileDialog.Filter = filter;
            pSaveFileDialog.Title = title;
            pSaveFileDialog.RestoreDirectory = true;

            if (!string.IsNullOrEmpty(defaultFileName))
            {
                pSaveFileDialog.FileName = defaultFileName;
            }
            //同打开文件，也可指定任意类型的文件
            if (pSaveFileDialog.ShowDialog() == true)
            {
                return pSaveFileDialog.FileName;

            }

            return null;
        }
        public static string OpenDialog(string filter)
        {
            var pOpenFileDialog = new OpenFileDialog();

            pOpenFileDialog.Multiselect = false;
            pOpenFileDialog.Title = "打开文件";
            pOpenFileDialog.Filter = "全部文件|*.*";
            if (filter != null)
            {
                pOpenFileDialog.Filter = filter;
            }
            if (pOpenFileDialog.ShowDialog() == true)
            {
                return pOpenFileDialog.FileName;

            }
            return null;
        }
        public static string OpenImgDialog()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "(*.jpg,*.png,*.jpeg,*.bmp,*.gif)|*.jgp;*.png;*.jpeg;*.bmp;*.gif|All files(*.*)|*.*";

            openFile.Title = "选择要打开的图片文件";
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;
            openFile.Multiselect = true;
            openFile.ShowDialog();

            if (string.IsNullOrEmpty(openFile.FileName))
            {
                return null;
            }
            return openFile.FileName;


        }

        public static string OpenSelectFolderDialog(string root=null)
        {

            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();

            // 设置对话框的属性
            folderDialog.Description = "请选择一个文件夹";
            folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
            if (root != null)
            {
                folderDialog.SelectedPath = root;
            }

            folderDialog.ShowNewFolderButton = true;
            // 打开文件夹选择对话框，并处理结果
            var result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // 用户选择了文件夹，可以获取选中的文件夹路径
                string selectedPath = folderDialog.SelectedPath;
                Console.WriteLine($"选择的文件夹路径：{selectedPath}");

                return selectedPath;
            }

            return null;
        }

    }

    /*
    public class HImgConverter
    {
        //函数原型 
        public static HImage HobjectToHImage(HObject hobject, bool rgb = false)
        {
            HImage image = new HImage();


            if (rgb)
            {
                HTuple pointerRed, pointerGreen, pointerBlue, type, width, height;
                HOperatorSet.GetImagePointer3(hobject, out pointerRed, out pointerGreen, out pointerBlue, out type, out width, out height);
                image.GenImage3(type, width, height, pointerRed, pointerGreen, pointerBlue);
            }
            else
            {
                HTuple pointer, type, width, height;
                HOperatorSet.GetImagePointer1(hobject, out pointer, out type, out width, out height);
                image.GenImage1(type, width, height, pointer);
            }

            return image;
        }

        
    }
    */
}
