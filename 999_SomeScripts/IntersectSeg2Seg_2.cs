using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOBA;
namespace TestAPI
{
    class Program
    {
        // Given three colinear points p, q, r, the function checks if
        // point q lies on line segment 'pr'
        private static bool onSegment(LogicVector3 p, LogicVector3 q, LogicVector3 r)
        {
            if (q.x <= MathUtils.Max(p.x, r.x) && q.x >= MathUtils.Min(p.x, r.x) &&
                q.z <= MathUtils.Max(p.z, r.z) && q.z >= MathUtils.Min(p.z, r.z))
                return true;

            return false;
        }

        // To find orientation of ordered triplet (p, q, r).
        // The function returns following values
        // 0 --> p, q and r are colinear
        // 1 --> Clockwise
        // 2 --> Counterclockwise
        private static int orientation(LogicVector3 p, LogicVector3 q, LogicVector3 r)
        {
            // See https://www.geeksforgeeks.org/orientation-3-ordered-points/
            // for details of below formula.
            //long val = (long)(q.z - p.z) * (long)(r.x - q.x) - (long)(q.x - p.x) * (long)(r.z - q.z);

            long val = (long)(q.z - p.z) * (long)(r.x - q.x) - (long)(q.x - p.x) * (long)(r.z - q.z);

            if (val == 0) return 0;  // colinear
            
            return (val > 0) ? 1 : 2;
        }

        // The main function that returns true if line segment 'p1q1'
        // and 'p2q2' intersect.
        private static bool doIntersect(LogicVector3 p1, LogicVector3 q1, LogicVector3 p2, LogicVector3 q2)
        {
            // Find the four orientations needed for general and
            // special cases
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
            {
                return true;
            }

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and q2 are colinear and q2 lies on segment p1q1
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases
        }


        static void Main(string[] args)
        {
            LogicVector3 p1 = new LogicVector3(23630, 2000, -21215);
            LogicVector3 q1 = new LogicVector3(37702, 2000, -21115);
            LogicVector3 p2 = new LogicVector3(-251414, 2000, -27466);
            LogicVector3 q2 = new LogicVector3(-181414, 2000, 93774);

            //LogicVector3 p1 = new LogicVector3(-124705, 2000, -122537);
            //LogicVector3 q1 = new LogicVector3(-117868, 2000, -130577);
            //LogicVector3 p2 = new LogicVector3(-340000, 2000, -20000);
            //LogicVector3 q2 = new LogicVector3(-270000, 2000, 101240);

            bool b = doIntersect(p1, q1, p2, q2);
            //Console.ReadLine();
        }
    }
}
