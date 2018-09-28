namespace WebServer
open System.Diagnostics
open System.Reflection

module Constants = 
    let private serverConstant = 
        "URiegel Server" + ((FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly().Location)).ProductVersion).ToString()

    let server = serverConstant
