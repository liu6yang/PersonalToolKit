#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

public class DebugUILine : MonoBehaviour
{
    static Vector3[] fourCorners = new Vector3[4];
    //List<string> myShader = new List<string>();
    void Start()
    {
        //myShader.Add("Particles/Alpha Blended Premultiply");
        //myShader.Add("Particles/Alpha Blended");
        //myShader.Add("Particles/Additive");
        //myShader.Add("Unlit/Transparent Cutout");
        //myShader.Add("Mobile/Particles/Alpha Blended");
        //myShader.Add("Mobile/Particles/Alpha Blended Premultiply");
        //myShader.Add("Mobile/Particles/Additive");
    }
    void OnDrawGizmos()
    {
        foreach (MaskableGraphic g in GameObject.FindObjectsOfType<MaskableGraphic>())
        {
            if (g.raycastTarget)
            {
                RectTransform rectTransform = g.transform as RectTransform;
                rectTransform.GetWorldCorners(fourCorners);
                Gizmos.color = Color.red;
                for (int i = 0; i < 4; i++)
                    Gizmos.DrawLine(fourCorners[i], fourCorners[(i + 1) % 4]);

            }
        }
    }
    void Update()
    {
        Renderer[] renders = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renders)
        {
            if (r != null)
            {
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null || m.shader == null)
                        continue;
                    //if (myShader.Contains(m.shader.name))
                    {
                        Shader shader = Shader.Find(m.shader.name);
                        m.shader = shader;
                    }
                }
            }
        }
    }
}
#endif