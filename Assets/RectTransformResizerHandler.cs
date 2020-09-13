using UnityEngine;
using System;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(RectTransformResizerHandler))]
class RectTransformResizerHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("配置"))
        {
            var scr = target as RectTransformResizerHandler;
            serializedObject.Update();
            Undo.RecordObject(scr.gameObject, "RectTransformResizerHandlerRedo");
            scr.Action();
        }
        GUILayout.Label(@"此脚本用于把 RectTransform 尺寸等比放大：
1. 已经考虑到自身及父节点的 Layout 组件。
2. 必须将 Game 视窗和CanvasScale 分辨率配置到你想要的
3. 倍率是整型，1920 → 3840 → 7680 （2k→4k→8k）
4. 游戏对象以 ~ 号结尾的过滤掉不会被处理，可以使用此方式跳过处理
");
    }
}
#endif

public class RectTransformResizerHandler : MonoBehaviour
{
    [SerializeField] int factor = 2;
    [SerializeField] bool needSetDirty = true; //RectTransform 会自动dirty 但是其他组件不会（具体哪些不会，不知道）

    public void Action()
    {
        UpdateScale(null);
        DestroyImmediate(this);
    }
    private void UpdateScale(Transform t)
    {
        if (!t) t = transform;
        HandleThisRectTransform(t);
        if (t.childCount > 0)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform tr = t.GetChild(i);
                if (!tr.name.EndsWith("~"))
                {
                    UpdateScale(tr);
                }
            }
        }
    }

    private void HandleThisRectTransform(Transform item)
    {
        Action HandleRectTransformPose = () =>
        {
            var rt = item as RectTransform;
            // 1 . 先确认自身 Size 和 Pose 是不是身不由己。
            var layoutGroup = item.parent.GetComponent<LayoutGroup>();
            var lele = item.GetComponent<LayoutElement>();
            if (layoutGroup && layoutGroup.enabled && (!lele || (lele && lele.enabled && !lele.ignoreLayout)))
            {
                if (layoutGroup is GridLayoutGroup) return;
                var group = layoutGroup as HorizontalOrVerticalLayoutGroup;
                if (!group.childControlHeight)
                {
                    var ds = rt.sizeDelta;
                    ds[1] *= factor;
                    rt.sizeDelta = ds;
                }
                if (!group.childControlWidth)
                {
                    var ds = rt.sizeDelta;
                    ds[0] *= factor;
                    rt.sizeDelta = ds;
                }
            }
            else
            {
                //2. 针对左侧的 PosX+Width 与 left + right 的分情况赋值
                if (rt.anchorMax.x == rt.anchorMin.x)   //只要他俩相等就需要对 PosX 和 Width 赋值
                {
                    var pos = rt.anchoredPosition;
                    pos[0] *= factor;
                    rt.anchoredPosition = pos;
                    var ds = rt.sizeDelta;
                    ds[0] *= factor;
                    rt.sizeDelta = ds;
                }
                else //不相等的话，就对left 和 right 赋值
                {
                    var max = rt.offsetMax;
                    var min = rt.offsetMin;
                    max.x *= factor;
                    min.x *= factor;
                    rt.offsetMin = min;
                    rt.offsetMax = max;
                }
                //3. 针对左侧的 PosY+Height  与 top + bottom 的分情况赋值，规律同上
                if (rt.anchorMax.y == rt.anchorMin.y) 
                {
                    var pos = rt.anchoredPosition;
                    pos[1] *= factor;
                    rt.anchoredPosition = pos;
                    var ds = rt.sizeDelta;
                    ds[1] *= factor;
                    rt.sizeDelta = ds;
                }
                else
                {
                    var max = rt.offsetMax;
                    var min = rt.offsetMin;
                    max.y *= factor;
                    min.y *= factor;
                    rt.offsetMin = min;
                    rt.offsetMax = max;
                }
            }
            rt.ForceUpdateRectTransforms();
#if UNITY_EDITOR
            if (needSetDirty) UnityEditor.EditorUtility.SetDirty(rt);
#endif
        };

        //0. 处理 LayoutElement
        var ele = item.GetComponent<LayoutElement>();
        if (ele)
        {
            if (!ele.ignoreLayout)
            {
                ele.minHeight *= factor;
                ele.minWidth *= factor;
                ele.preferredHeight *= factor;
                ele.preferredWidth *= factor;
            }
        }

        //1. 处理 RectTransform size 和 pose
        HandleRectTransformPose();
        //2. 处理LayoutGroup
        LayoutGroup layout = item.GetComponent<LayoutGroup>();
        if (layout)
        {
            if (layout is GridLayoutGroup)
            {
                var grid = layout as GridLayoutGroup;
                if (grid)
                {
                    grid.padding.top *= factor;
                    grid.padding.bottom *= factor;
                    grid.padding.left *= factor;
                    grid.padding.right *= factor;
                    grid.cellSize *= factor;
                    grid.spacing *= factor;
                }
            }
            else
            {
                var horizontalOrVertical = layout as HorizontalOrVerticalLayoutGroup;
                if (horizontalOrVertical)
                {
                    horizontalOrVertical.padding.top *= factor;
                    horizontalOrVertical.padding.bottom *= factor;
                    horizontalOrVertical.padding.left *= factor;
                    horizontalOrVertical.padding.right *= factor;
                    horizontalOrVertical.spacing *= factor;
                }
            }
#if UNITY_EDITOR
            if (needSetDirty) UnityEditor.EditorUtility.SetDirty(layout);
#endif
        }
        //3. 处理 Text 组件  
        Text text = item.GetComponent<Text>();
        if (text)
        {
            text.fontSize *= factor;
            if (text.resizeTextForBestFit)
            {
                text.resizeTextMaxSize *= factor;
                text.resizeTextMinSize *= factor;
            }
#if UNITY_EDITOR
            if (needSetDirty) UnityEditor.EditorUtility.SetDirty(text);
#endif
        }
    }
}
