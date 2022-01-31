This one doesn't require any special setup. However, this document is useful in that it describes some of the most basic configuration options you have available to you. It is not an exhautive list of configuration options. Others are introduced in more complex examples linked from the [Documentation Readme](README.md).

You can see this setup [in action](https://www.youtube.com/watch?v=94KWLLI9S3s). 

# Basic Setup

Select `Tools -> Wizards Code -> AI -> Add Automated Explorer Camera`. This will add the various components to your scene as well as convert your camera to a Cinemachine camera. 

# Camera Follow Target

This is the object that is moved around the scene, your camera will follow this object. You can add a model under this object (there is actually a sphere that is disabled by default as it can be helpful when debugging navigation). Attached to this object you will find:

## Move To Waypoint (Script)

This component manages the behaviour of the follow target. All parameters should have tooltips and should be reasonably self-explanatory. But here's a quick summary of some of the more interesting parts:

### Sensors

The sensors section describes how the body senses waypoints that will define its flightpath:

  * Waypoint Sensor - the [Sensor Tookit](https://bit.ly/SensorTookit) provided Waypoint Sensor, see below.
  * Waypont Arrival Sensor - the [Sensor Tookit](https://bit.ly/SensorTookit) provided Waypoint Arrival Sensor, see below.
  * Waypoint Prefab - the waypoint prefab that sensors will be trying to detect
  * Strategy - defines how the next candidate waypoints are selected. Note that waypoints can have a `Next Waypoint` defined. If it is defined it will take precedent over the list created by this strategy. Note also that the weight of candidate waypoints is used to make the final selection (see below), but the initial shortlist is created using this strategy. Strategies include `Furhest` (select the furthest detected waypoint), "Nearest" (select the nearest detected waypoint), "Random" select a random waypoint from the list detected.
  * Randomness - this prevents the same path being taken every time. The lower this value the more mathematicaly certain the selection criteria is. A value of 0 will result in the same path each time, that is no randomness.
  

### Steering

  * Steering Rig - the steering Rig used to steer this body (see below)
  * Stuck Duration and Stuck Tolerance: This defines when the target is stuck. this can happen when the target cannot find a way around an obstacle. The duration is the time the object must be stuck in place before an alternative path is sought. The tolerance is the distance that can be moved to reset the stuck timer. If the target is considered stuck it will attempt to reverse out of the current position and select a new waypoint.
  * Face Towards Target - if set to true then the body will turn to face the current target waypoint

### Other

  * Take photo on arrival: If you have [Photo Session](https://github.com/TheWizardsCode/PhotoSession) and this setting is true then a new photo will be taken each time the target reaches a waypoint. Note if this is set to true but the Phot Session code is not present a warning will be displayed in the console. 
  * Randomize Starting Position - if true the body will select a random waypoint as its starting position. However, these waypoints must be present in the scene on start, spawned waypoints will not be considered. If no waypoint is found then no change will be made to the start position.

## Steering Rig (Child)

The `Camera Steering Rig` is an extension of their `Steering Rig` provided by [Sensor Toolkit](https://bit.ly/SensorTookit) which is required for this code to work.

The critical parameters for this use case are described below, consult the Sensor Toolkit documentation for information on other parameters:

  * Turn Force - (think "turning speed") the maximum torgque that can be applied to the rigid body, for most use cases a value between 0 and 1 seems to work well but you may get different results.
  * Move Force - (think "forward speed") The maximum forward force that can be applied to the rigid body. What this should be set to depends alot on the rigid bodies mass and drag.
  * Strafe Force - (think "horizontal/backward speed") the maximum sideways or backwards force that can be applied to the rigid body. What this should be set to depends alot on the rigid bodies mass and drag.

NOT USED - the following parameters are (at the time of writing) still exposed in the inspector but they are not to be set here, the description below explains how to set them.

  * Destination Transform - This will be set by the `Move To Target` component on the root object.
  * Face Forward Transform - This will be set according to the `Face Toward Target` setting on the `Move To Target` component on the root object.

ADDITIONAL PARAMETERS the following are parameters added by this project that influence the steering behaviour.

### Flight Randomization

You can optionally added random forces to the object to create variations in the flight. This can make a flightpath look more organic.

  * RandomizeX - select whether you want to randomize in the horizontal direction or not
  * RandomizeY - select whether you want to randomize in the vertical direction or not
  * RandomizeZ - select whether you want to randomize in the forward direction or not

If any of these paramaters are set to try you will be able to set the follwoing parameters:

  * Randomization Frequency - how many seconds should pass between changes in the randomization forces
  * Randomization Factor - the % of the appropriate force (`MoveForce`, `StrafeForce`) that can be applied to the body. Note that the force can be applied in either direction but that it will always center around a `Vector3.zero` force to prevent it from veering off course. 

### Height Management

If the `Maintain Height` property is set to true this section controls how height is managed for this body. Note that Height is also managed by the Sensor Toolkit sensor array. The difference is that this section manages a preferred height while the sensor array ensures the body does not hit an obstacle.

  * Min Height - the minimum height that the body can normally fly at. If the sensor array has a sensor that defines a greater minimum height it will take precedence over this setting.
  * Max Height - The maximum height this body is normally allowed to fly at.
  * Optimal Height - the height that this body will prefer to fly at if possible. The body will tend towards this height unless there is an obstacle or waypoint that required it move to a different height.

## WaypointSensor (Child)

The `Waypoint Sensor` is provided by [Sensor Toolkit](https://bit.ly/SensorTookit) which is required for this code to work. See the Sensor Toolkit for complete documentation here are some of the essential parameters for this use case:

  * `Sensor Range` the radius, in world units, within which waypoints will be detected.

# Follow CmM VCam1

This is a standard Cinemachine Virtual Camera. To use this camera your main camera will have had a `Cinemachine Brain` added. You can replace this with any Cinemachine camera you want.

# Waypoint Spawner

This controls the random spawning of waypoints in your scene, if you only want predefined waypoints delete or disable this object and skip to the next section. 

Waypoints are spawned at Random within an area defined by this object. As with other components we have provided Tooltips and hope things are reasonably self-explanatory (ask if not, see above). Note that there are Gizmos that will be visible when this object is selected. A shaded blue box indicates the area within which waypoints can spawn, ensure you move this box to a suitable area and scale it using the values in the inspector (PR to make this interactable in the scene view would be awesome).

You can define obstruction layers which will prevent waypoints being spawned in areas that collide. These areas may be objects in the scene but they could also be exclusion zones if you want to avoid spawning in large areas.

In the debug section you can turn on some additional gizmos to show sample areas that can (green spheres) and cannot (red spheres) be spawned in. For large spawn areas this can slow down the editor considerably so you will want to leave these off most of the time, but they can be useful. Not the visualization is not complete, it creates a grid of sample points and tests if they are a valid spwn point. It does not test every point within the spawn area. 

## Waypoints

By default there are no waypoints placed in the scene until runtime. However, you can add them manually if you want to. The easiest way is to grap the `Waypoint` prefab and drop it into your scene, though you can make any object a waypoint by ensuring that it is setup the same way as this prefab, paying attention to the physics aspects (rigid body, collider) and the `Way Point` script.

The `Way Point` script has the following key parameters:

Weight: this is used to influence the the liklihood of this waypoint being selected. The higher this value the more likely it is to be selected over other candidates in the shortlist (which is generated using the selection criteria defined in the `Move To Waypoint` script).

ReEnable Wait Time: this defines whether the spawn point should regenerate or not after it has been visited. A value of 0 means once visited it is removed from the scene and never returns. If there is a value > 0 then the waypoint will be disabled for this many seconds. Once re-enabled the camera may return to it.

Note: at the time of writing I'm wondering why I have a RigidBody on here, wouldn't it be better on the sensor object? Exploration and PRs welcome.
