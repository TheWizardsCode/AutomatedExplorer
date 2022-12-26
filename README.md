# Automated Explorer Cam

This is an Automated Explorer that will explore your scene for you. You can see it in action in [Kalmeer's videos](https://www.youtube.com/channel/UCWpDCbdbHySwZ2VshDv6VZw). It can be used to create cut scenes in which the camera navigates from one point to another through a 3D scene. It can also be used as a follow cam for a flying object in your scene. The flying object is uses this code to navigate the scene and our follow goes to work. It is also use for automated scene testing. Simply leave it running with a screen recorder and/or capturing images at every waypoint then review the video/stills in your own time.

Note, although the implementation can be made to work in internal spaces it has not been tested and we expect challenges in many such scenes. There is no pathfinding, only sensor movement, and so the current implementation is unable to identify an optimal path. However, it should be possible to use an algorithm such as A* to pathfind and then place waypoints along this path. In this situation the sensor navigation would then avoid obstacles in 3D space along the path. We would love a PR to implement this.

## Features

  * Camera navigates in 3D space between waypoints
  * Place Waypoints by hand or using the included spawner
  * Exclude areas from the spawn region
  * Take a photo upon waypoint arrival
  * Various waypoint selection criteria (e.g. Nearest, Random, Furthest, Weighted Interest, Next Waypoint)
  * Use any Cinemachine Camera (Follow a flying creature for example)
  * Animator parameters to drive a flight animation controller

## Installation of Latest Release

This is the easiest way of installing the code. Start with a standard Unity project and then follow the steps below. Note, using this method you cannot edit the sourcecode as packages are imported in read only mode. If you want to work with the code use the method described in the next section.

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

### Configure your installation

This setup will work out of the box, but there are a great many options that allow you to customize behaviour. Take a look at our [examples](Documentation/README.md) to get started.

## Installation Of Development Code

We are a big fan of enabling our users to improve the Automated Explorer Cam, so we would encourage you to use the source code, it's not much harder than using the latest release.

  1. Create a Unity project within which to do your camera dev work
  2. Add `Cinemachine` using the package manager
  3. Fork the project on GitHub by clicking the icon in the top right
  4. Clone the repo into your Unity projects Asset folder `git clone [YOUR_FORK_URL]`
  5. Optionally install PhotoSession as described above

This project is your main project and has the Unity test scenes etc. This is where you will do your testing of any major changes and ensure any pull requests you issue are correctly setup.  To make use of your local development code in another project follow the steps in the previous section, but rather than adding the package from a Git URL point to the copy on your local filesystem. You will then be able to edit the package code from within any of the projects that add the package in this way.

Always check your changes in the development scenes before issuing a PR.

## Using the Automated Explorer Explorer

If you can't figure it out after reading the below you should raise an issue on GitHub. If you want to get more involved then come and find us on [Wizards Code Discord](http://bit.ly/WizardsCodeDiscord). Note, this is open shource code. We will try to help you but there are no guarantees unless you are willing to do the necessary leg-work yourself. We are all busy, but help us to help you and all will be good. 

Pull requests to documentation after we've taken the time to help you is the best way to say thank you (we love code too!).

Select `Tools -> Wizards Code -> AI -> Add Automated Explorer Camera`. This will add the various components to your scene as well as convert your camera to a Cinemachine camera. The components added are:

This setup will work out of the box, but there are a great many options that allow you to customize behaviour. Take a look at our [examples](Documentation/README.md) to get started.

