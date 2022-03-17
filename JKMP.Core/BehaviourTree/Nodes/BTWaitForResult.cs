using BehaviorTree;

namespace JKMP.Core.BehaviourTree.Nodes
{
    /// <summary>
    /// Waits for any result from the child node and returns its result.
    /// </summary>
    public class BTWaitForResult : IBTdecorator
    {
        private readonly bool runChild;

        /// <summary>
        /// Creates a new instance of the <see cref="BTWaitForResult"/> class.
        /// </summary>
        /// <param name="child">The node to wait on for a result.</param>
        /// <param name="runChild">If true the child will be run by this node. This should be false if the child is already run by something else.</param>
        public BTWaitForResult(IBTnode child, bool runChild = true) : base(child)
        {
            this.runChild = runChild;
        }

        /// <inheritdoc />
        protected override BTresult MyRun(TickData tickData)
        {
            BTresult result;
                
            if (runChild)
            {
                result = Child.Run(tickData);
            }
            else
            {
                result = Child.last_result;
            }

            if (result is BTresult.Success or BTresult.Failure)
            {
                return result;
            }

            return BTresult.Running;
        }
    }
}