using System;
using System.Collections.Generic;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        [XmlRoot("action")]
        public class Action : TaskNode, BulletMLNode
        {
            [XmlAttribute(AttributeName = "label")]
            public string Label;

            [XmlElement("changeDirection", typeof(ChangeDirection))]
            [XmlElement("changeSpeed", typeof(ChangeSpeed))]
            [XmlElement("accel", typeof(Accelerate))]
            [XmlElement("wait", typeof(Delay))]
            [XmlElement("vanish", typeof(Vanish))]
            [XmlElement("repeat", typeof(Repeat))]
            [XmlElement("action", typeof(Action))]
            [XmlElement("actionRef", typeof(Reference<Action>))]
            [XmlElement("fire", typeof(Fire))]
            [XmlElement("fireRef", typeof(Reference<Fire>))]
            public List<TaskNode> Sequence;
        }
    }

    namespace Implementation
    {
        using Specification;
        using Microsoft.Xna.Framework;

        public class SequenceResult
        {
            public List<IBullet> Made = new List<IBullet>();
            public bool Removed = false;
        }

        public abstract class ExecutableStep : Step
        {
            protected List<Step> Steps;

            public float LastDirection = 0;
            public float LastSpeed = 0;

            public ExecutableStep(TaskNode node, BulletMLSpecification spec, float[] Parameters) : base(node, Parameters)
            {
            }

            abstract public SequenceResult Execute(float delta, BulletFactory factory, Vector2 target);
        }

        /// <summary>
        /// Given an action, iterate through it according to its timeline
        /// </summary>
        public class Sequence : ExecutableStep
        {
            private float[] _parentParameters;
            
            private Step CurrentAction
            {
                get
                {
                    return Steps[Index];
                }
            }

            /// <summary>
            /// Current Action Index (used when waiting)
            /// </summary>
            private int Index = 0;
        
            private BulletMLSpecification _spec;
            private Reference<Action> _reference;

            protected override bool IsDone()
            {
                return Index >= Steps.Count;
            }
        
            public override void Reset()
            {
                Index = 0;
                foreach (Step s in Steps)
                {
                    s.Reset();
                }
            }

            public override void UpdateParameters(float[] Parameters)
            {
                if (_reference != null)
                {
                    _parentParameters = Parameters;
                    ParamList = _reference.GetParams(Parameters);
                } else {
                    ParamList = Parameters;
                }
                foreach (Step s in Steps)
                {
                    s.UpdateParameters(ParamList);
                }
            }

            protected override void SetBullet(IBullet bullet)
            {
                foreach (Step s in Steps)
                {
                    s.Bullet = bullet;
                }
            }

            public Sequence(Reference<Action> reference, BulletMLSpecification spec, float[] parameters)
                : this(spec.NamedActions[reference.Label], spec, reference.GetParams(parameters))
            {
                _reference = reference;
                _parentParameters = parameters;
            }

            public Sequence(Action action, BulletMLSpecification spec, float[] parameters) : base(action, spec, parameters)
            {
                _spec = spec;
                _parentParameters = parameters;
                Steps = new List<Step>();
                foreach (TaskNode node in action.Sequence)
                {
                    Steps.Add(StepFactory.make(node, spec, parameters));
                }
            }
            
            /// <summary>
            /// Steps through this action, returning any new actors that may have been created by it
            /// </summary>
            /// <param name="actor">The bullet that an action is to affect</param>
            /// <param name="timer">Game timer</param>
            /// <param name="target">Position of the target being shot at.  Used when no rotation is set</param>
            /// <returns></returns>
            public override SequenceResult Execute(float delta, BulletFactory factory, Vector2 target)
            {
                SequenceResult result = new SequenceResult();
                
                if (Done)
                {
                    return null;
                }

                if (CurrentAction is ExecutableStep)
                {
                    ExecutableStep e = (ExecutableStep)CurrentAction;
                    e.LastDirection = LastDirection;
                    e.LastSpeed = LastSpeed;
                    SequenceResult subResult = e.Execute(delta, factory, target);
                    LastSpeed = e.LastSpeed;
                    LastDirection = e.LastDirection;

                    if (subResult != null)
                    {
                        result.Removed = result.Removed || subResult.Removed;
                        foreach (IBullet a in subResult.Made)
                        {
                            result.Made.Add(a);
                        }
                    }
                }
                else if (CurrentAction is FireBullet)
                {
                    FireBullet fire = (FireBullet)CurrentAction;
                    IBullet ib = fire.Execute(factory, LastDirection, LastSpeed, target);
                    LastDirection = ib.Rotation;
                    LastSpeed = ib.Speed;
                    result.Made.Add(ib);
                }
                else if (CurrentAction is MutateStep)
                {
                    MutateStep step = (MutateStep)CurrentAction;
                    if (!step.Started)
                    {
                        step.Target = target;
                    }
                    step.Mutate(delta);
                }
                else if (CurrentAction is RemoveSelf)
                {
                    result.Removed = true;
                }
                if (CurrentAction is TimedStep)
                {
                    (CurrentAction as TimedStep).Update(delta);
                }

                if (CurrentAction.Done)
                {
                    CurrentAction.Finish();
                    Index++;
                }
            
                return result;
            }
        }

        public class Parallel : ExecutableStep
        {
            public Parallel(List<Action> Actions, BulletMLSpecification spec, float[] Parameters) : base(null, spec, Parameters)
            {
                Steps = new List<Step>();
                foreach (Action node in Actions)
                {
                    Steps.Add(new Sequence(node, spec, ParamList));
                }
            }

            protected override bool IsDone()
            {
                foreach (Step s in Steps)
                {
                    if (!s.Done)
                    {
                        return false;
                    }
                }
                return true;
            }

            public override void Reset()
            {
                foreach (Step s in Steps)
                {
                    s.Reset();
                }
            }

            protected override void SetBullet(IBullet bullet)
            {
                foreach (Step s in Steps)
                {
                    s.Bullet = bullet;
                }
            }

            public override SequenceResult Execute(float delta, BulletFactory factory, Vector2 target)
            {
                SequenceResult result = new SequenceResult();

                foreach (Sequence s in Steps)
                {
                    s.LastDirection = LastDirection;
                    s.LastSpeed = LastSpeed;
                    SequenceResult subResult = s.Execute(delta, factory, target);
                    LastDirection = s.LastDirection;
                    LastSpeed = s.LastSpeed;

                    if (subResult != null)
                    {
                        result.Removed = result.Removed || subResult.Removed;
                        foreach (IBullet a in subResult.Made)
                        {
                            result.Made.Add(a);
                        }
                    }
                }

                return result;
            }
        }
    }
}
