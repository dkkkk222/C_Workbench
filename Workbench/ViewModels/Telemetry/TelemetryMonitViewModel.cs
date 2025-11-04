using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Events;
using Workbench.Communication;
using Workbench.Controls.Controls.Scottplot;
using Workbench.Db.Tables;
using Workbench.Events;
using Workbench.Models;
using Workbench.Utils;

namespace Workbench.ViewModels.Telemetry
{
    public class TelemetryMonitViewModel : AvaDocument
    {
        public System.Timers.Timer _timer = new System.Timers.Timer();
        private readonly ProjectManager _projectManager;
        private readonly IEventAggregator _eventAggregator;
        public TelemetryMonitViewModel(IEventAggregator eventAggregator, ProjectManager projectManager)
        {
            _projectManager = projectManager;
            _eventAggregator = eventAggregator;
            _timer.Interval = 1000; // 设置触发间隔
            _timer.Elapsed += _timer_Elapsed; ; // 设置触发事件

        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var convertProjectTag = UtilsFunc.HexStringToBytes(ProjectTag);
                _projectManager.CurrentProject.CommService?.QueryTelemetryOnceAsync(1000, convertProjectTag[0]);
            }
            catch(Exception ex)
            {

            }

        }

        #region property
        private bool _IsStart=false;
        public bool IsStart
        {
            get => _IsStart;
            set => SetProperty(ref _IsStart, value);
        }
        private List<byte> _ProjectList=new List<byte>();
        public List<byte> ProjectList
        {
            get => _ProjectList;
            set => SetProperty(ref _ProjectList, value);
        }

        private string _ProjectTag;
        public string ProjectTag
        {
            get => _ProjectTag;
            set=>SetProperty(ref _ProjectTag, value);
        }

        public ObservableCollection<OptionModel> _CycleSource = new ObservableCollection<OptionModel>()
        {
            new OptionModel()
        {
            Label = "1000ms", Value = 0
        },
            new OptionModel()
        {
            Label = "1500ms", Value = 1
        },
            new OptionModel()
        {
            Label = "2000ms", Value = 2
        },
        };
        public ObservableCollection<OptionModel> CycleSource
        {
            get => _CycleSource;
            set => SetProperty(ref _CycleSource, value);
        }

        public OptionModel _SelectedCycle;
        /// <summary>
        /// 选择的档位
        /// </summary>
        public OptionModel SelectedCycle
        {
            get => _SelectedCycle;
            set
            {
                if (SetProperty(ref _SelectedCycle, value))
                {
                    int ms = 1000;
                    switch(value.Value)
                    {
                        case 0:
                            ms = 1000;
                            break;
                        case 1:
                            ms = 1500;
                            break;
                        case 2:
                            ms = 2000;
                            break;
                    }
                    _timer.Interval = ms;
                }
            }
        }

        private ObservableCollection<TelemetrySliceField> showTelemetryList = new ObservableCollection<TelemetrySliceField>();
        public ObservableCollection<TelemetrySliceField> ShowTelemetryList
        {
            get => showTelemetryList;
            set => SetProperty(ref showTelemetryList, value);
        }

        private ObservableCollection<TelemetryMonit> _bitFields = new ObservableCollection<TelemetryMonit>();
        public ObservableCollection<TelemetryMonit> BitFields
        {
            get { return _bitFields; }
            set { SetProperty(ref _bitFields, value); }
        }

        private WpfPlotSteamBase wpfPlotControl = new WpfPlotSteamBase("监测图", "间距", "幅值", yMin: -30, yMax: 30, defaultXCount: 5000);
        [JsonIgnore]
        public WpfPlotSteamBase WpfPlotControl
        {
            get => wpfPlotControl;
            set => SetProperty(ref wpfPlotControl, value);
        }
        #endregion

        #region Command
        public DelegateCommand StartAllCommand => new DelegateCommand(() =>
        {
            if(string.IsNullOrEmpty(ProjectTag))
            {
                HandyControl.Controls.MessageBox.Show("请输入遥控标识!");
                return;
            }
            if (!_timer.Enabled)
                _timer.Start();
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsStart = true;
            });
            
        });
        public DelegateCommand StopAllCommand => new DelegateCommand(() =>
        {
            if (_timer.Enabled)
                _timer.Stop();
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsStart = false;
            });
        });

        private DelegateCommand _closeCommand;
        public override DelegateCommand CloseCommand =>
            _closeCommand ?? (_closeCommand = new DelegateCommand(() =>
            {
                _eventAggregator.GetEvent<CloseTabEvent>().Publish(this.ContentId);
            }));

        public DelegateCommand<object> CheckedCommand => new DelegateCommand<object>((e) =>
        {

        });
        public DelegateCommand<object> UnCheckedCommand => new DelegateCommand<object>((e) =>
        {

        });
        #endregion

        public override void LoadData()
        {
            ShowTelemetryList.Clear();
            if (_projectManager.CurrentProject.CommService is PcmuUartService puService)
            {
                ShowTelemetryList.AddRange(puService._tlmSlices);
            }
            
        }
    }
}
