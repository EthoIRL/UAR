# UAR
Windows program that attempts to reverse recoil in fps games using computer vision optical flow.

## Pros
* No need to input individual gun recoil settings manually
* Works on some games* (cs2 does not work)

## Cons
* Guns with large amount of recoil will throw off vertical correction
* Taxing on system resources depending on what cpu/gpu is present
* Initial vertical recoil is not recognized until after x frames
* Low poly / flat textured games will cause issues for optical flow 

## Mid-ground
* Moving / Strafing in game will cause offsetting this can be seen as both a pro & con

## Requirements
* Windows
* Helious with (RemoteState & Input Server modules built) or bring your own mouse input handling.
* Modern Cpu (Mid range and above from the last 5 years should suffice)
* **NVIDIA** GPU is optional (**Does not benefit performance**)

## Best games
- [x] Apex Legends
- [x] Roblox (Some games)
- [ ] BattleBit Remastered (Flat textures)
- [ ] CS2 (Hipfire recoil)
- [ ] Rainbow Six Siege (Recoil too heavy)

## Dependencies
* [Emgu.CV](https://github.com/emgucv/emgucv)
    * Cuda or Windows runtime (both work)
* SharpDX