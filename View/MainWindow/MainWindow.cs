﻿using Gtk;

using PokemonTrackerEditor.Model;

using System;
using System.Collections.Generic;

namespace PokemonTrackerEditor.View.MainWindow {
    class MainWindow : Window {
        public MainProg Main { get; private set; }
        public DependencyEntry CurrentSelection { get; set; }

        private string windowTitle = "Pokémon Map Tracker Rule Set Editor";
        private TreeView locationTreeView;
        private TreeView storyItemsTreeView;

        private Dictionary<string, TreeView> locationConditionsTreeViews;

        public void SetRuleSet(RuleSet ruleSet, string filename) {
            SetRuleSetPath(filename);
            locationTreeView.Model = ruleSet.Model;
            locationTreeView.Selection.SelectIter(TreeIter.Zero);
            CurrentSelection = null;
        }

        public void SetRuleSetPath(string filename) {
            Title = windowTitle + ": " + (filename ?? "Unsaved rule set");
        }

        public void UpdateEditorSelection(DependencyEntry entry) {
            CurrentSelection = entry;
            if (entry != null) {
                locationConditionsTreeViews["Items"].Model = entry.ItemsModel;
                locationConditionsTreeViews["Pokémon"].Model = entry.PokemonModel;
                locationConditionsTreeViews["Trades"].Model = entry.TradesModel;
                locationConditionsTreeViews["Trainers"].Model = entry.TrainersModel;
                //locationConditionsTreeViews["Story Items"].Model = entry.StoryItemsConditions.Model;
            }
            else {
                foreach (TreeView treeView in locationConditionsTreeViews.Values) {
                    treeView.Model = null;
                }
            }
        }

        private Frame CreateCheckConditionList(string title, Check.CheckType type) {
            Frame frame = new Frame(title);
            VBox condBox = new VBox { Spacing = 5 };

            TreeView condTreeView = new TreeView();
            locationConditionsTreeViews[title] = condTreeView;

            TreeViewColumn condLocationColumn = new TreeViewColumn { Title = "Location", Resizable = true };
            CellRendererText condLocationColumnText = new CellRendererText();
            condLocationColumn.PackStart(condLocationColumnText, true);
            condLocationColumn.SetCellDataFunc(condLocationColumnText, new TreeCellDataFunc(Renderers.ConditionLocation));
            condTreeView.AppendColumn(condLocationColumn);

            TreeViewColumn condCheckColumn = new TreeViewColumn { Title = "Check", Resizable = true };
            CellRendererText condCheckColumnText = new CellRendererText();
            condCheckColumn.PackStart(condCheckColumnText, true);
            condCheckColumn.SetCellDataFunc(condCheckColumnText, new TreeCellDataFunc(Renderers.ConditionName));
            condTreeView.AppendColumn(condCheckColumn);

            ScrolledWindow condTreeViewScrolledWindow = new ScrolledWindow { condTreeView };
            condBox.PackStart(condTreeViewScrolledWindow, true, true, 0);

            Toolbar condBoxControls = new Toolbar();
            ToolButton addCondition = new ToolButton(Stock.Add);
            addCondition.Clicked += (object sender, EventArgs args) => { ButtonCallbacks.OnAddConditionClick(this, type); };
            condBoxControls.Insert(addCondition, 0);
            ToolButton removeCondition = new ToolButton(Stock.Remove);
            removeCondition.Clicked += (object sender, EventArgs args) => { ButtonCallbacks.OnRemoveConditionClick(this, condTreeView); };
            condBoxControls.Insert(removeCondition, 1);

            condBox.PackStart(condBoxControls, false, false, 0);

            frame.Add(condBox);
            return frame;
        }

        private TreeViewColumn CreateLocationColumn(string title, TreeCellDataFunc func) {
            TreeViewColumn column = new TreeViewColumn() { Title = title, Resizable = true };
            CellRendererText columnText = new CellRendererText();
            column.PackStart(columnText, false);
            column.SetCellDataFunc(columnText, func);
            return column;
        }

