using System;
using UnityEngine;

namespace UnboundLib.Utils
{
    public struct TimeSince : IEquatable<TimeSince>
    {
        private float time;

        public static implicit operator float(TimeSince ts) => Time.realtimeSinceStartup - ts.time;

        public static implicit operator TimeSince(float ts) => new TimeSince { time = Time.realtimeSinceStartup - ts };

        public float Absolute => this.time;

        public float Relative => this;

        public override string ToString() => string.Format("{0}", (object) this.Relative);

        public static bool operator ==(TimeSince left, TimeSince right) => left.Equals(right);

        public static bool operator !=(TimeSince left, TimeSince right) => !(left == right);

        public override bool Equals(object obj) => obj is TimeSince o && this.Equals(o);

        public bool Equals(TimeSince o) => (double) this.time == (double) o.time;

        public override int GetHashCode() => time.GetHashCode();
    }
}