namespace Http2
open System.IO

module HPack =
    type private State = 
        ReadHeaderRepresentation = 0 
        | ReadIndexedHeader = 2
        | IndexedHeaderName = 3
        | ReadLiteralHeaderNameLengthPrefix = 4
        | ReadLiteralHeaderValueLengthPrefix = 8

    type private IndexType = Incremental = 0 | None = 1| Never = 2

    let Decode (payload: Stream) = 
        
        let mutable state = State.ReadHeaderRepresentation
        let mutable indexType = IndexType.None
        let mutable requiredMaxDynamicTableSizeChange = false
        let mutable headerIndex = 0y

        let setName () =
            if headerIndex < 0y then
                ()
            else
                ()
           
        use binaryReader = new BinaryReader (payload)
        
        let rec decode () = 
            if binaryReader.BaseStream.Length - binaryReader.BaseStream.Position > 0L then
                match state with
                | State.ReadHeaderRepresentation -> 
                    let byte = binaryReader.ReadSByte ()
                    if requiredMaxDynamicTableSizeChange && (byte &&& 0xe0y) <> 0x20y then 
                        failwith "max dynamic table size required"
                    else
                        match byte with
                        | _ when byte < 0y -> 
                            headerIndex <- (byte &&& 0x7Fy)
                            match headerIndex with 
                            | 0y -> failwith (sprintf "Index value %d not allowed" headerIndex)
                            | 0x7Fy -> state <- State.ReadIndexedHeader
                            | _ -> () // TODO: index header
                        | _ when (byte &&& 0x40y) = 0x40y -> 
                            indexType <- IndexType.Incremental
                            headerIndex <-byte &&& 0x3Fy
                            match headerIndex with
                            | 0y -> state <- State.ReadLiteralHeaderNameLengthPrefix
                            | 0x3Fy -> state <- State.IndexedHeaderName
                            | _ -> 
                                setName ()
                                state <- State.ReadLiteralHeaderValueLengthPrefix
                        |_ -> ()
                | _ -> failwith "Invalid state"
            else
                ()

        decode ()    

        

