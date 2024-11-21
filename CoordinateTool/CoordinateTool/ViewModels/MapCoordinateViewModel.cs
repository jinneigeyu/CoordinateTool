using CoordinateToolDotNet.Common;
using CoordinateToolUI.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Input;

namespace CoordinateToolUI.ViewModels
{
    public class MapCoordinateViewModel : BindableBase
    {
        private IEventAggregator _ea;

        private IDialogService _dialog;


        public MapCoordinateViewModel(IDialogService dialog, IEventAggregator eventAggregator)
        {

            _ea = eventAggregator;

            _dialog = dialog;

            _ea.GetEvent<StringEvent>().Subscribe(e =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var s = e;

                    if (!string.IsNullOrEmpty(s))
                    {

                        try
                        {
                            MapFile = s;
                            LoadMapFileCommand.Execute(s);
                        }
                        catch (Exception )
                        {
                            
                        }
                       
                    }
                });
            });

        }


        private string _mapFile;
        public string MapFile
        {
            get => _mapFile;
            set => SetProperty(ref _mapFile, value);
        }


        private string _SrcFile;
        public string SrcFile
        {
            get => _SrcFile;
            set => SetProperty(ref _SrcFile, value);
        }



        private ObservableCollection<Point2dCS> _pixels = new ObservableCollection<Point2dCS>();
        public ObservableCollection<Point2dCS> Pixels { get => _pixels; set => SetProperty(ref _pixels, value); }


        private ObservableCollection<Point2dCS> _dst = new ObservableCollection<Point2dCS>();
        public ObservableCollection<Point2dCS> Dst { get => _dst; set => SetProperty(ref _dst, value); }

        public ICommand LoadMapFileCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                string file = Common.Helper.FileDialog.OpenDialog(null);
                if (file == null)
                    return;
                if (!File.Exists(file))
                    return;

                MapFile = file;

            });
        }




        public ICommand LoadSrcFileCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                string file = Common.Helper.FileDialog.OpenDialog(null);
                if (file == null)
                    return;

                SrcFile = file;


                var ps = Tool.LoadPointsFile(file);

                if (ps != null)
                {
                    Pixels = new ObservableCollection<Point2dCS>(ps);
                }
            });
        }


        public ICommand CalcCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                try
                {


                    if (Pixels.Count != 0)
                    {
                        if (MapFile != null)
                        {
                            List<ALGLibNS.Point2d> ps = new List<ALGLibNS.Point2d>();

                            for (int i = 0; i < Pixels.Count; i++)
                            {
                                ps.Add(new ALGLibNS.Point2d() { x = Pixels[i].X, y = Pixels[i].Y });
                            }

                            var dstPoints = ALGLibNS.ALGLib.CalculateDstPoints(MapFile, ps);

                            if (dstPoints != null)
                            {
                                Dst = new ObservableCollection<Point2dCS>();

                                foreach (var p in dstPoints)
                                {
                                    Dst.Add(new Point2dCS() { X = p.x, Y = p.y });
                                }

                                //PublishEvent.BoxMessage(new MessageType("Success", "Map Coordinate Success!"));
                            }

                        }
                    }

                }
                catch (Exception ex)
                {

                    PublishEvent.BoxMessage(new MessageType("Error", ex.Message));
                }


            });
        }



        public ICommand SaveDstCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {

                string file = Common.Helper.FileDialog.SaveDialogEx("mapped_dst.txt");
                if (file == null)
                    return;
                
                Tool.WriteText(Dst.ToList(), file);

            });
        }
    }
}
