using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
            // ДЗ: подредактировать код = вынести построение стен в отдельный метод
            // напр., вызвали метод CreateWalls

            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = GetLevels(doc, "Уровень 1");
            Level level2 = GetLevels(doc, "Уровень 2");

            CreateWalls(doc, level1, level2);

            return Result.Succeeded;
        }

        public static Level GetLevels(Document doc, string levelName)
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

        public static void CreateWalls(Document doc, Level level1, Level level2)
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
            // поскольку в массиве будем перебирать точки попарно, т.е. построим, то добавим в массив точку иакую же, как и стартовая
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
            }

            transaction.Commit();
        }
    }
}
