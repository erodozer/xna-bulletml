﻿using System;
using System.Collections.Generic;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        [XmlRoot("action")]
        public class Action : TaskNode
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
            [XmlElement("actionRef", typeof(ActionRef))]
            [XmlElement("fire", typeof(Fire))]
            [XmlElement("fireRef", typeof(FireRef))]
            public List<TaskNode> Sequence;
        }

        [Serializable()]
        [XmlType("actionRef")]
        public class ActionRef : TaskNode
        {
            [XmlAttribute(AttributeName = "label")]
            [XmlText]
            public string Label;

            [XmlElement("param", typeof(Param))]
            public Param[] Parameters;

            public float[] GetParams(float[] param)
            {
                float[] eval = new float[Parameters.Length];
                for (int i = 0; i < eval.Length; i++)
                {
                    eval[i] = Parameters[i].GetValue(param);
                }
                return eval;
            }
        }
    }

    namespace Implementation
    {
        using Specification;
        using Microsoft.Xna.Framework;

        /// <summary>
        /// Given an action, iterate through it according to its timeline
        /// </summary>
        public class Sequence : Step
        {
            private float[] _parameters;
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
            private ActionRef _reference;

            private float _LastDirection = 0;
            private float _LastSpeed = 0;

            protected override bool IsDone()
            {
                return Index >= Steps.Count;
            }
        
            public void Reset()
            {
                Index = 0;
                foreach (Step s in Steps)
                {
                    if (s is Sequence)
                    {
                        (s as Sequence).Reset();
                    }
                    else if (s is RepeatSequence)
                    {
                        (s as RepeatSequence).Reset();
                    }    
                }
                // refresh all parameters to recalculate on reset
                UpdateParameters(_parameters);
            }

            public override void UpdateParameters(float[] Parameters)
            {
                if (_reference != null)
                {
                    _parameters = _reference.GetParams(_parentParameters);
                } else {
                    _parameters = Parameters;
                }
                foreach (Step s in Steps)
                {
                    s.UpdateParameters(_parameters);
                }
            }

            public Sequence(Specification.ActionRef reference, BulletMLSpecification spec, float[] parameters)
                : this(spec.FindAction(reference.Label), spec, reference.GetParams(parameters))
            {
                _reference = reference;
                _parentParameters = parameters;
            }

            public Sequence(Specification.Action action, BulletMLSpecification spec, float[] parameters)
            {
                _spec = spec;
                Steps = new List<Step>();
                _parameters = parameters;
                foreach (TaskNode node in action.Sequence)
                {
                    if (node is Specification.Action)
                    {
                        Steps.Add(new Sequence(node as Action, spec, parameters));
                    }
                    else if (node is Specification.ActionRef)
                    {
                        Specification.ActionRef r = node as ActionRef;
                        Steps.Add(new Sequence(r, spec, parameters));
                    }
                    else if (node is Specification.Repeat)
                    {
                        Steps.Add(new RepeatSequence(node as Repeat, spec, parameters));
                    }
                    else if (node is Specification.Fire)
                    {
                        Steps.Add(new FireBullet(node as Fire, spec, parameters));
                    }
                    else if (node is Specification.FireRef)
                    {
                        Specification.FireRef r = node as Specification.FireRef;
                        Steps.Add(new FireBullet(r, spec, parameters));
                    }
                    else if (node is Specification.Vanish)
                    {
                        Steps.Add(new RemoveSelf());
                    }
                    else if (node is Specification.ChangeDirection)
                    {
                        Steps.Add(new SetDirectionMutation(node as Specification.ChangeDirection, parameters));
                    }
                    else if (node is Specification.ChangeSpeed)
                    {
                        Steps.Add(new SetDirectionMutation(node as Specification.ChangeDirection, parameters));
                    }
                    else if (node is Specification.Delay)
                    {
                        Steps.Add(new TimedStep(node as Specification.Delay, parameters));
                    }
                }
            }

        
        
            /// <summary>
            /// Steps through this action, returning any new actors that may have been created by it
            /// </summary>
            /// <param name="actor">The bullet that an action is to affect</param>
            /// <param name="timer">Game timer</param>
            /// <param name="target">Position of the target being shot at.  Used when no rotation is set</param>
            /// <returns></returns>
            public Tuple<List<IBullet>, bool> Execute(IBullet actor, float delta, BulletFactory factory, Vector2 target)
            {
                List<IBullet> made = new List<IBullet>();
                bool removed = false;
                if (Done)
                {
                    return null;
                }

                if (CurrentAction is RepeatSequence)
                {
                    RepeatSequence repeat = CurrentAction as RepeatSequence;
                    if (!repeat.Done)
                    {
                        while (!repeat.Sequence.Done && !repeat.Done)
                        {
                            repeat.Sequence._LastDirection = _LastDirection;
                            repeat.Sequence._LastSpeed = _LastSpeed;
                            Tuple<List<IBullet>, bool> result = repeat.Sequence.Execute(actor, delta, factory, target);
                            _LastDirection = repeat.Sequence._LastDirection;
                            _LastSpeed = repeat.Sequence._LastSpeed;

                            if (result != null)
                            {
                                removed = removed || result.Item2;
                                foreach (IBullet a in result.Item1)
                                {
                                    made.Add(a);
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
                    FireBullet fire = CurrentAction as FireBullet;
                    IBullet ib = fire.Execute(actor, factory, _LastDirection, _LastSpeed, target);
                    _LastDirection = ib.Rotation;
                    _LastSpeed = ib.Speed;
                    made.Add(ib);
                }
                else if (CurrentAction is MutateStep<Mutate>)
                {
                    MutateStep<Mutate> step = (CurrentAction as MutateStep<Mutate>);
                    step.Mutate(actor, delta);
                    step.Elapsed += delta;
                }
                else if (CurrentAction is Sequence)
                {
                    Sequence subSequence = CurrentAction as Sequence;
                    Tuple<List<IBullet>, bool> result = subSequence.Execute(actor, delta, factory, target);
                    removed = removed || result.Item2;
                    foreach (IBullet a in result.Item1)
                    {
                        made.Add(a);
                    }
                }
                else if (CurrentAction is RemoveSelf)
                {
                    removed = true;
                }
                if (CurrentAction is TimedStep)
                {
                    (CurrentAction as TimedStep).Update(delta);
                }

                if (CurrentAction.Done)
                {
                    Index++;
                }
            
                return new Tuple<List<IBullet>, bool>(made, removed);
            }
        }

    }
}
