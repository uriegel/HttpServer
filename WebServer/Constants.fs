namespace WebServer
open System.Diagnostics
open System.Reflection

module Constants = 
    let private serverConstant = 
        // TODO: "URiegel Server" + ((FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly().Location)).ProductVersion).ToString()
        "URiegel Server"
    let server = serverConstant
