using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WingTextEditor.Core;
using WingTextEditor.MVVM.Model;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;


using System.Reflection;
using System.Text.Json;
using System.Windows.Controls;

namespace WingTextEditor.MVVM.ViewModel
{

    public class MainViewModel : ObservableItem
    {

        public ObservableCollection<MenuModel> menus { get; set; }
        private ObservableCollection<TabControlModel> tabControlModels { get; set; }
        public ObservableCollection<TabControlModel> TabControlModels
        {
            get { return tabControlModels; }
            set
            {
                tabControlModels = value;
                OnPropertyChanged("TabControlModels");
            }
        }

        private TabControlModel activePage { get; set; }

        public TabControlModel ActivePage
        {
            get { return activePage; }
            set
            {
                activePage = value;
                OnPropertyChanged("ActivePage");
            }
        }
        private string selectedText { get; set; }

        public string SelectedText
        {
            get { return selectedText; }
            set
            {
                selectedText = value;
                OnPropertyChanged("SelectedText");

            }
        }
        private string output { get; set; }

        public string Output
        {
            get { return output; }
            set
            {
                output = value;
                OnPropertyChanged("Output");

            }
        }






        private ICommand exitCommand;

        private ICommand newCommand;

        private ICommand selectTextCommand;

        private ICommand saveCommand;

        private ICommand loadCommand;

        private ICommand newPageCommand;

        private ICommand deleteSelectedPageCommand;

        private ICommand runCommand { get; set; }

        public ICommand RunCommand
        {
            get { return runCommand; }
            set
            {
                runCommand = value;
                OnPropertyChanged("RunCommand");
            }
        }

        public MainViewModel()
        {
            SelectedText = "";
            menus = new ObservableCollection<MenuModel>();
            tabControlModels = new ObservableCollection<TabControlModel>();
            exitCommand = new RelayCommand(Exit);
            newCommand = new RelayCommand(New);
            selectTextCommand = new RelayCommand(SelectText, CanSelectText);
            saveCommand = new RelayCommand(Save);
            loadCommand = new RelayCommand(Load);
            newPageCommand = new RelayCommand(NewPage, canNewPage);
            deleteSelectedPageCommand = new RelayCommand(DeleteSelectedPage, CanDeleteSelectedPage);
            RunCommand = new RelayCommand(Run);
            List<MenuModel> FileMenu = new List<MenuModel>()
            {
                new MenuModel(){Name="New" ,Action=newCommand},
                new MenuModel() { Name = "New Page" ,Action= newPageCommand, CommandParameter="Page"},
                new MenuModel() { Name = "Delete Selected Page" ,Action= deleteSelectedPageCommand},
                new MenuModel() { Name = "Save",Action=saveCommand},
                new MenuModel() { Name = "Load" , Action=loadCommand},
                new MenuModel() { Name = "Exit", Action = exitCommand}
            };
            List<MenuModel> EditMenu = new List<MenuModel>()
            {
                new MenuModel(){Name="Selected Text" ,Action=selectTextCommand},
            };
            menus.Add(new MenuModel() { Name = "File", MenuModels = FileMenu });
            menus.Add(new MenuModel() { Name = "Edit", MenuModels = EditMenu });
            tabControlModels.Add(new TabControlModel { Name = "Main Page" });
            ActivePage = tabControlModels[0];
        }

        public void Run(object obj)
        {

            if(ActivePage is not null && activePage.PageText is not null)
                Output=Wing.Execute(activePage.PageText);


           
        }
        public void Exit(object obj)
        {
            Environment.Exit(0);
        }

        public void New(object obj)
        {
            MessageBoxResult rslt = MessageBox.Show("Would you like to create a new file?",
                "New", MessageBoxButton.OKCancel);
            if (rslt == MessageBoxResult.OK)
            {
                tabControlModels.Clear();
                tabControlModels.Add(new TabControlModel { Name = "Main Page" });
                ActivePage = tabControlModels[0];
            }
        }
        public void SelectText(object obj)
        {
            MessageBox.Show(selectedText);

        }
        public bool CanSelectText(object obj)
        {
            return ActivePage is not null && SelectedText != "";
        }
        public void Save(object obj)
        {
            string text = JsonSerializer.Serialize(TabControlModels);
            File.WriteAllText("Saves/Project.json", text);


        }
        public void Load(object obj)
        {
            string text = File.ReadAllText("Saves/Project.json");
            ObservableCollection<TabControlModel> temp = JsonSerializer.Deserialize<ObservableCollection<TabControlModel>>(text);
            tabControlModels.Clear();
            foreach (TabControlModel model in temp)
                tabControlModels.Add(model);
            ActivePage = tabControlModels[0];
            string selectedLanguage = File.ReadAllText("Saves/languagesettings.json");
        }
        public void NewPage(object obj)
        {
            string name;
            if (tabControlModels.Count == 0)
                name = "Main Page";
            else
                name = obj as string + tabControlModels.Count;
            TabControlModels.Add(new TabControlModel() { Name = name });
            if (tabControlModels.Count == 1)
                ActivePage = tabControlModels[0];
        }
        public bool canNewPage(object obj)
        {
            return TabControlModels.Count < 7;

        }
        public void DeleteSelectedPage(object obj)
        {

            tabControlModels.Remove(ActivePage);
        }
        public bool CanDeleteSelectedPage(object obj)
        {
            return TabControlModels.Count > 0;
        }





    }
}
