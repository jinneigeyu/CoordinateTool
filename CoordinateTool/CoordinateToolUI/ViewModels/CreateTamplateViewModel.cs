using ALGLibNS;
using ALGTool.Events;
using HalconDotNet;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Resources;

namespace ALGTool.ViewModels
{
    public enum ROIType
    {
        Rect,
        Circle
    }

    public class TemplateParamCS : BindableBase
    {
        private bool _isShm = true;
        public bool IsShm
        {
            get => _isShm;

            set => SetProperty(ref _isShm, value);
        }


        private bool _isGrayImg = true;
        public bool IsGrayImg
        {
            get => _isGrayImg;
            set => SetProperty(ref _isGrayImg, value);
        }


        private double _minContrast = 15;
        public double MinContrast
        {
            get => _minContrast; set => SetProperty(ref _minContrast, value);
        }

        private string _metric = "ignore_global_polarity";
        public string Metric
        {
            get => _metric; set => SetProperty(ref _metric, value);
        }

        private string _contrast = "auto";
        public string Contrast
        {
            get => _contrast;
            set => SetProperty(ref _contrast, value);
        }

        public TemplateParam ToParam()
        {
            return new TemplateParam()
            {
                is_grayImg = _isGrayImg,
                is_shapemodel = _isShm,
                min_contrast = _minContrast,
                metric = _metric,
                contrast = _contrast
            };
        }

    }

    public class ControlHelper
    {
        /// <summary>
        /// 根据控件的Name获取控件对象
        /// </summary>
        /// <typeparam name="T">控件类型</typeparam>
        /// <param name="controlName">Name</param>
        /// <returns></returns>
        public static T GetControlObject<T>(string controlName, object userControl)
        {
            try
            {
                Type type = userControl.GetType();
                FieldInfo fieldInfo = type.GetField(controlName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    T obj = (T)fieldInfo.GetValue(userControl);
                    return obj;
                }
                else
                {
                    return default(T);
                }

            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }

    public class CreateTamplateViewModel : BindableBase
    {
        protected HSmartWindowControlWPF HWin;
        protected HTuple hv_DrawID;
        protected HImage templateImg = null;
        protected HDrawingObject.HDrawingObjectCallback drawingCallBack;
        protected object image_lock = new object();
        protected object image_lock1 = new object();
        protected HTuple roiParam;

        protected double imgW;
        protected double imgH;

        private HTuple _roiValues;


        public CreateTamplateViewModel()
        {
            drawingCallBack += OnHalconDrawingCallBack;
        }


        private ObservableCollection<string> _metrics = new ObservableCollection<string>()
        {
            "use_polarity",   "ignore_global_polarity" ,  "ignore_local_polarity",  "ignore_color_polarity"
        }
        ;
        public ObservableCollection<string> Metrics
        {
            get => _metrics;
            set => SetProperty(ref _metrics, value);
        }

        private ObservableCollection<string> _contrasts = new ObservableCollection<string>()
        {
            "auto", "auto_contrast", "auto_contrast_hyst"
        }
       ;
        public ObservableCollection<string> Contrasts
        {
            get => _contrasts;
            set => SetProperty(ref _contrasts, value);
        }


        private int _roiTypeIndex = 0;
        public int RoiTypeIndex
        {
            get => _roiTypeIndex;
            set
            {
                SetProperty(ref _roiTypeIndex, value);
            }
        }

        private bool? _useRGB = null;
        public bool? UseRGB
        {
            get => _useRGB;
            set
            {
                SetProperty(ref _useRGB, value);


            }
        }

        private string _templateFileName = "temp";
        public string TemplateFileName
        {
            get => _templateFileName;
            set => SetProperty(ref _templateFileName, value);
        }

        private string _outFolder = "output";
        public string OutFolder
        {
            get => _outFolder;
            set => SetProperty(ref _outFolder, value);
        }

        private TemplateParamCS _templateParamCS = new TemplateParamCS();
        public TemplateParamCS TemplateParamCS
        {
            get => _templateParamCS;
            set => SetProperty(ref _templateParamCS, value);
        }

        private string _imgPath;
        public string ImagePath
        {
            get => _imgPath;
            set => SetProperty(ref _imgPath, value);
        }


        public ROIType ROIType { get; private set; }

        public ICommand LoadImgCmd
        {
            get
            {
                return new DelegateCommand<object>((obj) =>
                {
                    ImagePath = Common.Helper.FileDialog.OpenImgDialog();
                    if (string.IsNullOrEmpty(ImagePath))
                    {
                        //PublishEvent.PublishInfoBubbleMsgEvent("未选择图片文件，加载图片失败");

                        PublishEvent.BoxMessage(new MessageType("Error", "Empty image path!"));
                        return;
                    }

                    HOperatorSet.ReadImage(out var image, ImagePath);
                    // 显示图像
                    //HOperatorSet.DispImage(image, HWin.HalconWindow);
                    //HOperatorSet.DispObj(image, HWin.HalconWindow);
                    bool isrgb = false;
                    // 获取HSmartWindowControlWPF的宽度和高度
                    templateImg = Common.Helper.HImgConverter.HobjectToHImage(image, isrgb);

                    HOperatorSet.DispObj(templateImg, HWin.HalconWindow);

                    var controlWidth = HWin.ActualWidth;
                    var controlHeight = HWin.ActualHeight;

                    HWin.HalconWindow.SetPart(0, 0, controlWidth - 1, controlWidth - 1);
                    HWin.HalconWindow.SetWindowExtents(0, 0, (int)controlWidth - 1, (int)controlHeight - 1);

                    HTuple imgw, imgh;
                    HOperatorSet.GetImageSize(templateImg, out imgw, out imgh);
                    imgH = imgh;
                    imgW = imgw;
                });
            }
        }

        public ICommand SetOutDirCmd
        {
            get => new DelegateCommand(() =>
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();

                // 设置对话框的属性
                folderDialog.Description = "请选择一个文件夹";
                folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
                folderDialog.ShowNewFolderButton = true;

                // 打开文件夹选择对话框，并处理结果
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    // 用户选择了文件夹，可以获取选中的文件夹路径
                    string selectedPath = folderDialog.SelectedPath;
                    Console.WriteLine($"选择的文件夹路径：{selectedPath}");

                    OutFolder = selectedPath;
                }
                else
                {
                    PublishEvent.BoxMessage(new MessageType("Error", "用户取消了选择文件夹操作"));

                }

            });
        }


