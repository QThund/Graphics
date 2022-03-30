// Copyright 2021 Alejandro Villalba Avila
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.

using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Captures the meshes of all the child shadow caster 2Ds and blends them into one, replacing them.
    /// When the shadows are to be renderer, only 1 drawcall is executed.
    /// </summary>
    [AddComponentMenu("Rendering/2D/Shadow Caster 2D Optimizer (Experimental)")]
    [ExecuteAlways]
    public class ShadowCaster2DOptimizer : ShadowCaster2D
    {
        protected void Start()
        {
            if(Application.isPlaying)
            {
                GenerateBlendedMesh();
            }
        }

        protected new void OnEnable()
        {
            if(!Application.isPlaying)
            {
                GenerateBlendedMesh();
            }
        }

        public void GenerateBlendedMesh()
        {
            ShadowCaster2D[] shadowCasters = transform.GetComponentsInChildren<ShadowCaster2D>(true);

            if (shadowCasters.Length > 0)
            {
                m_Mesh = new Mesh();

                int vertexCount = 0;

                for (int i = 0; i < shadowCasters.Length; ++i)
                {
                    vertexCount += shadowCasters[i].mesh.vertexCount;
                }

                List<Vector3> vertices = new List<Vector3>(vertexCount);
                List<int> triangles = new List<int>(vertexCount);
                List<Vector4> tangents = new List<Vector4>(vertexCount * 3);
                List<Color> extrusions = new List<Color>(vertexCount);
                int firstTriangleInMesh = 0;

                for (int i = 0; i < shadowCasters.Length; ++i)
                {
                    if (shadowCasters[i] == this)
                    {
                        continue;
                    }

                    Mesh shadowCasterMesh = shadowCasters[i].mesh;

                    Vector3[] shadowCasterMeshVertices = shadowCasterMesh.vertices;
                    Color[] shadowCasterMeshExtrusions = shadowCasterMesh.colors;

                    for (int j = 0; j < shadowCasterMeshVertices.Length; ++j)
                    {
                        // When the vertices of the shadow casters are added, they are in local space. They have to be transformed to world space
                        // (the optimizer is assumed to be at [0,0] in world space)
                        Vector4 point = shadowCasterMeshVertices[j];
                        point.w = 1.0f;
                        vertices.Add(shadowCasters[i].transform.localToWorldMatrix * point);

                        // Vertices used in shadow rendering store a "color" whose first 2 components are the local position of a vertex and the last 2 components are the local position of the next vertex, forming an edge
                        Vector2 extrusion1 = shadowCasters[i].transform.localToWorldMatrix * new Vector4(shadowCasterMeshExtrusions[j].r, shadowCasterMeshExtrusions[j].g, 0.0f, 1.0f);
                        Vector2 extrusion2 = shadowCasters[i].transform.localToWorldMatrix * new Vector4(shadowCasterMeshExtrusions[j].b, shadowCasterMeshExtrusions[j].a, 0.0f, 1.0f);
                        extrusions.Add(new Color(extrusion1.x, extrusion1.y, extrusion2.x, extrusion2.y));
                    }

                    // Tangents are stored as normals of every edge that point to the interior of the shape
                    tangents.AddRange(shadowCasterMesh.tangents);

                    int[] shadowCasterMeshTriangles = shadowCasterMesh.triangles;

                    // An offset is added to the indices of a shape, according to the amount of vertices that precede to the vertices of the current shape
                    for (int j = 0; j < shadowCasterMeshTriangles.Length; ++j)
                    {
                        triangles.Add(shadowCasterMeshTriangles[j] + firstTriangleInMesh);
                    }

                    firstTriangleInMesh += shadowCasterMeshVertices.Length;

                    // All the shadow casters are disabled
                    if(Application.isPlaying)
                    {
                        Destroy(shadowCasters[i]);
                    }
                    else
                    {
                        shadowCasters[i].enabled = false;
                    }
                }

                m_Mesh.vertices = vertices.ToArray();
                m_Mesh.tangents = tangents.ToArray();
                m_Mesh.colors = extrusions.ToArray();
                m_Mesh.triangles = triangles.ToArray();

                Vector3 thisPosition = transform.position;

                // All the positions are moved so the local origin is at zero, no matter the world position of this component
                for (int i = 0; i < vertices.Count; ++i)
                {
                    vertices[i] -= thisPosition;
                    extrusions[i] -= new Color(thisPosition.x, thisPosition.y, thisPosition.x, thisPosition.y);
                }

                m_Mesh.vertices = vertices.ToArray();
                m_Mesh.colors = extrusions.ToArray();
            }
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 0.0f, 1.0f, (Mathf.Sin(Time.realtimeSinceStartup) + 1.0f) * 0.5f);

            Vector3[] vertices = m_Mesh.vertices;
            int[] triangles = m_Mesh.triangles;
            Vector3 position = transform.position;

            for (int i = 0; i < triangles.Length; i+=3)
            {
                Gizmos.DrawLine(vertices[triangles[i]] + position, vertices[triangles[i+1]] + position);
                Gizmos.DrawLine(vertices[triangles[i+1]] + position, vertices[triangles[i+2]] + position);
                Gizmos.DrawLine(vertices[triangles[i+2]] + position, vertices[triangles[i]] + position);
            }
        }

#endif

    }
}
