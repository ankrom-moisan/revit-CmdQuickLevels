using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


namespace QuickLevels
{
    [Transaction(TransactionMode.Manual)]

    public class CmdQuickLevels : IExternalCommand
    {
        private Document _doc;
        private ElementId _floorPlanType;
        private ElementId _ceilingPlanType;
        public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements)

        {
            _doc = commandData.Application.ActiveUIDocument.Document;
            _floorPlanType = GetViewFamily(ViewFamily.FloorPlan);
            _ceilingPlanType= GetViewFamily(ViewFamily.CeilingPlan);

            double offset = 20;
            double maxLevel = GetMaxLevel() + offset;

            Result result = CreateLevel(maxLevel);
            return result;
        }

        private ElementId GetViewFamily(ViewFamily familyType)
        {
            IEnumerable<ViewFamilyType> viewFamilyFloorPlan =
               from elem in new FilteredElementCollector(_doc)
               .OfClass(typeof(ViewFamilyType))
               let type = elem as ViewFamilyType
               where type.ViewFamily == familyType
               select type;
            return viewFamilyFloorPlan.First().Id;
        }

        private Result CreateLevel(double level)
        {
            using (Transaction tx = new Transaction(_doc))
            {
                tx.Start("Create New Level");
                try
                {
                    Level newLevel = _doc.Create.NewLevel(level);
                    if (null == newLevel)

                        newLevel.Name = "New Name";

                    ViewPlan.Create(_doc, _floorPlanType, newLevel.Id);
                    ViewPlan.Create(_doc, _ceilingPlanType, newLevel.Id);

                    tx.Commit();
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }
            }
            return Result.Succeeded;
        }

        private double GetMaxLevel()
        {
            IList<double> myValue = new List<double>();
            ElementCategoryFilter modelLevel = new ElementCategoryFilter(BuiltInCategory.OST_Levels);

            FilteredElementCollector coll = new FilteredElementCollector(_doc);
            IList<Element> levelList = coll.WherePasses(modelLevel).ToElements();
            foreach (Element e in levelList)
            {
                Parameter parameterLevel = e.get_Parameter(BuiltInParameter.LEVEL_ELEV);
                if (parameterLevel == null)
                {
                    continue;
                }

                double paramValue = parameterLevel.AsDouble();
                myValue.Add(paramValue);
            }
            return myValue.Max();
        }

    }
}