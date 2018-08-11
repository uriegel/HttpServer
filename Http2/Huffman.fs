namespace Http2

open System
open System.IO
open HuffmanTree

module Huffman = 

    open System.Text

    let private writeByte (stream: Stream) byte =
        let oneByte = [| byte |]
        stream.Write(oneByte, 0, 1)

    let encode (text: string) = 
        let bytesToEncode = Encoding.UTF8.GetBytes text
        use resultStream = new MemoryStream (bytesToEncode.Length)
        use resultWriter = new BinaryWriter (resultStream)

        let rec encodeByte index recentCurrent recentN =
            let bt = bytesToEncode.[index] &&& 0xFFuy
            let (symbol, length, _) = HuffmanTree.huffmanTuples.[int bt]
            let current = recentCurrent <<< length ||| symbol
            let mutable n = recentN + length
            
            while n >= 8 do
                n <- n - 8
                resultWriter.Write (byte (current >>> n))
            
            if index < bytesToEncode.Length - 1 then
                encodeByte (index + 1) current n
            else if n > 0 then
                let lastCurrent = current <<< (8 - n) ||| (0xFF >>> n) // EOS
                resultWriter.Write (byte lastCurrent)
            ()

        encodeByte 0 0 0
        resultWriter.Flush ()
        resultStream.Capacity <- int resultStream.Length
        resultStream.GetBuffer ()

    let decode (bytes: byte[]) = 
        let decodedStream = new MemoryStream (bytes.Length * 2)
        let bitIndeces = [|0..7|]
        Array.Reverse bitIndeces
        let mutable node = Some huffmanTree
        let firstBits = 
            bytes 
            |> Seq.map (fun byte -> 
                bitIndeces    
                |> Seq.map (fun index -> ((byte &&& (1uy <<< index)) >>> index))
                )
            |> Seq.collect (fun n -> n)
        ()
        for firstBit in firstBits do
            node <- getSubtree node (firstBit = 0uy)
            match node with
            | None -> failwith "Invalid"
            | Some node1 ->
                match node1 with
                | Leaf leaf -> 
                    writeByte decodedStream ((byte)leaf)
                    node <- Some huffmanTree
                | Branch branch -> ()

        decodedStream.Capacity <- (int)decodedStream.Length
        let buffer = decodedStream.GetBuffer()
        Encoding.UTF8.GetString (buffer)
