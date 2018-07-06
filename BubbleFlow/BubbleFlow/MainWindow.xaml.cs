﻿using Dreambuild.Collections;
using Dreambuild.Extensions;
using Dreambuild.Geometry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Vector = Dreambuild.Geometry.Vector;

namespace BubbleFlow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Current { get; private set; }
        public Point Origin { get; private set; }
        public NodeBubble SelectedBubble { get; set; }
        public double Scale { get; set; }

        public Dictionary<Guid, NodeBubble> Bubbles { get; } = new Dictionary<Guid, NodeBubble>();
        public DoubleDictionary<Guid, Guid, BezierLink> Arrows { get; } = new DoubleDictionary<Guid, Guid, BezierLink>();
        public List<BoolExpression> Expressions { get; } = new List<BoolExpression>();
        private List<Line> GridLines { get; } = new List<Line>();

        public MainWindow()
        {
            this.InitializeComponent();

            MainWindow.Current = this;

            DataManager.New();
            this.InitializeTool();

            this.AddGridLinesToCanvas();
        }

        //private void InitNodesAndConnections(Workflow flow)
        //{
        //    //var workflowId = flow.id;

        //    this.Nodes.Clear();
        //    foreach (var node in flow.Nodes)
        //    {
        //        foreach (var child in MyCanvas.Children)
        //        {
        //            if (child is NodeBubble bubble)
        //            {
        //                if (bubble.Text == node.Name)
        //                {
        //                    this.Nodes.Add(bubble, node);
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    this.Links.Clear();
        //    foreach (var link in flow.Links)
        //    {
        //        var start = flow.NodesStore[link.From].GetPosition();
        //        var end = flow.NodesStore[link.To].GetPosition();
        //        foreach (var child in MyCanvas.Children)
        //        {
        //            if (child is BezierLink arrow)
        //            {
        //                if (arrow.StartPoint == start && arrow.EndPoint == end)
        //                {
        //                    this.Links.Add(link, arrow);
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}

        private void AddGridLinesToCanvas()
        {
            this.GridLines.Clear();
            int count = 10000;
            for (int i = 0; i <= count; i++)
            {
                var vline = new Line { X1 = 0, X2 = 0, Y1 = 0, Y2 = 100000, StrokeThickness = 0.2 };
                vline.Stroke = new SolidColorBrush(Colors.DarkGray);
                this.GridLines.Add(vline);
                Canvas.SetLeft(vline, i * 10 - 50000);
                Canvas.SetTop(vline, -50000);
                this.MyCanvas.Children.Add(vline);

                var hline = new Line { X1 = 0, X2 = 100000, Y1 = 0, Y2 = 0, StrokeThickness = 0.2 };
                hline.Stroke = new SolidColorBrush(Colors.DarkGray);
                this.GridLines.Add(hline);
                Canvas.SetLeft(hline, -50000);
                Canvas.SetTop(hline, i * 10 - 50000);
                this.MyCanvas.Children.Add(hline);
            }
        }

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

            this.Scale = 1;
            ViewerToolManager.AddTool(new WheelScalingTool());
            ViewerToolManager.ExclusiveTool = new PanCanvasTool();
            ViewerToolManager.SetFrameworkElement(this);
        }

        private void ResetTool()
        {
            ViewerToolManager.ExclusiveTool = new PanCanvasTool();
        }

        private void Zoom(Extents extents)
        {
            Scale = Math.Max(extents.Range(0) / this.ActualWidth, extents.Range(1) / this.ActualHeight);
            Origin = new Point(this.ActualWidth / 2 - extents.Center().X / Scale, this.ActualHeight / 2 - extents.Center().Y / Scale);
            RenderLayers();
        }

        internal void PanCanvas(System.Windows.Vector displacement)
        {
            this.Origin += displacement;
            this.RenderLayers();
        }

        internal void ScaleCanvas(double scale, Point basePoint)
        {
            double scale0 = this.Scale;
            double vx = basePoint.X - Origin.X;
            double vy = basePoint.Y - Origin.Y;
            double v1x = (scale0 / scale) * vx;
            double v1y = (scale0 / scale) * vy;
            double v2x = vx - v1x;
            double v2y = vy - v1y;

            this.Scale = scale;
            this.Origin = new Point(Origin.X + v2x, Origin.Y + v2y);
            this.RenderLayers();
        }

        private void RenderLayers()
        {
            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform { CenterX = 0, CenterY = 0, ScaleX = 1 / Scale, ScaleY = 1 / Scale });
            transform.Children.Add(new TranslateTransform { X = Origin.X, Y = Origin.Y });
            this.MyCanvas.RenderTransform = transform;
            ViewerToolManager.Tools.ForEach(tool => tool.Render());
            if (!this.MyCanvas.Children.Contains(this.GridLines[0]))
            {
                this.AddGridLinesToCanvas();
            }
        }

        private void Submit()
        {
            if (DataManager.Validate())
            {
                this.Save();
            }
        }

        private void Save()
        {
            if (DataManager.CurrentFileName == null)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (.json)|*.json|All files (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    DataManager.SaveAs(saveFileDialog.FileName);
                }

                return;
            }

            DataManager.SaveAs(DataManager.CurrentFileName);
        }

        private void Open()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (.json)|*.json|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                DataManager.Open(openFileDialog.FileName);
            }
        }

        //private void InitializeExpression()
        //{
        //    this.Expressions.Add(new BoolExpression(this.GetRule1()));
        //    this.Expressions.Add(new BoolExpression(this.GetRule2()));
        //}

        //private Func<bool> GetRule1()
        //{
        //    foreach (var node in this.Nodes.Keys)
        //    {
        //        int index = this.Nodes.Keys.ToList().IndexOf(node);
        //        if (this.Links.Keys.All(c => c.From != index && c.To != index))
        //        {
        //            return () => false;
        //        }
        //    }
        //    return () => true;
        //}

        //private Func<bool> GetRule2()
        //{
        //    int startcount = 0;
        //    int endcount = 0;
        //    foreach (var node in this.Nodes.Keys)
        //    {
        //        int index = this.Nodes.Keys.ToList().IndexOf(node);
        //        if (this.Links.Keys.All(c => c.To != index) && this.Links.Keys.Any(c => c.From == index))
        //        {
        //            startcount++;
        //        }
        //        if (this.Links.Keys.All(c => c.From != index) && this.Links.Keys.Any(c => c.To == index))
        //        {
        //            endcount++;
        //        }
        //    }
        //    if (startcount == 1 && endcount == 1)
        //    {
        //        return () => true;
        //    }
        //    return () => false;
        //}

        #region General event handlers

        private void MyCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetTool();
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            ViewerToolManager.Tools.ForEach(x => x.KeyDownHandler(sender, e));
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this.MyCanvas);
            Message.Text = string.Format("{0:0.00},{1:0.00}", pos.X, pos.Y);
        }

        #endregion

        #region Command event handlers

        private void PanButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new PanCanvasTool();
        }

        private void ZoomExtentsButton_Click(object sender, RoutedEventArgs e)
        {
            var bubblePositions = this.MyCanvas.Children
                .Cast<UIElement>()
                .Where(element => element is NodeBubble)
                .Cast<NodeBubble>()
                .Select(bubble => new Vector(bubble.Position.X, bubble.Position.Y))
                .ToArray();

            if (bubblePositions.Length == 0)
            {
                return;
            }

            var extents = new Extents(bubblePositions);
            this.Zoom(extents.Offset(200));
        }

        private void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new AddNodeTool();
        }

        private void AddConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new AddLinkTool();
        }

        private void EditConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new EditLinkTool();
        }

        private void MoveNodeButton_Click(object sender, RoutedEventArgs e)
        {
            ViewerToolManager.ExclusiveTool = new MoveNodeTool();
        }

        private void DeleteNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.SelectedBubble != null)
            {
                var nodeID = this.SelectedBubble.NodeID;
                this.MyCanvas.Children.Remove(this.SelectedBubble);
                this.Bubbles.Remove(nodeID);
                this.SelectedBubble = null;

                this.Arrows.RealValues.ForEach(arrow =>
                {
                    if (arrow.FromNodeID == nodeID || arrow.ToNodeID == nodeID)
                    {
                        this.MyCanvas.Children.Remove(arrow);
                        this.Arrows.Remove(arrow.FromNodeID, arrow.ToNodeID);
                    }
                });
            }

            ViewerToolManager.ExclusiveTool = new PanCanvasTool();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Expressions.Clear();
            //InitializeExpression();

            bool flag = false;
            if (Expressions.Count == 0)
            {
                flag = true;
            }
            if (Expressions.Count > 0 && Expressions.All(x => x.GetValue()))
            {
                flag = true;
            }

            if (flag)
            {
                Submit();
            }
            else
            {
                MessageBox.Show("Invalid node(s) detected.");
            }
        }

        #endregion
    }

    public class BoolExpression
    {
        private Func<bool> _expression;

        public BoolExpression()
        {
            _expression = new Func<bool>(() => true);
        }

        public BoolExpression(Func<bool> expression)
        {
            _expression = expression;
        }

        public bool GetValue()
        {
            return _expression();
        }

        public void SetExpression(Func<bool> expr)
        {
            _expression = expr;
        }
    }

    public static class Gui
    {
        /// <summary>
        /// Shows a multi-inputs window.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="entries">The input entries.</param>
        public static void MultiInputs(string title, Dictionary<string, string> entries)
        {
            var mi = new MultiInputs();
            mi.Ready(entries, title);
            mi.ShowDialog();
        }
    }
}
