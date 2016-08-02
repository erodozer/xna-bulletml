using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using github.io.nhydock.BulletML.Specification;

/// <summary>
/// XML Specification parsing for BulletML
/// </summary>
namespace github.io.nhydock.BulletML
{
    [XmlRoot(ElementName = "bulletml", Namespace = "http://www.asahi-net.or.jp/~cs8k-cyu/bulletml")]
    public class BulletMLSpecification
    {
        private static readonly IDictionary<string, BulletMLSpecification> PATTERN_CACHE = new Dictionary<string, BulletMLSpecification>();
        public static BulletMLSpecification loadSpec(string path)
        {
            BulletMLSpecification _spec = null;
            if (!PATTERN_CACHE.TryGetValue(path, out _spec))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BulletMLSpecification));
                using (StreamReader reader = new StreamReader(path))
                {
                    _spec = (BulletMLSpecification)serializer.Deserialize(reader);

                    //only allow top level actions and things to be named
                    foreach (Bullet b in _spec.Bullets)
                    {
                        if (b.Label != null)
                        {
                            _spec.NamedBullets[b.Label] = b;
                        }
                    }
                    foreach (Action a in _spec.Actions)
                    {
                        if (a.Label != null)
                        {
                            _spec.NamedActions[a.Label] = a;
                        }
                    }
                    foreach (Fire f in _spec.FirePatterns)
                    {
                        if (f.Label != null)
                        {
                            _spec.NamedFire[f.Label] = f;
                        }
                    }
                }
                PATTERN_CACHE[path] = _spec;
            }
            return _spec;
        }
        
        /// <summary>
        /// Bullet definitions contained within the specification
        /// </summary>
        [XmlElement("bullet", typeof(Bullet))]
        public List<Bullet> Bullets = new List<Bullet>();
        /// <summary>
        /// Collection of named, reusable actions
        /// </summary>
        [XmlElement("action", typeof(Action))]
        public List<Action> Actions = new List<Action>();
        /// <summary>
        /// Collection of firing patterns
        /// </summary>
        [XmlElement("fire", typeof(Fire))]
        public List<Fire> FirePatterns = new List<Fire>();

        [XmlIgnore]
        public Dictionary<string, Bullet> NamedBullets = new Dictionary<string, Bullet>();
        [XmlIgnore]
        public Dictionary<string, Action> NamedActions = new Dictionary<string, Action>();
        [XmlIgnore]
        public Dictionary<string, Fire> NamedFire = new Dictionary<string, Fire>();

        public Bullet FindBullet(string name)
        {
            Bullet v;
            NamedBullets.TryGetValue(name, out v);
            return v;
        }

        public Action FindAction(string name)
        {
            Action a;
            NamedActions.TryGetValue(name, out a);
            return a;
        }

        public Fire FindFire(string name)
        {
            Fire f;
            NamedFire.TryGetValue(name, out f);
            return f;
        }
    }
}
