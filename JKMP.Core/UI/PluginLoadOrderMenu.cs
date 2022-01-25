using BehaviorTree;
using JumpKing.Util;

namespace JKMP.Core.UI
{
    internal class PluginLoadOrderMenu : IBTnode, IDrawable
    {
        protected override BTresult MyRun(TickData tickData)
        {
            return BTresult.Failure;
        }

        public void Draw()
        {
            
        }
    }
}