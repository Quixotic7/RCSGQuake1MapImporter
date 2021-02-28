#if UNITY_EDITOR || RUNTIME_CSG

namespace RealtimeCSG.Quake1Importer
{
    /// <summary>
    /// Represents a Quake 1 Brush Side.
    /// </summary>
    public class MapBrushSide
    {
        public MapPlane Plane;
        public string Material;
        public MapVector2 Offset;
        public float Rotation;
        public MapVector2 Scale;

        public MapVector3 t1;
        public MapVector3 t2;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Quake 1 Brush Side " + " '" + Material + "' " + " " + Plane;
        }
    }
}

#endif