using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorTree;
using JumpKing.PauseMenu;
using JumpKing.Util;

namespace JKMP.Core.UI
{
    public partial class ModalDialog
    {
        private class ModalManager : IBTnode, IDrawable
        {
            private readonly List<IDrawable> drawables;
            private readonly Stack<ModalDialog> dialogs = new();
            private bool unpauseWhenEmpty;

            public ModalManager(List<IDrawable> drawables)
            {
                if (instance != null)
                    throw new InvalidOperationException("ModalManager singleton already exists");
                
                this.drawables = drawables;
                instance = this;
            }

            public override void OnDispose()
            {
                foreach (var dialog in dialogs)
                {
                    dialog.OnDispose();
                }

                dialogs.Clear();
            }

            public void PushModal(ModalDialog dialog)
            {
                if (dialogs.Count == 0 && PauseManager.instance?.IsPaused == false)
                {
                    PauseManager.SetPause(true);
                    unpauseWhenEmpty = true;
                }
                
                dialogs.Push(dialog);
            }
            
            protected override BTresult MyRun(TickData tickData)
            {
                // Check that this drawable is the topmost one
                if (drawables[drawables.Count - 1] != this)
                {
                    drawables.Remove(this);
                    drawables.Add(this);
                }
                
                while (dialogs.Count > 0)
                {
                    var dialog = dialogs.Peek();
                    var result = dialog.Run(tickData);

                    if (result == BTresult.Running)
                        return BTresult.Running;

                    dialogs.Pop().OnDispose();
                }
                
                if (unpauseWhenEmpty)
                {
                    PauseManager.SetPause(false);
                    unpauseWhenEmpty = false;
                }
                
                return BTresult.Success;
            }

            public void Draw()
            {
                if (last_result != BTresult.Running)
                    return;

                // Need to draw in reverse order so that the topmost dialog is drawn last
                foreach (var dialog in dialogs.Reverse())
                {
                    dialog.Draw();
                }
            }
        }
    }
}