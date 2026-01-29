using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;
using log4net;
using PPEC.Communication.DB;
using PPEC.Communication.Model;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Workbench.Db.IService;
using Workbench.Models;
using Workbench.Utils;
using Workbench.Views.Telemetry;
using Workbench.Views.Windows;
using Workbench.Views;
using static LinqToDB.Reflection.Methods.LinqToDB;
using System.IO;
using NPOI.SS.Formula.Functions;
using System.Windows.Data;
using System.ComponentModel;
using NPOI.XSSF.Streaming.Values;

namespace Workbench.ViewModels.Telemetry
{
    public class TelemetryManagerViewModel : BindableBase, IDialogAware
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TelemetryManagerViewModel));
        private ProjectManager _projectManager;
        private ICpService _cpService;
        public MainServices mainService { get; set; }
        private IDialogService _dialogService;
        private readonly IMapper _mapper;
        public TelemetryManagerViewModel(FileHandler fileHandler, IContainerProvider containerProvider, IMapper mapper, ProjectManager projectManager, ICpService cpService, IDialogService dialogService)
        {
            _dialogService= dialogService; 
            _projectManager = projectManager;
            _mapper = mapper;
            _cpService = cpService;
            TelemetryItemsView = CollectionViewSource.GetDefaultView(TelemetryItems);
            TelemetryItemsView.Filter = TelemetryFilter;
            IsTelemetryReturnCheck = _projectManager.CurrentProject.IsTelemetryReturnCheck;
        }

        private ObservableCollection<TelemetryCode> _telemetryItems = new ObservableCollection<TelemetryCode>();
        /// <summary>
        /// 详情
        /// </summary>
        public ObservableCollection<TelemetryCode> TelemetryItems
        {
            get => _telemetryItems;
            set 
            {
                if (SetProperty(ref _telemetryItems, value))
                {
                    // 换了整个集合时，重新获取视图
                    TelemetryItemsView = CollectionViewSource.GetDefaultView(_telemetryItems);
                    TelemetryItemsView.Filter = TelemetryFilter;
                }
            }
        }

        private ICollectionView _telemetryItemsView;
        public ICollectionView TelemetryItemsView
        {
            get => _telemetryItemsView;
            private set => SetProperty(ref _telemetryItemsView, value);
        }


        public bool _isTelemetryReturnCheck;
        public bool IsTelemetryReturnCheck
        {
            get => _isTelemetryReturnCheck;
            set
            {
                if(SetProperty(ref _isTelemetryReturnCheck, value))
                {
                    _projectManager.CurrentProject.IsTelemetryReturnCheck = value;
                }

            }
        }

        private bool _checkAll = false;
        public bool CheckAll
        {
            get => _checkAll;
            set
            {
                SetProperty(ref _checkAll, value);               
                // 只对当前视图（已筛选后的数据）做全选/全不选
                if (TelemetryItemsView == null)
                    return;

                foreach (var obj in TelemetryItemsView)
                {
                    if (obj is TelemetryCode item)
                    {
                        item.IsChecked = value;
                    }
                }
            }
        }

        private string _treeKeyword;
        public string TreeKeyword
        {
            get => _treeKeyword;
            set
            {
                if (SetProperty(ref _treeKeyword, value))
                {
                    // 文本变化就刷新过滤
                    TelemetryItemsView?.Refresh();
                }
            }
        }

        #region Method
        private bool TelemetryFilter(object obj)
        {
            if (obj is not TelemetryCode item)
                return false;

            if (string.IsNullOrWhiteSpace(TreeKeyword))
                return true; // 没填搜索条件时，全部显示

            var keyword = TreeKeyword.Trim();

            // 下面示例：按编号、名称、指令码等模糊匹配
            return (item.Code != null && item.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (item.Name != null && item.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                || (item.Type != null && item.Type.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
        public async Task GetTeleLisst()
        {
            TelemetryItems.Clear();
            var teleList=await _cpService.GetTeleList(_projectManager.CurrentProject.Chip.ChipId);
            TelemetryItems.AddRange(teleList); 
        }

        public async Task DelCode(string codeId)
        {
            await _cpService.DeleteTeleByChipAsync(codeId);
            await GetTeleLisst();
        }

        public async Task UpdateCode(TelemetryCode item)
        {
            await _cpService.UpdateTelemetryAsync(item);
        }

        public DelegateCommand AddCommand => new DelegateCommand(() => {
            _dialogService.Show(nameof(TelemetryAddView), new DialogParameters(),async result =>
            {
                if(result.Result!= ButtonResult.Cancel)
                {
                    await GetTeleLisst();
                }
                
            }, nameof(ShowAddWindow));
        });
        public DelegateCommand BatchDelCommand => new DelegateCommand(async () =>
        {
            var result = MessageBox.Show("是否批量删除指令", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                
                    try
                    {
                        var tempRemove = TelemetryItems.Where(t => t.IsChecked).ToArray();
                        foreach (var seq in tempRemove)
                        {
                            await _cpService.DeleteTeleByChipAsync(seq.Id);                            
                             
                        }
                        await GetTeleLisst();
                    CheckAll = false;
                    }
                    catch (Exception ex)
                    {

                    }
                
            }
        });
        #endregion

        #region Command
        public DelegateCommand ExportCommand => new DelegateCommand(async () =>
        {
            try
            {
            
                var ListTele = await _projectManager.GetTeleLisst(_projectManager.CurrentProject.Chip.ChipId);

                var exportService = new ExcelExportService();
                byte[] excelData = exportService.ExportTelemetryCodes(ListTele);
                ShowSaveFileDialog(excelData);
                MessageBox.Show("遥控指令导出完成!");
              
            }
            catch (Exception ex)
            {
            }
        });
        private void ShowSaveFileDialog(byte[] excelData)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel文件 (*.xls)|*.xls",
                FileName = "遥控指令导出.xls"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, excelData);
                MessageBox.Show("导出成功！");
            }
        }
        public DelegateCommand ImportCommand => new DelegateCommand(async () =>
        { 
            try
            {
                string filePath=ChooseDirectory();
                RegisterExcelResolve registerExcelResolve = new RegisterExcelResolve();
                var telemetryCommand = registerExcelResolve.Telemetry(filePath);
                List<TelemetryCode> ListInsert = new List<TelemetryCode>();
                foreach (var param1 in telemetryCommand)
                {
                    string telemetryId = Guid.NewGuid().ToString("N");
                    ListInsert.Add(new TelemetryCode
                    {
                        Id = telemetryId,
                        ChipId = _projectManager.CurrentProject.Chip.ChipId,
                        Name = param1.CommandName,
                        Code = param1.CommandCode,
                        Category=param1.Category,
                        SubCategory=param1.SubCategory,
                        Type = ((int)param1.CommandType).ToString(),
                        Length = param1.CommandLength.ToString()
                    });
                }
                await _cpService.SaveTeleListAsync(_projectManager.CurrentProject.Chip.ChipId, ListInsert);
                await GetTeleLisst();
                MessageBox.Show("遥控指令导入完成!");
            }
            catch(Exception ex)
            {

            }
        });
        public string ChooseDirectory()
        {
            var path = string.Empty;
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Files (*.xlsx)|*.xlsx";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = openFileDialog.FileName;
            }
            return path;
        }
        #endregion
        #region DialogInfo

        public event Action<IDialogResult> RequestClose;

        public string Title => "指令信息";

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }
        public int AddOrEdit = 0;
        public async void OnDialogOpened(IDialogParameters parameters)
        {
            await GetTeleLisst();
             
        }

        #endregion
    }
}
