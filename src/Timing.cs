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
        public class TimedStep : Step
        {
            public Specification.Tweenable Action;
            protected float[] _parameters;
            public float Term = 0;
            public float Elapsed = 0;

            public TimedStep(Specification.Tweenable action, float[] Parameters)
            {
                Action = action;
                UpdateParameters(Parameters);
            }

            public override void UpdateParameters(float[] Parameters)
            {
                _parameters = Parameters;
                Term = Action.Frames(Parameters) / 60f;
            }

            protected override bool IsDone()
            {
                return Elapsed >= Term;
            }

            public void Update(float delta)
            {
                Elapsed += delta;
            }
        }
    }
}
