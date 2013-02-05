PNet
====
Networking middleware for Unity using Lidgren, with a custom dedicated server.

Examples pending.


Directions
----------
This library uses Lidgren(client/server) and SlimMath(server), with some patches to Lidgren to work with Unity. SVN patch files are included, as well as svn repo addresses. You will need to check out both of these repos, as well as patch them. Instructions are included in an additional text file.

The PNet.dll, PNetU.dll, and Lidgren.Network.dll should be copied to a Plugins folder in unity. PNet, PNetS, SlimMath, and Lidgren.Network should be referenced in the server.


Notes
-----
The server works similarly to Unity, where gameobjects have components attached to them.  Networking also works similarly to unity's networking, with methods on components being marked with an RpcAttribute.

Some differences in PNetS:
  * there are no transforms. GameObjects are transforms.
  * gameobjects should not be spawned by themselves if you want to network them. Use Network.Instantiate, which correctly sets up a networked gameobjects.
  * the server never exists in one 'scene', and instead runs them all at the same time as rooms. Gameobjects have a room variable that will return which room they are a part of.
  * rooms are not necessarily scenes in unity. Instead, you should define when a client should switch scenes, if it makes sense.
  * as rooms are not necessarily scenes, they additionally have no default behaviour to them. nor do they load anything in to them. You'll need to make child classes for that.
  * components derive from Component, not from Monobehaviour. Similarly, Component will call Awake, Start, Update, and LateUpdate.
  * Coroutines require that you specify if they are a 'root coroutine', for an initial call to StartCoroutine. Additional calls to StartCoroutine inside a coroutine have to be defined as being a child coroutine.
  * RPC's can be verified by the server. The NetworkInfo object that is passed into the delegate for that rpc has a variable called continueForwarding. If set to false, the rpc will not go to other clients if the rpcmode was Other or All.
  
Some differences from the Unity Network class on the client
  * the RpcAttribute requires you specify the ID of the rpc, which is a byte. It is recommended you set up a shared project on the client and server that has these IDs defined as constants, so that you actually have a name for the byte values.
  * as rpc's only get passed to the same NetworkView as them, you can overlap RPC IDs, so long as you aren't overlapping components listening to said RPCs
  * Network instantiated objects need to exist in the Resources folder on the client.
  * NetworkViews can have custom tick rates, completely independent of one another. 
  * Only the server can call Network.Instantiate, for obvious security reasons.
  * Scripts with Rpc marks need to be attached to the prefab before it is instantiated, as that is when the attributes are found and subscribed. If you want to attach a component afterward, you can use the NetworkView.SubscribeToRPC method.
