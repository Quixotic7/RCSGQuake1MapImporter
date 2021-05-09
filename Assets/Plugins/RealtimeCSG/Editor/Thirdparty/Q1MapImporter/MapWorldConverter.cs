using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;


namespace RealtimeCSG.Quake1Importer
{
    /// <summary>
    /// Converts a Quake 1 Map to RealtimeCSG Brushes.
    /// </summary>
    public static class MapWorldConverter
    {
        private static int s_Scale = 32;
        private static float _conversionScale = 1.0f / 32;

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
        /// Imports the specified world into the RealtimeCSG.
        /// </summary>
        /// <param name="rootTransform">Transform to be parent of RealtimeCSG brushes</param>
        /// <param name="world">The world to be imported.</param>
        public static void Import(Transform rootTransform, MapWorld world)
        {
            try
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

                    if (entity.tbId >= 0)
                    {
                        var name = String.IsNullOrEmpty(entity.tbName) ? "Unnamed" : entity.tbName;
                        var t = CreateGameObjectWithUniqueName(name, mapTransform);
                        var eContainer = new EntityContainer(t, entity);
                        entitiesById.Add(entity.tbId, eContainer);

                        if (entity.tbType == "_tb_layer")
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

                foreach (var l in layers)
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

                    if (entity.ClassName == "worldspawn")
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
                    else if (entity.tbType == "_tb_group")
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
                        else if (!isLayer)
                        {
                            brushParent.SetParent(defaultLayer);
                        }
                    }

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
                            UnityEditor.EditorUtility.DisplayProgressBar("RealtimeCSG: Importing Quake 1 Map", "Converting Quake 1 Brushes To RealtimeCSG Brushes (" + (i + 1) + " / " + entity.Brushes.Count + ")...", i / (float)entity.Brushes.Count);
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
                                // Debug.Log("RealtimeCSG: Tried to find material '" + materialName + "' but it couldn't be found in the project.");
                            }

                            if (valveFormat)
                            {
                                // calculate the texture coordinates.
                                int w = 256;
                                int h = 256;
                                if (materials[j].mainTexture != null)
                                {
                                    w = materials[j].mainTexture.width;
                                    h = materials[j].mainTexture.height;
                                }

                                var uAxis = new VmfAxis(side.t1, side.Offset.X, side.Scale.X);
                                var vAxis = new VmfAxis(side.t2, side.Offset.Y, side.Scale.Y);
                                textureMatrices[j] = CalculateTextureCoordinates(planes[j], w, h, uAxis, vAxis);
                            }
                        }

                        bool controlMeshSuccess = true;

                        if (valveFormat)
                        {
                            controlMeshSuccess = BrushFactory.CreateControlMeshFromPlanes(out rcsgBrush.ControlMesh, out rcsgBrush.Shape, planes, null, null, materials, textureMatrices, TextureMatrixSpace.WorldSpace);
                        }
                        else
                        {
                            controlMeshSuccess = BrushFactory.CreateControlMeshFromPlanes(out rcsgBrush.ControlMesh, out rcsgBrush.Shape, planes, null, null, materials);
                        }

                        if (controlMeshSuccess)
                        {
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
                                    // This shouldn't be needed due to setting texture matrix
                                    rcsgBrush.Shape.TexGens[j].Scale = tScale;

                                    rcsgBrush.Shape.TexGens[j].Translation.x = SafeDivision(side.Offset.X, w);
                                    rcsgBrush.Shape.TexGens[j].Translation.y = SafeDivision(-side.Offset.Y, h);

                                    //rcsgBrush.Shape.TexGens[j].RotationAngle += 180 + side.Rotation; // Textures often need to be flipped or rotated 180 to match
                                }
                                else
                                {
                                    rcsgBrush.Shape.TexGens[j].Scale = tScale;

                                    if (side.Offset.X != 0)
                                        rcsgBrush.Shape.TexGens[j].Translation.x = side.Offset.X / Mathf.Max(w, float.Epsilon);
                                    else
                                        rcsgBrush.Shape.TexGens[j].Translation.x = 0;

                                    if (side.Offset.Y != 0)
                                        rcsgBrush.Shape.TexGens[j].Translation.y = side.Offset.Y / Mathf.Max(h, float.Epsilon);
                                    else
                                        rcsgBrush.Shape.TexGens[j].Translation.y = 0;

                                    rcsgBrush.Shape.TexGens[j].RotationAngle = 180 + side.Rotation;
                                }
                            }
                        }
                        else
                        {
                            GameObject.DestroyImmediate(rcsgBrush.gameObject);
                        }
                    }
                }
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

