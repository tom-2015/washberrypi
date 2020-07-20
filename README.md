# WashberryPi
The main controller board of our washing machine was broken so I decided to do something stupid: create a new controller board with a Raspberry Pi on it!
It turned out not so easy as thought, especially starting centrifuge without having unbalance making the machine to start walking proved a challenge.
After a few months testing and designing new PCB it was finally working again. It's not yet fully completed but if anyone else ever wants to do something this crazy I decided to put everything open source on github.

# Requirements
It's designed for a Samsung co bubble with DC motor but with some adjustments in software and PCB it will probably work with other machines that have a DC motor.
You'll need some skills for PCB design in Eagle, programming Raspberry Pi (Visual Basic .NET) and microcontrollers (Microchip PIC) in C.

# Contens
The repository contains 4 projects:
* Washmachine.sln: the main Visual Studio VB .NET project with software that runs on the Raspberry. This project is the main program of the washing machine. It controls all the components and provides a web interface.
* eagle directory: contains the Eagle files and Gerber files for the PCB that holds the Raspberry Pi Zero W, PIC microcontroller, relays and motor control components.
* pic directory: contains washing_machine.__c which is a Source Boost project containing C-code for the microcontroller that drives the TRIAC for motor.
* www directory: this is the web interface main directory.

# TIPS
Some tips if you want to make your washing machine Raspberry Pi controlled:
* be careful when applying power to the motor, I accidently applied power too fast resulting in a broken bearing! When testing the motor circuit add some resistors in series with the motor.
* If I need to redo the project I would consider using PWM with an IGBT or MOSFET to drive the motor and not using the original Samsung design with a TRIAC. This is because I think it's easier using PWM which in theory could be connected directly to the Pi instead of having a different microcontroller for it.
* Use a Raspberry Pi (2,3,4) with quad core instead of the single core Raspberry Pi Zero W. I needed to use an accelerometer to capture the drum vibrations and this required a thread to read 400 samples a second over I²C. This single core is often interrupted doing other stuff making the accelerometer buffer overflow.