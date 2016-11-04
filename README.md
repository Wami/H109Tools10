# H109Tools10
It's a pity that hubsan has not provided a way change such important parameters of the drone as home return altitude.

So after hours of fiddling around hubsan flash tool I found a way to change parameters of the drone.
With released program you can change:

    altitude limit (0 to disable)
    distance limit (0 to disable)
    home return altitude (SafeAltitude)
    max nav speed (speed in GPS mode and home return speed) (more than 5m/s will likely cause a crash)
    warning voltage
    landing voltage (it's reasonable not to make it too low)

The program was checked on h109s low edition but may work on h109s high and h501s.
Its is buggy but sometimes does what it should do. Anyone can fork it it and make it better.

IMPORTANT WARNING!
All responsibility for potential crashes, damages and so on lies on you.
There are no checks and tests so any change can lead to crash.
It can be illegal to alter parameters in some countries.

How to:

    connect (or reconnect) the drone
    launch the app
    wait until it shown "connected"
    press "Get params"
    wait until it shown "connected"
    press "Edit params"
    if nothing happened repeat from step 4
    change parameters
    press "Save"
    wait somewhat near 10 seconds
    repeat 1-7 and check that everything is saved.
    make a test flight in a safe area!!!

