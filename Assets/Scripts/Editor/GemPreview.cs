using RedGame.GemCreator;
using UnityEditor;
using UnityEngine;

namespace RedGame.EditorTools
{
    public class GemPreview
    {
        private GemShape _shape;
        
        public void DrawPreview(GemShape shape, Rect rect)
        {
            _shape = shape;
            
            Handles.matrix = Matrix4x4.Translate(rect.center) * Matrix4x4.Scale(
                new Vector3(1, -1, 1) * 50);

            //DrawPreviewFaceNormal();
            

            DrawPreviewOutline();
            

            Handles.matrix = Matrix4x4.identity;
            _shape = null;
        }

        private void DrawPreviewFaceNormal()
        {
            Vector3 frontNormal = Vector3.forward;
            Handles.color = NormalToColor(frontNormal);
            
            Handles.DrawAAConvexPolygon(_shape.InnerPoints);
            for (int i = 0; i < _shape.FaceNormals.Length; i++)
            {
                Vector3[] facePoints = _shape.FacePoints[i];
                Vector3 normal = _shape.FaceNormals[i];
                Handles.color = NormalToColor(normal);
                Handles.DrawAAConvexPolygon(facePoints);
            }
        }

        private void DrawPreviewOutline()
        {
            Handles.color = Color.white;
            foreach (GemShape.GemEdge edge in _shape.OuterEdges)
            {
                //Handles.color = NormalToColor(edge.Normal);
                Handles.DrawLine(edge.P1, edge.P2);
            }
            
            foreach (GemShape.GemEdge edge in _shape.InnerEdges)
            {
                //Handles.color = NormalToColor(edge.Normal);
                Handles.DrawLine(edge.P1, edge.P2);
            }

            foreach (GemShape.GemEdge edge in _shape.ConnectEdges)
            {
                //Handles.color = NormalToColor(edge.Normal);
                Handles.DrawLine(edge.P1, edge.P2);
            }
        }

        private Color NormalToColor(Vector3 normal)
        {
            Vector3 n = normal * 0.5f + Vector3.one * 0.5f;
            return new Color(n.x, n.y, n.z);
        }
    }
}