#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
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
        /// imported into RealtimeCSG.
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

        public static Matrix4x4 GenerateLocalToPlaneSpaceMatrix(Vector3 normal, float distance)
        {
            normal = -normal;

            CalculateTangents(normal, out Vector3 tangent, out Vector3 biNormal);
            //var pointOnPlane = normal * planeVector.w;

            return new Matrix4x4(
                new Vector4(tangent.x, biNormal.x, normal.x, 0.0f),
                new Vector4(tangent.y, biNormal.y, normal.y, 0.0f),
                new Vector4(tangent.z, biNormal.z, normal.z, 0.0f),
                new Vector4(0, 0, -distance, 1.0f)
            );
        }

        public static void CalculateTangents(Vector3 normal, out Vector3 tangent, out Vector3 binormal)
        {
            tangent = Vector3.Cross(normal, ClosestTangentAxis(normal)).normalized;
            binormal = Vector3.Cross(normal, tangent).normalized;
        }

        public static Vector3 ClosestTangentAxis(Vector3 vector)
        {
            var absX = Mathf.Abs(vector.x);
            var absY = Mathf.Abs(vector.y);
            var absZ = Mathf.Abs(vector.z);

            if (absY > absX && absY > absZ)
                return new Vector3(0, 0, 1);

            return new Vector3(0, -1, 0);
        }

        public static Matrix4x4 GetUVMatrix(Vector4 u, Vector4 v)
        {
            var m = new Matrix4x4(
                u,
                v,
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1)
            );

            return Matrix4x4.Transpose(m);
        }

        private static Matrix4x4 CalculateTextureCoordinates(Plane clip, int textureWidth, int textureHeight, VmfAxis UAxis, VmfAxis VAxis)
        {
            //var localToPlaneSpace = GenerateLocalToPlaneSpaceMatrix(clip.normal, clip.distance);
            //var planeSpaceToLocal = Matrix4x4.Inverse(localToPlaneSpace);

            UAxis.Translation %= textureWidth;
            VAxis.Translation %= textureHeight;

            if (UAxis.Translation < -textureWidth / 2f)
                UAxis.Translation += textureWidth;

            if (VAxis.Translation < -textureHeight / 2f)
                VAxis.Translation += textureHeight;

            var scaleX = textureWidth * UAxis.Scale * _conversionScale;
            var scaleY = textureHeight * VAxis.Scale * _conversionScale;

            var uoffset = Vector3.Dot(Vector3.zero, new Vector3(UAxis.Vector.X, UAxis.Vector.Z, UAxis.Vector.Y)) + (UAxis.Translation / textureWidth);
            var voffset = Vector3.Dot(Vector3.zero, new Vector3(VAxis.Vector.X, VAxis.Vector.Z, VAxis.Vector.Y)) + (VAxis.Translation / textureHeight);

            var uVector = new Vector4(UAxis.Vector.X / scaleX, UAxis.Vector.Z / scaleX, UAxis.Vector.Y / scaleX, uoffset);
            var vVector = new Vector4(VAxis.Vector.X / scaleY, VAxis.Vector.Z / scaleY, VAxis.Vector.Y / scaleY, voffset);
            var matrix = GetUVMatrix(uVector, -vVector);

            //return matrix * planeSpaceToLocal;

            return matrix; // World space working better than plane space
        }
    }
}