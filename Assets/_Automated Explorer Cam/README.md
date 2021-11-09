The Automated Explorer Camera is an AI driven camera that will explore a scene. It is ideal for providing previews of a scene.

This system required the Sensor Toolkit fromt the asset store: https://bit.ly/SensorTookit (affiliate link)

# Adding to your Scene

Your scene must have a Camera tagged as `mainCamera`. If your scene has a terrain then the system is mostly setup automatically. However, if you are using a mesh terrain or world you will have some additional setup. Even with a terrain you might want to review the settings as they may not be optimal for your world.

To create an initial setup select `Tools/Wizards Code/AI/Add Automated Explorer Camera`. This will add the system in a default configuration. The next section describes the components used and the main configuration options, although there are more options for you to explore.

## Components

### Main Camera and Follow Camera

The `Main Camera` will have a Cinemachine Brain added if it doesn't already have one. For configuration options see the [Cinemachine documentation](https://unity.com/unity/features/editor/art-and-design/cinemachine).

`Follow CM vcam1` is a Cinemachine Virtual Camera set up to follow the `Camera Follow Target`. The initial configuration is ideal for many scenarios, however you have the full power of [Cinemachine](https://unity.com/unity/features/editor/art-and-design/cinemachine) at your disposal.

### Waypoints

Waypoints are places that the camera can visit. You can place them manually or randomly using a spawner.

`Waypoint Spawner` will spawn waypoints randomly within a box area. By default the waypoints will be spawned slightly higher than the value of `Clearance Radius` parameter above the terrain (or the origin if using a mesh). The `Clearance Radius` is also used to ensure the waypoint is not too close to other objects in the scene. You can add child objects with colliders to prevent items spawning in defined areas.

You can add additional spawners manually if you want to do so.

`Waypoint` these are spawned at runtime by the `Waypoint Spawner`, but you can also add them manually if you want to encourage the camera to go to certain areas. If you want to be sure that they camera will visit them then set the weight to 0. Note that anything below 1, but close to it, will add randomness to the selection. The further from o the more randomness will be used.

### Camera Follow Target

`Camera Follow Target` is the item that will actually navigate the scene. By default this is not visible though there is a red sphere that can be used for debugging purposes. If you would like the camera to follow something add the model as a child of this object. The camera will select a waypoint using the strategy defined in the Follow Target component.

You can configure the speed of the movement in the `SphericalSteeringRig` which is a child of the `Cmaera Follow Target`. There are many other configuration options in the various components. Take a nosy around and, if you need help, join us on discord at http://bit.ly/WizardsCodeDiscord


