using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RCSGQ1MapImporter : MonoBehaviour
{
    [Tooltip("If true the Textures axis from the Valve map will be used to attempt to align the textures.\n " +
        "This is kindof buggy atm and doesn't work well for angled surfaces.")]
    public bool adjustTexturesForValve = true;
}
