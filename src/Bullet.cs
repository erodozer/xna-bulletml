using System;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework
{
    public static class Extends
    {
        /// <summary>
        /// 12 o'clock is up, angle is clockwise
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static float AngleDeg(this Vector2 vec)
        {
            return MathHelper.ToDegrees(vec.AngleRad());
        }

        public static Vector2 AngleDeg(this Vector2 vec, float deg)
        {
            return vec.AngleRad(MathHelper.ToRadians(deg));
        }

        public static float AngleRad(this Vector2 vec)
        {
            return (float)Math.Atan2(vec.X, -vec.Y);
        }

        public static Vector2 AngleRad(this Vector2 vec, float rad)
        {
            float l = Math.Abs(vec.Length());
            vec.Y = -(float)Math.Cos(rad);
            vec.X = (float)Math.Sin(rad);
            vec *= l;
            return vec;
        }

        public static float AngleBetween(this Vector2 vec, Vector2 target)
        {
            return (float)Math.Atan2(target.X - vec.X, -(target.Y - vec.Y));
        }

        public static float AngleBetweenDeg(this Vector2 vec, Vector2 target)
        {
            return MathHelper.ToDegrees(vec.AngleBetween(target));
        }
    }
}

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        [Serializable()]
        [XmlType("bullet")]
        public class Bullet
        {
            [XmlElement(ElementName = "direction", IsNullable = true)]
            public Direction Direction;
            [XmlElement(ElementName = "speed", IsNullable = true)]
            public Speed Speed;
            [XmlAttribute(AttributeName = "label")]
            public string Label;
            [XmlElement("action", typeof(Action), IsNullable = true)]
            public Action Action;
            [XmlElement("actionRef", typeof(ActionRef), IsNullable = true)]
            public ActionRef Reference;
        }

        [Serializable()]
        [XmlType("bulletRef")]
        public class BulletRef : TaskNode
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
        public class IBullet
        {
            public Vector2 Position = new Vector2(0, 0);
            public Vector2 Scale = new Vector2(1, 1);
            public Vector2 Velocity = new Vector2(0, 0);
            public float X
            {
                get { return Position.X; }
                set { Position.X = value; }
            }
            public float Y
            {
                get { return Position.Y; }
                set { Position.Y = value; }
            }
            public float Horizontal
            {
                get { return Velocity.X; }
                set
                {
                    Velocity.X = value;
                    TweenVelocity.X = value;
                    _rotation = Velocity.AngleDeg();
                    _speed = Velocity.Length();
                }
            }
            public float Vertical
            {
                get { return Velocity.Y; }
                set
                {
                    Velocity.Y = value;
                    TweenVelocity.Y = value;
                    _rotation = Velocity.AngleDeg();
                    _speed = Velocity.Length();
                }
            }
            // rotation of the bullet in degrees
            private float _rotation = 0;
            public float Rotation
            {
                get { return _rotation; }
                set
                {
                    _rotation = value;
                    _tweenRotate = value;
                    Vector2 norm = new Vector2(Speed, 0);
                    norm.AngleDeg(value);
                    Velocity = norm;
                    TweenVelocity = norm;
                }
            }
            private float _speed;
            public float Speed
            {
                get { return _speed; }
                set
                {
                    _speed = value;
                    Vector2 norm = Vector2.UnitX;
                    norm *= value;
                    norm = norm.AngleDeg(Rotation);
                    Velocity = norm;
                    TweenVelocity = norm;
                }
            }
            //Tweening Values
            public Vector2 TweenVelocity = new Vector2(0, 0);
            private float _tweenRotate = 0;
            public float TweenRotate
            {
                get
                {
                    return _tweenRotate;
                }
                set
                {
                    Vector2 norm = new Vector2(TweenVelocity.Length(), 0);
                    norm.AngleDeg(value);
                    TweenVelocity = norm;
                }
            }
        }

        /// <summary>
        /// Generator of Bullet entities for our ECS
        /// </summary>
        public abstract class BulletFactory
        {
            /// <summary>
            /// Creates a new bullet from a fire event
            /// </summary>
            /// <param name="fire"></param>
            /// <returns></returns>
            public abstract IBullet Create(Sequence Sequence);
        }
    }    
}
