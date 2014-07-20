<p align="center">
  <img src="http://fc08.deviantart.net/fs70/f/2014/200/8/9/pnetlogo_by_faikie-d7rfr8n.png" width="150" />
</p>
PNet
===
Networking middleware for Unity using Lidgren, with a custom dedicated server.


Directions
----------
This library uses Lidgren(client/server) and SlimMath(server), with some patches to Lidgren to work with Unity. An SVN patch file is included, as well as svn repo addresses. You will need to check out both of these repos, as well as patch Lidgren. Instructions are included in an additional text file.

The PNet.dll, PNetC.dll, PNetU.dll, and Lidgren.Network.dll should be copied to a Plugins folder in unity. PNet, PNetS, SlimMath, and Lidgren.Network should be referenced in the server.
When writing for Unity, reference the PNetU namespace, as it is Unity glue code to PnetC.

Notes
-----
The server works similarly to Unity, where gameobjects have components attached to them.  Networking also works similarly to unity's networking, with methods on components being marked with an RpcAttribute.

Some differences in PNetS:
  * there are no transforms. GameObjects are transforms.
  * gameobjects should not be spawned by themselves if you want to network them. Use room.NetworkInstantiate, which correctly sets up a networked gameobjects.
  * the server never exists in one 'scene', and instead runs them all at the same time as rooms. Gameobjects have a room variable that will return which room they are a part of.
  * rooms are not necessarily scenes in unity. Instead, you should define when a client should switch scenes, if it makes sense.
  * as rooms are not necessarily scenes, they additionally have no default behaviour to them. nor do they load anything in to them. You'll need to make RoomBehaviours and attach them to the room
  * components derive from Component, not from Monobehaviour. Similarly, Component will call Awake, Start, Update, and LateUpdate (as well as a few others)
  * Coroutines require that you specify if they are a 'root coroutine', for an initial call to StartCoroutine. Additional calls to StartCoroutine inside a coroutine have to be defined as being a child coroutine.
  * RPC's can be verified by the server. The NetworkInfo object that is passed into the delegate for that rpc has a variable called continueForwarding. If set to false, the rpc will not go to other clients if the rpcmode was Other or All.
  
Some differences from the Unity Network class on the client
  * the RpcAttribute requires you specify the ID of the rpc, which is a byte. It is recommended you set up a shared project on the client and server that has these IDs defined as constants, so that you actually have a name for the byte values.
  * as rpc's only get passed to the same NetworkView as them, you can overlap RPC IDs, so long as you aren't overlapping components listening to said RPCs
  * Network instantiated objects need to exist in the Resources folder on the client.
  * NetworkViews can have custom tick rates, completely independent of one another. 
  * Only the server can call NetworkInstantiate, for obvious security reasons.  Set up your own RPCs to request a spawn if clients should be able to do so
  * Scripts with Rpc marks need to be attached to the prefab on the client before it is instantiated, as that is when the attributes are found and subscribed. If you want to attach a component afterward, you can use the NetworkView.SubscribeToRPC method.  The server, however, will continue to subscribe marked methods after object instantiation, so you do not need to use NetworkView.SubscribeToRPC

Yaml serialization/deserialization for the server's Resources.Load and GameObject.Serialize is performed using this library: https://github.com/jbruening/YamlSerializer-Fork

License: 
---
PNet is distributed under the MIT license as following:

The MIT License (MIT) Copyright (c) 2012 Justin Bruening jubruening@gmail.com

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
