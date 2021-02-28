using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;


namespace RealtimeCSG.Quake1Importer
{
    /// <summary>
    /// Converts a Quake 1 Map to SabreCSG Brushes.
    /// </summary>
    public static class MapWorldConverter
    {
        private static int s_Scale = 32;

        private static Transform CreateGameObjectWithUniqueName(string name, Transform parent)
        {
            var go = new GameObject(UnityEditor.GameObjectUtility.GetUniqueNameForSibling(parent, name));
            go.transform.SetParent(parent);
            return go.transform;
        }

        private class EntityContainer
        {
            public Transform transform;
            public MapEntity entity;

            public EntityContainer(Transform t, MapEntity e)
            {
                transform = t;
                entity = e;
            }
        }

        // Trenchbroom texture matrix math

        //    void ParallelTexCoordSystem::applyRotation(const vm::vec3& normal, const FloatType angle) {
        //        const vm::quat3 rot(normal, angle);
        //    m_xAxis = rot* m_xAxis;
        //    m_yAxis = rot* m_yAxis;
        //}

        /*
         * vm::mat4x4 TexCoordSystem::toMatrix(const vm::vec2f& o, const vm::vec2f& s) const {
            const vm::vec3 x = safeScaleAxis(getXAxis(), s.x());
            const vm::vec3 y = safeScaleAxis(getYAxis(), s.y());
            const vm::vec3 z = getZAxis();

            return vm::mat4x4(x[0], x[1], x[2], o[0],
                          y[0], y[1], y[2], o[1],
                          z[0], z[1], z[2],  0.0,
                           0.0,  0.0,  0.0,  1.0);
        }
         */

        private static float SafeDivision(float numerator, float denominator)
        {
            return (Mathf.Approximately(denominator, 0)) ? 0 : numerator / denominator;
        }

        private static Vector3 SafeDivision(Vector3 numerator, float denominator)
        {
            return (Mathf.Approximately(denominator, 0)) ? Vector3.zero : numerator / denominator;
        }

