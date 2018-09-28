namespace WebServer
open System
open System.IO
open System.Text

// Implemented according to RFC 7541
// https://httpwg.org/specs/rfc7541.html
module HPack =

    type HeaderField = 
        | FieldIndex of Index
        | Field of Field
    and Index =
        | StaticIndex of StaticTableIndex
        | DynamicIndex of uint32
        | Key of string
    and Field = {
        Key: Index
        Value: string
    }

    type internal State = 
        ReadHeaderRepresentation = 0 
        | ReadIndexedHeader = 2
        | IndexedHeaderName = 3
        | ReadLiteralHeaderNameLengthPrefix = 4
        | ReadLiteralHeaderValueLengthPrefix = 8

    type internal IndexType = Incremental = 0 | None = 1| Never = 2

    let decode (payload: Stream) = 
        use binaryReader = new BinaryReader (payload)

        let getHeaderValue () =
            let firstByte = binaryReader.ReadByte ()
            let huffman = ((firstByte &&& 0x80uy) = 0x80uy) // 10000000
            let length = firstByte &&& 0x7Fuy // 01111111
            let bytes: byte[] = Array.zeroCreate (int length) 
            binaryReader.Read (bytes, 0, bytes.Length) |> ignore
            if huffman then 
                Huffman.decode bytes 
            else
                Encoding.UTF8.GetString bytes

        let decodeIndexedHeaderField firstByte =
            FieldIndex (StaticIndex (LanguagePrimitives.EnumOfValue<byte, StaticTableIndex> (firstByte &&& 0x7Fuy)))

        let decodeLiteralHeaderField firstByte =
            let decodeWithIncrementalIndex () = 
                let index = firstByte &&& 0x3Fuy // 00111111
                Field {
                    Key =                        
                        match index with 
                        | 0uy -> Key (getHeaderValue ())
                        | _ -> StaticIndex (LanguagePrimitives.EnumOfValue<byte, StaticTableIndex> index)
                    Value = getHeaderValue ()
                }

            let decodeNeverIndexed () = FieldIndex (StaticIndex StaticTableIndex.NeverIndexed)

            let decodeWithoutIndexing firstByte = 
                match firstByte with
                | 0uy ->
                    Field {
                        Key = Key( getHeaderValue ())
                        Value = getHeaderValue ()
                    }
                | _ -> FieldIndex (StaticIndex StaticTableIndex.NeverIndexed)

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
                yield headerField
                if binaryReader.BaseStream.Position < binaryReader.BaseStream.Length then
                    yield! decodeNextHeaderField ()
            }                    

        decodeNextHeaderField ()
        |> Seq.toArray
        
    let encode (headerFields: HeaderField list) =
        use memoryStream = new MemoryStream ()
        use binaryWriter = new BinaryWriter (memoryStream)

        let getEncodedLength (text: byte[]) = 
            let rec getEncodedLength index = 
                match index with
                | _ when index = text.Length -> 0
                | _  -> HuffmanTree.lengths.[int (text.[index] &&& 0xFFuy)] + getEncodedLength (index + 1)

            let length = getEncodedLength 0
            (length + 7) >>> 3

        let encodeStaticIndex index = 
            let byte = index ||| 0x80uy // 10000000
            binaryWriter.Write byte

        let encodeValue text = 
            let len = getEncodedLength text
            let maskedLen = byte len ||| 0x80uy // 10000000 
            binaryWriter.Write maskedLen
            let encodedValue = Huffman.encode text
            binaryWriter.Write encodedValue


        let encodeStaticIncremental index text = 
            let byt = index ||| 0x40uy // 01000000
            binaryWriter.Write byt
            encodeValue text

        let encodeIncremental key value = 
            let byt = 0x40uy // 01000000
            binaryWriter.Write byt
            encodeValue key
            encodeValue value

        let encodeHeaderField headerField =
            match headerField with 
            | FieldIndex index ->
                match index with
                | StaticIndex key -> encodeStaticIndex (byte key)
                | DynamicIndex key -> ()
                | Key key -> ()
            | Field field -> 
                match field.Key with
                | StaticIndex key -> encodeStaticIncremental (byte key) (Encoding.UTF8.GetBytes field.Value)
                | DynamicIndex key -> ()
                | Key key -> encodeIncremental (Encoding.UTF8.GetBytes key) (Encoding.UTF8.GetBytes field.Value)
                
                ()

        headerFields    
        |> List.iter (fun n -> encodeHeaderField n)
        binaryWriter.Flush ()
        memoryStream.Capacity <- int memoryStream.Length
        memoryStream.GetBuffer ()
        

