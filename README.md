# xdelta-sharp 
**xdelta-sharp** is a port of [xdelta](https://github.com/jmacd/xdelta) from
Joshua MacDonald. The original version is written in C and updated frequently.
This is a port to C# (a .NET language) with the objetive of being compatible in
many platforms with the same assembly.
*xdelta* is a compression tool to create delta/patch files from binary files using the algorithm `VCDIFF` described in the [RFC 3284](https://tools.ietf.org/html/rfc3284).

The current version supports **descompression** *without external compression* (default settings in *xdelta*).
It has been used in the [patcher tool](https://github.com/pleonex/Ninokuni/tree/master/Programs/NinoPatcher) for Ninokuni Spanish translation.

*\.NET Core 2.1 Port, CoreRT support and NUnit to Xunit conversion are done by @jmacato*