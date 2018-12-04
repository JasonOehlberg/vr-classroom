      
# VR Classroom
A classroom simulation VR simulation built with Unity

![VR Classroom](https://github.com/JasonOehlberg/vr-classroom/blob/master/ClassroomStill4.jpg)
## Overview
The VR Classroom is an ongoing project for the creation of a semi-realistic classroom environment. The project's aim is to create classroom behavioral scenerios for use by the College of Education at Northeastern State University. This project lays the groundwork for future improvements and scenerios to be created with the purpose of being utilized by students pursuing degrees in education.The simulation is built with [Unity](https://unity3d.com/), a leading gaming engine and [Oculus Rift](https://www.oculus.com/rift/), a leader in virtual reality hardware technology. VR Classroom was created in conjunction with a capstone project by Jason Oehlberg pursuing a bachelor's degree in Computer Science at Northeastern State University.

## Getting Started

- Download and Install [Unity](https://unity3d.com/get-unity/download) version: **2018.2012f1** or later.
- Install these asset libraries from the [Unity Assset Store](https://www.assetstore.unity3d.com/)
   - [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022)
   - [School Classroom Pack](https://assetstore.unity.com/packages/3d/props/interior/school-classroom-pack-116794)
   - [LipSync Pro](https://assetstore.unity.com/packages/tools/animation/lipsync-pro-32117)
- Clone this repository or download zip file
```
git clone https://github.com/JasonOehlberg/vr-classroom.git
```
- Follow the steps in this [video](https://www.youtube.com/watch?v=sxvKGVDmYfY) closely for Oculus Integration
- The code for the voice recognition is located in the [StudentBehavoir.cs](https://github.com/JasonOehlberg/vr-classroom/blob/master/Assets/MyClassroom/Scripts/StudentBehavior.cs) script
- Review the documentation for [UnityEngine.Windows.Speech](https://docs.microsoft.com/en-us/windows/mixed-reality/voice-input-in-unity)
- For easy use of git and GitHub download and install the [Github for Unity](https://assetstore.unity.com/packages/tools/version-control/github-for-unity-118069) asset library.

## Description
The simulation takes place in a common classroom environment. The classroom consists of several desks occupied by *Students*. The user steps into the environment using the virtual reality headset and hand-held controllers. Most of the interaction in the environment is controlled by voice recognition. The Unity Engine exposes the library for Windows Speech allowing for keyword recognition.

The simulation begins with the *Students* working quietly at their desks. Each of the individual *Student's* names are displayed in red above their heads. When the keyword "Attendance" is announced by the user each of the *Students* stops working, sits attentively at their desks and their name is displayed in green. As the user calls the names of each of the *Students*, the *Student* raises their hand, the name returns to the default red color and they go back to working quietly. When the name of a *Student* is called, outside of the attendance scenerio, the *Student* again sits at attention and the name color appears in blue.
> *For a more detailed description of the project, please refer to the video links below*
___
[Oculus VR Walkthrough](https://www.youtube.com/watch?v=X7hNp3HNgV8)
___
[Walkthrough without VR Hardware](https://www.youtube.com/watch?v=E3SAvxoFuv0)
___
[Test Scene](https://www.youtube.com/watch?v=bITyW3xjzXQ) Using Alex for future project integration
___

## Development
The project was developed using [Unity](https://unity3d.com/) (gaming engine), [Oculus Rift](https://www.oculus.com/rift/) (VR hardware device), [autoDesk Character Generator](https://charactergenerator.autodesk.com/) (3d model generator) and [Mixamo](https://www.mixamo.com/) (online animation software). 
Three asset packages, aquired from the [Untiy Asset Store](https://assetstore.unity.com/) are also involved in the development of the project.
1. [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022) -- Advanced rendering, social, platform, audio, and Avatars support for Oculus VR devices and some Open VR supported devices. **FREE**
2. [School Classroom Pack](https://assetstore.unity.com/packages/3d/props/interior/school-classroom-pack-116794) -- This set includes classrooms, blackboards, curtains, clocks, desks, and corridors, as well as boxed lunch, chalks, bags, cleaning tools, buckets, fried noodle sandwiches, love letters and supernatural fiery balls. **PAID** 
3. [LipSync Pro](https://assetstore.unity.com/packages/tools/animation/lipsync-pro-32117) -- LipSync Pro is an editor extension for creating high-quality lipsyncing and facial animation inside Unity. **PAID**
> *Has not been integrated into main classroom scene as of build 2*
### Technologies Used
- **Unity** -- Used for building 3d environment, script integration, game object manipulation, and building scenes.
- **Oculus Rift** --Hardware used for virtual reality integration in conjunction with Unity.
- **AutoDesk Character Creator** -- Used for generating all the 3d humanoid models found in the project. Each character generated came with skeleton and blend shapes for easy manipulation and animation.
- **Mixamo** -- Used for basic character animations.
- **Windows Speech** -- Used for voice recognition
### Helpful links
> - **Holistic3D**
>     - [YouTube Channel](https://www.youtube.com/channel/UCp_SOgsRYdLfIEWLjM62ZJg)
>     - [Mixamo & Unity](https://www.youtube.com/watch?v=BEIaakl9vJE)
>     - [Fuse + Mixamo + Unity Workflow](https://www.youtube.com/watch?v=uC_ruUS_xRQ)
>     - [Finite State Machines](https://www.youtube.com/watch?v=NEvdyefORBo)
>-  **Maya** -- Free software for students
>     - [Link](https://www.autodesk.com/education/free-software/featured)
> - **Rogo Digital LipSync Pro Documentation**
>     - [Link](https://lipsync.rogodigital.com/)

## Contributing
Northeastern State University Computer Science students are welcome fork and contribute to the project benefitting the College of Education. If you would like to contribute as research or as a capstone project please contact your mentor.

## Note
This repository does not include the Oculus integration needed to complete the VR Classroom simulation. Because this repository will be available to NSU students wanting to continue the project, modification can be made without Oculus integration. For further information on Oculus Rift integration please refer to the [documentation](https://developer.oculus.com/documentation/) or review this YouTube [video](https://www.youtube.com/watch?v=sxvKGVDmYfY) for a quick tutorial.
