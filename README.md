# MCCBounceEnable
A program that alters the tick rate of Halo 2 Classic in Halo:MCC to enable super bouncing.
This program utilizes memory pattern searching to ensure that any updates to Halo:MCC will not break this tool.

[Bounce Demo](https://gfycat.com/totaldarlingcirriped)

## Download
[Download](https://drive.google.com/file/d/1fn04J53QILqpXK5MP5dx_UsxU4VKA3Eh/edit) - Last Updated 7/27/2020

## Warning
This program is not meant to be used with the MCC anticheat. Please ensure that EAC is disabled by selecting the second launch option at runtime.

### Disclaimer
The following is my analysis of how and why this works the way it does. This has not been confirmed by me personally.
## About
One of the many reasons people play Halo 2 is for the nostalgia of playing the game everyone used to know and love as a kid. In the original Halo 2, the Xbox was capped at 30 frames per second. This meant that the physics engine of the game was also running at 30 ticks per second to match the frame rate. Porting the original game to PC allowed the developers to increase the frame rate and tick rate to 60. This means that the physics engine can match the increased frame rate of 60 frames per second on the Xbox One/PC. Super bounces originally worked because 30 ticks allows for a large enough gap in-between each tick so that if a player hits a surface hard enough and their player falls slightly into a crack of the mesh, the engine will detect this and attempt to "bounce" the player out of the mesh. In certain instances, this can result in the player being bounced very high distances and allows for players to reach parts of the map that aren't normally able to be reached. The increased tick rate of 60 allows for more "checks" per second and it becomes increasingly more difficult to manipulate the physics engine like you were once able to a kid. Dropping the tick rate back down to 30 allows for super bounces to be achieved on PC.

## Requirements
.NET Framework 4.6.1
