using UnityEngine;

namespace RedGame.GemCreator
{
    public class GemNormalRender
    {
        private static readonly int s_faceNormal = Shader.PropertyToID("_FaceNormal");
        private static readonly int s_smoothDistance = Shader.PropertyToID("_SmoothDistance");
        private static readonly int s_edgePoints = Shader.PropertyToID("_EdgePoints");
        private static readonly int s_edgeNormals = Shader.PropertyToID("_EdgeNormals");
        private static readonly int s_edgeCount = Shader.PropertyToID("_EdgeCount");
        private static readonly int s_smoothPower = Shader.PropertyToID("_SmoothPower");

        private Material _normalMaterial;
        
        private readonly Vector4[] _edgePointsParam = new Vector4[10];
        private readonly Vector4[] _edgeNormalsParam = new Vector4[10];

        
        public void RenderNormalRt(Rect rect, GemShape shape, float innerHeight, float smoothDistance, float smoothPower)
        {
            CreateMaterial();
            
            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            GL.LoadPixelMatrix(rect.xMin, rect.xMax, rect.yMin, rect.yMax);

            
            Matrix4x4 mat = Matrix4x4.Translate(rect.center)
                            * Matrix4x4.Scale(Vector3.one * (rect.height * 0.5f));

            _normalMaterial.SetFloat(s_smoothDistance, smoothDistance);
            _normalMaterial.SetFloat(s_smoothPower, smoothPower);
            RenderFontFaceNormal(shape, mat);
            
            RenderBorderFacesNormal(shape, innerHeight, mat);
            GL.PopMatrix();
            
            Graphics.SetRenderTarget(null);
        }

        private void CreateMaterial()
        {
            if (_normalMaterial)
                return;
            
            Shader shader = Shader.Find("GemCreator/GemNormal");
            _normalMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void RenderFontFaceNormal(GemShape shape, Matrix4x4 worldMatrix)
        {
            Vector3[] vertices = new Vector3[3];
            int[] triangles = {0, 1, 2};
            vertices[0] = Vector3.zero;
            var innerPoints = shape.InnerPoints;
            var connectEdges = shape.ConnectEdges;
            var innerEdges = shape.InnerEdges;
            for (int i = 0; i < innerPoints.Length; i++)
            {
                vertices[1] = innerPoints[i];
                vertices[2] = innerPoints[(i + 1) % innerPoints.Length];
                RenderFaceNormal(worldMatrix, vertices, triangles, Vector3.forward, 
                    new GemShape.GemEdge[]
                    {
                        connectEdges[i],
                        connectEdges[(i + 1) % connectEdges.Length],
                        innerEdges[(i - 1 + innerEdges.Length) % innerEdges.Length],
                        innerEdges[i],
                        innerEdges[(i + 1) % innerEdges.Length]
                    });
            }
            
        }

        private void RenderBorderFacesNormal(GemShape shape, float innerHeight, Matrix4x4 worldMatrix)
        {
            var innerPoints = shape.InnerPoints;
            var outerPoints = shape.OuterPoints;
            var connectEdges = shape.ConnectEdges;
            var innerEdges = shape.InnerEdges;
            
            for (int i = 0; i < outerPoints.Length; i++)
            {
                Vector3 p3 = outerPoints[i];
                Vector3 p2 = innerPoints[i];
                Vector3 p1 = innerPoints[(i + 1) % innerPoints.Length];
                Vector3 p0 = outerPoints[(i + 1) % outerPoints.Length];
                p1.z = innerHeight;
                p2.z = innerHeight;
                Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                p1.z = 0;
                p2.z = 0;
                
                RenderFaceNormal(worldMatrix, 
                    new[] {p0, p1, p2, p3}, 
                    new[] {0, 1, 2, 0, 2, 3}, 
                    normal, new[]
                    {
                        connectEdges[i],
                        connectEdges[(i + 1) % connectEdges.Length],
                        
                        //_outerEdges[(i - 1 + _outerEdges.Length) % _outerEdges.Length],
                        //_outerEdges[i], 
                        //_outerEdges[(i + 1) % _outerEdges.Length],
                        
                        innerEdges[(i - 1 + innerEdges.Length) % innerEdges.Length],
                        innerEdges[i],
                        innerEdges[(i + 1) % innerEdges.Length]
                    });
            }
        }
        
        private void RenderFaceNormal(Matrix4x4 worldMatrix, 
            Vector3[] vertices, int[] indices, Vector3 normal, GemShape.GemEdge[] edges)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                
                GemShape.GemEdge edge = edges[i];
                _edgePointsParam[i] = new Vector4(edge.P1.x, edge.P1.y, edge.P2.x, edge.P2.y);
                _edgeNormalsParam[i] = edge.Normal;
            }
            
            _normalMaterial.SetVectorArray(s_edgePoints, _edgePointsParam);
            _normalMaterial.SetVectorArray(s_edgeNormals, _edgeNormalsParam);
            _normalMaterial.SetInt(s_edgeCount, edges.Length);
            _normalMaterial.SetVector(s_faceNormal, normal);
            _normalMaterial.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            GL.modelview = worldMatrix;
            for (int i = 0; i < indices.Length; i += 3)
            {
                GL.Vertex(vertices[indices[i]]);
                GL.Vertex(vertices[indices[i + 1]]);
                GL.Vertex(vertices[indices[i + 2]]);
            }
            GL.End();
            GL.Flush();
        }

        
    }
}