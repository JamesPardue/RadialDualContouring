# Radial Dual Contouring

Abstract

RadialDC (Radial Dual Contouring) is a method of large scale world mesh generation.
Like Dual Contouring it divides space up into voxels and uses density functions to determine, place, and connect vertexes of the world mesh.
See https://www.cs.rice.edu/~jwarren/papers/dualcontour.pdf for more details on dual contouring.
RadialDC implements all the primary features of Dual Contouring but extends the algoririthm in two ways: Dynamic Voxel Sizing and Dynamic Voxel Density Evaluation.

Dynamic Voxel Sizing

One of the limits of Dual Contouring are the size and detail of the world are limited by the time it taxes to process and individual voxel can limit the size and/or detail of the world.
Dynamic Voxel Sizing allows for faster, more effcient, mesh creation by chaing the size of the voxel being evaluated based on its distance from the player, thereby speeding things up by not evaluating unnessary details.
This technique is called Radial DC because in order to ensure a crack free mesh over changing voxel sizes, the voxel is generated outwards from the detailed center of the mesh, and uses a set of rules to ensure no cracks or duplication of work.

Dynamic Voxel Density Evaluation

Dynamic Voxel Density Evaluation exitends this further by changing the density evaluation equations used depending on the detail level assigned to the voxel.
For example a high detail assigned voxel, closer to the player, may have several levels of evaluations such a multiple octaves of simplex noise, while voxel farther away may be assigned a lower lelve of detail and simpler evaluation function.

Core and Outer Voxels

In order to best faciliate large detailed worlds the code is divided into the generation of Core Voxels, which make up several tiers of the highest detial, and Outer Voxels, which more expand the size of the voxel in each layers as they move away to the player until theh hit a preset limit.
For both Core and Outer Voxels, the process is split into 3 sections: (1) the creation of the voxels and there positions, (2) evaluation of voxel vertecies, (3) using voxel verticies to generate mesh data (including triangle arrays).
This process is repeated for both COre and Outer Voxels.

Setup

A setup set is done to set the smallest voxel sixe, the size of the detail tiers, and other specifications for the world mesh.
These are currently coded into the system, but could be set at any time before generation.
Addtionally another step is used to set all the flags used for the control flow used in the multi-threading execution.

Multi-Threading

In order to obtain the maximum amount of speed, as well as allow for a seemless transition for the player as new world meshes are generated, the code has been multi-threaded.
Of these steps (1) and (3) require a single core to generate data arrays, and are merely run on a seperate core which does not faciliate the players current state.
Step (2) is run in full parallel, with each voxel evaualted as a seperate thread and then spread across as many cores in parallel as the system is capable.
Because the Unity Engine used to run this particular version of the code requires a main thread to continually run and only check on the other threads, the code was written in a 'flow gate' style.
This means that the main thread runs the functions to check and, if possible, advance the process to next stage once every frame.
Finally a thread is created to generate the mesh using the various mesh data arrays (position, normal, uv, triangles) and pointer to the new mesh is returned along with a boolean statign whether the all tasks have been complete.

Mesh Regeneration

To keep players in the high detail area, a new world mesh is generated whenever a player is over half the distance from the center of the high detail area to it's edge.
When a new world mesh is generated, a new center is chosen closest to the player. Because this is done off the main thread and before the player can be away from the high detail areas, the player experiences no loading times or interruption in the detail of the world.
This mesh regeneration is displayed in the testing code, which allows for simple movement in the world and seemless rengeration around the player.

Recipe Blocks

Recipe Blocks are an abstraction which allows for design of the world using density functions.
For example density function could be 'painted', or more effciently such 'painting' movements could be recorded and stored in reciple block units. These units are then drawn from for the density evualtion.
For now, only simple Reciple Blocks are generated, but the system could be extended to allow much deeper levels of semi or fully procedural world design.

Results

Currently this system allows for generating a roughly 4 mile cube in 1-2 seconds.
This should be sufficient for many types of play experiences.


