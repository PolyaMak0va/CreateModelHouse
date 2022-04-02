using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CreateModelHouse
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreateModelHouseTask : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = GetLevels(doc, "Уровень 1");
            Level level2 = GetLevels(doc, "Уровень 2");

            CreateWalls(doc, level1, level2);

            return Result.Succeeded;
        }

        private static Level GetLevels(Document doc, string levelName)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level = listLevel
             .Where(x => x.Name.Equals(levelName))
             .FirstOrDefault();

            return level;
        }

        private static void CreateWalls(Document doc, Level level1, Level level2)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);

            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            //поскольку в массиве будем перебирать точки попарно, т.е. построим, то добавим в массив точку иакую же, как и стартовая
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc);
            transaction.Start("Построение стен");

            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);

                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);

                if (walls[i] == walls[0])
                    AddDoor(doc, level1, walls[0]);
                else
                    AddWindow(doc, level1, walls[i]);

            }
            transaction.Commit();
        }

        private static void AddWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(w => w.Name.Equals("0915 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;

            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!windowType.IsActive)
            {
                windowType.Activate();
            }

            var window = doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);

            Parameter parameterHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            double heightType = UnitUtils.ConvertToInternalUnits(900, UnitTypeId.Millimeters);
            parameterHeight.Set(heightType);
        }

        private static void AddDoor(Document doc, Level level1, Wall wall)
        {
            // найдём нужный типоразмер
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(d => d.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;

            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);

            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
            {
                doorType.Activate();
            }

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }
    }
}

