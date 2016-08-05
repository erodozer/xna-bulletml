using Microsoft.Xna.Framework;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System;
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

        public abstract class MutateStep : TimedStep
        {
            public bool Started;
            public Vector2 Target;

            public MutateStep(Mutate action, float[] Parameters) : base(action, Parameters){}

            abstract public void Mutate(float delta);
        }

        public class SetDirectionMutation : MutateStep
        {
            float Rotate;

            public SetDirectionMutation(ChangeDirection action, float[] Parameters) : base(action, Parameters) {
                Rotate = ((ChangeDirection)Node).Direction.Angle(ParamList);
            }

            public override void Reset()
            {
                base.Reset();
                Started = false;
            }

            public override void UpdateParameters(float[] Parameters)
            {
                base.UpdateParameters(Parameters);

                Rotate = ((ChangeDirection)Node).Direction.Angle(ParamList);
            }

            public override void Mutate(float delta)
            {
                Started = true;
                ChangeDirection node = (ChangeDirection)Node;
                float rotate = 0;
                if (node.Direction.Type == Direction.AIM)
                {
                    rotate = VectorHelper.AngleBetweenDeg(Bullet.Position, Target) + Rotate;
                }
                else if (node.Direction.Type == Direction.RELATIVE)
                {
                    rotate = Bullet.Rotation + Rotate;
                }
                else if (node.Direction.Type == Direction.ABSOLUTE)
                {
                    rotate = Rotate;
                }
                
                if (Term > 0 && !Done)
                {
                    Bullet.TweenRotate = MathHelper.Lerp(Bullet.Rotation, rotate, 1 - Elapsed / Term);
                }
                else
                {
                    Bullet.Rotation = rotate;
                }
            }

            public override void Finish()
            {
                if (Term > 0)
                {
                    ChangeDirection node = (ChangeDirection)Node;
                    float rotate = 0;
                    if (node.Direction.Type == Direction.AIM)
                    {
                        rotate = VectorHelper.AngleBetweenDeg(Bullet.Position, Target) + Rotate;
                    }
                    else if (node.Direction.Type == Direction.RELATIVE)
                    {
                        rotate = Bullet.Rotation + Rotate;
                    }
                    else if (node.Direction.Type == Direction.ABSOLUTE)
                    {
                        rotate = Rotate;
                    }
                    Bullet.Rotation = rotate;
                }
            }
        }

        public class SetSpeedMutation : MutateStep
        {
            float Speed;

            public SetSpeedMutation(ChangeSpeed action, float[] Parameters) : base(action, Parameters) {
                Speed = ((ChangeSpeed)Node).Speed.Rate(ParamList);
            }

            public override void UpdateParameters(float[] Parameters)
            {
                base.UpdateParameters(Parameters);
                Speed = ((ChangeSpeed)Node).Speed.Rate(ParamList);
            }

            public override void Mutate(float delta)
            {
                float to = Speed;
                if (Term > 0 && !Done)
                {
                    // TODO Stretch Horizontal and Vertical into a total Speed
                    float now = Bullet.Velocity.Length();
                    float speed = MathHelper.Lerp(now, to, Elapsed / Term);
                    Bullet.TweenSpeed = speed;
                }
                else
                {
                    Bullet.Speed = to;
                }
            }

            public override void Finish()
            {
                if (Term > 0)
                {
                    Bullet.Speed = Speed;
                }
            }
        }

        public class AccelerationMutation : MutateStep
        {
            public AccelerationMutation(Accelerate action, float[] Parameters) : base(action, Parameters) { }

            public override void Mutate(float delta)
            {
                if (Term > 0 && !Done)
                {
                    Bullet.TweenVelocity.X += (Node as Accelerate).X.Rate(ParamList) * (delta / Term);
                    Bullet.TweenVelocity.Y += (Node as Accelerate).Y.Rate(ParamList) * (delta / Term);
                    Bullet.TweenRotate = VectorHelper.AngleDeg(Bullet.TweenVelocity);
                }
                else
                {
                    Bullet.Horizontal += (Node as Accelerate).X.Rate(ParamList);
                    Bullet.Vertical += (Node as Accelerate).Y.Rate(ParamList);
                }
            }
        }

    }
}
