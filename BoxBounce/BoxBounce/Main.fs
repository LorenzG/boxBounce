namespace BoxBounce
open GosLib
module rs = GosLib.RhinoScriptSyntax

type NewPlugIn () =     
    inherit Rhino.PlugIns.PlugIn()
    static member val Instance = NewPlugIn()
    // Singelton: http://stackoverflow.com/questions/2691565/how-to-implement-singleton-pattern-syntax
    // Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    // class. DO NOT create instances of this class yourself. It is the
    // responsibility of Rhino to create an instance of this class.
    // <para>To complete plug-in information, please also see all PlugInDescription
    // attributes in AssemblyInfo.cs (you might need to click 
    // "Project" -> "Show All Files" to see it in the "Solution Explorer" window).

    
    // You can override methods here to change the plug-in behavior on
    // loading and shut down, add options pages to the Rhino _Option command
    // and mantain plug-in wide options in a document.

[<System.Runtime.InteropServices.Guid("ce66e7f8-7f16-476d-a30e-6540a04c25eb")>]
type BoxBounce () = // calls for the command that starts sthe script editor
    inherit Rhino.Commands.Command()    
    static member val Instance = BoxBounce() 
    // Singelton: http://stackoverflow.com/questions/2691565/how-to-implement-singleton-pattern-syntax
    // Rhino only creates one instance of each command class defined in a
    // plug-in, so it is safe to store a refence in a static property.
                    
    override this.EnglishName = "BoxBounce" //The command name as it appears on the Rhino command line.
           
    override this.RunCommand (doc, mode)  =        
        rs.print "*** Game Start ***"
        //rs.command "-fullscreen enter"
        Game.run doc
        rs.print "*** Game End ***"
        Rhino.Commands.Result.Success

