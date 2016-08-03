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

        /// <summary>
        /// Given an action, iterate through it according to its timeline
        /// </summary>
        public class Sequence : Step
        {
            private float[] _parentParameters;
            private List<Step> Steps;
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

            private float _LastDirection = 0;
            private float _LastSpeed = 0;

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

            public Sequence(Reference<Action> reference, BulletMLSpecification spec, float[] parameters)
                : this(spec.NamedActions[reference.Label], spec, reference.GetParams(parameters))
            {
                _reference = reference;
                _parentParameters = parameters;
            }

            public Sequence(Action action, BulletMLSpecification spec, float[] parameters) : base(action, parameters)
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
            public SequenceResult Execute(IBullet actor, float delta, BulletFactory factory, Vector2 target)
            {
                SequenceResult result = new SequenceResult();
                
                if (Done)
                {
                    return null;
                }

                if (CurrentAction is RepeatSequence)
                {
                    RepeatSequence repeat = (RepeatSequence)CurrentAction;
                    if (!repeat.Done)
                    {
                        while (!repeat.Sequence.Done && !repeat.Done)
                        {
                            repeat.Sequence._LastDirection = _LastDirection;
                            repeat.Sequence._LastSpeed = _LastSpeed;
                            SequenceResult subResult = repeat.Sequence.Execute(actor, delta, factory, target);
                            _LastDirection = repeat.Sequence._LastDirection;
                            _LastSpeed = repeat.Sequence._LastSpeed;

                            if (result != null)
                            {
                                result.Removed = result.Removed || subResult.Removed;
                                foreach (IBullet a in subResult.Made)
                                {
                                    result.Made.Add(a);
                                }
                            }
                            if (repeat.Sequence.Done)
                            {
                                repeat.Index++;
                                repeat.Sequence.Reset();
                            }
                        };
                    }
                }
                else if (CurrentAction is FireBullet)
                {
                    FireBullet fire = (FireBullet)CurrentAction;
                    IBullet ib = fire.Execute(actor, factory, _LastDirection, _LastSpeed, target);
                    _LastDirection = ib.Rotation;
                    _LastSpeed = ib.Speed;
                    result.Made.Add(ib);
                }
                else if (CurrentAction is MutateStep<Mutate>)
                {
                    MutateStep<Mutate> step = (MutateStep<Mutate>)CurrentAction;
                    step.Mutate(actor, delta);
                    step.Elapsed += delta;
                }
                else if (CurrentAction is Sequence)
                {
                    Sequence subSequence = (Sequence)CurrentAction;
                    SequenceResult subResult = subSequence.Execute(actor, delta, factory, target);
                    if (subResult != null)
                    {
                        result.Removed = result.Removed || result.Removed;
                        foreach (IBullet a in subResult.Made)
                        {
                            result.Made.Add(a);
                        }
                    }
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
                    Index++;
                }
            
                return result;
            }
        }

    }
}