        public static Matrix4x4 GetTextMatrix(Vector3 tX, Vector3 tY, Vector2 offset, Vector2 scale)
        {
            //var x = SafeDivision(tX, scale.x);
            //var y = SafeDivision(tY, scale.y);

            var x = tX * scale.x;
            var y = tY * scale.y;
            var z = Vector3.Cross(tX, tY);

            return new Matrix4x4(
                new Vector4(x.x, x.y, x.z, offset.x),
                new Vector4(y.x, y.y, y.z, offset.y),
                new Vector4(z.x, z.y, z.z, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
            );
        }

        /// <summary>
        /// Imports the specified world into the SabreCSG model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="world">The world to be imported.</param>
        /// <param name="scale">The scale modifier.</param>
        public static void Import(Transform rootTransform, MapWorld world)
        {
            //model.BeginUpdate();

            // create a material searcher to associate materials automatically.
            MaterialSearcher materialSearcher = new MaterialSearcher();

            // group all the brushes together.
            //GroupBrush groupBrush = new GameObject("Quake 1 Map").AddComponent<GroupBrush>();
            //groupBrush.transform.SetParent(model.transform);

            var mapTransform = CreateGameObjectWithUniqueName(world.mapName, rootTransform);
            mapTransform.position = Vector3.zero;

            // Index of entities by trenchbroom id
            var entitiesById = new Dictionary<int, EntityContainer>();

            var layers = new List<EntityContainer>();

            for (int e = 0; e < world.Entities.Count; e++)
            {
                var entity = world.Entities[e];

                //EntityContainer eContainer = null;

                if(entity.tbId >= 0)
                {
                    var name = String.IsNullOrEmpty(entity.tbName) ? "Unnamed" : entity.tbName;
                    var t = CreateGameObjectWithUniqueName(name, mapTransform);
                    var eContainer = new EntityContainer(t, entity);
                    entitiesById.Add(entity.tbId, eContainer);

                    if(entity.tbType == "_tb_layer")
                    {
                        layers.Add(eContainer);
                        eContainer.transform.SetParent(null); // unparent until layers are sorted by sort index
                    }
                }
            }

            var defaultLayer = CreateGameObjectWithUniqueName("Default Layer", mapTransform);

            //var worldSpawnModel = OperationsUtility.CreateModelInstanceInScene(defaultLayer);
            //worldSpawnModel.name = "WorldSpawn";
            //worldSpawnModel.transform.SetParent(mapTransform);

            layers = layers.OrderBy(l => l.entity.tbLayerSortIndex).ToList(); // sort layers by layer sort index

            foreach(var l in layers)
            {
                l.transform.SetParent(mapTransform); // parent layers to map in order
            }

            bool valveFormat = world.valveFormat;

            // iterate through all entities.
            for (int e = 0; e < world.Entities.Count; e++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Importing Quake 1 Map", "Converting Quake 1 Entities To Brushes (" + (e + 1) + " / " + world.Entities.Count + ")...", e / (float)world.Entities.Count);
#endif
                MapEntity entity = world.Entities[e];

                Transform brushParent = mapTransform;

                bool isLayer = false;
                bool isTrigger = false;

                if(entity.ClassName == "worldspawn")
                {
                    brushParent = defaultLayer;
                }
                else if (entity.tbType == "_tb_layer")
                {
                    isLayer = true;
                    if (entitiesById.TryGetValue(entity.tbId, out EntityContainer eContainer))
                    {
                        brushParent = eContainer.transform;
                    }
                }
                else if(entity.tbType == "_tb_group")
                {
                    if (entitiesById.TryGetValue(entity.tbId, out EntityContainer eContainer))
                    {
                        brushParent = eContainer.transform;
                    }
                }
                else
                {
                    if (entity.ClassName.Contains("trigger")) isTrigger = true;

                    brushParent = CreateGameObjectWithUniqueName(entity.ClassName, mapTransform);
                }

                if (brushParent != mapTransform && brushParent != defaultLayer)
                {
                    if (entity.tbGroup > 0)
                    {
                        if (entitiesById.TryGetValue(entity.tbGroup, out EntityContainer eContainer))
                        {
                            brushParent.SetParent(eContainer.transform);
                        }
                    }
                    else if (entity.tbLayer > 0)
                    {
                        if (entitiesById.TryGetValue(entity.tbLayer, out EntityContainer eContainer))
                        {
                            brushParent.SetParent(eContainer.transform);
                        }
                    }
                    else if(!isLayer)
                    {
                        brushParent.SetParent(defaultLayer);
                    }
                }

                   

                //if(entity.)

                if (entity.Brushes.Count == 0) continue;


                var model = OperationsUtility.CreateModelInstanceInScene(brushParent);
                var parent = model.transform;

                if (isTrigger)
                {
                    model.Settings = (model.Settings | ModelSettingsFlags.IsTrigger | ModelSettingsFlags.SetColliderConvex | ModelSettingsFlags.DoNotRender);
                }

                //GroupBrush entityGroup = new GameObject(entity.ClassName).AddComponent<GroupBrush>();
                //entityGroup.transform.SetParent(groupBrush.transform);


                // iterate through all entity solids.
                for (int i = 0; i < entity.Brushes.Count; i++)
                {
                    MapBrush brush = entity.Brushes[i];
#if UNITY_EDITOR
                    if (world.Entities[e].ClassName == "worldspawn")
                        UnityEditor.EditorUtility.DisplayProgressBar("SabreCSG: Importing Quake 1 Map", "Converting Quake 1 Brushes To SabreCSG Brushes (" + (i + 1) + " / " + entity.Brushes.Count + ")...", i / (float)entity.Brushes.Count);
#endif
                    // don't add triggers to the scene.
                    // Triggers will get placed in entity model now
                    //if (brush.Sides.Count > 0 && IsSpecialMaterial(brush.Sides[0].Material))
                    //    continue;


                    var name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(parent, "Brush");
                    var gameObject = new GameObject(name);
                    var rcsgBrush = gameObject.AddComponent<CSGBrush>();
                    var t = gameObject.transform;

                    gameObject.transform.SetParent(parent, true);
                    gameObject.transform.position = new Vector3(0.5f, 0.5f, 0.5f); // this aligns it's vertices to the grid
                                                                                   //                                                               //BrushFactory.CreateCubeControlMesh(out brush.ControlMesh, out brush.Shape, Vector3.one);

                    var planes = new Plane[brush.Sides.Count];
                    var textureMatrices = new Matrix4x4[brush.Sides.Count];
                    var materials = new Material[brush.Sides.Count];


                    Debug.Log($"Brush sides {brush.Sides.Count}");

                    // Get planes for all sides of the brush
                    for (int j = 0; j < brush.Sides.Count; j++)
                    {
                        MapBrushSide side = brush.Sides[j];

                        var pa = t.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) / (float)s_Scale);
                        var pb = t.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) / (float)s_Scale);
                        var pc = t.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) / (float)s_Scale);

