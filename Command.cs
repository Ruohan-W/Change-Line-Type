#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

            // declare correct line style formate
            string lineStyleNamingConvention = "STM-EP";

            
            // retrive all detail lines and their line styles
            IEnumerable<CurveElement> detailLines = (IEnumerable<CurveElement>)FindAllDetailCurves(doc).Item1;
            IEnumerable<string> detailLineStyles = (IEnumerable<string>)FindAllDetailCurves(doc).Item2;

            // filter through all detail lines to retrive the ones with incorrect line style
            IEnumerable<CurveElement> targetedDetailCurve = FindDetailLinesWithIncorrectLineStyle(detailLines, detailLineStyles, lineStyleNamingConvention);

            // test
            IList<string> names = NameRequiredLineStyle(doc, targetedDetailCurve);

            /*
            // get all avaliable graphic styles for detail lines that follow the naming convention
            IEnumerable<GraphicsStyle> existingTargetedGrpahicStyle = getAllCorrectGraphicStyle(doc, lineStyleNamingConvention);
            */

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
                    MainInstruction = "Zero detail line",
                    MainContent = "There is no detail lines in current project",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                };
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();

                return (null, null);
            }
        }

        // filter out the detail lines with the incorrect LineStyle
        private static IEnumerable<CurveElement> FindDetailLinesWithIncorrectLineStyle(IEnumerable<CurveElement> curves, IEnumerable<string> lineStyles, string lineStyleName)
        {
            // declare detailCurves with incorrect LineStyle
            IList<CurveElement> incorrectedCurve = new List<CurveElement>();

            int count = 0;

            var cS = curves.Zip(lineStyles, (x, y) => new Tuple<CurveElement, string>(x, y)).ToList();

            foreach (var c in cS)
            {
                string cStyle = c.Item2;
                bool incorrect = !cStyle.StartsWith(lineStyleName) | !cStyle.StartsWith("<");

                if (incorrect)
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

        // collect all graphic styles that follows standard. 
        private static IEnumerable<GraphicsStyle> getAllCorrectGraphicStyle(Document doc, string lineStyleName)
        {
            IEnumerable<GraphicsStyle> targetedGraphicStyles = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>()
                .Where(gs => gs.GraphicsStyleCategory.ToString().Contains(lineStyleName));

            // report result with task dialog
            if (targetedGraphicStyles != null)
            {
                string graphicsStyleNamesConcatenate = string.Join(", ", targetedGraphicStyles);

                TaskDialog td = new TaskDialog("Success")
                {
                    Title = "avaliable graphic styles",
                    AllowCancellation = true,
                    MainContent = "found avaliable grphic styles",
                    MainInstruction = $"{targetedGraphicStyles.Count()} availible graphic styles ({graphicsStyleNamesConcatenate}) of detail lines that follow the naming convention",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                };
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();
            }
            else 
            { 

            }

            return targetedGraphicStyles;
        }

        // test whether the needed line style is existing in the project
        private static IList<string> NameRequiredLineStyle(Document doc, IEnumerable<CurveElement> curves)
        {
            // find the needed linestyle
            IList<string> curveStyleNameLst = new List<string>();

            foreach (CurveElement c in curves)
            {           
                string cWeight = null;
                Autodesk.Revit.DB.Color cColor = null;
                string cPatterName = null;

                // get the graphic style of the curve
                GraphicsStyle cG = (GraphicsStyle)c.LineStyle;
                // retrive the line weigth
                cWeight = cG.GraphicsStyleCategory.GetLineWeight(GraphicsStyleType.Projection).ToString();

                // retrive the line color
                cColor = cG.GraphicsStyleCategory.LineColor;

                // retrive the line pattern
                ElementId cPatternId = cG.GraphicsStyleCategory.GetLinePatternId(GraphicsStyleType.Projection);
                if (cPatternId != null)
                {
                    LinePatternElement cPattern = doc.GetElement(cPatternId) as LinePatternElement;
                    if (cPattern != null)
                    {
                        cPatterName = cPattern.GetLinePattern().Name;
                    }
                }
                #region testing
                // test see the data retrived
                string name = $"line weight: {cWeight} - line color: {cColor.Red}, {cColor.Green}, {cColor.Blue} - {cPatterName}";
                curveStyleNameLst.Add(name);

                TaskDialog td = new TaskDialog("testing")
                {
                    Title = "retrive data of curves",
                    AllowCancellation = true,
                    MainContent = $"{curveStyleNameLst[0]}",
                    MainInstruction = "review the data below:",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                };

                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();
                #endregion

                // give the proper name for the lines

            }
            return curveStyleNameLst;
        }

        private static int TestIfBlackAndSolid(Autodesk.Revit.DB.Color cColor, string cPatterName)
        {
            int caseNum = 0;

            // check whether the line is black
            int isBlack = 1;
            if (cColor.Red != 0 | cColor.Green != 0 | cColor.Blue != 0)
            {
                isBlack = 0;
            }
            // check whether the line is solid
            int isSolid = 1;
            if (cPatterName != "solid")
            {
                isSolid = 0;
            }

            caseNum = isBlack + isSolid + 1;

            return caseNum;
        }

        int ColorDiff(Autodesk.Revit.DB.Color c1, Autodesk.Revit.DB.Color c2)
        {
            return (int)Math.Sqrt((c1.Red - c2.Red)^2
                                   + (c1.Green - c2.Green)^2
                                   + (c1.Blue - c2.Blue)^2);
        }

        /*
        // create line style following the nameing convention
        private static void CreateLineStyle(Document doc, string detailLineName,IList<byte> c, int detailLineWeight) 
        {
            // retrive categories and BuiltIn category for lines from the document settings
            Settings settings = doc.Settings;
            Categories cats = settings.Categories;
            Category lineCat = cats.get_Item(BuiltInCategory.OST_Lines);

            // create new line style with the NewSubcategory method
            Category lineStyleCat = cats.NewSubcategory(lineCat, detailLineName);

            // set the line weight and line color for the new line weight.
            lineCat.LineColor = new Autodesk.Revit.DB.Color(c[0], c[1], c[2]);
            lineCat.SetLineWeight(detailLineWeight, GraphicsStyleType.Projection);
        }

        
        // select lines whose name is not following the project standard by filtering the name of LineType.
        
        private static (string, string, string) compareCurveAttribute(IEnumerable<CurveElement> curve)
        {
            // get the Style of all detail lines, 
            // get the Line pattern of all detail lines
            // get the Line color of all detail lines


        };

        */

        // change the the line types of selected lines

    }
}
