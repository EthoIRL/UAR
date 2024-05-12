# UAR
Windows program that attempts to reverse recoil in fps games using [optical flow](https://en.wikipedia.org/wiki/Optical_flow).

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

## Performance
Screenshotting takes on average 0.05ms @ 600x200 resolution
Optical flow modules
  - PyrLK: 0.8ms +/-0.4ms (Sparse, 5800x3d)
  - Nvidia 2.0: 2.0ms +/-0.2ms (Dense, 3060 Ti)

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