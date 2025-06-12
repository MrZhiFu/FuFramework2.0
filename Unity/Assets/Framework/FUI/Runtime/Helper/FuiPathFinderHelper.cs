using System;
using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    public static class GObjectExtensions
    {
        /// <summary>
        /// 获取UI路径
        /// </summary>
        /// <param name="self">GObject对象</param>
        /// <returns>UI路径</returns>
        public static string GetUIPath(this GObject self) => FuiPathFinderHelper.GetUIPath(self);
    }

    /// <summary>
    /// FGUI 路径帮助类
    /// </summary>
    
    public static class FuiPathFinderHelper
    {
        /// <summary>
        /// 根据UI对象获取UI路径,
        /// </summary>
        /// <param name="targetGObj">目标UI对象</param>
        /// <returns>UI所在路径</returns>
        public static string GetUIPath(GObject targetGObj)
        {
            var resultList = new List<string>();
            SearchUIParent(targetGObj, resultList);
            resultList.Reverse();
            return string.Join("/", resultList);
        }

        /// <summary>
        /// 搜索目标UI对象的所有父节点
        /// </summary>
        /// <param name="target">目标UI对象</param>
        /// <param name="resultList">结果列表</param>
        private static void SearchUIParent(GObject target, List<string> resultList)
        {
            while (true)
            {
                if (target.parent != null)
                {
                    resultList.Add(target.name);
                    target = target.parent;
                    continue;
                }

                resultList.Add(target.name);
                break;
            }
        }

        /// <summary>
        /// 根据路径获取目标UI对象
        /// 如：传入路径：/GRoot/UISynthesisScene/ContentBox/ListSelect/1990197248/icon
        /// 则返回的icon的UI对象
        /// </summary>
        /// <param name="path">UI路径</param>
        /// <returns>UI对象</returns>
        public static GObject GetUIByPath(string path)
        {
            var pathArr = path.Split('/');
            var pathQueue = new Queue<string>();
            foreach (var item in pathArr)
            {
                if (item == "GRoot") continue;
                pathQueue.Enqueue(item);
            }

            try
            {
                return SearchUIChild(GRoot.inst, pathQueue);
            }
            catch (Exception exception)
            {
                Log.Error($"路径错误 : 找不到指定路径下 {path} 的UI对象, error : " + exception);
            }

            return null;
        }

        /// <summary>
        /// 搜索指定起始节点下且路径指定的子节点。
        /// 如果传入的路径中有$符号,则认为是用索引查找，如$1代表父节点的第一个子节点
        /// </summary>
        /// <param name="start">起始节点</param>
        /// <param name="pathQueue">路径</param>
        /// <returns></returns>
        private static GObject SearchUIChild(GComponent start, Queue<string> pathQueue)
        {
            while (true)
            {
                if (pathQueue.Count <= 0) return start;

                var name = pathQueue.Dequeue();
                GObject child;

                // 如果传入的路径中有$符号,则认为是用索引查找，如$1代表父节点的第一个子节点
                if (name[0] == '$')
                {
                    child = start.GetChild(name);
                    if (child == null)
                    {
                        var idxStr = name.Substring(1);
                        var idx = int.Parse(idxStr);
                        if (idx < 0 || idx >= start.numChildren) throw new Exception("路径错误");
                        child = start.GetChildAt(idx);
                    }
                }
                else
                {
                    child = start.GetChild(name);
                }

                if (child == null) throw new Exception("路径错误");
                if (pathQueue.Count <= 0) return child;
                if (child is not GComponent gObject) throw new Exception("路径错误");

                start = gObject;
            }
        }

        /// <summary>
        /// 路径是否包含指定目标UI对象
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="gObject">目标UI对象</param>
        /// <returns></returns>
        public static bool IsIncludeInPath(string path, GObject gObject)
        {
            if ("all".ToLower() == path) return false;

            var nameList = new List<string>();
            foreach (var v in path.Split('/'))
            {
                if (v == "GRoot") continue;
                nameList.Add(v);
            }

            var current = gObject;
            var gObjectList = new List<GObject> { current, };
            while (current.parent != null && current.parent.name != "GRoot")
            {
                current = current.parent;
                gObjectList.Add(current);
            }

            // 反转链表
            gObjectList.Reverse();

            // 路径长度小于,肯定是不对的
            if (gObjectList.Count < nameList.Count) return false;

            for (var i = 0; i < nameList.Count; i++)
            {
                if (gObjectList[i].name == nameList[i]) continue;
                
                // 如果名称以'$'开头，则尝试将其解析为索引，并检查当前对象在其父级中的索引是否匹配
                if (nameList[i][0] == '$')
                {
                    var idxStr = nameList[i].Substring(1);
                    var idx = int.Parse(idxStr);
                    if (gObjectList[i].parent.GetChildIndex(gObjectList[i]) == idx) continue;
                    return false;
                }

                return false;
            }

            return true;
        }
    }
}