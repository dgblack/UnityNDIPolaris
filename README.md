# UnityNDIPolaris
Unity integration for NDI Polaris IR tracker

Simply drag and drop NDIPolaris folder into Unity's Assets to import it.

1. Add NDIPolarisStreamer script to a game object  
2. Enter correct COM port and path to a SROM file (geometric definition of the rigid body to be tracked)  
3. PublishedTransform contains the measured pose returned by the NDI Polaris device. The NDIToUnity matrix can transform this pose to a desired coordinate system depending on where in space the tracker is.

For generating a SROM file, use NDI 6dArchitect. The NDI Capture, Configure, and Track are also useful for getting set up. These are available on support.ndi.com.
