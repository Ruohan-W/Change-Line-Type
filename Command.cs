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

            IEnumerable<CurveElement> detailLines = (IEnumerable<CurveElement>)FindAllDetailCurves(doc).Item1;
            IEnumerable<string> detailLineStyles = (IEnumerable<string>)FindAllDetailCurves(doc).Item2;

            IEnumerable<CurveElement> targetedDetailCurve = FindDetailLinesWithIncorrectLineStyle(detailLines, detailLineStyles);


            return Result.Succeeded;
        }

        // get all detail lines in current models and report the graphic styles with TaskDialog
        private static (IEnumerable<CurveElement>, IEnumerable<string>) FindAllDetailCurves(Document doc)
        {
            // get all detail lines in current models
            CurveElementFilter filter_detail = new CurveElementFilter(CurveElementType.DetailCurve);
            IEnumerable<CurveElement> detailCurves = new FilteredElementCollector(doc)
                .WherePasses(filter_detail)
                .Cast<CurveElement>();

            IList<string> cStyleNameLst = new List<string>();

            if (detailCurves != null)
            {             
                // get all the Name of LineStyles
                foreach (CurveElement l in detailCurves)
                {
                    Element cStyle = l.LineStyle;
                    string cStyleName = cStyle.Name;

                    cStyleNameLst.Add(cStyleName);
                };

                // return all the collected detail curves
                return (detailCurves, cStyleNameLst);
            }
            else
            {
                // report that there is no detail curves in this project
                TaskDialog td = new TaskDialog("Fail")
                {
                    Title = "No detail lines found",
                    AllowCancellation = true,
                    MainInstruction = "Zero detail line found",
                    MainContent = "There is no detail lines in this project",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                };
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();

                return (null, null);
            }
        }

        // filter the line in the incorrect LineStyle
        private static IEnumerable<CurveElement> FindDetailLinesWithIncorrectLineStyle(IEnumerable<CurveElement> curves, IEnumerable<string> lineStyles)
        {
            // declare detailCurves with incorrect LineStyle
            IList<CurveElement> incorrectedCurve = new List<CurveElement>();
            int count = 0;

            var cS = curves.Zip(lineStyles, (x, y) => new Tuple<CurveElement, string>(x, y))
                            .ToList();

            foreach (var c in cS) 
            {
                string cStyle = c.Item2;
                if (!cStyle.StartsWith("STM"))
                {
                    count++;
                    incorrectedCurve.Add(c.Item1);
                }
            }

            if (count != 0)
            {
                // report result with task dialog
                TaskDialog td = new TaskDialog("Success")
                {
                    Title = "Incorrect line style",
                    AllowCancellation = true,
                    MainInstruction = "Retrive detail lines with incorrect line style",
                    MainContent = $"{count} of {cS.Count} detail line(s) have incorrect line style.",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                };
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();
            }
            else 
            {
                // report result with task dialog
                TaskDialog td = new TaskDialog("Fail 002")
                {
                    Title = "None",
                    AllowCancellation = true,
                    MainInstruction = "No detail line with incorrect line style",
                    MainContent = "",
                    MainIcon = TaskDialogIcon.TaskDialogIconNone,
                };
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();
            }

            // return detailCurves with incorrect LineStyle
            IEnumerable<CurveElement> targetedDetailCurve = incorrectedCurve;
            return targetedDetailCurve;
        }

        // select lines whose name is not following the project standard by filtering the name of LineType.
        // get the Style of all detail lines, 
        // get the Line pattern of all detail lines
        // get the Line color of all detail lines

        // change the the line types of selected lines

    }
}
