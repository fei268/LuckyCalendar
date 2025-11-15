using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;

public class SwipeView : PointerManipulator
{
    private VisualElement container;

    private float pointerDownX;
    private float containerStartX;

    private int pageIndex = 0;
    private int pageCount;
    private float pageWidth;

    private bool dragging = false;
    private const float swipeThreshold = 0.20f; // 滑动20%页面才翻页
    private const int animDuration = 220;

    public SwipeView(VisualElement target)
    {
        this.target = target;
        container = target;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        pageCount = container.childCount;
        pageWidth = container.resolvedStyle.width;

        LayoutPages();
        GoToPage(pageIndex, false);
    }

    void LayoutPages()
    {
        for (int i = 0; i < pageCount; i++)
        {
            container[i].style.left = pageWidth * i;
            container[i].style.position = Position.Absolute;
        }
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        dragging = true;
        pointerDownX = evt.position.x;
        containerStartX = container.transform.position.x;
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!dragging) return;

        float dx = evt.position.x - pointerDownX;

        // 限制滑动速度避免跨多页
        dx = Mathf.Clamp(dx, -pageWidth, pageWidth);

        float newX = containerStartX + dx;
        container.transform.position = new Vector3(newX, 0, 0);
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (!dragging) return;
        dragging = false;

        float dx = evt.position.x - pointerDownX;
        float ratio = Mathf.Abs(dx) / pageWidth;

        if (ratio >= swipeThreshold)
        {
            if (dx < 0) NextPage();
            else PrevPage();
        }
        else
        {
            // 回弹
            GoToPage(pageIndex, true);
        }
    }

    void NextPage()
    {
        pageIndex = (pageIndex + 1) % pageCount;
        AnimateToPage(pageIndex);
    }

    void PrevPage()
    {
        pageIndex = (pageIndex - 1 + pageCount) % pageCount;
        AnimateToPage(pageIndex);
    }

    void GoToPage(int index, bool animated)
    {
        float target = -index * pageWidth;

        if (animated)
            AnimateToX(target);
        else
            container.transform.position = new Vector3(target, 0, 0);
    }

    void AnimateToPage(int index)
    {
        float target = -index * pageWidth;
        AnimateToX(target);

        // 重排无限循环（动画结束后执行）
        container.schedule.Execute(() =>
        {
            RearrangeInfiniteContainer(index);
        }).StartingIn(animDuration + 20);
    }

    // 这里不使用 Easing，直接用 experimental.animation 的简化重载
    void AnimateToX(float targetX)
    {
        // 使用 UI Toolkit 的 experimental animation (目标位置 + 时长)
        container.experimental.animation.Position(new Vector3(targetX, 0, 0), animDuration);
    }

    void RearrangeInfiniteContainer(int index)
    {
        // 把所有页面重排到正确位置（保证不会跳页）
        LayoutPages();

        // 重置容器位置
        container.transform.position = new Vector3(-index * pageWidth, 0, 0);
    }

    public void JumpToPage(int index, bool animated = true)
    {
        index = Mathf.Clamp(index, 0, pageCount - 1);
        pageIndex = index;
        GoToPage(pageIndex, animated);
    }
}
