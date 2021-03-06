﻿using Dreambuild.Extensions;
using Dreambuild.Gis.Desktop.Properties;
using Dreambuild.Gis.Desktop.Utils;
using Dreambuild.Gis.Display;
using Dreambuild.Gis.Formats;
using Dreambuild.Utils;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace Dreambuild.Gis.Desktop
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, ILanguageSwitcher
    {
        #region Fields and constructor

        public static MainWindow Current { get; private set; }
        public static IPlugin DemoInstance { get; private set; } // newly 20130506

        private MapLayerPanelControl _mapLayerControl = new MapLayerPanelControl();
        private SearchPanelControl _searchControl = new SearchPanelControl();
        private PropertyPanelControl _propertyControl = new PropertyPanelControl();
        private FeaturePanelControl _featureControl = new FeaturePanelControl();

        private string _locale = AppConfig.GetValue("Language");

        public MainWindow()
        {
            LocalizationHelper.SetLocale(_locale);
            InitializeComponent();

            MainWindow.Current = this;

            SearchPanel.Content = _searchControl;
            PropertyPanel.Content = _propertyControl;
            FeaturePanel.Content = _featureControl;
            LayerPanel.Content = _mapLayerControl;

            this.InitializeTool();

            MapDataManager.MapDataChanged += MapDataManager_MapDataChanged;
            SelectionSet.SelectionChanged += SelectionSet_SelectionChanged;
            MyCanvas.NeedToInitializeViewerTools += MapControl_NeedToInitializeViewerTools;
            MyCanvas.LayersChanged += MapControl_LayersChanged;
            MyCanvas.ViewChanged += MapControl_ViewChanged;
            MyCanvas.MouseMove += MapControl_MouseMove;
            MyCanvas.ChildrenChanged += MyCanvas_ChildrenChanged;
        }

        #endregion

        #region Events

        public event Action BeforeMapShow; // newly 20140624
        protected void OnBeforeMapShow()
        {
            BeforeMapShow?.Invoke();
        }

        public event Action AfterMapShow;
        protected void OnAfterMapShow()
        {
            AfterMapShow?.Invoke();
        }

        #endregion

        #region Methods

        private void InitializeTool()
        {
            var toggleButtons = this.Toolbar.Children
                .Cast<ButtonBase>()
                .Where(button => button is ToggleButton)
                .Cast<ToggleButton>()
                .ToArray();

            toggleButtons.ForEach(toggleButton => toggleButton.Click += (sender, e) =>
            {
                toggleButtons.ForEach(other => other.IsChecked = false);
                (sender as ToggleButton).IsChecked = true;
            });

            ViewerToolManager.ExclusiveTool = new PanCanvasTool();
        }

        private void ResetTool()
        {
            ViewerToolManager.ExclusiveTool = new PanCanvasTool();
        }

        private void LoadDemo()
        {
            var demos = PluginManager.GetBuiltInPlugins();
            var name = CityGisConfig.XValue.ElementX("Demo").AttValue("Name");
            if (demos.Any(x => x.Name == name))
            {
                var demo = demos.First(x => x.Name == name);
                LoadDemo(demo);
            }
            else
            {
                var demo = new MainDemo();
                MainWindow.DemoInstance = demo;
                demo.OnLoad();
            }
        }

        private void LoadDemo(IPlugin plugin)
        {
            AppTitle.Text = plugin.Name;
            this.Title = "CityGIS Desktop - " + plugin.Name;
            MainWindow.DemoInstance = plugin;
            plugin.OnLoad();
        }

        public void AddPanel(string key, FrameworkElement panelControl, int order = -1)
        {
            string name = LocalizationHelper.GetString(key);
            var expander = new Expander
            {
                Header = name,
                HeaderStringFormat = key,
                Content = panelControl,
                Margin = new Thickness(5),
                Background = Brushes.Beige,
                BorderBrush = Brushes.LightGray,
                IsExpanded = true
            };

            if (order == -1)
            {
                PanelStack.Children.Add(expander);
            }
            else
            {
                PanelStack.Children.Insert(order, expander);
            }
        }

        public void HidePanel(params string[] names)
        {
            foreach (Expander expander in PanelStack.Children)
            {
                if (names.Contains(expander.Header.ToString()))
                {
                    expander.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void HideMenu()
        {
            TheMenu.Visibility = Visibility.Collapsed;
        }

        private bool PromptSaveChanges()
        {
            if (MapDataManager.IsHashChanged())
            {
                var result = MessageBox.Show(
                    messageBoxText: "Changes are not saved. Save now?",
                    caption: "Warning",
                    button: MessageBoxButton.YesNoCancel,
                    icon: MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    MenuItem_Save(null, null);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }

            return true;
        }

        public void NewMap()
        {
            if (PromptSaveChanges() == false)
            {
                return;
            }
            MapDataManager.New();
        }

        public void OpenMap()
        {
            if (PromptSaveChanges() == false)
            {
                return;
            }

            var ofd = new OpenFileDialog
            {
                Title = "Choose file",
                Filter = "City Information Markup Language (*.ciml)|*.ciml"
            };

            if (ofd.ShowDialog() == true)
            {
                MapDataManager.Open(ofd.FileName);
            }
        }

        private void ImportDxfMap()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Choose file",
                Filter = "AutoCAD DXF (*.dxf)|*.dxf"
            };

            if (ofd.ShowDialog() == true)
            {
                var importer = new DxfImporter(ofd.FileName);
                MapDataManager.Import(importer.GetMap());
            }
        }

        private void ImportShpMap()
        {
            var fbd = new FolderBrowserDialog
            {
                Description = "Choose Shapefile folder."
            };

            if (System.IO.Directory.Exists(Settings.Default.LatestShpFolder))
            {
                fbd.SelectedPath = Settings.Default.LatestShpFolder;
            }

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var importer = new MultipleShapefileImporter(fbd.SelectedPath);
                MapDataManager.Import(importer.GetMap());
                Settings.Default.LatestShpFolder = fbd.SelectedPath;
                Settings.Default.Save();
            }
        }

        #endregion

        #region Map event handlers

        void MyCanvas_ChildrenChanged()
        {
            this.FeatureCount.Text = MyCanvas.ElementCount.ToString();
        }

        void MyCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (MyCanvas.IsMapInitialized)
            {
                MyCanvas.RenderLayers();
            }
        }

        void MapControl_MouseMove(object sender, MouseEventArgs e)
        {
            var p = MyCanvas.GetWorldCoord(e.GetPosition(MapControl.Current));
            this.Message.Text = string.Format("({0:0.000}, {1:0.000})", p.X, p.Y);
        }

        void MapControl_ViewChanged()
        {
            this.SRuler.SetScaleRulerValue(MyCanvas.Scale);
        }

        void MapControl_LayersChanged()
        {
            this._mapLayerControl.Update();
        }

        void MapControl_NeedToInitializeViewerTools()
        {
            this.ResetTool();
        }

        void SelectionSet_SelectionChanged()
        {
            this._propertyControl.Update();
        }

        void MapDataManager_MapDataChanged()
        {
            this.OnBeforeMapShow();
            this.MyCanvas.InitializeMap(MapDataManager.LatestMap);
            this.ResetTool();
            this._mapLayerControl.Update();
            this._searchControl.ResetQuery();
            MyCanvas.TempLayers.ForEach(x => x.Children.Clear());
            this.OnAfterMapShow();
        }

        #endregion

        #region Toolbar event handlers

        private void PanButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new PanCanvasTool();
        }

        private void ZoomButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new RectScaleTool();
        }

        private void ZoomExtentsButton_Click(object sender, RoutedEventArgs e)
        {
            MyCanvas.ZoomExtents();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new SelectionTool();
        }

        private void RectSelectButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new RectSelectionTool();
        }

        private void MeasureButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new MeasureTool();
        }

        private void AreaMeasureButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new AreaMeasureTool();
        }

        #endregion

        #region Window event handlers

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetTool();
                SelectionSet.ClearSelection(); // newly 20120725
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            ViewerToolManager.Tools.ForEach(x => x.KeyDownHandler(sender, e));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (PromptSaveChanges() == false)
            {
                e.Cancel = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDemo();
        }

        #endregion

        #region Menu event handlers

        private void MenuItem_New(object sender, RoutedEventArgs e)
        {
            NewMap();
        }

        private void MenuItem_Open(object sender, RoutedEventArgs e)
        {
            OpenMap();
        }

        private void MenuItem_Import(object sender, RoutedEventArgs e)
        {
            var header = (sender as MenuItem).Header.ToString();
            if (header.Contains("DXF"))
            {
                ImportDxfMap();
            }
            else if (header.Contains("Shapefile"))
            {
                ImportShpMap();
            }
            else if (header.Contains("Bitmap"))
            {
                MainDemo demo = new MainDemo();
                demo.OpenBitmapReference();
            }
        }

        private void MenuItem_Export(object sender, RoutedEventArgs e)
        {
            var header = (sender as MenuItem).Header.ToString();
            if (header.Contains("DXF"))
            {
                // todo: implement dxf export
            }
            else if (header.Contains("Shapefile"))
            {
                var fbd = new FolderBrowserDialog
                {
                    Description = "Choose Shapefile folder."
                };

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var exporter = new MultipleShapefileExporter(MapDataManager.LatestMap);
                    exporter.Export(fbd.SelectedPath);
                }
            }
            else if (header.Contains("Image"))
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "JPEG Image (*.jpg)|*.jpg"
                };

                if (sfd.ShowDialog() == true)
                {
                    MapControl.Current.SaveImage(sfd.FileName);
                    MainDemo.TryOpenFile(sfd.FileName);
                }
            }
        }

        private void MenuItem_Save(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MapDataManager.LatestFileName))
            {
                MapDataManager.SaveAs(MapDataManager.LatestFileName);
                MessageBox.Show(string.Format("File saved."));
            }
            else
            {
                MenuItem_SaveAs(sender, e);
            }
        }

        private void MenuItem_SaveAs(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "City Information Markup Language (*.ciml)|*.ciml"
            };

            if (sfd.ShowDialog() == true)
            {
                MapDataManager.SaveAs(sfd.FileName);
            }
        }

        private void MenuItem_Exit(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void MenuItem_Topic(object sender, RoutedEventArgs e)
        {
            MainDemo.TryOpenFile("help.pdf");
        }

        private void MenuItem_About(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                messageBoxText: $"{MainWindow.DemoInstance.Name}\n Powered by CityGIS technology\nAuthor: WANG Yang\ndreambuild@qq.com\nCopyright 2014",
                caption: "About");
        }

        private void MenuItem_SwitchLanguage(object sender, RoutedEventArgs e)
        {
            _locale = _locale == "zh-CN" ? "en-US" : "zh-CN";
            LocalizationHelper.SetLocale(_locale);

            this.RefreshLanguage();
        }

        #endregion

        #region ILanguageSwitcher members

        public void RefreshLanguage()
        {
            var stackPanel = MainWindow.Current.FindName("PanelStack") as StackPanel;
            foreach (var element in stackPanel.Children)
            {
                if (element is Expander expander)
                {
                    if (!string.IsNullOrEmpty(expander.HeaderStringFormat))
                    {
                        expander.Header = LocalizationHelper.GetString(expander.HeaderStringFormat);
                    }

                    if (expander.Content is ILanguageSwitcher switcher)
                    {
                        switcher.RefreshLanguage();
                    }
                }
            }

            if (MainWindow.DemoInstance is ILanguageSwitcher languageSwitcher)
            {
                languageSwitcher.RefreshLanguage();
            }
        }

        #endregion

        // todo: Notification popup
    }
}