                        planes[j] = new Plane(pa, pb, pc);

                        //var tScale = tRight.normalized * side.Scale.X + tForward.normalized + tUp.normalized * side.Scale.Y;

                        //var tScale = Vector3.one;

                        if (IsExcludedMaterial(side.Material))
                        {
                            // polygon.UserExcludeFromFinal = true;
                        }
                        // detect collision-only brushes.
                        if (IsInvisibleMaterial(side.Material))
                        {
                            // pr.IsVisible = false;
                        }
                        // find the material in the unity project automatically.
                        //Material material;
                        // try finding the texture name with '*' replaced by '#' so '#teleport'.
                        string materialName = side.Material.Replace("*", "#");
                        materials[j] = materialSearcher.FindMaterial(new string[] { materialName });
                        if (materials[j] == null)
                        {
                            materials[j] = CSGSettings.DefaultMaterial;
                            // Debug.Log("SabreCSG: Tried to find material '" + materialName + "' but it couldn't be found in the project.");
                        }

                        if (valveFormat)
                        {

                            //var tRight = new Vector3(side.t1.X, side.t1.Z, side.t1.Y);
                            //var tUp = new Vector3(side.t2.X, side.t2.Z, side.t2.Y);

                            //var tRight = new Vector3(side.t1.X, side.t1.Z, side.t1.Y);
                            //var tUp = new Vector3(side.t2.X, side.t2.Z, side.t2.Y);


                            //var tRight = new Vector3(side.t1.X, side.t1.Y, side.t1.Z);
                            //var tUp = new Vector3(side.t2.X, side.t2.Y, side.t2.Z);

                            // Z, Y, X and X, Y, Z produce similar planes, though texture often needs to be rotated 180. 
                            var tRight = new Vector3(side.t1.X, side.t1.Y, side.t1.Z);
                            var tUp = new Vector3(side.t2.X, side.t2.Y, side.t2.Z);
                            textureMatrices[j] = GetTextMatrix(tRight, tUp, Vector2.zero, Vector2.one);

                            /*

                            //tUp *= -1;
                            var tForward = Vector3.Cross(-tUp, tRight);
                            //var tRot = Quaternion.LookRotation(-tForward, tUp) * Quaternion.AngleAxis(180, planes[j].normal);
                            var tRot = Quaternion.LookRotation(tForward, -tUp);


                            // calculate the texture coordinates.
                            int w = 32;
                            int h = 32;
                            if (materials[j].mainTexture != null)
                            {
                                w = materials[j].mainTexture.width;
                                h = materials[j].mainTexture.height;
                            }

                            var resize = new Vector2(1.0f / w, 1.0f / h);

                            //Debug.Log($"W = {w} + h = {h}");

                            //var scaleAdjust = new Vector2(32 / w)

                            var tScale = new Vector2(
                                (32.0f / w) / Mathf.Max(side.Scale.X, float.Epsilon),
                                (32.0f / h) / Mathf.Max(side.Scale.Y, float.Epsilon));

                            var tOffset = tRight.normalized * side.Offset.X * resize.x + tUp.normalized * side.Offset.Y * resize.y;

                            //textureMatrices[j] = Matrix4x4.TRS(tOffset, tRot, Vector3.one);

                            //textureMatrices[j] = GetTextMatrix(tRight, tUp, new Vector2(side.Offset.X, side.Offset.Y), new Vector2(side.Scale.X, side.Scale.Y));

                            //textureMatrices[j] = GetTextMatrix(tRight, tUp, new Vector2(side.Offset.X * resize.x, side.Offset.Y * resize.y), tScale);

                            textureMatrices[j] = GetTextMatrix(tRight, tUp, Vector2.zero, Vector2.one);
                            */
                        }

