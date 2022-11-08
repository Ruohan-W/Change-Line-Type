#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#endregion

namespace Change_Line_Type
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            IEnumerable<Element> detailLines = (IList<Element>)FindAllDetailCurves(doc);

            return Result.Succeeded;
        }

        // get all detail lines in current models and report the graphic styles with TaskDialog
        private static IEnumerable<Element> FindAllDetailCurves(Document doc)
        {
            // get all detail lines in current models
            CurveElementFilter filter_detail = new CurveElementFilter(CurveElementType.DetailCurve);
            IEnumerable<CurveElement> detailCurves = new FilteredElementCollector(doc)
                .WherePasses(filter_detail)
                .Cast<CurveElement>(); 

            if (detailCurves != null)
            {
                List<string> cStyleNameLst = new List<string>();

                foreach (CurveElement l in detailCurves)
                {
                    Element cStyle = l.LineStyle;
                    string cStyleName = cStyle.Name;

                    cStyleNameLst.Add(cStyleName);
                };

                string lineStyleNamesConcatenate = string.Join(", ", cStyleNameLst);

                TaskDialog td = new TaskDialog("Success")
                {
                    Title = "Found detail lines",
                    AllowCancellation = true,
                    MainInstruction = "Collected all detail lines in the project",
                    MainContent = $"name(s) of the {cStyleNameLst.Count} detail line(s) in the project: {lineStyleNamesConcatenate} ",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                };
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();

                return detailCurves;
            }
            else
            {
                TaskDialog td = new TaskDialog("Success")
                {
                    Title = "No detail lines found",
                    AllowCancellation = true,
                    MainInstruction = "Zero detail line found",
                    MainContent = "There is no detail lines in this project",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                };
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();

                return null;
            }
        }
        // select lines whose name is not following the project standard by filtering the name of LineType.
        // change the the line types of selected lines

    }
}
