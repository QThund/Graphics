using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class Ceiling : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Ceiling"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Ceil(inputExpression[0]) };
        }
    }
}
