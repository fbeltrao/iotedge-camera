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
[  ] Sample application allowing to control the camera from anywhere in the internet

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
  - Select your IoT Edge device in IoT Hub
  - Click on Module
