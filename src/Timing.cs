using System;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        public abstract class Tweenable : TaskNode
        {
            abstract public float Frames(float[] Parameters);
        }

        [Serializable()]
        [XmlType("wait")]
        public class Delay : Tweenable
        {
            [XmlText]
            public string _expression;
            override public float Frames(float[] parameters)
            {
                return Evaluator.Calculate(_expression, parameters);
            }
        }
    }

    namespace Implementation {
        using Specification;

        public class TimedStep : Step
        {
            public float Term = 0;
            public float Elapsed = 0;

            public TimedStep(Tweenable action, float[] Parameters) : base(action, Parameters)
            {
                Term = (Node as Tweenable).Frames(ParamList) / 60f;
            }

            public override void UpdateParameters(float[] Parameters)
            {
                ParamList = Parameters;
                Term = (Node as Tweenable).Frames(ParamList) / 60f;
            }

            protected override bool IsDone()
            {
                return Elapsed > Term;
            }

            public void Update(float delta)
            {
                Elapsed += delta;
            }

            public override void Reset()
            {
                Elapsed = 0;
            }
        }
    }
}
