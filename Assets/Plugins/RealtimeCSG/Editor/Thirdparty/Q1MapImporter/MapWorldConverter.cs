using System;
using System.Linq;
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


            var model = OperationsUtility.CreateModelInstanceInScene(rootTransform);

            var parent = model.transform;

            bool valveFormat = world.valveFormat;




            // iterate through all entities.
            for (int e = 0; e < world.Entities.Count; e++)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Importing Quake 1 Map", "Converting Quake 1 Entities To Brushes (" + (e + 1) + " / " + world.Entities.Count + ")...", e / (float)world.Entities.Count);
#endif
                MapEntity entity = world.Entities[e];

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
                    if (brush.Sides.Count > 0 && IsSpecialMaterial(brush.Sides[0].Material))
                        continue;


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

                    // clip all the sides out of the brush.
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

                            var tRight = new Vector3(side.t1.X, side.t1.Z, side.t1.Y);
                            var tUp = new Vector3(side.t2.X, side.t2.Z, side.t2.Y);

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

                            textureMatrices[j] = Matrix4x4.TRS(tOffset, tRot, Vector3.one);
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

                            //Debug.Log($"W = {w} + h = {h}");

                            //var scaleAdjust = new Vector2(32 / w)

                            var tScale = new Vector2(
                                (32.0f / w) / Mathf.Max(side.Scale.X, float.Epsilon),
                                (32.0f / h) / Mathf.Max(side.Scale.Y, float.Epsilon));

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


                            rcsgBrush.Shape.TexGens[j].RotationAngle -= side.Rotation;
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

        //// shoutouts to Jasmine Mickle for your insight and UV texture coordinates code.
        //private static void CalculateTextureCoordinates(PrimitiveBrush pr, Polygon polygon, int textureWidth, int textureHeight, Vector2 offset, Vector2 scale, float rotation)
        //{
        //    // feel free to improve this uv mapping code, it has some issues.
        //    // • 45 degree angled walls may not have correct UV texture coordinates (are not correctly picking the dominant axis because there are two).
        //    // • negative vertex coordinates may not have correct UV texture coordinates.

        //    // calculate texture coordinates.
        //    for (int i = 0; i < polygon.Vertices.Length; i++)
        //    {
        //        // we scaled down the level so scale up the math here.
        //        var vertex = (pr.transform.position + polygon.Vertices[i].Position) * s_Scale;

        //        Vector2 uv = new Vector2(0, 0);

        //        int dominantAxis = 0; // 0 == x, 1 == y, 2 == z

        //        // find the axis closest to the polygon's normal.
        //        float[] axes =
        //        {
        //            Mathf.Abs(polygon.Plane.normal.x),
        //            Mathf.Abs(polygon.Plane.normal.z),
        //            Mathf.Abs(polygon.Plane.normal.y)
        //        };

        //        // defaults to use x-axis.
        //        dominantAxis = 0;
        //        // check whether the y-axis is more likely.
        //        if (axes[1] > axes[dominantAxis])
        //            dominantAxis = 1;
        //        // check whether the z-axis is more likely.
        //        if (axes[2] >= axes[dominantAxis])
        //            dominantAxis = 2;

        //        // x-axis:
        //        if (dominantAxis == 0)
        //        {
        //            uv.x = vertex.z;
        //            uv.y = vertex.y;
        //        }

        //        // y-axis:
        //        if (dominantAxis == 1)
        //        {
        //            uv.x = vertex.x;
        //            uv.y = vertex.y;
        //        }

        //        // z-axis:
        //        if (dominantAxis == 2)
        //        {
        //            uv.x = vertex.x;
        //            uv.y = vertex.z;
        //        }

        //        // rotate the texture coordinates.
        //        uv = uv.Rotate(-rotation);
        //        // scale the texture coordinates.
        //        uv = uv.Divide(scale);
        //        // move the texture coordinates.
        //        uv += offset;
        //        // finally divide the result by the texture size.
        //        uv = uv.Divide(new Vector2(textureWidth, textureHeight));

        //        polygon.Vertices[i].UV = uv;
        //    }
        //}

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