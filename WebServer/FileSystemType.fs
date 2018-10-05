namespace WebServer

type FileType = {
    path: string
    query: string option 
}

type FileSystemType = 
    | File of FileType
    | Redirection of string
