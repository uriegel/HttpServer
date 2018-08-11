namespace Http2
open System
open System.IO
open System.Text

// Implemented according to RFC 7541
// https://httpwg.org/specs/rfc7541.html
module HPack =

    type HeaderField = 
        | FieldIndex of StaticTableIndex
        | Field of Field
    and Field = {
        Key: Key
        Value: string
    }
    and Key = 
        | Index of StaticTableIndex
        | Key of string

    type internal State = 
        ReadHeaderRepresentation = 0 
        | ReadIndexedHeader = 2
        | IndexedHeaderName = 3
        | ReadLiteralHeaderNameLengthPrefix = 4
        | ReadLiteralHeaderValueLengthPrefix = 8

    type internal IndexType = Incremental = 0 | None = 1| Never = 2

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

        let decodeIndexedHeaderField firstByte =
            FieldIndex (LanguagePrimitives.EnumOfValue<byte, StaticTableIndex> (firstByte &&& 0x7Fuy))

        let decodeLiteralHeaderField firstByte =
            let decodeWithIncrementalIndex () = 
                let index = firstByte &&& 0x3Fuy // 00111111
                Field {
                    Key =                        
                        match index with 
                        | 0uy -> Key (getHeaderValue ())
                        | _ -> Index (LanguagePrimitives.EnumOfValue<byte, StaticTableIndex> index)
                    Value = getHeaderValue ()
                }

            let decodeNeverIndexed () = FieldIndex StaticTableIndex.NeverIndexed

            let decodeWithoutIndexing firstByte = 
                match firstByte with
                | 0uy ->
                    Field {
                        Key = Key( getHeaderValue ())
                        Value = getHeaderValue ()
                    }
                | _ -> FieldIndex StaticTableIndex.NeverIndexed

            match (firstByte &&& 0x40uy) = 0x40uy with // 01000000
            | true -> decodeWithIncrementalIndex ()
            | false when (firstByte &&& 0x10uy) = 0x10uy -> decodeNeverIndexed () // 00010000 
            | false -> decodeWithoutIndexing firstByte

        let rec decodeNextHeaderField () = 
            seq {
                let firstByte = binaryReader.ReadByte ()
                let headerField = 
                    if (firstByte &&& 0x80uy) = 0x80uy then // 10000000
                        decodeIndexedHeaderField firstByte
                    else
                        decodeLiteralHeaderField firstByte
                if binaryReader.BaseStream.Position = binaryReader.BaseStream.Length then
                    ()
                else
                    yield headerField
                    yield! decodeNextHeaderField ()
            }                    

        let result = 
            decodeNextHeaderField ()
            |> Seq.toArray
        result
        
        

