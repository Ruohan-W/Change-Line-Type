using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Change_Line_Type
{
    /// <summary>
    /// Interaction logic for UIWindow.xaml
    /// </summary>
    public partial class UIWindow : Window
    {
        public UIDocument uidoc { get; }
        public Document doc { get; }
        public UIWindow(UIDocument UiDoc)
        {
            uidoc = UiDoc;
            doc = UiDoc.Document;
            InitializeComponent();
            Title = "Change Line Style";
        }
    }
}
