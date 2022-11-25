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
        #region main method
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            #region inputs from User interface (later)
            // declare standar line style formate
            string lineStyleNamingConvention = "STM-EP";

            // standar line colors:
            // declare standar line color in Revit.DB.Color formate - the code below is STM color standard 
            IList<Autodesk.Revit.DB.Color> standardColorLst = new List<Autodesk.Revit.DB.Color>
            {
                new Autodesk.Revit.DB.Color(0, 0, 0), //black
                new Autodesk.Revit.DB.Color(240, 240, 240), //grey
                new Autodesk.Revit.DB.Color(255, 0, 0), //red
                new Autodesk.Revit.DB.Color(218, 0, 64), //dark red
                new Autodesk.Revit.DB.Color(65, 135, 64), //green
                new Autodesk.Revit.DB.Color(0, 153, 204), //blue
                new Autodesk.Revit.DB.Color(255, 204, 0) //yellow
            };
            // declare names of the standard line color
            IList<string> standardColorNameLst = new List<string> 
            {   "NOIR",
                "GRIS-240",
                "ROUGE",
                "ROUGE-218",
                "VERT",
                "BLEU",
                "JAUNE",
            };
            #endregion

            // retrive all detail lines and their line styles
            IEnumerable<CurveElement> detailLines = (IEnumerable<CurveElement>)FindAllDetailCurves(doc).Item1;
            IEnumerable<string> detailLineStyles = (IEnumerable<string>)FindAllDetailCurves(doc).Item2;

            // filter through all detail lines to retrive the ones with incorrect line style
            IEnumerable<CurveElement> targetedDetailCurve = FindDetailLinesWithIncorrectLineStyle(detailLines, detailLineStyles, lineStyleNamingConvention);

            //test
            ICollection<CurveElement> targetedDetailCurvesCol = targetedDetailCurve as ICollection<CurveElement>;

            IList<string> NameLst = new List<string>();

            if (targetedDetailCurvesCol.Any())
            {
                NameLst = NameRequiredLineStyle(doc, targetedDetailCurve, standardColorLst, standardColorNameLst);
            }

            NameLst = TrimString(NameLst, standardColorNameLst[0]);
            NameLst = TrimString(NameLst, "SOLID");

            TaskDialog td1 = new TaskDialog("Testing 001")
            {
                Title = "testing the modified name of the curves",
                AllowCancellation = true,
                MainInstruction = "review names below",
                MainContent = $"{String.Join(Environment.NewLine, NameLst)}",
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
            };
            td1.CommonButtons = TaskDialogCommonButtons.Ok;
            td1.Show();

            
            // get all avaliable graphic styles for detail lines that follow the naming convention
            IEnumerable<GraphicsStyle> existingTargetedGrpahicStyle = GetAllCorrectGraphicStyle(doc, lineStyleNamingConvention);

            return Result.Succeeded;
        }
        #endregion

        #region secondary methods
        // find all detail lines
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
                bool correct = cStyle.StartsWith(lineStyleName) | cStyle.StartsWith("<");

                if (!correct)
                {
                    count++;
                    incorrectedCurve.Add(c.Item1);
                }
            }

            if (count == 0)
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
        private static IEnumerable<GraphicsStyle> GetAllCorrectGraphicStyle(Document doc, string lineStyleName)
        {
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Lines);

            IEnumerable<GraphicsStyle> targetedGraphicStyles = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>()
                .Where(gs => gs.GraphicsStyleCategory.Parent != null)
                .Where(gs => gs.GraphicsStyleCategory.Parent.Parent == null);

            IList<string> cGNameLst = new List<string>();

            foreach (GraphicsStyle gS in targetedGraphicStyles)
            { 
                Category cat = gS.GraphicsStyleCategory;
                BuiltInCategory catBuiltIn = (BuiltInCategory)cat.Id.IntegerValue;

                if (!catBuiltIn.ToString().StartsWith("OST"))
                {
                    Category catParent = gS.GraphicsStyleCategory.Parent;
                    BuiltInCategory catPBuiltIn = (BuiltInCategory)catParent.Id.IntegerValue;

                    if (catPBuiltIn == BuiltInCategory.OST_Lines)
                    {
                        cGNameLst.Add(gS.Name);
                    }
                }
            }
          
            cGNameLst = cGNameLst.Distinct().ToList();

            Debug.WriteLine($"all {cGNameLst.Count} line type hopefully: {Environment.NewLine}{String.Join(Environment.NewLine, cGNameLst)}");

            return targetedGraphicStyles;
        }

        // test whether the needed line style is existing in the project
        private static IList<string> NameRequiredLineStyle(Document doc, IEnumerable<CurveElement> curves, IList<Autodesk.Revit.DB.Color> standarColorLst, IList<string> standarColorNameLst)
        {
            // declare empty IList to stor the Name of curve style
            IList<string> curveStyleNameLst = new List<string>();

            // collect weight, color, and pattern of the curves
            Tuple<IList<int?>, IList<Autodesk.Revit.DB.Color>, IList<string>> cDataLst = GetCurveData(doc, curves);
            // retrive the weight of the curves
            IList<int?> cWeightLst = cDataLst.Item1;
            // retreive the color of the curves
            IList<Autodesk.Revit.DB.Color> cColorLst = cDataLst.Item2;
            // retreive the line pattern of the curves
            IList<string> cPatterNameLst = cDataLst.Item3;

            // convert colors to the ones in the standard
            // declare empty placeholder for the closest colors in the standard
            IList<Autodesk.Revit.DB.Color> cClosestColorLst = new List<Autodesk.Revit.DB.Color>();
            IList<string> cClosestColorNameLst = new List<string>();

            foreach (Autodesk.Revit.DB.Color cColor in cColorLst) 
            {
                int cColorIndexInStandarColorLst = ClosestColorRGB(standarColorLst, cColor);

                Autodesk.Revit.DB.Color cClosestColor = standarColorLst[cColorIndexInStandarColorLst];
                cClosestColorLst.Add(cClosestColor);

                string cClosestColorName = standarColorNameLst[cColorIndexInStandarColorLst];
                cClosestColorNameLst.Add(cClosestColorName);
            }

            // zip everything together
            var zip = cWeightLst.Zip(cClosestColorNameLst, (cW, cC) => new { cW, cC }).Zip(cPatterNameLst, (t, cPN) => new { cWeight = t.cW, cColor = t.cC, cPatterName = cPN });
            
            // get the proper names
            foreach (var data in zip)
            {
                string curveStyleName = $"STM-EP{data.cWeight}-{data.cColor}-{data.cPatterName}";
                curveStyleNameLst.Add(curveStyleName);
            }

            return curveStyleNameLst;
        }
        #endregion

        #region helper methods
        private static Tuple<IList<int?>, IList<Autodesk.Revit.DB.Color>, IList<string> > GetCurveData(Document doc, IEnumerable<CurveElement> curves)
        {
            IList<int?> cWeightLst = new List<int?>();
            IList<Autodesk.Revit.DB.Color> cColorLst = new List<Autodesk.Revit.DB.Color>();
            IList<string> cPatternNamesLst = new List<string>();
            
            foreach (CurveElement c in curves)
            {
                // get graphic style
                GraphicsStyle cG = (GraphicsStyle)c.LineStyle;

                // get weight of curve in the nullabel int format
                int? cWeight = cG.GraphicsStyleCategory.GetLineWeight(GraphicsStyleType.Projection);
                cWeightLst.Add(cWeight);

                // get color of the curve in DB.Color format
                Autodesk.Revit.DB.Color cColor = cG.GraphicsStyleCategory.LineColor;
                cColorLst.Add(cColor);

                // get name of the curve's pattern in string format
                ElementId cPatternId = cG.GraphicsStyleCategory.GetLinePatternId(GraphicsStyleType.Projection);
                if (cPatternId != null)
                {
                    LinePatternElement cPattern = (LinePatternElement)doc.GetElement(cPatternId);
                    if (cPattern != null)
                    {
                        string cPatternName = cPattern.GetLinePattern().Name.ToUpper(); //last step is to convert the name to upper cases
                        cPatternNamesLst.Add(cPatternName);
                    }
                    else
                    {
                        cPatternNamesLst.Add("SOLID");
                    }
                }
            }

            return Tuple.Create(cWeightLst, cColorLst, cPatternNamesLst);
        }

        private static int ColorDiff(Autodesk.Revit.DB.Color c1, Autodesk.Revit.DB.Color c2)
        {
            return (int)Math.Sqrt((c1.Red - c2.Red)* (c1.Red - c2.Red)
                                   + (c1.Green - c2.Green)* (c1.Green - c2.Green)
                                   + (c1.Blue - c2.Blue)* (c1.Blue - c2.Blue));
        }

        private static int ClosestColorRGB(IList<Autodesk.Revit.DB.Color> colorLst, Autodesk.Revit.DB.Color targetColor) 
        {
            IList <int> colorDistanceList = new List<int>();

            foreach (Autodesk.Revit.DB.Color c in colorLst)
            {
                int colorDistance = ColorDiff(c, targetColor);
                
                colorDistanceList.Add(colorDistance);
            }

            int indexOfTheClosestColor = colorDistanceList.IndexOf(colorDistanceList.Min());
            return indexOfTheClosestColor;
        }

        private static IList<string> TrimString(IList<string> stringLst, string str)
        { 
            IList<string> result = new List<string>();

            foreach (string s in stringLst)
            {
                if (!String.IsNullOrWhiteSpace(s))
                {
                    if (!s.Contains(str))
                    {
                        result.Add(s);
                    }
                    else
                    {
                        int charLocation = s.IndexOf(str);

                        if (Char.IsDigit(s, charLocation - 1) | Char.IsLetter(s, charLocation - 1))
                        {
                            string subStr = s.Replace(str, "");
                            result.Add(subStr);
                        }
                        else
                        {
                            str = s[charLocation - 1] + str;
                            string subStr = s.Replace(str, "");
                            result.Add(subStr);
                        }
                    }

                }
            }

            return result;
        }

        #endregion
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
