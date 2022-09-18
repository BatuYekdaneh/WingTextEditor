using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WingTextEditor.Core;

namespace WingTextEditor.MVVM.Model
{
    public class MenuModel:ObservableItem
    {
        private string name { get; set; }
        public string Name { get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }
        private string tag { get; set; }
        public string Tag
        {
            get { return tag; }
            set
            {
                tag = value;
                OnPropertyChanged("Tag");
            }
        }
        private ICommand action { get; set; }
        public ICommand Action
        {
            get { return action; }
            set
            {
                action = value;
                OnPropertyChanged("Action");
            }
        }
        private object commandParameter { get; set; }
        public object CommandParameter
        {
            get { return commandParameter; }
            set
            {
                commandParameter = value;
                OnPropertyChanged("CommandParameter");
            }
        }
        private List<MenuModel> menuModels { get; set; }
        public List<MenuModel> MenuModels
        {
            get { return menuModels; }
            set
            {
                menuModels = value;
                OnPropertyChanged("MenuModels");
            }
        }
        public override string? ToString()
        {
            return Name;
        }
    }
}
