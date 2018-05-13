using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MOBA;

public class IntersecSegmentCircle : MonoBehaviour
{

    public LogicVector3 start;
    public LogicVector3 end;
    public LogicVector3 center;
    public int radius;

    public int moveX = 0;
    public int moveY = 0;
    public int moveZ = 0;



    private void OnDrawGizmos()
    {
        LogicVector3 p = start + new LogicVector3(moveX, moveY, moveZ);
        LogicVector3 q = end + new LogicVector3(moveX, moveY, moveZ);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(p.x, p.y, p.z), new Vector3(q.x, q.y, q.z));

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(center.x, center.y, center.z), radius);

        bool b = IntersectSegCircle(center, radius, p, q);

        Debug.LogError("================== " + b);
    }

    private bool IntersectSegCircle(LogicVector3 center, int radius, LogicVector3 p, LogicVector3 q)
    {
        //一元二次方程 a(x^2) + bx + c = 0
        center.y = 0;
        p.y = 0;
        q.y = 0;

        LogicVector3 d = q - p;
        LogicVector3 f = p - center;

        int a = LogicVector3.DotD4(d, d);//必然 > 0,有最小值
        int b = 2 * LogicVector3.DotD4(f, d);
        int c = LogicVector3.DotD4(f, f) - (int)(((long)radius * (long)radius) / MathUtils.iPointUnit);
        long discriminant = (long)b * (long)b - 4 * (long)a * (long)c;

        //方程有解
        if (discriminant >= 0)
        {
            int value_0 = c;
            int value_1 = a + b + c;
            //在[0,1]上存在值
            if (value_0 > 0 && value_1 < 0)
            {
                return true;
            }
            if (value_0 < 0 && value_1 > 0)
            {
                return true;
            }
            if (value_0 == 0 || value_1 == 0)
            {
                return true;
            }

            //最小值时,0 <= -b/2a <= 1, 且 a > 0 (必然)
            bool bMinIn01 = false;
            if (-b <= 2 * a && -b >= 0)
            {
                bMinIn01 = true;
            }

            if (bMinIn01)
            {
                //最小值为value = (4ac-b*b)/(4a), 因 discriminant = b*b - 4ac 且 discriminant >=0
                //如discriminant > 0 ,4ac-b*b <= 0, 符号由-4a决定
                if (discriminant == 0)
                {
                    return true;
                }
                else
                {
                    //value_sign_min = -4 * a; 必然 < 0
                    if (value_0 > 0 && value_1 > 0) //value_0和value_1 必然同时 > 或 < 0
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
