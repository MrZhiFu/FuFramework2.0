using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 数学帮助类
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// 检查两个矩形是否相交
        /// </summary>
        /// <param name="src"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool CheckIntersect(RectInt src, RectInt target)
        {
            var minX = Math.Max(src.x, target.x);
            var minY = Math.Max(src.y, target.y);
            var maxX = Math.Min(src.x + src.width,  target.x + target.width);
            var maxY = Math.Min(src.y + src.height, target.y + target.height);
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
            var minX = Math.Max(x1, x2);
            var minY = Math.Max(y1, y2);
            var maxX = Math.Min(x1 + w1, x2 + w2);
            var maxY = Math.Min(y1 + h1, y2 + h2);
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
            var minX = Math.Max(x1, x2);
            var minY = Math.Max(y1, y2);
            var maxX = Math.Min(x1 + w1, x2 + w2);
            var maxY = Math.Min(y1 + h1, y2 + h2);
            if (minX >= maxX || minY >= maxY) return false;

            rect.x      = minX;
            rect.y      = minY;
            rect.width  = Math.Abs(maxX - minX);
            rect.height = Math.Abs(maxY - minY);
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
    }
}