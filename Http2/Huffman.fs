namespace Http2

open System
open System.IO

open HuffmanTree

module Huffman = 
    open System.Text

    let writeByte (stream: Stream) byte =
        let oneByte = [| byte |]
        stream.Write(oneByte, 0, 1)

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
