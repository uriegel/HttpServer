namespace Http2

open System

module Huffman = 

    /// Huffman coding uses a binary tree whose leaves are the input symbols

    /// and whose internal nodes are the combined expected frequency of all the

    /// symbols beneath them.

    let symbols = 
        [| 
            0x23 
            0x47
            0x33
        |]

