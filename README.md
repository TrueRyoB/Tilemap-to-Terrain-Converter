# About
This is an editor tool to efficiently create terrain for 3D platformer game for Unity by scanning through an assigned tilemap.<br><br>
<img src="https://github.com/TrueRyoB/Tilemap-to-Terrain-Converter/blob/main/photos%20for%20readme/screenshot%20march%2030th.png" width="700px"><br>

# Steps that I am following
1)Design a mechanism<br>
2)Create a mesh generation method <- here (March 31st, 2025)<br>
3)Create a tilemap reader<br>
4)Combine two together<br>

# Speficically Where I am right now
> Design Phase (solved!) <br>
・Scan through data in TileMap by checking every cells inside the bound<br>
・Alleviate the memory load by parsing it into the range containing a starting point, ending point, and a list of ranges indicating empty blocks<br>
・Create a list of column<br>
・Calculate vertices of contour of the tilemap by scanning through the list<br>
・Pass the list to the mesh generator<br>
<br>
> Mesh Generation Phase <br>
・Understanding runtime mesh generation mechansim (solved!)<br>
・Implementing a system that outputs an actual solid object given vertices (solved!)<br>
・Improving the system so that there may exist holes inside　← here<br>
<br>
> Tilemap Reader Phase <br>
・Create a system that controls an enumerator representing a type of terrain<br>
・Design a class to be inherited<br>
・Let the class call mesh generation function itself<br>
・End?<br>

# Current Strategy (reminder to myself kind of)
・Triangulating based on more than one holes is so dynamic and frustrating that would require implementing a brand-new methodology<br>
・I am not a brilliant boy<br>
・So, I deided to deploy a merge strategy - about concatenating small pieces of mesh together that were cut off about the mid point of the holes for n itmes<br>
