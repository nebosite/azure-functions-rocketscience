# Simple Http Trigger Example

This shows a very simple example of how to hook up the RocketScience Azure functions framework.

Projects:
* TriggerExample - This is an azure function project.  Not much code will go here, just the endpoints plus calls into RocketScience.  
* ServiceLibrary - Most of your code will be here.  There are a few advantages to having a separate library:
** Escape from DLL (nuget) Hell.   The Azure functions library has a lot of weird deep dependencies.  Another library insulates us from that somewhat.
** Better separation of concerns.  The library focuses on functionality and RocketScience focuses on making the service behave well.
