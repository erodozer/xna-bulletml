using System;

namespace github.io.nhydock.BulletML
{
    namespace Specification
    {
        using System.Xml.Serialization;

        [Serializable()]
        [XmlType("fire")]
        public class Fire : TaskNode, BulletMLNode
        {
            [XmlElement(ElementName = "direction", IsNullable = true)]
            public Direction Direction;
            [XmlElement(ElementName = "speed", IsNullable = true)]
            public Speed Speed;
            [XmlAttribute(AttributeName = "label")]
            public string Label;
            [XmlElement("bullet", typeof(Bullet), IsNullable = true)]
            public Bullet Bullet;
            [XmlElement("bulletRef", typeof(Reference<Bullet>), IsNullable = true)]
            public Reference<Bullet> Reference;
        }
    }

    namespace Implementation
    {
        using Specification;
        using Microsoft.Xna.Framework;
        using System.Collections.Generic;
        /// <summary>
        /// Step that creates a new bullet following specifications
        /// </summary>
        public class FireBullet : Step
        {
            private BulletMLSpecification _spec;
            private Bullet _bulletTemplate;
            private Fire _fire;
            private Reference<Fire> _reference;
            private float[] _parentParameters;
            private Boolean _fired = false;
            
            public FireBullet(Reference<Fire> reference, BulletMLSpecification spec, float[] Parameters)
                : this(spec.NamedFire[reference.Label], spec, reference.GetParams(Parameters))
            {
                _reference = reference;
                _parentParameters = Parameters;
            }

            public FireBullet(Fire fire, BulletMLSpecification spec, float[] Parameters) : base(fire, Parameters)
            {
                _fire = fire;
                _spec = spec;
                _bulletTemplate = fire.Bullet ?? spec.NamedBullets[fire.Reference.Label];
            }

            protected override bool IsDone()
            {
                return _fired;
            }

            public override void UpdateParameters(float[] Parameters)
            {
                if (_reference != null)
                {
                    _parentParameters = Parameters;
                    ParamList = _reference.GetParams(Parameters);
                }
                else
                {
                    ParamList = Parameters;
                }
            }

            public IBullet Execute(BulletFactory factory, float LastDirection, float LastSpeed, Vector2 target)
            {
                IBullet ib;
                // wrap bullet actions into a sequence
                if (_bulletTemplate.Sequence.Count > 0)
                {
                    List<Action> actions = new List<Action>();
                    foreach (TaskNode t in _bulletTemplate.Sequence)
                    {
                        if (t is Reference<Action>)
                        {
                            actions.Add(_spec.NamedActions[((Reference<Action>)t).Label]);
                        }
                        else if (t is Action)
                        {
                            actions.Add((Action)t);
                        }
                    }
                    ExecutableStep FiringAction = new Parallel(actions, _spec, ParamList);
                    ib = factory.Create(FiringAction);
                }
                else
                {
                    // used with tag <bullet/> to make a new blank bullet with no special actions
                    ib = factory.Create(null);
                }
                ib.Position = Bullet.Position;

                if (_fire.Direction != null)
                {
                    switch (_fire.Direction.Type)
                    {
                        case Direction.ABSOLUTE:
                            ib.Rotation = _fire.Direction.Angle(ParamList); break;
                        case Direction.RELATIVE:
                            ib.Rotation = Bullet.Rotation + _fire.Direction.Angle(ParamList); break;
                        case Direction.SEQUENCE:
                            ib.Rotation = LastDirection + _fire.Direction.Angle(ParamList); break;
                        case Direction.AIM:
                            ib.Rotation = VectorHelper.AngleBetweenDeg(ib.Position, target) + _fire.Direction.Angle(ParamList); break;
                    }
                }
                else if (_bulletTemplate.Direction != null)
                {
                    switch (_bulletTemplate.Direction.Type)
                    {
                        case Direction.ABSOLUTE:
                            ib.Rotation = _bulletTemplate.Direction.Angle(ParamList); break;
                        case Direction.RELATIVE:
                            ib.Rotation = Bullet.Rotation + _bulletTemplate.Direction.Angle(ParamList); break;
                        case Direction.SEQUENCE:
                            ib.Rotation = LastDirection + _bulletTemplate.Direction.Angle(ParamList); break;
                        case Direction.AIM:
                            ib.Rotation = VectorHelper.AngleBetweenDeg(ib.Position, target) + _bulletTemplate.Direction.Angle(ParamList); break;
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
                        ib.Speed = _fire.Speed.Rate(ParamList);
                    }
                    else if (_fire.Speed.Type == Speed.RELATIVE)
                    {
                        ib.Speed = Bullet.Speed + _fire.Speed.Rate(ParamList);
                    }
                    else if (_fire.Speed.Type == Speed.SEQUENCE)
                    {
                        ib.Speed = LastSpeed + _fire.Speed.Rate(ParamList);
                    }
                }
                else if (_bullet.Speed != null)
                {
                    if (_bulletTemplate.Speed.Type == Speed.ABSOLUTE)
                    {
                        ib.Speed = _bulletTemplate.Speed.Rate(ParamList);
                    }
                    else if (_bulletTemplate.Speed.Type == Speed.RELATIVE)
                    {
                        ib.Speed = Bullet.Speed + _bulletTemplate.Speed.Rate(ParamList);
                    }
                    else if (_bulletTemplate.Speed.Type == Speed.SEQUENCE)
                    {
                        ib.Speed = LastSpeed + _bulletTemplate.Speed.Rate(ParamList);
                    }
                }
                _fired = true;
                return ib;
            }
        }
    }
}
