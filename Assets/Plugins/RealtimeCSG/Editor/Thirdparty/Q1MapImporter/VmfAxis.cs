// MIT License
// Based off Henry's Source importer https://github.com/Henry00IS/Chisel.Import.Source

namespace RealtimeCSG.Quake1Importer
{
    /// <summary>
    /// Represents a Hammer UV Axis.
    /// </summary>
    public class VmfAxis
    {
        /// <summary>
        /// The x, y, z vector.
        /// </summary>
        public MapVector3 Vector;

        /// <summary>
        /// The UV translation.
        /// </summary>
        public float Translation;

        /// <summary>
        /// The UV scale.
        /// </summary>
        public float Scale;

        /// <summary>
        /// Initializes a new instance of the <see cref="VmfAxis"/> class.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="translation">The translation.</param>
        /// <param name="scale">The scale.</param>
        public VmfAxis(MapVector3 vector, float translation, float scale)
        {
            Vector = vector;
            Translation = translation;
            Scale = scale;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "VmfAxis " + Vector + ", T=" + Translation + ", S=" + Scale;
        }
    }
}