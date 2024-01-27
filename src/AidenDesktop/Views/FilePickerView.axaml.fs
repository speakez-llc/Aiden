namespace AidenDesktop.Views

open System.Collections.Generic
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Northwoods.Go.Avalonia
open Northwoods.Go.Models
open Northwoods.Go
open Northwoods.Go.Layouts



type NodeData() =
    
    let mutable _key = ""
    let mutable _shape = "RoundedRectangle"
    let mutable _parent = ""

    member this.Key
        with get() = _key
        and set(value) = _key <- value
    member this.Shape
        with get() = _shape
        and set(value) = _shape <- value
    member this.Parent
        with get() = _parent
        and set(value) = _parent <- value

// LinkData is used in the GraphLinksModel to define the link relationship between nodes
type LinkData() =
    let mutable _from = ""
    let mutable _to = ""
    let mutable _text = ""

    member this.From
        with get() = _from
        and set(value) = _from <- value
    member this.To
        with get() = _to
        and set(value) = _to <- value
    member this.Text
        with get() = _text
        and set(value) = _text <- value

// The basic model does not allow for node relationships
type DiagramModel() =
    inherit Model<NodeData, string, string>()

// The tree model allows for single parent link relationships between nodes using the extra link data
type DiagramTreeModel() =
    inherit TreeModel<NodeData, string, obj>()

type DiagramGraphLinksModel() =
    inherit GraphLinksModel<NodeData, string, obj, LinkData, string, string>()

type FilePickerView () as this = 
    inherit UserControl ()

    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    
        
    override this.OnApplyTemplate(e) =
        base.OnApplyTemplate(e)

        let con = this.FindControl<DiagramControl>("diagramControl")
        let diag = con.Diagram
        diag.UndoManager.IsEnabled <- true

        // Layout
        let layout = TreeLayout(Angle = 90, LayerSpacing = 35)
        diag.Layout <- layout

        let start = Spot(1.0, 0.0)
        let stop = Spot(1.0, 1.0)

        let paint = LinearGradientPaint(dict [(0f, "#ffffff"); (1f, "#444444")], start, stop)    
  
        let fill = Northwoods.Go.Brush(paint)

        let shape = Shape("RoundedRectangle")
        shape.Width <- 100.0
        shape.Height <- 50.0
        shape.Fill <- fill
        //shape.Fill <- "green"
        
        shape.Opacity <- 0.25 // NOTE: this does not seem to work with a gradient fill
        //shape.Background <- Northwoods.Go.Brush.op_Implicit("#ffffff")
        
        let box = TextBlock()
        
        box.Stretch <- Stretch.Horizontal
        box.Alignment <- Spot.Center
        box.TextAlign <- TextAlign.Center
        box.VerticalAlignment <- Spot.Center
        box.Background <- Northwoods.Go.Brush.op_Implicit("#ffffff")
        let nodeTemplate =
            Node()
                .Add(
                    //Shape("RoundedRectangle")
                    shape
                        .Bind("Figure", "Shape"),
                        
                    //TextBlock()
                    box
                        // This binds the Text property of the TextBlock to the Key property of NodeData
                        .Bind("Text", "Key")
                )
        diag.NodeTemplate <- nodeTemplate

        // Simple Model case, does not allow for node relationships
        let model = DiagramModel()
        model.NodeDataSource <-
            [
                NodeData(Key = "Alpha", Shape = "Ellipse")
                NodeData(Key = "Beta")
                NodeData(Key = "Gamma")
            ]
        // Tree model allows for single parent link relationships between nodes
        let treemodel = DiagramTreeModel()
        treemodel.NodeDataSource <-
            [
                NodeData(Key = "Alpha", Shape = "Ellipse")
                NodeData(Key = "Beta", Parent = "Alpha")
                NodeData(Key = "Gamma", Parent = "Alpha")
                NodeData(Key = "Delta", Parent = "Beta")
                NodeData(Key = "Epsilon", Parent = "Beta")
                NodeData(Key = "Zeta", Parent = "Gamma")
            ]
        
        // GraphLinksModel allows for multiple parent link relationships between nodes
        let graphmodel = DiagramGraphLinksModel()
        graphmodel.NodeDataSource <-
            [
                NodeData(Key = "Alpha", Shape = "Ellipse")
                NodeData(Key = "Beta", Parent = "Alpha")
                NodeData(Key = "Gamma", Parent = "Alpha")
            ]
        graphmodel.LinkDataSource <-
            [
                LinkData(From = "Alpha", To = "Beta")
                LinkData(From = "Alpha", To = "Gamma")
                LinkData(From = "Beta", To = "Gamma")
            ]
        
        diag.Model <- treemodel

        
        ()