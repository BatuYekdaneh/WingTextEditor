using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WingTextEditor.Core;

namespace WingTextEditor.MVVM.Model
{

    [Serializable()]
    public class TabControlModel : ObservableItem
    {
        private string name { get; set; }
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }
        private string pageText { get; set; }
        public string PageText
        {
            get { return pageText; }
            set
            {

                pageText = value;
                OnPropertyChanged("PageText");
            }
        }

        public TabControlModel()
        {
            PageText = "";
        }

        public override string? ToString()
        {
            return Name;
        }
    }
}
