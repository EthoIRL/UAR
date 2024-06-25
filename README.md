# UAR
_POC_ (Proof of concept) Program that attempts to reverse recoil in fps games using [optical flow](https://en.wikipedia.org/wiki/Optical_flow).

[![Showcase Example](https://img.youtube.com/vi/kYlMhwhjni4/0.jpg)](https://youtu.be/kYlMhwhjni4) 
<br> _Movement jerk during aim-in has been since fixed_

## Pros
* No need to reverse engineer static gun recoil
* Can handle randomized static recoil patterns

## Cons
* **Only Apex Legends** seems to currently work (Low recoil + timings)
* Guns with large amount of recoil will throw off vertical correction completely
* Initial _vertical_ recoil is not recognized until after x frames
* Low poly / flat textured games will cause issues for optical flow 
* Potentially taxing on system resources depending on what cpu/gpu is present

## Mid-ground
* Moving / Strafing in game will cause offsetting this can be seen as both a pro & con

## Requirements
* Windows
* Helious with (RemoteState & Input Server modules built) or bring your own mouse input handling.
* Modern Cpu (Mid range and above from the last 5 years should suffice)
* **NVIDIA** GPU is optional (**Access to Nvidia OF module**)

## Performance
Screenshotting takes on average 0.05ms @ 600x200 resolution
- Optical flow modules
  - PyrLK: 0.8ms +/-0.4ms (Sparse, 5800x3d)
  - Nvidia 2.0: 2.0ms +/-0.2ms (Dense, 3060 Ti)
  - Nvidia 2.0: 1.5ms +/-0.1ms (Dense, 4070 Ti Super)

## Usage
If you want to use this codebase you'll need to provide a mouse controller (injection & state).

## Dependencies
* [Emgu.CV](https://github.com/emgucv/emgucv)
    * Cuda or Windows runtime
* SharpDX