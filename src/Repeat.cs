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
        using Microsoft.Xna.Framework;
        using Specification;

        public class RepeatSequence : ExecutableStep
        {
            public int Repeat;
            public int Index;
            public ExecutableStep Sequence;

            public RepeatSequence(Repeat action, BulletMLSpecification spec, float[] Parameters) : base(action, spec, Parameters)
            {
                Repeat = (int)action.Times(Parameters);
                if (action.Action != null)
                {
                    Sequence = new Sequence(action.Action, spec, Parameters);
                }
                else
                {
                    Sequence = new Sequence(action.Reference, spec, Parameters);
                }
            }

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
                ParamList = Parameters;
            }

            protected override void SetBullet(IBullet bullet)
            {
                Sequence.Bullet = Bullet;
            }

            public override SequenceResult Execute(float delta, BulletFactory factory, Vector2 target)
            {
                SequenceResult result = new SequenceResult();

                if (!Done)
                {
                    bool reset = true;
                    while (reset && !Done)
                    {
                        Sequence.LastDirection = LastDirection;
                        Sequence.LastSpeed = LastSpeed;
                        SequenceResult subResult = Sequence.Execute(delta, factory, target);
                        LastDirection = Sequence.LastDirection;
                        LastSpeed = Sequence.LastSpeed;

                        if (result != null)
                        {
                            result.Removed = result.Removed || subResult.Removed;
                            foreach (IBullet a in subResult.Made)
                            {
                                result.Made.Add(a);
                            }
                        }
                        if (Sequence.Done)
                        {
                            Index++;
                            Sequence.Finish();
                            Sequence.Reset();
                            reset = true;
                        }
                        else
                        {
                            reset = false;
                        }
                    };
                }
                return result;
            }
        }
    }
}
