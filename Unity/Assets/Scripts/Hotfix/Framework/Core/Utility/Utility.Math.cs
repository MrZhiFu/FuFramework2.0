using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 数学计算相关的实用函数。
        /// 功能：
        ///     1. 屏幕像素和厘米与英寸的转换。
        ///     2. 检查两个矩形是否相交。
        ///     3. 两个矩形相交的点。
        ///     4. 角度转换为四元数。
        ///     5. 计算二维距离。
        ///     6. 计算从一个向量到另一个向量的360度角度。
        ///     7. 求点到直线的距离。
        ///     8. 判断射线是否碰撞到球体。
        ///     9. 去掉三维向量的Y轴，把向量投射到xz平面。
        ///     10. 判断目标点是否位于向量的左边。
        /// </summary>
        public static class Math
        {
            /// <summary>
            /// 屏幕每英寸点数 默认为windows dpi
            /// </summary>
            private const int DefaultDpi = 96;

            /// <summary>
            /// 英寸到厘米(1英寸 = 2.54厘米)
            /// </summary>
            private const float InchesToCentimeters = 2.54f;

            /// <summary>
            /// 厘米到英寸(1厘米 = 0.3937英寸)
            /// </summary>
            private const float CentimetersToInches = 1f / InchesToCentimeters;

            /// <summary>
            /// 获取屏幕每英寸点数。
            /// </summary>
            public static float ScreenDpi => Screen.dpi <= 0 ? DefaultDpi : Screen.dpi;


            /// <summary>
            /// 将像素转换为厘米。
            /// </summary>
            /// <param name="pixel">像素。</param>
            /// <returns>厘米。</returns>
            public static float Pixel2Centimeter(float pixel)
            {
                if (ScreenDpi <= 0) throw new InvalidOperationException("您必须先设置屏幕 DPI.");
                return InchesToCentimeters * pixel / ScreenDpi;
            }

            /// <summary>
            /// 将厘米转换为像素。
            /// </summary>
            /// <param name="centimeters">厘米。</param>
            /// <returns>像素。</returns>
            public static float Centimeter2Pixel(float centimeters)
            {
                if (ScreenDpi <= 0) throw new InvalidOperationException("您必须先设置屏幕 DPI.");
                return CentimetersToInches * centimeters * ScreenDpi;
            }

            /// <summary>
            /// 将像素转换为英寸。
            /// </summary>
            /// <param name="pixel">像素。</param>
            /// <returns>英寸。</returns>
            public static float Pixel2Inches(float pixel)
            {
                if (ScreenDpi <= 0) throw new InvalidOperationException("您必须先设置屏幕 DPI.");
                return pixel / ScreenDpi;
            }

            /// <summary>
            /// 将英寸转换为像素。
            /// </summary>
            /// <param name="inches">英寸。</param>
            /// <returns>像素。</returns>
            public static float Inches2Pixels(float inches)
            {
                if (ScreenDpi <= 0) throw new InvalidOperationException("您必须先设置屏幕 DPI.");
                return inches * ScreenDpi;
            }

            /// <summary>
            /// 检查两个矩形是否相交
            /// </summary>
            /// <param name="src"></param>
            /// <param name="target"></param>
            /// <returns></returns>
            public static bool CheckIntersect(RectInt src, RectInt target)
            {
                var minX = System.Math.Max(src.x, target.x);
                var minY = System.Math.Max(src.y, target.y);
                var maxX = System.Math.Min(src.x + src.width,  target.x + target.width);
                var maxY = System.Math.Min(src.y + src.height, target.y + target.height);
                return minX < maxX && minY < maxY;
            }

            /// <summary>
            /// 检查两个矩形是否相交
            /// </summary>
            /// <param name="x1"></param>
            /// <param name="y1"></param>
            /// <param name="w1"></param>
            /// <param name="h1"></param>
            /// <param name="x2"></param>
            /// <param name="y2"></param>
            /// <param name="w2"></param>
            /// <param name="h2"></param>
            /// <returns></returns>
            public static bool CheckIntersect(int x1, int y1, int w1, int h1, int x2, int y2, int w2, int h2)
            {
                var minX = System.Math.Max(x1, x2);
                var minY = System.Math.Max(y1, y2);
                var maxX = System.Math.Min(x1 + w1, x2 + w2);
                var maxY = System.Math.Min(y1 + h1, y2 + h2);
                return minX < maxX && minY < maxY;
            }

            /// <summary>
            /// 检查两个矩形是否相交，并返回相交的区域
            /// </summary>
            /// <param name="x1"></param>
            /// <param name="y1"></param>
            /// <param name="w1"></param>
            /// <param name="h1"></param>
            /// <param name="x2"></param>
            /// <param name="y2"></param>
            /// <param name="w2"></param>
            /// <param name="h2"></param>
            /// <param name="rect"></param>
            /// <returns></returns>
            private static bool CheckIntersect(int x1, int y1, int w1, int h1, int x2, int y2, int w2, int h2, out RectInt rect)
            {
                rect = default;
                var minX = System.Math.Max(x1, x2);
                var minY = System.Math.Max(y1, y2);
                var maxX = System.Math.Min(x1 + w1, x2 + w2);
                var maxY = System.Math.Min(y1 + h1, y2 + h2);
                if (minX >= maxX || minY >= maxY) return false;

                rect.x      = minX;
                rect.y      = minY;
                rect.width  = System.Math.Abs(maxX - minX);
                rect.height = System.Math.Abs(maxY - minY);
                return true;
            }

            /// <summary>
            /// 检查两个矩形相交的点
            /// </summary>
            /// <param name="x1">A 坐标X</param>
            /// <param name="y1">A 坐标Y</param>
            /// <param name="w1">A 宽度</param>
            /// <param name="h1">A 高度</param>
            /// <param name="x2">B 坐标X</param>
            /// <param name="y2">B 坐标Y</param>
            /// <param name="w2">B 宽度</param>
            /// <param name="h2">B 高度</param>
            /// <param name="intersectPoints">交叉点列表</param>
            /// <returns>返回是否相交</returns>
            public static bool CheckIntersectPoints(int x1, int y1, int w1, int h1, int x2, int y2, int w2, int h2, int[] intersectPoints)
            {
                var dPt = new Vector2Int();
                if (false == CheckIntersect(x1, y1, w1, h1, x2, y2, w2, h2, out var rectInt)) return false;

                for (var i = 0; i < w1; i++)
                {
                    for (var n = 0; n < h1; n++)
                    {
                        if (intersectPoints[i * h1 + n] != 1) continue;
                        dPt.x = x1 + i;
                        dPt.y = y1 + n;

                        if (!rectInt.Contains(dPt)) continue;
                        intersectPoints[i * h1 + n] = 0;
                    }
                }

                return true;
            }

            /// <summary>
            /// 勾股定理
            /// </summary>
            /// <param name="x">边长x</param>
            /// <param name="y">边长y</param>
            /// <returns>勾股定理结果</returns>
            public static float PythagoreanTheorem(float x, float y) => Mathf.Sqrt(x * x + y * y);


            /// <summary>
            /// 将角度转换为四元数。
            /// </summary>
            /// <param name="angle">角度</param>
            /// <returns>对应的四元数</returns>
            public static Quaternion AngleToQuaternion(int angle)
            {
                return Quaternion.AngleAxis(-angle, Vector3.up) * Quaternion.AngleAxis(90, Vector3.up);
            }

            /// <summary>
            /// 根据源向量和目标向量计算四元数。
            /// </summary>
            /// <param name="source">源向量</param>
            /// <param name="dire">目标向量</param>
            /// <returns>对应的四元数</returns>
            public static Quaternion GetVector3ToQuaternion(Vector3 source, Vector3 dire)
            {
                var nowPos = source;
                if (nowPos == dire) return new Quaternion();

                Vector3 direction = (dire - nowPos).normalized;
                return Quaternion.LookRotation(direction, Vector3.up);
            }

            /// <summary>
            /// 计算二维距离。
            /// </summary>
            /// <param name="v1">第一个三维坐标</param>
            /// <param name="v2">第二个三维坐标</param>
            /// <returns>两点之间的二维距离</returns>
            public static float Distance2D(Vector3 v1, Vector3 v2)
            {
                Vector2 d1 = new Vector2(v1.x, v1.z);
                Vector2 d2 = new Vector2(v2.x, v2.z);
                return Vector2.Distance(d1, d2);
            }

            /// <summary>
            /// 根据角度获取四元数。
            /// </summary>
            /// <param name="angle">角度</param>
            /// <returns>对应的四元数</returns>
            public static Quaternion GetAngleToQuaternion(float angle)
            {
                return Quaternion.AngleAxis(-angle, Vector3.up) * Quaternion.AngleAxis(90, Vector3.up);
            }

            /// <summary>
            /// 计算从一个向量到另一个向量的360度角度。
            /// </summary>
            /// <param name="from">起始向量</param>
            /// <param name="to">目标向量</param>
            /// <returns>360度角度</returns>
            public static float Vector3ToAngle360(Vector3 from, Vector3 to)
            {
                float   angle = Vector3.Angle(from, to);
                Vector3 cross = Vector3.Cross(from, to);
                return cross.y > 0 ? angle : 360 - angle;
            }

            /// <summary>
            /// 求点到直线的距离，采用数学公式Ax+By+C = 0; d = A*p.x + B * p.y + C / sqrt(A^2 + B ^ 2)
            /// </summary>
            /// <param name="startPoint">线的起点</param>
            /// <param name="endPoint">线的终点</param>
            /// <param name="point">点</param>
            /// <returns>点到直线的距离</returns>
            public static float DistanceOfPointToVector(Vector3 startPoint, Vector3 endPoint, Vector3 point)
            {
                Vector2 startVe2 = IgnoreYAxis(startPoint);
                Vector2 endVe2   = IgnoreYAxis(endPoint);

                float a = endVe2.y              - startVe2.y;
                float b = startVe2.x            - endVe2.x;
                float c = endVe2.x * startVe2.y - startVe2.x * endVe2.y;

                float   denominator = Mathf.Sqrt(a * a + b * b);
                Vector2 pointVe2    = IgnoreYAxis(point);

                return Mathf.Abs((a * pointVe2.x + b * pointVe2.y + c) / denominator);
            }

            /// <summary>
            /// 判断射线是否碰撞到球体，如果碰撞到，返回射线起点到碰撞点之间的距离
            /// </summary>
            /// <param name="ray">射线</param>
            /// <param name="center">中心点</param>
            /// <param name="redis">半径</param>
            /// <param name="dist">距离</param>
            /// <returns>是否碰撞</returns>
            public static bool RayCastSphere(Ray ray, Vector3 center, float redis, out float dist)
            {
                dist = 0;
                Vector3 ma       = center - ray.origin;
                float   distance = Vector3.Cross(ma, ray.direction).magnitude / ray.direction.magnitude;
                if (distance < redis)
                {
                    float op = PythagoreanTheorem(Vector3.Distance(center, ray.origin), distance);
                    float rp = PythagoreanTheorem(redis,                                distance);
                    dist = op - rp;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// 去掉三维向量的Y轴，把向量投射到xz平面。
            /// </summary>
            /// <param name="vector3">三维向量</param>
            /// <returns>投影后的二维向量</returns>
            public static Vector2 IgnoreYAxis(Vector3 vector3)
            {
                return new Vector2(vector3.x, vector3.z);
            }

            /// <summary>
            /// 判断目标点是否位于向量的左边
            /// </summary>
            /// <param name="vector3">向量</param>
            /// <param name="originPoint">原点</param>
            /// <param name="targetPoint">目标点</param>
            /// <returns>True if on left, false if on right</returns>
            public static bool PointOnLeftSideOfVector(Vector3 vector3, Vector3 originPoint, Vector3 targetPoint)
            {
                Vector2 originVec2  = IgnoreYAxis(originPoint);
                Vector2 pointVec2   = (IgnoreYAxis(targetPoint) - originVec2).normalized;
                Vector2 vector2     = IgnoreYAxis(vector3);
                float   verticalX   = originVec2.x;
                float   verticalY   = -verticalX * vector2.x / vector2.y;
                Vector2 norVertical = new Vector2(verticalX, verticalY).normalized;
                float   dotValue    = Vector2.Dot(norVertical, pointVec2);

                return dotValue < 0f;
            }
        }
    }
}