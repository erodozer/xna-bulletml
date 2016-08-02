using System;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        [Serializable()]
        [XmlType("fire")]
        public class Fire : TaskNode
        {
            [XmlElement(ElementName = "direction", IsNullable = true)]
            public Direction Direction;
            [XmlElement(ElementName = "speed", IsNullable = true)]
            public Speed Speed;
            [XmlAttribute(AttributeName = "label")]
            public string Label;
            [XmlElement("bullet", typeof(Bullet), IsNullable = true)]
            public Bullet Bullet;
            [XmlElement("bulletRef", typeof(BulletRef), IsNullable = true)]
            public BulletRef Reference;
        }

        [Serializable()]
        [XmlType("fireRef")]
        public class FireRef : TaskNode
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
        /// Step that creates a new bullet following specifications
        /// </summary>
        public class FireBullet : Step
        {
            private BulletMLSpecification _spec;
            private Bullet _bullet;
            private Fire _fire;
            private FireRef _reference;
            private float[] _parameters;
            private float[] _parentParameters;
            private Boolean _fired = false;

            public FireBullet(FireRef reference, BulletMLSpecification spec, float[] Parameters)
                : this(spec.FindFire(reference.Label), spec, reference.GetParams(Parameters))
            {
                _reference = reference;
                _parentParameters = Parameters;
            }

            public FireBullet(Fire fire, BulletMLSpecification spec, float[] Parameters)
            {
                _fire = fire;
                _spec = spec;
                _bullet = fire.Bullet ?? spec.FindBullet(fire.Reference.Label);
                _parameters = Parameters;
            }

            protected override bool IsDone()
            {
                return _fired;
            }

            public override void UpdateParameters(float[] Parameters)
            {
                if (_reference != null)
                {
                    _parameters = _reference.GetParams(_parentParameters);
                }
                else
                {
                    _parameters = Parameters;
                }
            }

            public IBullet Execute(IBullet parent, BulletFactory factory, float LastDirection, float LastSpeed, Vector2 target)
            {
                IBullet ib;
                if (_bullet.Action != null || _bullet.Reference != null)
                {
                    Action action = _bullet.Action ?? _spec.FindAction(_bullet.Reference.Label);
                    Sequence seq = new Sequence(action, _spec, _parameters);
                    ib = factory.Create(seq);
                }
                else
                {
                    // used with tag <bullet/> to make a new blank bullet with no special actions
                    ib = factory.Create(null);
                }
                ib.Position = parent.Position;

                if (_fire.Direction != null)
                {
                    switch (_fire.Direction.Type)
                    {
                        case Direction.ABSOLUTE:
                            ib.Rotation = _fire.Direction.Angle(_parameters); break;
                        case Direction.RELATIVE:
                            ib.Rotation = parent.Rotation + _fire.Direction.Angle(_parameters); break;
                        case Direction.SEQUENCE:
                            ib.Rotation = LastDirection + _fire.Direction.Angle(_parameters); break;
                        case Direction.AIM:
                            ib.Rotation = VectorHelper.AngleBetweenDeg(ib.Position, target) + _fire.Direction.Angle(_parameters); break;
                    }
                }
                else if (_bullet.Direction != null)
                {
                    switch (_bullet.Direction.Type)
                    {
                        case Direction.ABSOLUTE:
                            ib.Rotation = _bullet.Direction.Angle(_parameters); break;
                        case Direction.RELATIVE:
                            ib.Rotation = parent.Rotation + _bullet.Direction.Angle(_parameters); break;
                        case Direction.SEQUENCE:
                            ib.Rotation = LastDirection + _bullet.Direction.Angle(_parameters); break;
                        case Direction.AIM:
                            ib.Rotation = VectorHelper.AngleBetweenDeg(ib.Position, target) + _bullet.Direction.Angle(_parameters); break;
                    }
                }
                // blank bullets should chase the player
                else
                {
                    ib.Rotation = VectorHelper.AngleBetweenDeg(ib.Position, target);
                }

                if (_fire.Speed != null)
                {
                    if (_fire.Speed.Type == Speed.ABSOLUTE)
                    {
                        ib.Speed = _fire.Speed.Rate(_parameters);
                    }
                    else if (_fire.Speed.Type == Speed.RELATIVE)
                    {
                        ib.Speed = parent.Rotation + _fire.Speed.Rate(_parameters);
                    }
                    else if (_fire.Speed.Type == Speed.SEQUENCE)
                    {
                        ib.Speed = LastSpeed + _fire.Speed.Rate(_parameters);
                    }
                }
                else if (_bullet.Speed != null)
                {
                    if (_bullet.Speed.Type == Speed.ABSOLUTE)
                    {
                        ib.Speed = _bullet.Speed.Rate(_parameters);
                    }
                    else if (_bullet.Speed.Type == Speed.RELATIVE)
                    {
                        ib.Speed = parent.Rotation + _bullet.Speed.Rate(_parameters);
                    }
                    else if (_bullet.Speed.Type == Speed.SEQUENCE)
                    {
                        ib.Speed = LastSpeed + _bullet.Speed.Rate(_parameters);
                    }
                }
                _fired = true;
                return ib;
            }
        }
    }
}
