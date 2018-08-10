namespace Http2
open System
open System.IO
open System.Text

// Implemented according to RFC 7541
// https://httpwg.org/specs/rfc7541.html
module HPack =

    type State = 
        ReadHeaderRepresentation = 0 
        | ReadIndexedHeader = 2
        | IndexedHeaderName = 3
        | ReadLiteralHeaderNameLengthPrefix = 4
        | ReadLiteralHeaderValueLengthPrefix = 8

    type IndexType = Incremental = 0 | None = 1| Never = 2

    let Decode (payload: Stream) = 
        use binaryReader = new BinaryReader (payload)

        let getHeaderValue () =
            let firstByte = binaryReader.ReadByte ()
            let huffman = ((firstByte &&& 0x80uy) = 0x80uy) // 10000000
            let length = firstByte &&& 0x7Fuy // 01111111
            let bytes: byte[] = Array.zeroCreate (int length) 
            binaryReader.Read(bytes, 0, bytes.Length) |> ignore
            if huffman then 
                Huffman.Decode bytes 
            else
                Encoding.UTF8.GetString bytes

        let getHeaderName () = 
            ()

        let getHeaderFromIndex index =
            ()
        
        let decodeIndexedHeaderField firstByte =
            ()

        let decodeLiteralHeaderField firstByte =
            let decodeWithIncrementalIndex () = 
                let index = firstByte &&& 0x3Fuy // 00111111
                let key = 
                    match index with 
                    | 0uy -> getHeaderName ()
                    | _ -> getHeaderFromIndex index
                let value = getHeaderValue ()
                ()

            let decodeNeverIndexed () = ()

            let decodeWithoutIndexing () = ()

            let affe = (firstByte &&& 0x7Fuy) = 0x7Fuy

            match (firstByte &&& 0x40uy) = 0x40uy with // 01000000
            | true -> decodeWithIncrementalIndex ()
            | false when (firstByte &&& 0x10uy) = 0x10uy -> decodeNeverIndexed () // 00010000 
            | false -> decodeWithoutIndexing ()

        let rec decodeNextHeaderField () = 
            let firstByte = binaryReader.ReadByte ()
            if (firstByte &&& 0x80uy) = 0x80uy then // 10000000
                decodeIndexedHeaderField firstByte
            else
                decodeLiteralHeaderField firstByte
            decodeNextHeaderField ()
                    
        decodeNextHeaderField ()
        ()                
            //if binaryReader.BaseStream.Length - binaryReader.BaseStream.Position > 0L then
            //    match state with
            //    | State.ReadHeaderRepresentation -> 
            //        let byte = binaryReader.ReadSByte ()
            //        if requiredMaxDynamicTableSizeChange && (byte &&& 0xe0y) <> 0x20y then 
            //            failwith "max dynamic table size required"
            //        else
            //            match byte with
            //            | _ when byte < 0y -> 
            //                headerIndex <- (byte &&& 0x7Fy)
            //                match headerIndex with 
            //                | 0y -> failwith (sprintf "Index value %d not allowed" headerIndex)
            //                | 0x7Fy -> state <- State.ReadIndexedHeader
            //                | _ -> () // TODO: index header
            //            | _ when (byte &&& 0x40y) = 0x40y -> 
            //                indexType <- IndexType.Incremental
            //                headerIndex <-byte &&& 0x3Fy
            //                match headerIndex with
            //                | 0y -> state <- State.ReadLiteralHeaderNameLengthPrefix
            //                | 0x3Fy -> state <- State.IndexedHeaderName
            //                | _ -> 
            //                    setName ()
            //                    state <- State.ReadLiteralHeaderValueLengthPrefix
            //            |_ -> ()
            //    | _ -> failwith "Invalid state"
            //else
            //    ()


        