                        //CalculateTextureCoordinates(pr, polygon, w, h, new Vector2(side.Offset.X, -side.Offset.Y), new Vector2(side.Scale.X, side.Scale.Y), side.Rotation);
                    }

                    bool controlMeshSuccess = true;

                    if (valveFormat) {
                        controlMeshSuccess = BrushFactory.CreateControlMeshFromPlanes(out rcsgBrush.ControlMesh, out rcsgBrush.Shape, planes, null, null, materials, textureMatrices, TextureMatrixSpace.WorldSpace);
                    }
                    else {
                        controlMeshSuccess = BrushFactory.CreateControlMeshFromPlanes(out rcsgBrush.ControlMesh, out rcsgBrush.Shape, planes, null, null, materials);
                    }

                    if (controlMeshSuccess)
                    {
                        //BrushFactory.CreateControlMeshFromPlanes(out rcsgBrush.ControlMesh, out rcsgBrush.Shape, planes, null, null, materials);
                        //BrushFactory.CreateControlMeshFromPlanes(out rcsgBrush.ControlMesh, out rcsgBrush.Shape, planes);


                        for (int j = 0; j < brush.Sides.Count; j++)
                        {
                            MapBrushSide side = brush.Sides[j];

                            // calculate the texture coordinates.
                            int w = 32;
                            int h = 32;
                            if (materials[j].mainTexture != null)
                            {
                                w = materials[j].mainTexture.width;
                                h = materials[j].mainTexture.height;
                            }

                            var tScale = new Vector2(
                                SafeDivision((32.0f / w), side.Scale.X),
                                SafeDivision((32.0f / h), side.Scale.Y));

                            if (valveFormat)
                            {
                                //var cScale = rcsgBrush.Shape.TexGens[j].Scale;
                                //tScale.x = Mathf.Abs(tScale.x) * Mathf.Sign(cScale.x);
                                //tScale.y = Mathf.Abs(tScale.y) * Mathf.Sign(cScale.y);

                                rcsgBrush.Shape.TexGens[j].Scale = tScale;

                                rcsgBrush.Shape.TexGens[j].Translation.x = SafeDivision(side.Offset.X, w);
                                rcsgBrush.Shape.TexGens[j].Translation.y = SafeDivision(-side.Offset.Y, h);

                                rcsgBrush.Shape.TexGens[j].RotationAngle += 180 + side.Rotation; // Textures often need to be flipped or rotated 180 to match
                            }
                            else
                            {
                                

                                //Debug.Log($"W = {w} + h = {h}");

                                //var scaleAdjust = new Vector2(32 / w)

                                

                                rcsgBrush.Shape.TexGens[j].Scale = tScale;

                                //Debug.Log($"ScaleX {tScale.x} ScaleY {tScale.y}");

                                if (side.Offset.X != 0)
                                    rcsgBrush.Shape.TexGens[j].Translation.x = side.Offset.X / Mathf.Max(w, float.Epsilon);
                                else
                                    rcsgBrush.Shape.TexGens[j].Translation.x = 0;

                                if (side.Offset.Y != 0)
                                    rcsgBrush.Shape.TexGens[j].Translation.y = side.Offset.Y / Mathf.Max(h, float.Epsilon);
                                else
                                    rcsgBrush.Shape.TexGens[j].Translation.y = 0;

                                rcsgBrush.Shape.TexGens[j].RotationAngle = 180 + side.Rotation;

                                //if (valveFormat)
                                //{
                                //    rcsgBrush.Shape.TexGens[j].RotationAngle -= side.Rotation;
                                //}
                                //else
                                //{
                                //    rcsgBrush.Shape.TexGens[j].RotationAngle = 180 + side.Rotation;
                                //}
                            }
                        }
                    }
                    else
                    {
                        GameObject.DestroyImmediate(rcsgBrush.gameObject);
                    }

                    //InternalCSGModelManager.CheckForChanges();
                    //InternalCSGModelManager.UpdateMeshes();
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif

            try
            {
                
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                InternalCSGModelManager.CheckForChanges();
                InternalCSGModelManager.UpdateMeshes();

                //model.EndUpdate();
            }
        }

        /// <summary>
        /// Determines whether the specified name is an excluded material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an excluded material; otherwise, <c>false</c>.</returns>
        private static bool IsExcludedMaterial(string name)
        {
            if (name.StartsWith("sky"))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is an invisible material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an invisible material; otherwise, <c>false</c>.</returns>
        private static bool IsInvisibleMaterial(string name)
        {
            switch (name)
            {
                case "clip":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is a special material, these brush will not be
        /// imported into SabreCSG.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is a special material; otherwise, <c>false</c>.</returns>
        private static bool IsSpecialMaterial(string name)
        {
            switch (name)
            {
                case "trigger":
                case "skip":
                case "waterskip":
                    return true;
            }
            return false;
        }
    }
}