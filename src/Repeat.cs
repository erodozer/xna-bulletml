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
            [XmlElement("actionRef", typeof(Reference<Action>))]
            public Reference<Action> Reference;
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
            public int Repeat;
            public int Index;
            public Sequence Sequence;

            public override void Reset()
            {
                Index = 0;
            }

            protected override bool IsDone()
            {
                return Index > Repeat;
            }

            public override void UpdateParameters(float[] Parameters)
            {
                Repeat = (int)(Node as Repeat).Times(Parameters);
                Sequence.UpdateParameters(Parameters);
            }

            public RepeatSequence(Repeat action, BulletMLSpecification spec, float[] Parameters) : base(action, Parameters)
            {
                Repeat = (int)action.Times(Parameters);
                Sequence = new Sequence(action.Action ?? spec.NamedActions[action.Reference.Label] as Action, spec, Parameters);
            }
        }
    }
}
