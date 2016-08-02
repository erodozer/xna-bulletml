using System;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        [Serializable()]
        [XmlType("repeat")]
        public class Repeat : TaskNode
        {
            public const int INFINITE = -1;

            [XmlElement("action", typeof(Action))]
            public Action Action;
            [XmlElement("actionRef", typeof(ActionRef))]
            public ActionRef Reference;
            [XmlElement("times")]
            public string _expression;

            public float Times(float[] parameters)
            {
                return Evaluator.Calculate(_expression, parameters);
            }
        }
    }

    namespace Implementation
    {
        using Specification;

        public class RepeatSequence : Step
        {
            Specification.Repeat _action;
            public int Repeat;
            public int Index;
            public Sequence Sequence;

            public void Reset()
            {
                Index = 0;
            }

            protected override bool IsDone()
            {
                return Index > Repeat;
            }

            public override void UpdateParameters(float[] Parameters)
            {
                Repeat = (int)_action.Times(Parameters);
                Sequence.UpdateParameters(Parameters);
            }

            public RepeatSequence(Specification.Repeat action, BulletMLSpecification spec, float[] Parameters)
            {
                _action = action;
                Repeat = (int)action.Times(Parameters);
                Sequence = new Sequence(action.Action ?? spec.FindAction(action.Reference.Label), spec, Parameters);
            }
        }
    }
}
