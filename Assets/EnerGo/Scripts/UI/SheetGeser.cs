using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnerGo.UI
{
    // Lets a bottom sheet be swiped down to close. Buttons and sliders keep their own touches; a drag starting anywhere
    // else moves the sheet with the finger, then it either closes (the caller slides it out) or springs back.
    public sealed class SheetGeser
    {
        private readonly VisualElement sheet;
        private readonly Func<bool> close;
        private int pointer = -1;
        private bool dragging;
        private float startY, lastY, lastT, velocity;

        public SheetGeser(VisualElement sheet, Func<bool> close)
        {
            this.sheet = sheet;
            this.close = close;
            var handle = new VisualElement { pickingMode = PickingMode.Ignore };
            handle.AddToClassList("eg-sheet__pegangan");
            sheet.Add(handle);
            sheet.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
            sheet.RegisterCallback<PointerMoveEvent>(e => { if (e.pointerId == pointer) Move(e.position.y, Time.unscaledTime, e.pointerId); });
            sheet.RegisterCallback<PointerUpEvent>(e => { if (e.pointerId == pointer) Up(); });
            sheet.RegisterCallback<PointerCancelEvent>(e => { if (e.pointerId == pointer) Up(); });
        }

        private void OnDown(PointerDownEvent e)
        {
            for (var v = e.target as VisualElement; v != null && v != sheet; v = v.parent)
                if (v is Button || v.ClassListContains("eg-slider")) return;
            Down(e.position.y, Time.unscaledTime, e.pointerId);
        }

        // Public so editor checks can drive a swipe without a touch screen.
        public void Down(float y, float time, int id)
        {
            pointer = id;
            dragging = false;
            startY = lastY = y;
            lastT = time;
            velocity = 0f;
        }

        public void Move(float y, float time, int id)
        {
            float drag = y - startY;
            if (!dragging)
            {
                if (!SheetSwipe.StartsDrag(drag)) return;
                dragging = true;
                if (id >= 0) sheet.CapturePointer(id);
                sheet.style.transitionDuration = new List<TimeValue> { new TimeValue(0f) }; // follow the finger exactly
            }
            float dt = time - lastT;
            if (dt > 0f) velocity = (y - lastY) / dt;
            lastY = y;
            lastT = time;
            sheet.style.translate = new Translate(0f, SheetSwipe.Offset(drag));
        }

        public void Up()
        {
            int id = pointer;
            pointer = -1;
            if (id >= 0 && sheet.HasPointerCapture(id)) sheet.ReleasePointer(id);
            if (!dragging) return;
            dragging = false;
            bool shut = SheetSwipe.ShouldClose(lastY - startY, sheet.layout.height, velocity) && close();
            // Back to the class-driven position: slides out when just closed, springs back otherwise.
            sheet.style.transitionDuration = StyleKeyword.Null;
            sheet.style.translate = StyleKeyword.Null;
            if (!shut) sheet.AddToClassList("is-terbuka");
        }
    }
}