        public ICommand CmdLoaded
        {
            get
            {
                return new DelegateCommand<object>((obj) =>
                {

                    ALGLib.InitAlg();
                    HWin = ControlHelper.GetControlObject<HSmartWindowControlWPF>("hWin", obj);

                });
            }
        }


        public ICommand AddRoiCmd
        {
            get => new DelegateCommand<object>((obj) =>
            {
                if (templateImg == null)
                {
                    PublishEvent.BoxMessage(new MessageType("Error", "Image is null!"));
                    return;
                }

                int type = (int)obj;

                ROIType = (ROIType)type;

                if (ROIType == ROIType.Rect)
                    roiParam = new HTuple("row1").TupleConcat("column1").TupleConcat("row2").TupleConcat("column2");
                else
                    roiParam = new HTuple("row").TupleConcat("column").TupleConcat("radius");

                hv_DrawID?.Dispose();
                if (type == 0)
                    HOperatorSet.CreateDrawingObjectRectangle1(imgH / 2 - 100, imgW / 2 - 100, imgH / 2 + 100, imgW / 2 + 100, out hv_DrawID);
                else
                {
                    HOperatorSet.CreateDrawingObjectCircle(imgH / 2, imgW / 2, 100, out hv_DrawID);
                }
                SetCallbacks(hv_DrawID);
            });
        }



        public ICommand CreateTemplateCmd
        {
            get => new DelegateCommand<object>((obj) =>
            {
                if (templateImg == null || hv_DrawID == null || ImagePath == null)
                {
                    PublishEvent.BoxMessage(new MessageType("Error", "Check load image and Roi"));

                    return;
                }

                var p = TemplateParamCS.ToParam();
                string extension = TemplateParamCS.IsShm ? ".shm" : ".ncc";
                var templatePath = Path.Combine(OutFolder, TemplateFileName + extension);
                Directory.CreateDirectory(OutFolder);

                var roi = getROIRect(ROIType);

                int ret = 0;
                if (ROIType == ROIType.Rect)
                {
                     var rect = (ALGLibNS.Rect)roi;
                    ret = ALGLib.CreateTemplateRect(ImagePath, rect, p, templatePath);
                }
                else if (ROIType == ROIType.Circle)
                {
                    Circle circle = (Circle)roi;

                    ret = ALGLib.CreateTemplateCircle(ImagePath, circle, p, templatePath);

                }
                else
                {
                    PublishEvent.BoxMessage(new MessageType("Error", "Invalid Roi Type"));
                    return;

                }


                PublishEvent.BoxMessage(new MessageType("Success", "Create template finish:" + templatePath));

            });
        }



        protected void SetCallbacks(HTuple draw_id)
        {

            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(drawingCallBack);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_resize", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_drag", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_attach", ptr);
            HOperatorSet.SetDrawingObjectCallback(draw_id, "on_select", ptr);
            lock (image_lock)
            {
                HOperatorSet.SetDrawingObjectParams(draw_id, "color", "green");
                HOperatorSet.AttachDrawingObjectToWindow(HWin.HalconWindow, draw_id);
            }
        }



        private void OnHalconDrawingCallBack(IntPtr draw_id, IntPtr window_handle, string type)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    lock (image_lock)
                    {
                        //刷新半径 圆心
                        HOperatorSet.GetDrawingObjectParams(hv_DrawID, roiParam, out _roiValues);

                        //Center.Y = values[0].D;
                        //Center.X = values[1].D;
                        //Radius = values[2].D;
                    }
                }));
            }
            catch (Exception e)
            {
                PublishEvent.BoxMessage(new MessageType("Error", e.Message));
            }
        }


        private object getROIRect(ROIType roiType)
        {
            if (roiType == ROIType.Rect)
            {
                var  roi = new ALGLibNS.Rect();

                roi.tl.y = _roiValues[0].D;
                roi.tl.x = _roiValues[1].D;

                roi.h = _roiValues[2].D - _roiValues[0].D;
                roi.w = _roiValues[3].D - _roiValues[1].D;
                return roi;

            }
            else
            {
                var  roi = new ALGLibNS.Circle();

                roi.center.x = _roiValues[1].D;
                roi.center.y = _roiValues[0].D;
                roi.radius = _roiValues[2].D;
                return roi;
            }

        }

    }
}