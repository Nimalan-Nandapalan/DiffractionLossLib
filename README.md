[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.me/DonateToFOSS)

# DiffractionLossLib v0.1
A library to assist with difrraction of loss calculations.

The current version of the library can be downloaded [here](https://github.com/Nimalan-Nandapalan/DiffractionLossLib/releases/latest).

This library implements [ITU P.526.14](https://www.itu.int/dms_pubrec/itu-r/rec/p/R-REC-P.526-14-201801-I!!PDF-E.pdf):<br />
https://www.itu.int/dms_pubrec/itu-r/rec/p/R-REC-P.526-14-201801-I!!PDF-E.pdf

The library is provided with an Excel Add-in. Simply install it or double click it to load it into the current instance, and the `CalculateDiffractionLoss()` function will be available in the formula bar.

Some settings for the library can be configured in the `DiffractionLossLib.dll.config` file. You will most likely want to change the value for `srtmCache`. This is the directory the library looks for SRTM elevation data. The directory will be created if it does not exist, and the library should download the data if it is missing for the given coordinates. 

Please see the wiki for more detailed documentation.

If you find this software useful please consider donating to help support its maintenance.

<br />
<p align="center">
    <a href="https://www.paypal.me/DonateToFOSS"></a>
        <img src="https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif" alt="Donate" />
    </a>
</p>
<br />

## Sources:
This software makes use of the following works:
* http://www.gavaghan.org/blog/free-source-code/geodesy-library-vincentys-formula/
* https://github.com/itinero/srtm

## LICENCE
The MIT License (MIT)

Copyright (c) 2019 Nimalan Nandapalan

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.