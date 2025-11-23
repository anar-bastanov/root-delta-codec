# Root Delta Image (RDI) Specification

**Version**: 1.0.0 <br>
**Status**: Draft <br>
**Author**: Anar Bastanov <br>
**Date**: 2025-11-09 <br>

## Overview

Root Delta Image (RDI) is a lossy raster image format that uses delta-based transforms to encode images. It targets compact storage and high visual fidelity while keeping the format simple to implement and easy to extend.

## Table of Contents

- [Overview](#overview)
- [1. Scope](#1-scope)
- [2. Normative References](#2-normative-references)
- [3. Definitions and Conventions](#3-definitions-and-conventions)
- [4. File Layout](#4-file-layout)
- [5. Header Structure](#5-header-structure)
- [6. Gap Region Definition](#6-gap-region-definition)
- [7. Payload Definition](#7-payload-definition)
- [8. Transform Modes](#8-transform-modes)
  - [8.1 Shared Primitives](#81-shared-primitives)
  - [8.2 Mode 5](#82-mode-5)
  - [8.3 Mode 6](#83-mode-6)
  - [8.4 Mode 8](#84-mode-8)
  - [8.5 Mode 9](#85-mode-9)
- [9. Validation Rules and Procedures](#9-validation-rules-and-procedures)
- [10. Conformance Clauses](#10-conformance-clauses)
- [11. Security Considerations](#11-security-considerations)
- [12. Appendix](#12-appendix)
- [13. Informative References](#13-informative-references)

## 1. Scope

This document normatively specifies the RDI file format version 1.0.0. It defines the on-disk layout, Header semantics, Gap Region, Payload encoding, Transform Modes, and validation rules required for conformance.

## 2. Normative References

The following documents are normative and govern interpretation of requirements in this specification:

1. Bradner, S., "Key words for use in RFCs to Indicate Requirement Levels," RFC 2119, IETF, 1997.
2. Deutsch, P. and J.-L. Gailly, "ZLIB Compressed Data Format Specification version 3.3," RFC 1950, IETF, 1996.

## 3. Definitions and Conventions

This section defines terminology and general conventions used throughout this specification:

* Little-endian — Multi-byte integer fields are stored least-significant byte first, unless stated otherwise.
* Integer fields — Unless otherwise stated, all integer fields are unsigned and represent non-negative values.
* Conformance language — Normative terms MUST, SHOULD, and MAY are to be interpreted as defined in RFC 2119.
* Transform output — The uncompressed byte sequence produced by a transform algorithm; it is not raw pixel data unless a transform specifically defines that mapping.
* File size — The total number of bytes in the RDI file.

## 4. File Layout

An RDI file consists of three primary regions arranged sequentially:

1. Header — A fixed-length structure containing all metadata required to interpret the Payload.
2. Gap Region — An implementation-defined block of bytes immediately following the Header.
3. Payload — The remaining bytes beginning at the offset defined in the Header and continuing to the end of the file.

```none
+------------------------+
| Header (fixed size)    |
+------------------------+
| Gap Region (undefined) |
+------------------------+
| Payload (ZLIB stream)  |
+------------------------+
```

RDI does not include a footer or global metadata beyond the Header. Implementations MUST determine Payload boundaries and interpretation solely from Header fields.

## 5. Header Structure

The file begins with a header of fixed length 28 bytes at offset 0. It defines the following fields at the given positions:

| Offset | Size (bytes) | Name           | Description                      |
| -----: | -----------: | :------------- | :------------------------------- |
|      0 |            8 | Signature      | Magic constant identifying RDI   |
|      8 |            2 | Version        | Format version number            |
|     10 |            4 | Data Offset    | Byte offset of the Payload start |
|     14 |            4 | Width          | Image width in pixels            |
|     18 |            4 | Height         | Image height in pixels           |
|     22 |            2 | Color Model    | Image color format               |
|     24 |            2 | Color Depth    | Bits per channel                 |
|     26 |            2 | Transform Mode | Selected encoding mode           |

## 5.1 Field Constraints

A conforming RDI reader MUST enforce these constraints:

1. Signature MUST equal the byte sequence `41 4E 52 00 52 44 49 00` (ASCII `ANR\0RDI\0`).
2. Version MUST equal the byte sequence `01 00` (unsigned 16-bit literal `1`).
3. Data Offset MUST be at least the Header length and MUST be less than the file size.
4. Width MUST be at least 1 and MUST NOT exceed 16,384 (2^14 pixels).
5. Height MUST be at least 1 and MUST NOT exceed 16,384 (2^14 pixels).
6. Color Model MUST be one of: GRAY = 1, RGB = 3, RGBA = 4.
7. Color Depth MUST equal 8.
8. Transform Mode MUST correspond to a registered mode in §8.

## 6. Gap Region Definition

The Gap Region immediately follows the Header and extends up to the Data Offset. Its length and contents are implementation-defined. Writers MAY utilize this region for auxiliary data, padding, or alignment purposes. Conforming RDI readers MUST NOT depend on the contents of this region for correct interpretation of the Payload. Specialized implementations MAY define conventions for this region when both writer and reader explicitly support them through external agreement.

## 7. Payload Definition

The Payload is the contiguous byte sequence from the Data Offset to the end of the file. Its contents in uncompressed form depend on the selected Transform Mode.

### 7.1 ZLIB Framing and Encoding

The Payload MUST satisfy the following constraints:

1. The Payload size, file size minus Data Offset, MUST NOT exceed 1 GiB (1,073,741,824 bytes).
2. The bytes in the Payload MUST form exactly one valid ZLIB stream as defined in RFC 1950. Concatenated ZLIB streams or other wrappers MUST NOT be present.
3. Preset dictionaries MUST NOT be used. The FDICT bit in the ZLIB header MUST be zero.
4. Encoders MAY select any compression level or compression strategy; this specification does not constrain that choice.
5. Decoders MUST decompress the ZLIB stream to completion.

### 7.2 Transform Output

The decompressed bytes are the transform output for the Transform Mode specified in the Header. Prior to interpreting decompressed bytes, decoders MUST resolve the Transform Mode and obtain its required transform output length as defined in §8.

The transform output MUST satisfy the following constraints:

1. The decompressed length MUST NOT exceed 1 GiB (1,073,741,824 bytes).
2. The decompressed length MUST be greater than or equal to the required transform output length for the selected Transform Mode.
3. If the decompressed length is greater than the required length, the decoder MUST process only the first required number of bytes and MUST ignore any excess.

## 8. Transform Modes

Transform Modes define how pixels are converted to the transform output that becomes the Payload after compression. Each registered Mode has an applicability set of Color Model values, a required transform output length function, a transform output layout, and a deterministic decode procedure.

Modes registered in this section are assigned code points. Modes not listed are reserved and invalid. Encoders MUST NOT use reserved or obsolete modes. Decoders MUST reject files that declare them. "Registered" denotes a valid value assigned by this specification; "supported" is an implementation claim and is not used in this registry.

The following table registers valid, obsolete, and reserved Transform Modes together with applicable Color Models:

| Mode | Status     | Applicable Color Models |
| ---: | :--------- | :---------------------- |
|    0 | Reserved   | —                       |
|    1 | Obsolete   | —                       |
|    2 | Reserved   | —                       |
|    3 | Obsolete   | —                       |
|    4 | Obsolete   | —                       |
|    5 | Registered | GRAY, RGB, RGBA         |
|    6 | Registered | RGB, RGBA               |
|    7 | Obsolete   | —                       |
|    8 | Registered | GRAY, RGB, RGBA         |
|    9 | Registered | RGB, RGBA               |
|   10 | Obsolete   | —                       |
|  ... | Reserved   | —                       |

### 8.1 Shared Primitives

All samples are 8-bit unsigned integers. Unless a Mode states otherwise, arithmetic on samples MUST use addition modulo 256. Rows MUST be processed top to bottom; within each row, columns MUST increase left to right.

#### 8.1.1 Channel Sets and Alpha

* Color Model defines channels: GRAY = {Y}, RGB = {R,G,B}, RGBA = {R,G,B,A}.
* Alpha channel A, if present, is the coverage: 0 = fully transparent, 255 = fully opaque.
* Color channels MUST NOT be premultiplied by alpha.
* Channel order is Mode-defined and MUST be stated in each Mode’s layout.
* This specification does not define transfer functions or chromaticities.

#### 8.1.2 Root Delta Definition

Root Delta encodes Delta, the signed difference between the current sample and the previous sample along the processing direction. The allowed difference range is -255 to +255. Delta is quantized to a Root Delta code value in the range 0..15.

The storage layout of Root Delta codes is defined by each Mode. Where a storage element contains bits that are not used to carry any Root Delta code, encoders MUST set those bits to zero and decoders MUST ignore them. Any Mode that uses Root Delta MUST define how the first sample of each row is carried.

#### 8.1.3 Root Delta Encoding

Encoders MUST compute Deltas and map them to Root Delta codes using this table:

| Deltas       | Root Delta |
| :----------- | ---------: |
| -255         |          1 |
| -254 to -253 |          2 |
| -252 to -249 |          3 |
| -248 to -241 |          4 |
| -240 to -225 |          5 |
| -224 to -193 |          6 |
| -192 to -161 |          7 |
| -160 to -128 |          8 |
| -127 to -95  |          9 |
| -94  to -63  |         10 |
| -62  to -31  |         11 |
| -30  to -15  |         12 |
| -14  to -7   |         13 |
| -6   to -3   |         14 |
| -2   to -1   |         15 |
| 0            |          0 |
| +1  to  +2   |          1 |
| +3  to  +6   |          2 |
| +7  to +14   |          3 |
| +15 to +30   |          4 |
| +31 to +62   |          5 |
| +63 to +94   |          6 |
| +95 to +127  |          7 |
| +128 to +160 |          8 |
| +161 to +192 |          9 |
| +193 to +224 |         10 |
| +225 to +240 |         11 |
| +241 to +248 |         12 |
| +249 to +252 |         13 |
| +253 to +254 |         14 |
| +255         |         15 |

#### 8.1.4 Root Delta Decoding

Decoders MUST map each Root Delta code to a Delta using this table:

| Root Delta | Delta |
| ---------: | ----: |
|          0 |     0 |
|          1 |     1 |
|          2 |     3 |
|          3 |     7 |
|          4 |    15 |
|          5 |    31 |
|          6 |    63 |
|          7 |    95 |
|          8 |   128 |
|          9 |   161 |
|         10 |   193 |
|         11 |   225 |
|         12 |   241 |
|         13 |   249 |
|         14 |   253 |
|         15 |   255 |

#### 8.1.5 Conversion Between RGB and YCoCg

All operations are integer. Divisions MUST use floor semantics on signed intermediates. Alpha channel, if present, is not part of this conversion and MUST pass unchanged.

Forward mapping from {R,G,B} to {Y,Co,Cg}:

```none
Y  = (2 * G + R + B + 2) / 4
Co = (R - B + 256) / 2
Cg = (2 * G - R - B + 513) / 4
```

Inverse mapping from {Y,Co,Cg} to {R,G,B}:

```none
R = Y + Co - Cg
G = Y + Cg - 128
B = Y - Co - Cg + 256
```

After the inverse, decoders MUST clamp R, G, and B to 0..255 using saturating arithmetic. Per-channel reconstruction error up to 1 is expected.

#### 8.1.6 YCoCg Channel Usage

Modes that use YCoCg MUST interpret Color Model and channels as follows:

* For Color Model GRAY, the channel set is {Y}. Y MUST be encoded and decoded directly.
* For Color Model RGB, each pixel {R,G,B} MUST be converted to {Y,Co,Cg} using §8.1.5 before encoding. The transform MUST operate on channels Y, Co, and Cg. After decoding, each pixel MUST be converted back from {Y,Co,Cg} to {R,G,B} using §8.1.5.
* For Color Model RGBA, each pixel {R,G,B} MUST be converted to {Y,Co,Cg} using §8.1.5 before encoding and alpha A MUST NOT be modified by this conversion. The transform MUST operate on channels Y, Co, Cg, and A. After decoding, each pixel MUST be converted from {Y,Co,Cg} to {R,G,B} using §8.1.5 and the decoded A channel MUST be used unchanged as the alpha component. Alpha MUST participate in any prediction and Root Delta coding that the Mode applies to its channels but MUST NOT be an input to the YCoCg conversion.

#### 8.1.7 Chroma Subsampling

Some Modes subsample chroma channels on a coarser grid and reconstruct them back to full resolution. This subsection defines the subsampling and reconstruction rules that such Modes MUST follow when operating on chroma channels.

The subsampled chroma grid has this size:

```none
SubsampledWidth  = (Width  + 1) / 2
SubsampledHeight = (Height + 1) / 2
```

Division is integer division with truncation toward zero.

Subsampling MUST follow these rules:

* Each subsampled chroma sample represents a block of up to 2 by 2 full-resolution pixels. For interior positions this block covers two adjacent columns and two adjacent rows. At the right or bottom edges, when a second column or row is not available, the last available column or row MUST be reused so that there are still four contributing pixels.
* For each such block, the subsampled chroma value MUST be the arithmetic mean of the four contributing full-resolution chroma values.

Reconstruction to full resolution MUST follow these rules:

* Each full-resolution pixel MUST be assigned a chroma value derived from nearby subsampled chroma samples.
* If the full-resolution pixel lies exactly at a subsampled position, the chroma value MUST be copied directly from that subsampled position.
* If the full-resolution pixel lies between two subsampled positions along a single axis and is aligned with a subsampled position along the other axis, the chroma value MUST be the arithmetic mean of the two neighboring subsampled values along that axis.
* If the full-resolution pixel lies between subsampled positions along both axes, the chroma value MUST be the arithmetic mean of the four neighboring subsampled values.

All arithmetic means in this subsection MUST be computed in integer arithmetic and rounded to the nearest integer. When the exact mean lies halfway between two integers, the higher integer value MUST be chosen. These rules MUST be applied independently to each chroma channel that a Mode subsamples using this scheme.

### 8.2 Mode 5

Mode 5 encodes each channel as a sequence of scanlines using a leader-plus-deltas scheme. For each scanline it stores the first sample of the channel as a verbatim leader and represents all remaining samples in that scanline as Root Delta codes that describe horizontal changes from one sample to the next. When applied to RGB or RGBA images, Mode 5 conceptually operates on Y, Co, and Cg channels in YCoCg space, with alpha channel A in RGBA carried separately.

Mode 5 MUST follow the YCoCg channel usage rules of §8.1.6 and the Root Delta semantics of §8.1.2-§8.1.4. Rows and columns MUST follow the standard image order defined in §8.1.

For each supported Color Model, Mode 5 MUST use the following channel order on the full-resolution grid:

* For Color Model GRAY, the only channel is Y at index 0.
* For Color Model RGB, channels at indices 0, 1, and 2 MUST be Y, Co, and Cg respectively.
* For Color Model RGBA, channels at indices 0, 1, 2, and 3 MUST be Y, Co, Cg, and A respectively.

#### 8.2.1 Transform Output Length

If the image has Width pixels per row, Height rows, and C channels, the transform output MUST contain this many bytes:

```none
Length = C * Height * Width
```

C is the number of channels implied by the Color Model as defined in §8.1.1 and §8.1.6. Division is integer division with truncation toward zero.

#### 8.2.2 Layout

The transform output consists of a leaders region followed by a Root Delta region.

The leaders region MUST contain one byte for each scanline of each channel's grid. Leaders MUST be grouped by channel index in ascending order. Within each channel, leaders MUST be stored in scanline order from the top row to the bottom row, one per scanline.

The Root Delta region MUST follow immediately after the leaders region. Root Delta bytes MUST also be grouped by channel index in ascending order. For a given channel, the Root Delta region MUST contain one byte for every sample of that channel that is not a leader. For each scanline, there MUST be one Root Delta byte for each sample after the first in that scanline. Within each channel, Root Delta bytes MUST be ordered by scanline from the top row to the bottom row, and within each scanline by column from the second sample to the last.

If Width equals 1 there MUST be no Root Delta codes for any channel and the transform output MUST consist only of leader bytes.

Each Root Delta code MUST be stored as a single byte whose numeric value is in the range 0..15 as defined in §8.1.2. Mode 5 MUST NOT pack more than one Root Delta code into a byte.

#### 8.2.3 Encoding

Channel samples MUST be derived from input pixels using the Color Model and YCoCg usage rules of §8.1.6.

Encoding MUST proceed as follows:

1. For each channel and each scanline, take the sample at the first pixel position in that channel's grid, write it into the leaders region at the slot for that channel and scanline, and set this value as the initial reconstructed sample for that scanline.
2. For each channel and each scanline, and for each later sample in that scanline, take the input sample for that channel, compute the signed difference between this sample and the current reconstructed sample in that scanline, and map this difference to a Root Delta code using §8.1.3.
3. For each such difference, write the Root Delta code to the next byte in the Root Delta region for that channel and scanline, obtain the reconstruction Delta using §8.1.4, and update the reconstructed sample by adding that Delta using the modulo-256 arithmetic defined in §8.1.

Each Root Delta in a scanline MUST therefore be taken relative to the last reconstructed sample in that scanline.

#### 8.2.4 Decoding

Decoding MUST reverse the encoding using the stored leaders, Root Delta bytes, and the tables and arithmetic defined in §8.1.

Decoding MUST proceed as follows:

1. For each channel and each scanline, read the leader byte for that channel and scanline from the leaders region, write this value as the decoded sample at the first pixel position in that scanline, and set it as the previous sample.
2. For each channel and each scanline, and for each later sample position in that scanline, read the next Root Delta code for that channel and scanline from the Root Delta region, convert the code to a reconstruction Delta using §8.1.4, add this Delta to the previous sample using the modulo-256 arithmetic of §8.1, write the result as the decoded sample at that position, and set that result as the previous sample for the next position in the scanline.

After all channels have been decoded, channel samples MUST be converted back to pixel values by applying the YCoCg channel usage rules of §8.1.6.

### 8.3 Mode 6

Mode 6 is a chroma-subsampled variant of the leader-plus-deltas scheme. It encodes luma Y at full resolution using horizontal Root Delta coding, encodes Co and Cg on a subsampled chroma grid using the same horizontal Root Delta coding, and reconstructs Co and Cg to full resolution using the chroma subsampling rules of §8.1.7. For Color Model RGBA, Mode 6 also encodes alpha A at full resolution using a separate leader-plus-deltas stream.

Mode 6 MUST follow the YCoCg channel usage rules of §8.1.6 and the Root Delta semantics of §8.1.2-§8.1.4. Rows and columns MUST follow the standard image order defined in §8.1.

For each supported Color Model, Mode 6 MUST use the following channel order:

* For Color Model RGB, channel 0 MUST be Y on the full-resolution grid, and channels 1 and 2 MUST be Co and Cg respectively on the subsampled chroma grid defined in §8.1.7.
* For Color Model RGBA, channels 0 and 1 MUST be A and Y respectively on the full-resolution grid, and channels 2 and 3 MUST be Co and Cg respectively on the subsampled chroma grid defined in §8.1.7.

#### 8.3.1 Transform Output Length

If the image has Width pixels per row, Height rows, and C channels, the transform output MUST contain this many bytes:

```none
Length = (C - 2) * Height * Width + 2 * SubsampledHeight * SubsampledWidth
```

C is the number of channels implied by the Color Model as defined in §8.1.1 and §8.1.6. The subsampled grid size is defined in §8.1.7. Division is integer division with truncation toward zero.

#### 8.3.2 Layout

The transform output consists of a leaders region followed by a Root Delta region. Both regions follow the basic ordering rules used by Mode 5.

The leaders region MUST contain one byte for each scanline of each channel's grid. Leaders MUST be grouped by channel index in ascending order. Within each channel, leaders MUST be stored from the first scanline of that channel's grid to the last. For channels that use the full-resolution grid, this grid is Width by Height. For channels that use the subsampled chroma grid, this grid has the size defined in §8.1.7.

The Root Delta region MUST follow immediately after the leaders region. It MUST contain one byte for each non-leader sample of every channel. For each channel, Root Delta bytes MUST be ordered by scanline from the top row of that channel's grid to the bottom row, and within each scanline from the second sample in that grid to the last. Each Root Delta byte MUST represent one Root Delta code in the range 0..15. Mode 6 MUST NOT pack multiple codes into a single byte.

If Width equals 1 there MUST be no Root Delta bytes for any channel, and the transform output MUST consist only of leader bytes.

#### 8.3.3 Encoding

Channel samples MUST be derived from input pixels using the Color Model and the YCoCg usage rules of §8.1.6.

Encoding MUST proceed as follows:

1. Derive full-resolution Y and, when present, A channel samples from the input pixels using the Color Model and §8.1.5.
2. Derive full-resolution Co and Cg channel samples from the input pixels using §8.1.5, then construct subsampled Co and Cg grids by applying the chroma subsampling rules of §8.1.7 to those full-resolution Co and Cg samples.
3. For each full-resolution channel grid, apply the Mode 5 encoding procedure of §8.2.3 to that grid, writing leaders and Root Delta bytes into the leaders and Root Delta regions in the order defined in §8.3.2.
4. For each subsampled Co and Cg grid, apply the Mode 5 encoding procedure of §8.2.3 to that subsampled grid, writing leaders and Root Delta bytes into the leaders and Root Delta regions in the order defined in §8.3.2.

The Root Delta codes produced for each grid MUST match the codes that would be produced by applying Mode 5 directly to that grid. Mode 6 differs from Mode 5 only in its use of subsampled chroma grids for Co and Cg and in the mapping of channels to full-resolution and subsampled grids.

#### 8.3.4 Decoding

Decoding MUST reverse the encoding using the stored leaders, Root Delta bytes, and the tables and arithmetic defined in §8.1. Decoded channel samples in Mode 6 MUST match the samples produced by the corresponding encoding process.

Decoding MUST proceed as follows:

1. For each full-resolution channel grid, treat its leaders and Root Delta bytes as a Width by Height grid and apply the Mode 5 decoding procedure of §8.2.4 to reconstruct that channel at full resolution.
2. For each subsampled Co and Cg grid, treat its leaders and Root Delta bytes as a grid of the size defined in §8.1.7 and apply the Mode 5 decoding procedure of §8.2.4 to reconstruct subsampled Co and Cg samples on that grid.
3. Upsample the reconstructed Co and Cg grids to full resolution using the chroma reconstruction rules of §8.1.7, obtaining Co and Cg values at each pixel position in the image.
4. For each pixel, take the reconstructed Y sample and the upsampled Co and Cg samples at that pixel position. For Color Model RGBA also take the reconstructed A sample at that pixel position. Convert Y, Co, and Cg back to R, G, and B using §8.1.5. For RGBA, write the reconstructed A sample unchanged as the alpha component.

After all pixels have been reconstructed, the resulting image MUST have Width, Height, and Color Model equal to the values in the Header.

### 8.4 Mode 8

Mode 8 is a packed variant of Mode 5. It uses the same leader-plus-deltas scanline structure and the same Root Delta codes as Mode 5, but stores those codes as 4-bit values packed two per byte instead of as separate bytes. This reduces the size of the transform output while preserving exactly the same prediction behaviour and per-sample reconstruction as Mode 5.

Mode 8 MUST follow the YCoCg channel usage rules of §8.1.6 and the Root Delta semantics of §8.1.2-§8.1.4. Rows and columns MUST follow the standard image order defined in §8.1. For any given image, an encoder that implements both Modes 5 and 8 MUST produce the same sequence of leaders and Root Delta codes; the only difference between the Modes is how those codes are stored in the transform output.

For each supported Color Model, Mode 8 MUST use the same channel order on the full-resolution grid as Mode 5:

* For Color Model GRAY, the only channel is Y at index 0.
* For Color Model RGB, channels at indices 0, 1, and 2 MUST be Y, Co, and Cg respectively.
* For Color Model RGBA, channels at indices 0, 1, 2, and 3 MUST be Y, Co, Cg, and A respectively.

#### 8.4.1 Transform Output Length

If the image has Width pixels per row, Height rows, and C channels, the transform output MUST contain this many bytes:

```none
Length = C * Height + (C * Height * (Width - 1) + 1) / 2
```

C is the number of channels implied by the Color Model as defined in §8.1.1 and §8.1.6. Division is integer division with truncation toward zero.

#### 8.4.2 Layout

The transform output consists of a leaders region followed by a packed Root Delta region.

The leaders region for Mode 8 MUST be identical to the leaders region for Mode 5. It MUST contain one byte for each scanline of each channel, grouped by channel index in ascending order and, within each channel, in scanline order from the top row to the bottom row.

The packed Root Delta region MUST follow immediately after the leaders. It MUST store the Root Delta codes for all channels in the same logical channel, scanline, and column order as Mode 5: all non-leader samples of channel 0, then all non-leader samples of channel 1, and so on. For each channel and scanline, there MUST be one Root Delta code for each sample after the first in that scanline.

The Root Delta codes form one linear sequence. The packed Root Delta region MUST store these codes in that sequence order, two codes per byte. The first Root Delta code in the sequence MUST be placed in the low four bits of the first byte. The second code MUST be placed in the high four bits of the first byte. The third code MUST be placed in the low four bits of the next byte, the fourth in the high four bits of that byte, and so on until all codes have been written.

If the number of Root Delta codes is odd, the high four bits of the last byte do not carry any Root Delta code. Encoders MAY write any Root Delta code value into that unused four-bit field. Decoders MUST ignore that field.

If Width equals 1 there MUST be no Root Delta codes for any channel and the transform output MUST consist only of leader bytes.

#### 8.4.3 Encoding

Channel interpretation and prediction in Mode 8 MUST match Mode 5.

Encoding MUST proceed as follows:

1. Derive channel samples from input pixels using the Color Model and YCoCg usage rules of §8.1.6.
2. For each channel and each scanline, compute leaders and Root Delta codes exactly as in Mode 5 and write them into the leaders and Root Delta regions in the layout described in §8.4.2.
3. After all Root Delta codes have been written, pack the Root Delta region by combining codes two per byte in the nibble positions defined in §8.4.2. If the total number of Root Delta codes is odd, write a valid Root Delta code value into the unused 4-bit slot of the final packed byte; this extra value MUST be ignored by decoders.

#### 8.4.4 Decoding

Decoded channel samples in Mode 8 MUST match the samples that Mode 5 would produce.

Decoding MUST proceed as follows:

1. Read all leader bytes from the leaders region as described in §8.4.2 and use them as the first decoded samples and initial predictor state for each channel and scanline.
2. Unpack Root Delta codes from the packed Root Delta region according to §8.4.2, obtaining the Root Delta sequence implied by Width, Height, and C.
3. For each channel and each scanline, apply the Root Delta codes in the same order and manner as Mode 5. Starting from the leader value, add the reconstruction Delta from §8.1.4 with the modulo-256 arithmetic of §8.1 to the previous sample at each step to obtain the next sample.

After all channels have been reconstructed, channel samples MUST be converted back to pixel values by applying the YCoCg channel usage rules of §8.1.6.

### 8.5 Mode 9

Mode 9 is a packed variant of Mode 6. It uses the same luma and chroma subsampling structure, channel interpretation, and Root Delta codes as Mode 6, but stores those codes as 4-bit values packed two per byte instead of as separate bytes. This reduces the size of the transform output while preserving exactly the same prediction behaviour and per-sample reconstruction as Mode 6.

Mode 9 MUST follow the YCoCg channel usage rules of §8.1.6, the Root Delta semantics of §8.1.2-§8.1.4, and the chroma subsampling rules of §8.1.7. Rows and columns MUST follow the standard image order defined in §8.1.

For each supported Color Model, Mode 9 MUST use the same channel order as Mode 6:

* For Color Model RGB, channel 0 MUST be Y on the full-resolution grid, and channels 1 and 2 MUST be Co and Cg respectively on the subsampled chroma grid defined in §8.1.7.
* For Color Model RGBA, channels 0 and 1 MUST be A and Y respectively on the full-resolution grid, and channels 2 and 3 MUST be Co and Cg respectively on the subsampled chroma grid defined in §8.1.7.

#### 8.5.1 Transform Output Length

If the image has Width pixels per row, Height rows, and C channels, the transform output MUST contain this many bytes:

```none
A := (C - 2) * Height + 2 * SubsampledHeight
B := (C - 2) * Height * (Width - 1) + 2 * SubsampledHeight * (SubsampledWidth - 1)
Length = A + (B + 1) / 2
```

C is the number of channels implied by the Color Model as defined in §8.1.1 and §8.1.6. The subsampled grid size is defined in §8.1.7. Division is integer division with truncation toward zero.

#### 8.5.2 Layout

The transform output consists of a leaders region followed by a packed Root Delta region.

The leaders region for Mode 9 MUST be identical to the leaders region for Mode 6. It MUST contain one byte for each scanline of each channel’s grid, grouped by channel index in ascending order and, within each channel, in scanline order from the top row to the bottom row. Channels that carry Y and A use the full-resolution grid. Channels that carry Co and Cg use the subsampled chroma grid defined in §8.1.7.

The packed Root Delta region MUST follow immediately after the leaders region. It MUST store the Root Delta codes for all channels in the same logical channel, scanline, and column order as Mode 6: all non-leader samples of channel 0, then all non-leader samples of channel 1, and so on. For each channel and scanline, there MUST be one Root Delta code for each sample after the first in that channel’s grid.

The Root Delta codes form one linear sequence. The packed Root Delta region MUST store these codes in that sequence order, two codes per byte. The first Root Delta code in the sequence MUST be placed in the low four bits of the first byte. The second code MUST be placed in the high four bits of the first byte. The third code MUST be placed in the low four bits of the next byte, the fourth in the high four bits of that byte, and so on until all codes have been written.

If the number of Root Delta codes is odd, the high four bits of the last byte do not carry any Root Delta code. Encoders MAY write any Root Delta code value into that unused four-bit field. Decoders MUST ignore that field.

If Width equals 1 there MUST be no Root Delta codes for any channel and the transform output MUST consist only of leader bytes.

#### 8.5.3 Encoding

Channel samples MUST be derived from input pixels using the Color Model and the YCoCg usage rules of §8.1.6. Mode 9 MUST use the same channel interpretation, chroma subsampling, and prediction steps as Mode 6.

Encoding MUST proceed as follows:

1. Conceptually apply the Mode 6 encoding procedure of §8.3.3 to the input image, producing leaders and a linear sequence of Root Delta codes for all channels in the channel, scanline, and column order described in §8.3.2.
2. Write the leader bytes to the leaders region exactly as in Mode 6.
3. Pack the Root Delta code sequence into the packed Root Delta region by combining codes two per byte in the nibble positions defined in §8.5.2. If the total number of Root Delta codes is odd, write a valid Root Delta code value into the unused 4-bit slot of the final packed byte; this extra value MUST be ignored by decoders.

The leaders and Root Delta codes produced for a given image in Mode 9 MUST match those that Mode 6 would produce for the same image before packing.

#### 8.5.4 Decoding

Decoded channel samples in Mode 9 MUST match the samples that Mode 6 would produce for the same image.

Decoding MUST proceed as follows:

1. Read all leader bytes from the leaders region as described in §8.5.2 and use them as the first decoded samples and initial predictor state for each channel and scanline.
2. Unpack Root Delta codes from the packed Root Delta region according to §8.5.2, obtaining the linear Root Delta sequence implied by Width, Height, C, and the layout rules of Mode 6.
3. Apply the Mode 6 decoding procedure of §8.3.4 using the unpacked Root Delta codes and the leaders, reconstructing all channels on their respective grids and upsampling Co and Cg to full resolution using §8.1.7.

After all channels have been reconstructed, channel samples MUST be converted back to pixel values by applying the YCoCg channel usage rules of §8.1.6.

## 9. Validation Rules and Procedures

This section defines when validation occurs and how a decoder MUST behave on failure.

1. The decoder MUST read the Header at file offset 0, enforce all constraints in §5.1, and determine the Payload region from Data Offset and file size in accordance with §7.1 before processing the Payload.
2. The decoder MUST decompress the Payload to completion as a single ZLIB stream as required by §7.1. Any decompression failure MUST be treated as a validation failure.
3. The decoder MUST obtain the required transform output length from the selected Transform Mode in §8 and validate the decompressed length according to §7.2.
4. The decoder MUST decode the transform output according to the selected Transform Mode in §8 and produce pixel data whose Width, Height, and Color Model equal the Header values. Any additional Mode-level checks defined in §8 MUST also pass.
5. If arithmetic on Header fields or Mode formulas would overflow or wrap when computing sizes, offsets, or lengths, the file MUST be rejected as invalid.
6. On any validation failure, the decoder MUST stop processing the file, release any resources associated with it, and report failure without recovery or error concealment.

## 10. Conformance Clauses

A conforming encoder MUST produce files that satisfy all normative requirements of §5-§8. A conforming decoder MUST validate and interpret files according to §5-§9, reject invalid files, and reconstruct the image when valid.

Support for all registered Transform Modes is not required. An implementation MAY support a subset of Transform Modes. The implementation MUST disclose the supported Mode and Color Model combinations in its conformance statement.

An implementation MAY claim conformance to multiple RDI versions. A decoder that claims multiple versions MUST select behavior by the Version field and MUST reject any file whose Version is not one of the versions it claims.

## 11. Security Considerations

This section provides operational guidance. Items here are recommendations and not additional format constraints.

Implementations SHOULD:

* Validate all sizes and offsets derived from the Header before allocation and arithmetic, and guard against integer overflow when computing sizes or offsets.
* Prefer streaming decompression and incremental processing to reduce peak memory.
* Bound CPU and memory used for decompression and transform steps.
* Treat all inputs as untrusted and handle failures without exposing internal state.
* Follow security guidance applicable to ZLIB as published with RFC 1950.

## 12. Appendix

This appendix is informative. Its subsections do not add any new conformance requirements.

### 12.1. File Identification

* Magic number (offset 0): `41 4E 52 00 52 44 49 00`
* Recommended file extension: `.rdi`
* Proposed media type: `image/rdi`

### 12.2. Example Header Encodings

These examples show only the 28-byte Header and the Gap Region, if present; Payload bytes are omitted.

### 12.2.1 Example 1

Below is a minimal GRAY image using Mode 5. The Header fields have these values:

* Signature: `41 4E 52 00 52 44 49 00`
* Version: `01 00`
* Data Offset: `1C 00 00 00` (28)
* Width: `01 00 00 00`
* Height: `01 00 00 00`
* Color Model: `01 00` (GRAY)
* Color Depth: `08 00`
* Transform Mode: `05 00`

Header (28 bytes):

```none
41 4E 52 00 52 44 49 00  01 00  1C 00 00 00
01 00 00 00  01 00 00 00  01 00  08 00  05 00
```

### 12.2.2 Example 2

Below is a small RGB image using Mode 6 with a 4-byte Gap Region. The Header fields have these values:

* Signature: `41 4E 52 00 52 44 49 00`
* Version: `01 00`
* Data Offset: `20 00 00 00` (32)
* Width: `02 00 00 00`
* Height: `03 00 00 00`
* Color Model: `03 00` (RGB)
* Color Depth: `08 00`
* Transform Mode: `06 00`

Header (28 bytes) + Gap Region (4 bytes):

```none
41 4E 52 00 52 44 49 00  01 00  20 00 00 00
02 00 00 00  03 00 00 00  03 00  08 00  06 00
00 00 00 00
```

### 12.3 Mode Summary

The following table summarizes the Modes defined in §8:

| Mode | Color Models    | Luma Grid | Chroma Grid | Root Delta Packing | Notes                    |
| ---: | :-------------- | :-------- | :---------- | :----------------- | :----------------------- |
|    5 | GRAY, RGB, RGBA | Full      | Full        | Bytes              | Baseline implementation  |
|    6 | RGB, RGBA       | Full      | Subsampled  | Bytes              | Chroma subsampled        |
|    8 | GRAY, RGB, RGBA | Full      | Full        | Packed nibbles     | Packed variant of Mode 5 |
|    9 | RGB, RGBA       | Full      | Subsampled  | Packed nibbles     | Packed variant of Mode 6 |

## 13. Informative References

The following documents are informative and provide background and context for this specification:

1. Malvar, H.S., G. J. Sullivan, and S. Srinivasan, "Lifting-Based Reversible Color Transforms for Image Compression," Proceedings of the IEEE International Conference on Image Processing (ICIP), 2008.
2. Randers-Pehrson, G. et al., "PNG (Portable Network Graphics) Specification, Version 1.2," W3C Recommendation, 2003.
3. ITU-T Recommendation T.81 | ISO/IEC 10918-1, "Digital compression and coding of continuous-tone still images (JPEG)," 1992.
4. Weinberger, M.J., G. Seroussi, and G. Sapiro, "LOCO-I: A Low Complexity, Context-Based, Lossless Image Compression Algorithm," Proceedings of the IEEE Data Compression Conference, 1996.
