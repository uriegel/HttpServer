namespace Http2

open System

module Huffman = 
    let affe = 2
    //let decode (bytes: byte[]) = 
        
    //    let guessedLength = input.Length * 2
    //    let decodedBytes = Array.zeroCreate guessedLength

    //    let mutable inputByteOffset = 0
    //    let mutable inputBitOffset = 0
    //    let mutable currentSymbolLength = 0
    //    // Padding is only valid in case all bits are 1's
    //    let mutable isValidPadding = true

    //    let treeNode = Http2.HuffmanTree.huffmanTree

    //        for (inputByteOffset = 0; inputByteOffset < input.Length; inputByteOffset++)
    //        {
    //            var bt = input[inputByteOffset];
    //            for (inputBitOffset = 7; inputBitOffset >= 0; inputBitOffset--)
    //            {
    //                // Fetch bit at offset position
    //                var bit = (bt & (1 << inputBitOffset)) >> inputBitOffset;
    //                // Follow the tree branch that is specified by that bit
    //                if (bit != 0)
    //                    treeNode = treeNode.Node1;
    //                else
    //                {
    //                    // Encountered a 0 bit
    //                    treeNode = treeNode.Node0;
    //                    isValidPadding = false;
    //                }
    //                currentSymbolLength++;

    //                if (treeNode == null)
    //                    throw new Exception("Invalid huffman code");

    //                if (treeNode.Value.HasValue)
    //                {
    //                    if (treeNode.Value != 256)
    //                    {
    //                        // We are at the leaf and got a value
    //                        if (outBuf.Length - byteCount == 0)
    //                        {
    //                            Console.WriteLine("Zu klein");
    //                            return "Zu klein";
    //                            // No more space - resize first
    //                            //var unprocessedBytes = input.Count - inputByteOffset;
    //                            //ResizeBuffer(
    //                            //    ref outBuf, byteCount, 2 * unprocessedBytes,
    //                            //    pool);
    //                        }
    //                        outBuf[byteCount] = (byte)treeNode.Value;
    //                        byteCount++;
    //                        treeNode = HuffmanTree.Root;
    //                        currentSymbolLength = 0;
    //                        isValidPadding = true;
    //                    }
    //                    else
    //                    {
    //                        // EOS symbol
    //                        // Fully receiving this is a decoding error,
    //                        // because padding must not be longer than 7 bits
    //                        throw new Exception("Encountered EOS in huffman code");
    //                    }
    //                }

    //            }
    //        }

    //        if (currentSymbolLength > 7)
    //        {
    //            // A padding strictly longer
    //            // than 7 bits MUST be treated as a decoding error.
    //            throw new Exception("Padding exceeds 7 bits");
    //        }

    //        if (!isValidPadding)
    //        {
    //            throw new Exception("Invalid padding");
    //        }

    //        // Convert the buffer into a string
    //        // TODO: Check if encoding is really correct
    //        var str = Encoding.ASCII.GetString(outBuf, 0, byteCount);
    //        return str;
    //    }

