# Automated Explorer Cam

This is an Automated Explorer that will explore your scene for you. You can see it in action in [Kalmeer's videos](https://www.youtube.com/channel/UCWpDCbdbHySwZ2VshDv6VZw). It can be used to create cut scenes in which the camera navigates from one point to another through a 3D scene. It can also be used as a follow cam for a flying object in your scene. The flying object is uses this code to navigate the scene and our follow goes to work. It is also use for automated scene testing. Simply leave it running with a screen recorder and/or capturing images at every waypoint then review the video/stills in your own time.

Note, although the implementation can be made to work in internal spaces it has not been tested and we expect challenges in many such scenes. There is no pathfinding, only sensor movement, and so the current implementation is unable to identify an optimal path. However, it should be possible to use an algorithm such as A* to pathfind and then place waypoints along this path. In this situation the sensor navigation would then avoid obstacles in 3D space along the path. We would love a PR to implement this.

IMPORTANT: this code is dependent on the excellent [Sensor Toolkit](https://assetstore.unity.com/packages/tools/ai/sensor-toolkit-88036?aid=1101l866w) asset. It's cheap and excellent, we don't believe in reinventing the wheel, though we will accept PRs that remove this dependency. If you don't already have it please use the link above as it is our affiliate link, buy yourself an asset, buy us coffee. It also uses Cinemachine, but this is a free package from Unity so no worries there.

## Features

  * Camera navigates in 3D space between waypoints
  * Place Waypoints by hand
  * Randomly spawn waypoints within certain areas
  * Exclude areas from the spawn region
  * Take a photo upon waypoint arrival
  * Various waypoint selection criteria (Nearest, Random, Furthest, Weighted Interest)
  * Free flying or Follow Camera (Follow a flying creature for example)

## Installation of Latest Release

This is the easiest way of installing the code. Start with a standard Unity project and then follow the steps below. 
Note, using this method you cannot edit the sourcecode as packages are imported in read only mode. If you want to work with the code use the method described in the next section.

### Install Sensor Toolkit

  1. Purchase and Install [Sensor Toolkit](https://assetstore.unity.com/packages/tools/ai/sensor-toolkit-88036?aid=1101l866w) (affiliate link)
  2. Add an Assembly Definition file called `Assets/SensorToolkit/sensortoolkit` - the default file created will be enough, but note that more nuance will be needed if you plan to make a build. PRs welcome.

### Install Automated Explorer

  1. Select `Window -> Package Manager`
  2. Click the '+" in the top left
  3. Select 'Add package from Git URL'
  4. Paste in https://github.com/TheWizardsCode/AutomatedExplorer.git

### OPTIONAL: Install PhotoSession

If you want to automatically take photos whenever a waypoint is reached you will also need to install PhotoSession:

  1. Select `Window -> Package Manager`
  2. Click the '+" in the top left
  3. Select 'Add package from Git URL'
  4. Paste in https://github.com/TheWizardsCode/PhotoSession.git

## Installation Of Development Code

We are a big fan of enabling our users to improve the Automated Explorer Cam, so we would encourage you to use the source code, it's not much harder than using the latest release.

  1. Create a Unity project within which to do your camera dev work
  1. Install Sensor toolike as described above
  2. Add `Cinemachine` using the package manager
  3. Fork the project on GitHub by clicking the icon in the top right
  4. Clone the repo into your Unity projects Asset folder `git clone [YOUR_FORK_URL]`
  5. In the project view select `Assets/DevTest PackageManifestConfig`
  6. Optionally install PhotoSession as described above

This project is your main project and has the Unity test scenes etc. This is where you will do your testing of any major changes and ensure any pull requests you issue are correctly setup.  To make use of your local development code in another project follow the steps in the previous section, but rather than adding the package from a Git URL point to the copy on your local filesystem. You will then be able to edit the package code from within any of the projects that add the package in this way.

Always check your changes in the development scenes before issuing a PR.

## Using the Automated Explorer Explorer

If you can't figure it out after reading the below you should raise an issue on GitHub. If you want to get more involved then come and find us on [Wizards Code Discord](http://bit.ly/WizardsCodeDiscord). Note, this is open shource code. We will try to help you but there are no guarantees unless you are willing to do the necessary leg-work yourself. We are all busy, but help us to help you and all will be good. 

Pull requests to documentation after we've taken the time to help you is the best way to say thank you (we love code too!).

Select `Tools -> Wizards Code -> AI -> Add Automated Explorer Camera`. This will add the various components to your scene as well as convert your camera to a Cinemachine camera. The components added are:

### Camera Follow Target

This is the object that is moved around the scene, your camera will follow this object. You can add a model under this object (there is actually a sphere that is disabled by default as it can be helpful when debugging navigation). Attached to this object you will find:

#### Move To Waypoint (Script)

This component manages the behaviour of the follow target. All parameters have tooltips and should be reasonably self-explanatory. But here's a quick summary of some of the more interesting parts:

Strategy: this defines how the next waypoint is selected. In all cases the weight of the waypoint is used to make the final selection (see below), but the initial shortlist is created using this strategy.

Randomness: this prevents the same path being taken every time. The lower this value the more mathematical the selection criteria is. A value of 0 will result in the same path each time.

Stuck Duration and Stuck Tolerance: This defines when the target is stuck. this can happen when the target cannot find a way around an obstacle. The duration is the time the object must be stuck in place, the tolerance is the distance that can be moved to reset the stuck timer. If the target is considered stuck it will attempt to reverse out of the current position and select a new waypoint.

Take photo on arrival: If you have [Photo Session](https://github.com/TheWizardsCode/PhotoSession) (forked from [Roland09](https://github.com/Roland09)) and this setting is true then a new photo will be taken each time the target reaches a waypoint.

#### Steering Rig (Child) and WaypointSensor (Child)

These are the Sensor Toolkit components for navigation. Consult the Sensor Toolkit documentation. However, do not that the `Camera Steering Component` is an extension of their `Steering Component` the additional fields under `Height Management` are designed to provide hints on navigating in the vertical axis.

### Follow Cm vcam1

This is a standard Cinemachine Virtual Camera. To use this camera your main camera will have had a `Cinemachine Brain` added.

### Waypoint Spawner

This controls the random spawning of waypoints in your scene, if you only want predefined waypoints delete or disable this object and skip to the next section. 

Waypoints are spawned at Random within an area defined by this object. As with other components we have provided Tooltips and hope things are reasonably self-explanatory (ask if not, see above). Note that there are Gizmos that will be visible when this object is selected. A shaded blue box indicates the area within which waypoints can spawn, ensure you move this box to a suitable area and scale it using the values in the inspector (PR to make this interactable in the scene view would be awesome).

You can define obstruction layers which will prevent waypoints being spawned in areas that collide. These areas may be objects in the scene but they could also be exclusion zones if you want to avoid spawning in large areas.

In the debug section you can turn on some additional gizmos to show sample areas that can (green spheres) and cannot (red spheres) be spawned in. For large spawn areas this can slow down the editor considerably so you will want to leave these off most of the time, but they can be useful. Not the visualization is not complete, it creates a grid of sample points and tests if they are a valid spwn point. It does not test every point within the spawn area. 

#### Waypoints

By default there are no waypoints placed in the scene until runtime. However, you can add them manually if you want to. The easiest way is to grap the `Waypoint` prefab and drop it into your scene, though you can make any object a waypoint by ensuring that it is setup the same way as this prefab, paying attention to the physics aspects (rigid body, collider) and the `Way Point` script.

The `Way Point` script has the following key parameters:

Weight: this is used to influence the the liklihood of this waypoint being selected. The higher this value the more likely it is to be selected over other candidates in the shortlist (which is generated using the selection criteria defined in the `Move To Waypoint` script).

ReEnable Wait Time: this defines whether the spawn point should regenerate or not after it has been visited. A value of 0 means once visited it is removed from the scene and never returns. If there is a value > 0 then the waypoint will be disabled for this many seconds. Once re-enabled the camera may return to it.

Note: at the time of writing I'm wondering why I have a RigidBody on here, wouldn't it be better on the sensor object? Exploration and PRs welcome.


