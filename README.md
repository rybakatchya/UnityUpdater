# UnityUpdater
 
Simple automatic updater for asset bundles and unity. Works on any platform. Client will only download bundles that have actually changed.

#Usage
Install as a unity package through the package manager.

Open Window> Unity Updater in unity3d. Set version to 1, press "build bundles"

Attach UpdateManager to any gameobject and press play. Your asset bundles will automatically be updated any time you change one.

Copy directory is used to automatically copy the output of built asset bundles to supplied path. This is great if you're running a local webserver to test on.

