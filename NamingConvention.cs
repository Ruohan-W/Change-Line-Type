using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Change_Line_Type
{
    class NamingConvention : ObservableObj
    {
        private string _prefix;

        public string Prefix
        {
            get { return _prefix; }
            set 
            { 
                _prefix = value;
                OnPropertyRaised("Prefix");
            }
        }

        private string _separator1;

        public string Seperator1
        {
            get { return _separator1; }
            set
            {
                _separator1 = value;
                OnPropertyRaised("Seperator1");
            }
        }

        private string _separator2;

        public string Seperator2
        {
            get { return _separator2; }
            set
            {
                _separator2 = value;
                OnPropertyRaised("Seperator2");
            }
        }

        private string _suffix;
        public string Suffix
        {
            get { return _suffix; }
            set
            {
                _suffix = value;
                OnPropertyRaised("Suffix");
            }
        }

        private string _comboBox1;
        public string ComboBox1 
        { 
            get { return _comboBox1; } 
            set 
            { 
                _comboBox1 = value;
                OnPropertyRaised("ComboBox1");
            }
        }

        private string _comboBox2;
        public string ComboBox2
        {
            get { return _comboBox2; }
            set
            {
                _comboBox2 = value;
                OnPropertyRaised("ComboBox2");
            }
        }

        private string _comboBox3;
        public string ComboBox3
        {
            get { return _comboBox3; }
            set
            {
                _comboBox3 = value;
                OnPropertyRaised("ComboBox3");
            }
        }

        public NamingConvention()
        {
            _prefix = "ACM";
            _separator1 = "-";
            _separator2 = "-";
            _suffix = "suffix";
        }

    }
}
