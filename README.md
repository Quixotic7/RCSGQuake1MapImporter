# RCSGQuake1MapImporter
A addon to RealtimeCSG to import Quake1 .map files from Trenchbroom into RealtimeCSG.

It supports both Valve 220 and Quake Standard formats. 

Imported based on [SabreCSG's Quake importer](https://github.com/sabresaurus/SabreCSG/tree/master/Scripts/Importers/Quake1). 

It's currently very simple and will import every brush, but not entities. Triggers will be ignored if the whole brush is a trigger. 

If there is a material in the project with the same name as a texture on a brush surface, that material will be applied on import.

There are some problems with texture orientations, mostly on angled surfaces. Surfaces that are aligned with the world axises are usually fine. Valve format has more problems than Quake. If you can perfect this, please submit a pull request. 

Assuming the material's textures have same dimensions as in Trenchbroom, the texture scale will be correct, and the offset is usually correct for on axis surfaces. 

Complex brushes with many sides may have troubles being used with RealtimeCSG. These brushes will be skipped. If you notice some missing brushes, try clipping them in to smaller brushes so they have less sides and reimport. 

## Requirements

You will need RealtimeCSG to use this addon. You can grab the latest version here. https://github.com/LogicalError/realtime-CSG-for-unity/

I used Unity 2019.4.19f1 but this should work fine in older versions of Unity and any version of 2019, can't confirm it will work with Unity 2020 and greater. 

## How to import textures

Export your textures from your wads using TexMex. Import them to a folder in Unity. 

Select "Tools -> CreateMaterialsForTextures" in Unity. 

Select all your textures, then click the "Create" button. A new folder of materials will be added. 

I recommend changing texture filtering to "Point (no filter)" for that crisp, pixelated look. 

## How to import a map

Create a new empty game object in your scene. 

Set it's world position to (0,0,0) or else texture offsets will be wrong. 

Add the script "RCSGQ1MapImporter" to the gameobject. 

Press the button "Import Map"

Wait.

Enjoy your map inside of Unity!

## Version History

**20210228** 

	-	Improved Valve Texture orientation, still not perfect, but improved. 
	-	Imported map now organized into Trenchbroom layers and groups. 
	-	Entities are placed in their own GameObject with the class name of the entity. 
	-	Models for Trigger entities will be set to use triggers and won't be rendered. 

**20210227** - Initial release of the addon. 

