using ALGTool.Events;
using Prism.Events;
using Prism.Mvvm;
using Prism.Dialogs; // using Prism.Services.Dialogs; for donet

namespace ALGTool.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        //private string _title = "Alg Tool";
        private string _title = "";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private IEventAggregator _ea;

        private IDialogService _dialog;

        public MainWindowViewModel(IDialogService dialog, IEventAggregator eventAggregator)
        {
            _ea = eventAggregator;

            _dialog = dialog;

            _ea.GetEvent<MessageEvent>().Subscribe(e =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(e.Message, e.Title);
                });
            });

        }
    }
}
