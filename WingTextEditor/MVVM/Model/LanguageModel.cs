using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WingTextEditor.Core;

namespace WingTextEditor.MVVM.Model
{
    public class LanguageModel : MenuModel
    {
        public LanguageModel()
        {
            Tag = "radiobutton";
            IsChecked=false;
        }
        private Language language { get; set; }
        public Language Language
        {
            get { return language; }
            set
            {
                language = value;
                OnPropertyChanged("Language");
            }
        }
        private bool isChecked { get; set; }
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }




    }
}
