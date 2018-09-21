namespace WebServer

type FileType = {
    Path: string
    Query: string option 
}

type FileSystemType = 
    | File of FileType
    | Redirection of string
