using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace StaircaseDetails
{
    public class StaircaseClass
    {
        [CommandMethod("StaircaseDetails")]
        public static void StaircaseDetails()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            MainWindow win = new MainWindow();

            try
            {
                Application.ShowModalWindow(win);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError showing window: {ex.Message}");
                return;
            }

            if (!win.WasGenerated)
            {
                ed.WriteMessage("\nStaircase details generation cancelled.");
                return;
            }

            try
            {
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    Database db = doc.Database;
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    EnsureLayer(tr, db, "STAIR-OUTLINE", 7);
                    EnsureLayer(tr, db, "STAIR-REBAR", 1);
                    EnsureLayer(tr, db, "STAIR-LANDING", 3);
                    EnsureLayer(tr, db, "STAIR-TEXT", 2);
                    EnsureLayer(tr, db, "STAIR-GRID", 4);
                    EnsureLayer(tr, db, "STAIR-HATCH", 4);

                    int sign = win.Orientation == "Right to Left" ? -1 : 1;

                    double floorHeightPerFlight = win.FloorHeight / win.NumberOfFlights;
                    int fallbackNumRisers = (int)Math.Round(floorHeightPerFlight / win.Riser);

                    Point2d overallOrigin = new Point2d(0, 0);
                    Point2d flightOrigin = overallOrigin;

                    Point2d flightEnd;
                    Point2d waistTopExtent = flightOrigin;   // default fallback
                    Point2d waistBottomExtent = flightOrigin;

                    for (int flightIdx = 0; flightIdx < win.Flights.Count; flightIdx++)
                    {
                        var flight = win.Flights[flightIdx];
                        bool isFlight1 = flightIdx == 0;

                        int numRisers = ParseIntOrDefault(flight.NumberOfSteps, fallbackNumRisers);

                        double lowerLandingThickness = ParseOrDefault(flight.LowerLandingThickness, win.WaistThickness);
                        double upperLandingWidth = ParseOrDefault(flight.UpperLandingWidth, 1000);

                        bool hasWallAtEnd = flight.WallAtEnd?.IsChecked == true;
                        double wallWidth = ParseOrDefault(flight.WallWidth, 0);

                        bool hasBeamAtStart = flight.BeamAtStart?.IsChecked == true;
                        double beamStartDepth = ParseOrDefault(flight.BeamStartDepth, 0);
                        double beamStartWidth = ParseOrDefault(flight.BeamStartWidth, 0);

                        bool hasBeamAtEnd = flight.BeamAtEnd?.IsChecked == true;
                        double beamEndDepth = ParseOrDefault(flight.BeamEndDepth, 0);
                        double beamEndWidth = ParseOrDefault(flight.BeamEndWidth, 0);

                        // Support at the BOTTOM of this flight (Flight_Origin)
                        bool hasLowerBeam;
                        double lowerBeamDepth, lowerBeamWidth;
                        bool hasSlabThickening = false;

                        if (isFlight1)
                        {
                            hasLowerBeam = win.IsGroundFloor && win.IsBeamSupport;
                            lowerBeamDepth = win.GroundBeamDepth;
                            lowerBeamWidth = win.GroundBeamWidth;
                            hasSlabThickening = win.IsGroundFloor && win.IsSlabThickening;
                        }
                        else
                        {
                            var prevFlight = win.Flights[flightIdx - 1];
                            hasLowerBeam = prevFlight.BeamAtEnd?.IsChecked == true;
                            lowerBeamDepth = ParseOrDefault(prevFlight.BeamEndDepth, 0);
                            lowerBeamWidth = ParseOrDefault(prevFlight.BeamEndWidth, 0);
                        }

                        // --- Build and draw the concrete hatch boundary (half-turn only, per current spec) ---

                        
                        //Point2d waistBottomExtent = flightOrigin;
                        //Point2d waistTopExtent = flightEnd;

                        if (win.IsHalfTurn)
                        {
                            var boundaryPts = BuildHalfTurnHatchBoundary(
                                flightOrigin, sign, win.Riser, win.Tread, numRisers, win.WaistThickness,
                                lowerLandingThickness, upperLandingWidth,
                                hasWallAtEnd, wallWidth,
                                hasBeamAtEnd, beamEndDepth, beamEndWidth,
                                hasBeamAtStart, beamStartDepth, beamStartWidth,
                                isFlight1,
                                hasLowerBeam, lowerBeamDepth, lowerBeamWidth,
                                hasSlabThickening,
                                out waistTopExtent, out waistBottomExtent);   // ← receive extended endpoints

                            DrawConcreteHatch(tr, btr, boundaryPts);
                        }

                        //Point2d flightEnd;
                        DrawFlightProfile(tr, btr, flightOrigin, sign, win.Riser, win.Tread, numRisers,
                                           win.WaistThickness, out flightEnd);


                        DrawReinforcement(tr, btr, flightOrigin, flightEnd, win.WaistThickness,
                                           win.LongitudinalBarSpacing, win.TransverseBarSpacing);

                        // --- Landings ---
                        double lowerLandingWidth = ParseOrDefault(flight.LowerLandingWidth, 1000);
                        //DrawLanding(tr, btr, new Point2d(flightOrigin.X - sign * lowerLandingWidth, flightOrigin.Y),
                        //            lowerLandingWidth, lowerLandingThickness);

                        double upperLandingThickness = ParseOrDefault(flight.UpperLandingThickness, win.WaistThickness);
                        //DrawLanding(tr, btr, flightEnd, upperLandingWidth, upperLandingThickness);

                        // Lower landing: extends from a point 'lowerLandingWidth' before flightOrigin,
                        // back UP TO flightOrigin (in the sign direction)
                        DrawLanding(tr, btr, new Point2d(flightOrigin.X - sign * lowerLandingWidth, flightOrigin.Y),
                                    sign, lowerLandingWidth, lowerLandingThickness);

                        // Upper landing: extends outward from flightEnd, away from the stairs, in the sign direction
                        DrawLanding(tr, btr, flightEnd, sign, upperLandingWidth, upperLandingThickness);

                        AddText(tr, btr, new Point3d(flightOrigin.X, flightOrigin.Y - 300, 0),
                                $"Flight {flight.FlightNumber} - Staircase {win.StaircaseNumber}", 150);

                        // --- Advance to next flight's origin ---
                        //double gap = win.IsHalfTurn ? win.ClearPlanDistance : 500;
                        //double nextX = flightEnd.X + sign * (upperLandingWidth + gap);
                        //flightOrigin = new Point2d(nextX, flightEnd.Y);

                        // Half-turn stairs typically reverse direction each flight
                        if (win.IsHalfTurn)
                        {
                            // Reverse direction first
                            sign = -sign;

                            // Second flight starts at the upper landing
                            flightOrigin = flightEnd;
                        }
                        else
                        {
                            double nextX = flightEnd.X + sign * (upperLandingWidth + 500);
                            flightOrigin = new Point2d(nextX, flightEnd.Y);
                        }
                    }

                    if (win.HasGrids)
                    {
                        DrawGrid(tr, btr, overallOrigin, win.GridDistance, win.GridLabel);
                    }

                    tr.Commit();
                }

                ed.WriteMessage("\nStaircase drawing generated.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError generating drawing: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ================= HATCH BOUNDARY (half-turn spec) =================

        private static List<Point2d> BuildHalfTurnHatchBoundary(
    Point2d flightOrigin,
    int sign,
    double riser,
    double tread,
    int numSteps,
    double waistThickness,
    double lowerLandingThickness,
    double upperLandingWidth,
    bool hasWallOrBeamAtEnd, double wallOrBeamWidthAtEnd,
    bool hasBeamAtEndOfLanding, double beamEndDepth, double beamEndWidth,
    bool hasBeamAtStartOfLanding, double beamStartDepth, double beamStartWidth,
    bool isFlight1,
    bool hasLowerBeam, double lowerBeamDepth, double lowerBeamWidth,
    bool hasSlabThickening,
    out Point2d waistTopExtent,      // NEW: top support point for waist/rebar line
    out Point2d waistBottomExtent)   // NEW: bottom support point for waist/rebar line
        {
            var pts = new List<Point2d>();

            Point2d p1 = new Point2d(flightOrigin.X + sign * -1000, flightOrigin.Y);
            pts.Add(p1);
            pts.Add(flightOrigin);

            Point2d cur = flightOrigin;
            for (int i = 0; i <= numSteps - 2; i++)
            {
                cur = new Point2d(cur.X, cur.Y + riser);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * tread, cur.Y);
                pts.Add(cur);
            }

            cur = new Point2d(cur.X, cur.Y + riser);
            pts.Add(cur);
            Point2d topOfStairs = cur;

            double endExtra = hasWallOrBeamAtEnd ? wallOrBeamWidthAtEnd : 0;
            cur = new Point2d(cur.X + sign * (upperLandingWidth + endExtra), cur.Y);
            pts.Add(cur);
            waistTopExtent = cur; // ← captured here: far end of upper landing (top support)

            if (hasBeamAtEndOfLanding)
            {
                cur = new Point2d(cur.X, cur.Y - beamEndDepth);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * -beamEndWidth, cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X, cur.Y + (beamEndDepth - waistThickness));
                pts.Add(cur);
            }

            if (!hasBeamAtStartOfLanding)
            {
                cur = new Point2d(topOfStairs.X + sign * tread, cur.Y);
                pts.Add(cur);
            }
            else
            {
                cur = new Point2d(topOfStairs.X + sign * (tread + beamStartWidth), cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X, cur.Y - (beamStartDepth - waistThickness));
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * -beamStartWidth, cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X, cur.Y + (beamStartDepth - waistThickness));
                pts.Add(cur);
            }
            Point2d p8 = cur;

            Point2d rayDir = new Point2d(-sign * numSteps * tread, -numSteps * riser);
            Point2d rayEnd = new Point2d(p8.X + rayDir.X, p8.Y + rayDir.Y);

            Point2d p9;
            if (!isFlight1 && hasLowerBeam)
            {
                double xLine2 = flightOrigin.X + sign * lowerBeamWidth;
                p9 = IntersectWithVerticalLine(p8, rayEnd, xLine2);
            }
            else if (!isFlight1 && !hasLowerBeam)
            {
                double yLine2 = flightOrigin.Y - lowerLandingThickness;
                p9 = IntersectWithHorizontalLine(p8, rayEnd, yLine2);
            }
            else
            {
                p9 = IntersectWithHorizontalLine(p8, rayEnd, flightOrigin.Y);
            }
            pts.Add(p9);
            waistBottomExtent = p9; // ← captured here: bottom intersection (lower support)
            cur = p9;

            if (!isFlight1 && hasLowerBeam)
            {
                cur = new Point2d(cur.X, flightOrigin.Y - lowerBeamDepth);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * -lowerBeamWidth, cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X, flightOrigin.Y - lowerLandingThickness);
                pts.Add(cur);
            }
            else if (!isFlight1 && !hasLowerBeam)
            {
                // no additional points
            }
            else if (isFlight1 && hasSlabThickening)
            {
                cur = new Point2d(flightOrigin.X + sign * 650, cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * -50, cur.Y - 50);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * -600, cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * -50, cur.Y + 50);
                pts.Add(cur);
            }
            else if (isFlight1 && hasLowerBeam)
            {
                cur = new Point2d(flightOrigin.X + sign * lowerBeamWidth, cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X, flightOrigin.Y - lowerBeamDepth);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * -lowerBeamWidth, cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X, flightOrigin.Y - lowerLandingThickness);
                pts.Add(cur);
            }
            else
            {
                cur = new Point2d(cur.X + sign * 1000, cur.Y);
                pts.Add(cur);
                cur = new Point2d(cur.X, cur.Y - (waistThickness / 2 + 25));
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * 35, cur.Y - 12.5);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * -70, cur.Y - 25);
                pts.Add(cur);
                cur = new Point2d(cur.X + sign * 35, cur.Y - 25);
                pts.Add(cur);
                cur = new Point2d(cur.X, flightOrigin.Y - waistThickness);
                pts.Add(cur);
            }

            Point2d qb1 = new Point2d(flightOrigin.X + sign * -1000, flightOrigin.Y - waistThickness);
            pts.Add(qb1);
            Point2d qb2 = new Point2d(qb1.X, qb1.Y + (waistThickness / 2 - 25));
            pts.Add(qb2);
            Point2d qb3 = new Point2d(qb2.X + sign * 35, qb2.Y + 12.5);
            pts.Add(qb3);
            Point2d qb4 = new Point2d(qb3.X + sign * -70, qb3.Y + 25);
            pts.Add(qb4);
            Point2d qb5 = new Point2d(qb4.X + sign * 35, qb4.Y + 25);
            pts.Add(qb5);

            pts.Add(p1);

            return pts;
        }

        private static Point2d IntersectWithVerticalLine(Point2d a, Point2d b, double x)
        {
            double t = (x - a.X) / (b.X - a.X);
            double y = a.Y + t * (b.Y - a.Y);
            return new Point2d(x, y);
        }

        private static Point2d IntersectWithHorizontalLine(Point2d a, Point2d b, double y)
        {
            double t = (y - a.Y) / (b.Y - a.Y);
            double x = a.X + t * (b.X - a.X);
            return new Point2d(x, y);
        }

        //private static void DrawConcreteHatch(Transaction tr, BlockTableRecord btr, List<Point2d> boundaryPoints)
        //{
        //    Polyline boundary = new Polyline();
        //    for (int i = 0; i < boundaryPoints.Count; i++)
        //    {
        //        boundary.AddVertexAt(i, boundaryPoints[i], 0, 0, 0);
        //    }
        //    boundary.Closed = true;
        //    boundary.Layer = "STAIR-HATCH";

        //    btr.AppendEntity(boundary);
        //    tr.AddNewlyCreatedDBObject(boundary, true);

        //    Hatch hatch = new Hatch();
        //    btr.AppendEntity(hatch);
        //    tr.AddNewlyCreatedDBObject(hatch, true);

        //    hatch.Layer = "STAIR-HATCH";
        //    hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
        //    hatch.PatternScale = 3;

        //    ObjectIdCollection loopIds = new ObjectIdCollection();
        //    loopIds.Add(boundary.ObjectId);
        //    hatch.AppendLoop(HatchLoopTypes.Default, loopIds);
        //    hatch.EvaluateHatch(true);
        //}

        private static void DrawConcreteHatch(Transaction tr, BlockTableRecord btr, List<Point2d> boundaryPoints)
        {
            Polyline boundary = new Polyline();
            for (int i = 0; i < boundaryPoints.Count; i++)
            {
                boundary.AddVertexAt(i, boundaryPoints[i], 0, 0, 0);
            }
            boundary.Closed = true;
            boundary.Layer = "STAIR-HATCH";

            btr.AppendEntity(boundary);
            tr.AddNewlyCreatedDBObject(boundary, true);

            Hatch hatch = new Hatch();
            btr.AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);

            hatch.Layer = "STAIR-HATCH";
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
            hatch.PatternScale = 3;

            ObjectIdCollection loopIds = new ObjectIdCollection();
            loopIds.Add(boundary.ObjectId);
            hatch.AppendLoop(HatchLoopTypes.Default, loopIds);
            hatch.EvaluateHatch(true);

            // Boundary was only needed to define the hatch loop — remove it so it doesn't
            // duplicate/ghost against the separately-drawn step outline
            boundary.UpgradeOpen();
            boundary.Erase();
        }

        // ================= EXISTING SIMPLE GEOMETRY (steps, landings, rebar, grid) =================

        private static double ParseOrDefault(TextBox box, double fallback)
        {
            if (box != null && double.TryParse(box.Text, out double val) && val > 0)
                return val;
            return fallback;
        }

        private static int ParseIntOrDefault(TextBox box, int fallback)
        {
            if (box != null && int.TryParse(box.Text, out int val) && val > 0)
                return val;
            return fallback;
        }

        private static void EnsureLayer(Transaction tr, Database db, string name, short colorIndex)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(name))
            {
                lt.UpgradeOpen();
                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = name,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                };
                lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);
            }
        }

        private static void DrawFlightProfile(Transaction tr, BlockTableRecord btr, Point2d start, int sign, double riser, double tread, int numRisers, double waistThickness, out Point2d endPoint)
        {
            Polyline stepProfile = new Polyline();
            Point2d pt = start;
            stepProfile.AddVertexAt(0, pt, 0, 0, 0);

            int vertexIndex = 1;
            for (int i = 0; i < numRisers; i++)
            {
                pt = new Point2d(pt.X, pt.Y + riser);
                stepProfile.AddVertexAt(vertexIndex++, pt, 0, 0, 0);

                if (i < numRisers - 1)
                {
                    pt = new Point2d(pt.X + sign * tread, pt.Y);
                    stepProfile.AddVertexAt(vertexIndex++, pt, 0, 0, 0);
                }
            }

            endPoint = pt;
            stepProfile.Layer = "STAIR-OUTLINE";
            btr.AppendEntity(stepProfile);
            tr.AddNewlyCreatedDBObject(stepProfile, true);

            double angle = start.GetVectorTo(endPoint).Angle;
            Vector2d offsetVec = new Vector2d(-Math.Sin(angle), Math.Cos(angle)) * -waistThickness;

            Line waistLine = new Line(
                new Point3d(start.X + offsetVec.X, start.Y + offsetVec.Y, 0),
                new Point3d(endPoint.X + offsetVec.X, endPoint.Y + offsetVec.Y, 0))
            { Layer = "STAIR-OUTLINE" };
            btr.AppendEntity(waistLine);
            tr.AddNewlyCreatedDBObject(waistLine, true);
        }

        private static void DrawLanding(Transaction tr, BlockTableRecord btr,
    Point2d basePoint, int sign, double width, double thickness)
        {
            Polyline landing = new Polyline();
            landing.AddVertexAt(0, basePoint, 0, 0, 0);
            landing.AddVertexAt(1, new Point2d(basePoint.X + sign * width, basePoint.Y), 0, 0, 0);
            landing.AddVertexAt(2, new Point2d(basePoint.X + sign * width, basePoint.Y - thickness), 0, 0, 0);
            landing.AddVertexAt(3, new Point2d(basePoint.X, basePoint.Y - thickness), 0, 0, 0);
            landing.Closed = true;
            landing.Layer = "STAIR-LANDING";

            btr.AppendEntity(landing);
            tr.AddNewlyCreatedDBObject(landing, true);
        }

        private static void DrawReinforcement(Transaction tr, BlockTableRecord btr,
            Point2d start, Point2d end, double waistThickness,
            int longSpacing, int transSpacing)
        {
            Line longBar = new Line(
                new Point3d(start.X, start.Y - waistThickness / 2, 0),
                new Point3d(end.X, end.Y - waistThickness / 2, 0))
            { Layer = "STAIR-REBAR" };
            btr.AppendEntity(longBar);
            tr.AddNewlyCreatedDBObject(longBar, true);

            double totalLength = start.GetDistanceTo(end);
            double angle = start.GetVectorTo(end).Angle;
            int numBars = (int)(totalLength / transSpacing);

            for (int i = 0; i <= numBars; i++)
            {
                double dist = i * transSpacing;
                if (dist > totalLength) break;

                Point2d basePt = new Point2d(
                    start.X + dist * Math.Cos(angle),
                    start.Y + dist * Math.Sin(angle));

                Vector2d perp = new Vector2d(-Math.Sin(angle), Math.Cos(angle)) * waistThickness;

                Line tick = new Line(
                    new Point3d(basePt.X, basePt.Y, 0),
                    new Point3d(basePt.X + perp.X, basePt.Y + perp.Y, 0))
                { Layer = "STAIR-REBAR" };
                btr.AppendEntity(tick);
                tr.AddNewlyCreatedDBObject(tick, true);
            }
        }

        private static void AddText(Transaction tr, BlockTableRecord btr, Point3d position, string text, double height)
        {
            DBText dbText = new DBText
            {
                Position = position,
                Height = height,
                TextString = text,
                Layer = "STAIR-TEXT"
            };
            btr.AppendEntity(dbText);
            tr.AddNewlyCreatedDBObject(dbText, true);
        }

        private static void DrawGrid(Transaction tr, BlockTableRecord btr, Point2d origin, double distance, string label)
        {
            Point3d gridStart = new Point3d(origin.X + distance, -1000, 0);
            Point3d gridEnd = new Point3d(origin.X + distance, 3000, 0);

            Line gridLine = new Line(gridStart, gridEnd) { Layer = "STAIR-GRID" };
            btr.AppendEntity(gridLine);
            tr.AddNewlyCreatedDBObject(gridLine, true);

            if (!string.IsNullOrWhiteSpace(label))
            {
                AddText(tr, btr, new Point3d(origin.X + distance - 50, 3050, 0), label, 200);
            }
        }
    }
}