        private ToolButton CreateFileButton(string title, string stock_id, EventHandler func) {
            ToolButton fileButton = new ToolButton(stock_id) { Label = title };
            fileButton.Clicked += func;
            return fileButton;
        }

        private ToolButton CreateCheckButton(string title, Check.CheckType type) {
            ToolButton addCheckButton = new ToolButton(Stock.Add) { Label = title };
            addCheckButton.Clicked += (object sender, EventArgs args) => { ButtonCallbacks.OnAddCheckClick(this, type); };
            return addCheckButton;
        }

        public MainWindow(MainProg main) : base("") {
            Main = main;
            windowTitle = "Pokémon Map Tracker Rule Set Editor";
            SetRuleSetPath(null);


            DeleteEvent += delegate { Application.Quit(); };
            Resize(600, 600);

            VBox mainBox = new VBox { Spacing = 5 };

            Notebook mainNotebook = new Notebook();

            // File selection
            Toolbar locationFileBox = new Toolbar();
            locationFileBox.Insert(CreateFileButton("New", Stock.New, (object sender, EventArgs args) => { ButtonCallbacks.OnNewFileClick(this); }), 0);
            locationFileBox.Insert(CreateFileButton("Open", Stock.Open, (object sender, EventArgs args) => { ButtonCallbacks.OnSelectFileClick(this); }), 1);
            locationFileBox.Insert(CreateFileButton("Save", Stock.Save, (object sender, EventArgs args) => { ButtonCallbacks.OnSaveFileClick(this); }), 2);

            mainBox.PackStart(locationFileBox, false, false, 0);

            HPaned editorPaned = new HPaned();

            // Locations view
            VBox locationTreeBox = new VBox { Spacing = 5 };
            ScrolledWindow locationTreeViewScrolledWindow = new ScrolledWindow();
            locationTreeView = new TreeView(main.RuleSet.Model);

            TreeSelection locationTreeSelection = locationTreeView.Selection;
            locationTreeSelection.Changed += (object sender, EventArgs args) => { EventCallbacks.OnLocationTreeSelectionChanged(this, (TreeSelection)sender); };

            TreeViewColumn locationNameColumn = new TreeViewColumn { Title = "Location", Resizable = true };

            CellRendererText locationNameColumnText = new CellRendererText { Editable = true };
            locationNameColumnText.Edited += (object sender, EditedArgs args) => { EventCallbacks.OnLocationNameEdited(this, new TreePath(args.Path), args.NewText); };
            locationNameColumn.PackStart(locationNameColumnText, true);
            locationNameColumn.SetCellDataFunc(locationNameColumnText, new TreeCellDataFunc(Renderers.DependencyEntryName));

            locationTreeView.AppendColumn(locationNameColumn);
            locationTreeView.AppendColumn(CreateLocationColumn("# Checks", new TreeCellDataFunc(Renderers.DependencyEntryCheckCount)));
            locationTreeView.AppendColumn(CreateLocationColumn("# Dependencies", new TreeCellDataFunc(Renderers.DependencyEntryDepCount)));
            locationTreeView.AppendColumn(CreateLocationColumn("# Conditions", new TreeCellDataFunc(Renderers.DependencyEntryCondCount)));
            locationTreeViewScrolledWindow.Add(locationTreeView);

            locationTreeBox.PackStart(locationTreeViewScrolledWindow, true, true, 0);

            // Location controls
            //HBox locationTreeControlsBox = new HBox { Spacing = 5 };
            Toolbar locationTreeToolbar = new Toolbar();
            ToolButton addCheckButton = new ToolButton(Stock.Add) { Label = "Location" };
            addCheckButton.Clicked += (object sender, EventArgs args) => { ButtonCallbacks.OnAddLocationClick(this); };
            locationTreeToolbar.Insert(addCheckButton, 0);
            locationTreeToolbar.Insert(CreateCheckButton("Item", Check.CheckType.ITEM), 1);
            locationTreeToolbar.Insert(CreateCheckButton("Pokémon", Check.CheckType.POKEMON), 2);
            locationTreeToolbar.Insert(CreateCheckButton("Trade", Check.CheckType.TRADE), 3);
            locationTreeToolbar.Insert(CreateCheckButton("Trainer", Check.CheckType.TRAINER), 4);

            ToolButton removeSelectedButton = new ToolButton(Stock.Remove) { Label = "Remove" };
            removeSelectedButton.Clicked += (object sender, EventArgs args) => { ButtonCallbacks.OnRemoveLocationOrCheckClick(this); };
            locationTreeToolbar.Insert(removeSelectedButton, 5);

            locationTreeBox.PackStart(locationTreeToolbar, false, false, 0);

            editorPaned.Add1(locationTreeBox);

            // Location editor
            locationConditionsTreeViews = new Dictionary<string, TreeView>();
            VBox conditionsBox = new VBox { Spacing = 5 };

            conditionsBox.PackStart(CreateCheckConditionList("Items", Check.CheckType.ITEM));
            conditionsBox.PackStart(CreateCheckConditionList("Pokémon", Check.CheckType.POKEMON));
            conditionsBox.PackStart(CreateCheckConditionList("Trades", Check.CheckType.TRADE));
            conditionsBox.PackStart(CreateCheckConditionList("Trainers", Check.CheckType.TRAINER));

            editorPaned.Add2(conditionsBox);
            editorPaned.Position = 400;

            mainNotebook.InsertPage(editorPaned, new Label { Text = "Locations" }, 0);

            /* Story items
            TreeView condTreeView = new TreeView();
            locationConditionsTreeViews[type] = condTreeView;
            TreeViewColumn condStoryItemColumn = new TreeViewColumn { Title = "Story Item", Resizable = true };
            CellRendererText condStoryItemColumnText = new CellRendererText();
            condStoryItemColumn.PackStart(condStoryItemColumnText, true);
            condStoryItemColumn.SetCellDataFunc(condStoryItemColumnText, new TreeCellDataFunc(Renderers.StoryItemConditionName));
            condTreeView.AppendColumn(condStoryItemColumn);

            TreeViewColumn condStoryItemCheckedColumn = new TreeViewColumn { Title = "Necessary" };
            CellRendererToggle condStoryItemCheckedColumnValue = new CellRendererToggle();
            condStoryItemCheckedColumn.PackStart(condStoryItemCheckedColumnValue, true);
            condStoryItemCheckedColumn.SetCellDataFunc(condStoryItemCheckedColumnValue, new TreeCellDataFunc(Renderers.StoryItemActive));
            condTreeView.AppendColumn(condStoryItemCheckedColumn);
            */

            VBox storyItemsBox = new VBox { Spacing = 5 };
            storyItemsTreeView = new TreeView(main.RuleSet.StoryItems.Model);
            storyItemsBox.PackStart(storyItemsTreeView, true, true, 0);

            Toolbar storyItemsToolbar = new Toolbar();
            ToolButton addCategoryButton = new ToolButton(Stock.Add) { Label = "Category" };
            storyItemsToolbar.Insert(addCategoryButton, 0);
            ToolButton addStoryItemButton = new ToolButton(Stock.Add) { Label = "Story Item" };
            storyItemsToolbar.Insert(addStoryItemButton, 1);
            ToolButton removeSelectedStoryItemButton = new ToolButton(Stock.Remove) { Label = "Remove" };
            storyItemsToolbar.Insert(removeSelectedStoryItemButton, 2);
            ToolButton moveUpButton = new ToolButton(Stock.GoUp) { Label = "Move Up" };
            storyItemsToolbar.Insert(moveUpButton, 3);
            ToolButton moveDownButton = new ToolButton(Stock.GoDown) { Label = "Move Down" };
            storyItemsToolbar.Insert(moveDownButton, 4);

            storyItemsBox.PackStart(storyItemsToolbar, false, false, 0);

            mainNotebook.InsertPage(storyItemsBox, new Label { Text = "Story Items" }, 1);

            // Pack it all
            mainBox.PackStart(mainNotebook, true, true, 0);

            Add(mainBox);

            ShowAll();
        }
    }
}
