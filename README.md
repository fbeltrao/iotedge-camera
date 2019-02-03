# IoT Edge Camera module for Rapberry PI

This repository contains a sample IoT Edge module allowing you to control a Raspberry camera remotely. The goal is to be able to quickly assemble required components (RaspberryPI + Pi Camera) and be able to take pictures, videos and timelapses remotely.

## Features list

The list below shows what is current available and what is planned for this solution. If you have a suggest please open an issue.

[x] Take still pictures
[ ] Expose additional options when taking still pictures
[x] Record timelapses
[ ] Expose additional options when taking timelapses
[ ] Record videos
[x] Local web application allowing control of the camera
[x] Local web application showing currently available pictures
[x] Integration with Azure Storage
[ ] Integration with IoT Edge Azure Storage
[ ] Demonstrate how to leverage Azure DevOps to build a CI pipeline running integration tests against the deployed module
[  ] Sample application allowing to control the camera from Internet

## Requirements

Even though the solution requires you to have an Azure IoT Hub, you can start with a [free version](https://azure.microsoft.com/en-us/free/).

- An Azure Account, you can start [for free](https://azure.microsoft.com/en-us/free/)
- An [Azure IoT Hub](https://docs.microsoft.com/en-us/azure/iot-fundamentals/)
- Optionally, an [Azure Storage](https://docs.microsoft.com/en-us/azure/storage/common/storage-introduction) account, to store photos taken by the Pi in the cloud
- [A Raspberry PI](https://www.raspberrypi.org/products/raspberry-pi-3-model-b-plus/)
- [A Raspberry Camera V2](https://www.raspberrypi.org/products/camera-module-v2/)

## Quick Setup

In order to give the sample application a try follow the steps below:

- [Install Raspian on your Raspberry PI](https://thepi.io/how-to-install-raspbian-on-the-raspberry-pi/)
  - [With ssh enabled](https://hackernoon.com/raspberry-pi-headless-install-462ccabd75d0)
  - [Preconfigured to use your wifi](https://styxit.com/2017/03/14/headless-raspberry-setup.html)
- [Connect the camera to your PI and ensure it is enabled](https://thepihut.com/blogs/raspberry-pi-tutorials/16021420-how-to-install-use-the-raspberry-pi-camera)
- [Install Azure IoT Edge on your Raspberry PI](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge-linux-arm)

- Install the camera module to your IoT Edge using the [Azure Portal](https://portal.azure.com/)
  - Navigate to your IoT Hub
  - Select "IoT Edge"
  - Select your IoT Edge used in your Pi in the device list
  - Click on "Set Modules"
  - On Deployment Modules click "Add" then "IoT Edge Module"
    - Name: camera
    - Image URI: fbeltrao/iotedge-camera:0.1-arm32v7
    - Container create options:
```json
{
  "ExposedPorts": {
    "80/tcp": {}
  },
  "Mounts": {
    "Type": "bind",
    "Source": "/home/pi/cameraoutput",
    "Destination": "/cameraoutput",
    "Mode": "",
    "RW": true,
    "Propagation": "rprivate"
  },
  "HostConfig": {
    "Privileged": true,
    "PortBindings": {
      "80/tcp": [
        {
          "HostPort": "5003"
        }
      ]
    },
    "Binds": [
      "/home/pi/cameraoutput:/cameraoutput"
    ]
  }
}
```
  - Click Save, Next (twice) and finally Submit. After a few seconds the module should be running on your Pi.

## Running using VsCode

If builiding IoT Edge solutions is not 

## Remarks

- Taking pictures is using the [MMAL Sharp package](https://github.com/techyian/MMALSharp)
- Thumbnail creation is using package [ImageSharp](https://github.com/SixLabors/ImageSharp)
- The [Mediatr](https://github.com/jbogard/MediatR) package is used to create a clean separation of concerns (actions, notifications, outside interfaces). It ultimately allows same code to be execute from IoT Hub module direct method calls or web api REST calls. Some required pumpling code found in this repository can help you quick start in adopting it in new or existing IoT Edge modules.