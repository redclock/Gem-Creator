using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedGame.GemCreator
{
    public class GemShape
    {
        public struct GemEdge
        {
            public Vector3 P1;
            public Vector3 P2;
            public Vector3 Normal;
        }
        
        private GemSetting _setting = new GemSetting();
        
        private Vector3[] _outerPoints = Array.Empty<Vector3>();
        private Vector3[] _innerPoints = Array.Empty<Vector3>();
        private Vector3[] _faceNormals = Array.Empty<Vector3>();
        private Vector3[][] _facePoints = Array.Empty<Vector3[]>();
        private GemEdge[] _outerEdges = Array.Empty<GemEdge>();
        private GemEdge[] _innerEdges = Array.Empty<GemEdge>();
        private GemEdge[] _connectEdges = Array.Empty<GemEdge>();
        
        
        public Vector3[] OuterPoints => _outerPoints;
        public Vector3[] InnerPoints => _innerPoints;
        public Vector3[] FaceNormals => _faceNormals;
        public Vector3[][] FacePoints => _facePoints;
        public GemEdge[] OuterEdges => _outerEdges;
        public GemEdge[] InnerEdges => _innerEdges;
        public GemEdge[] ConnectEdges => _connectEdges;
        
        public bool IsCreated => _outerPoints.Length > 0;
        
        public void CreateShape(GemSetting setting)
        {
            _setting = setting;
            Vector3[] basePoints = CreateBasePoints();
            Vector3[] innerBasePoints = CreateInnerPoints(_setting.innerLen, _setting.innerHeight, basePoints);
            
            float borderLen = Vector3.Distance(basePoints[0], basePoints[1]);
            float bevelLen = borderLen * _setting.bevelFactor;
            for (int i = 0; i < _setting.bevelIter; i++)
            {
                basePoints = CreateBeveledPoints(bevelLen, basePoints, borderLen * 0.01f);
            }
            
            _outerPoints = basePoints;
            
            float scale = GetScale(_outerPoints);

            borderLen = Vector3.Distance(innerBasePoints[0], innerBasePoints[1]);
            bevelLen = borderLen * _setting.bevelFactor;
            
            for (int i = 0; i < _setting.bevelIter; i++)
            {
                innerBasePoints = CreateBeveledPoints(bevelLen, innerBasePoints, borderLen * 0.01f);
            }
            
            _innerPoints = innerBasePoints;
            
            
            for (int i = 0; i < _outerPoints.Length; i++)
            {
                _outerPoints[i] *= scale;
            }
            
            for (int i = 0; i < _innerPoints.Length; i++)
            {
                _innerPoints[i] *= scale;
            }

            _facePoints = ComputeFaces(_outerPoints, _innerPoints, out _faceNormals);
            ComputeEdges(_outerPoints, _innerPoints);
        }

        private Vector3[] CreateBasePoints()
        {
            float anglePart = 2 * Mathf.PI / _setting.borderCount;
            float startAngle = -Mathf.PI / 2 - anglePart / 2;
            Vector3[] basePoints = new Vector3[_setting.borderCount];
            for (int i = 0; i < _setting.borderCount; i++)
            {
                float angle = anglePart * i + startAngle;
                float x = Mathf.Cos(angle);// * _setting.scaleWidth;
                float y = Mathf.Sin(angle);
                float z = 0;
                basePoints[i] = new Vector3(x, y, z);
            }

            return basePoints;
        }

        private void GetNeighbourPoints(Vector3[] points, int i, out Vector3 p0, out Vector3 p1, out Vector3 p2)
        {
            p0 = points[(i + points.Length - 1) % points.Length];
            p1 = points[i];
            p2 = points[(i + 1) % points.Length];
        }
        
        private void AddPointIfNotExists(List<Vector3> points, Vector3 p, float epsilon)
        {
            if (points.Count == 0)
            {
                points.Add(p);
                return;
            }
            
            Vector3 last = points[^1];
            if (Vector3.Distance(last, p) > epsilon)
            {
                points.Add(p);
            }
        }

        private Vector3[] CreateBeveledPoints(float bevelLen, Vector3[] basePoints, float epsilon)
        {
            if (bevelLen <= 0)
            {
                return basePoints;
            }
            

            List<Vector3> outerPoints = new List<Vector3>();
            for (int i = 0; i < basePoints.Length; i++)
            {
                GetNeighbourPoints(basePoints, i, out Vector3 p0, out Vector3 p1, out Vector3 p2);
                Vector3 dir1 = p0 - p1;
                Vector3 dir2 = p2 - p1;
                float borderLen = Mathf.Min(dir1.magnitude, dir2.magnitude);
                float boardBevelLen = Mathf.Min(borderLen / 2, bevelLen);
                dir1.Normalize();
                dir2.Normalize();
                AddPointIfNotExists(outerPoints, p1 + dir1 * boardBevelLen, epsilon);
                AddPointIfNotExists(outerPoints, p1 + dir2 * boardBevelLen, epsilon);
            }

            
            return outerPoints.ToArray();
        }
        
        private float GetScale(Vector3[] points)
        {
            if (points.Length == 0)
                return 1;
            
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 0; i < points.Length; i++)
            {
                bounds.Encapsulate(points[i]);
            }
            bounds.Expand(0.05f);

            return 1 / Mathf.Max(bounds.max.x, bounds.max.y, -bounds.min.x, -bounds.min.y);
        } 
        
        private Vector3[] CreateInnerPoints(float innerLen, float innerHeight, Vector3[] outerPoints)
        {
            Vector3[] innerPoints = new Vector3[outerPoints.Length];
            for (int i = 0; i < outerPoints.Length; i++)
            {
                GetNeighbourPoints(outerPoints, i, out Vector3 p0, out Vector3 p1, out Vector3 p2);
                Vector3 dir1 = (p0 - p1).normalized;
                Vector3 dir2 = (p2 - p1).normalized;
                Vector3 dir = (dir1 + dir2).normalized;
                float d = Vector3.Dot(dir1, dir);
                float l = innerLen / Mathf.Sqrt(1 - d * d);
                innerPoints[i] = p1 + dir * l;
                //innerPoints[i].z = innerHeight;
            }

            return innerPoints;
        }

        private Vector3[][] ComputeFaces(Vector3[] outerPoints, Vector3[] innerPoints, out Vector3[] faceNormals)
        {
            Vector3[][] facePoints = new Vector3[outerPoints.Length][];
            faceNormals = new Vector3[outerPoints.Length];
            for (int i = 0; i < outerPoints.Length; i++)
            {
                Vector3 p0 = outerPoints[i];
                Vector3 p1 = innerPoints[i];
                p1.z = _setting.innerHeight;
                Vector3 p2 = innerPoints[(i + 1) % innerPoints.Length];
                p2.z = _setting.innerHeight;
                Vector3 p3 = outerPoints[(i + 1) % outerPoints.Length];
                Vector3 normal = Vector3.Cross(p2 - p0, p1 - p0).normalized;

                p1.z = 0;
                p2.z = 0;
                facePoints[i] = new[]
                {
                    p0, p1, p2, p3
                };
                
                
                faceNormals[i] = normal;
            }

            return facePoints;
        }

        private void ComputeEdges(Vector3[] outerPoints, Vector3[] innerPoints)
        {
            // Compute outer edges
            _outerEdges = new GemEdge[outerPoints.Length];
            for (int i = 0; i < outerPoints.Length; i++)
            {
                Vector3 p1 = outerPoints[i];
                Vector3 p2 = outerPoints[(i + 1) % outerPoints.Length];
                Vector3 boarderNormal = Vector3.Cross(p2 - p1, Vector3.forward).normalized;
                Vector3 faceNormal = _faceNormals[i];
                Vector3 normal = (boarderNormal + faceNormal).normalized;
                _outerEdges[i] = new GemEdge
                {
                    P1 = p1,
                    P2 = p2,
                    Normal = normal
                };
            }
            
            // Compute inner edges
            _innerEdges = new GemEdge[innerPoints.Length];
            for (int i = 0; i < innerPoints.Length; i++)
            {
                Vector3 p1 = innerPoints[i];
                Vector3 p2 = innerPoints[(i + 1) % innerPoints.Length];
                Vector3 boarderNormal = Vector3.forward;
                Vector3 faceNormal = _faceNormals[i];
                Vector3 normal = (boarderNormal + faceNormal).normalized;
                _innerEdges[i] = new GemEdge
                {
                    P1 = p1,
                    P2 = p2,
                    Normal = normal
                };
            }
            
            // Compute connect edges
            _connectEdges = new GemEdge[outerPoints.Length];
            for (int i = 0; i < outerPoints.Length; i++)
            {
                Vector3 p1 = outerPoints[i];
                Vector3 p2 = innerPoints[i];
                Vector3 faceNormal1 = _faceNormals[i];
                Vector3 faceNormal2 = _faceNormals[(i - 1 + _faceNormals.Length) % _faceNormals.Length];
                Vector3 normal = (faceNormal1 + faceNormal2).normalized;
                _connectEdges[i] = new GemEdge
                {
                    P1 = p1,
                    P2 = p2,
                    Normal = normal
                };
            }
        }

    }
    
}