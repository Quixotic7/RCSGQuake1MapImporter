﻿#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;
using System.Linq;

namespace RealtimeCSG.Quake1Importer
{
    /// <summary>
    /// Represents a Quake 1 World.
    /// </summary>
    public class MapWorld
    {
        /// <summary>
        /// The brushes in the world (or null if no world).
        /// </summary>
        public List<MapBrush> Brushes
        {
            get
            {
                return Entities.Where(e => e.ClassName == "worldspawn").Select(e => e.Brushes).FirstOrDefault();
            }
        }

        /// <summary>
        /// The entities in the world.
        /// </summary>
        public List<MapEntity> Entities = new List<MapEntity>();

        public bool valveFormat = false;

        public string mapName;
    }
}

#endif