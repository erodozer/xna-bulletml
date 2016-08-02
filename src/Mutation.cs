using System;
using Microsoft.Xna.Framework;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        public class Mutate : Tweenable
        {
            [XmlElement(ElementName = "term", Type = typeof(Term))]
            public Term _term;

            override public float Frames(float[] Parameters)
            {
                return _term.Frames(Parameters);
            }
        }

        [Serializable()]
        [XmlType("changeDirecton")]
        public class ChangeDirection : Mutate
        {
            [XmlElement(ElementName = "direction")]
            public Direction Direction;
        }

        [Serializable()]
        [XmlType("changeSpeed")]
        public class ChangeSpeed : Mutate
        {
            [XmlElement(ElementName = "speed")]
            public Speed Speed;
        }

        [Serializable()]
        [XmlType("accel")]
        public class Accelerate : Mutate
        {
            [Serializable()]
            [XmlType("horizontal")]
            public class Horizontal : ValueNode
            {
                /// <summary>
                /// If the type is "relative", the acceleration is relative to the acceleration of this bullet. 
                /// If the type is "sequence", the acceleration is changing successively.
                /// </summary>
                [XmlAttribute(AttributeName = "type")]
                public string Type;
                [XmlText]
                public string _expression;

                public float Rate(float[] parameters)
                {
                    return Evaluator.Calculate(_expression, parameters);
                }
            }

            [Serializable()]
            [XmlType("vertical")]
            public class Vertical : ValueNode
            {
                /// <summary>
                /// If the type is "relative", the acceleration is relative to the acceleration of this bullet. 
                /// If the type is "sequence", the acceleration is changing successively.
                /// </summary>
                [XmlAttribute(AttributeName = "type")]
                public string Type;
                [XmlText]
                public string _expression;

                public float Rate(float[] parameters)
                {
                    return Evaluator.Calculate(_expression, parameters);
                }
            }

            [XmlElement(typeof(Horizontal))]
            public Horizontal X;
            [XmlElement(typeof(Vertical))]
            public Vertical Y;
        }
    }

    namespace Implementation
    {
        using Specification;

        public abstract class MutateStep<T> : TimedStep where T : Mutate
        {
            new public T Action;

            public MutateStep(T action, float[] Parameters) : base(action, Parameters)
            {
                Action = action;
            }

            abstract public void Mutate(IBullet actor, float delta);
        }

        public class SetDirectionMutation : MutateStep<ChangeDirection>
        {
            public SetDirectionMutation(ChangeDirection action, float[] Parameters) : base(action, Parameters) { }

            public override void Mutate(IBullet actor, float delta)
            {
                float rotate = Action.Direction.Angle(_parameters);
                if (Term > 0 && !Done)
                {
                    actor.TweenRotate = MathHelper.Lerp(actor.Rotation, rotate, 1 - Term / Elapsed);
                }
                else
                {
                    actor.Rotation = rotate;
                }
            }
        }

        public class SetSpeedMutation : MutateStep<ChangeSpeed>
        {
            public SetSpeedMutation(ChangeSpeed action, float[] Parameters) : base(action, Parameters) { }

            public override void Mutate(IBullet actor, float delta)
            {
                if (Term > 0 && !Done)
                {
                    // TODO Stretch Horizontal and Vertical into a total Speed
                    float now = actor.Velocity.Length();
                    float to = Action.Speed.Rate(_parameters);
                    float speed = MathHelper.Lerp(now, to, Elapsed / Term);
                    Vector2 mut = Vector2.UnitX;
                    mut *= speed;
                    mut = VectorHelper.AngleDeg(mut, actor.TweenRotate);
                    actor.TweenVelocity = mut;
                }
                else
                {
                    float speed = Action.Speed.Rate(_parameters);
                    actor.Speed = speed;
                }
            }
        }

        public class AccelerationMutation : MutateStep<Accelerate>
        {
            public AccelerationMutation(Accelerate action, float[] Parameters) : base(action, Parameters) { }

            public override void Mutate(IBullet actor, float delta)
            {
                if (Term > 0 && !Done)
                {
                    actor.TweenVelocity.X += Action.X.Rate(_parameters) * (delta / Term);
                    actor.TweenVelocity.Y += Action.Y.Rate(_parameters) * (delta / Term);
                    actor.TweenRotate = VectorHelper.AngleDeg(actor.TweenVelocity);
                }
                else
                {
                    actor.Horizontal += Action.X.Rate(_parameters);
                    actor.Vertical += Action.Y.Rate(_parameters);
                }
            }
        }

    }
}
