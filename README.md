# Le Pain
I am too tired to debug today so let me leave a screnshot here instead.[EFFECTIVE PROCRASTINATION] <br>
<img src="https://github.com/TrueRyoB/Tilemap-to-Terrain-Converter/blob/main/photos%20for%20readme/error%20april%201st.png" width="800px"><br>

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
・Improving the system so that there may exist holes inside (solved!)<br>
・Test to see if everything works as intended <-here<br>
<br>
> Tilemap Reader Phase <br>
・Create a system that controls an enumerator representing a type of terrain<br>
・Design a class to be inherited<br>
・Let the class call mesh generation function itself<br>
・End?<br>
