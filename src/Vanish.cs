using System;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        [Serializable()]
        [XmlType("vanish")]
        public class Vanish : TaskNode
        {
        }
    }

    namespace Implementation
    {
        public class RemoveSelf : Step
        {
            public override void UpdateParameters(float[] Parameters)
            {
                // Do Nothing
            }

            protected override bool IsDone()
            {
                return true;
            }
        }
    }
}
