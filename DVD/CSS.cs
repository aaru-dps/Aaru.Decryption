﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : CSS.cs
// Author(s)      : Rebecca Wallander <sakcheen+github@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles Content Scrambling System crypto functionality.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2020-2023 Rebecca Wallander
// ****************************************************************************/

// Based on information gathered from:
//  ISO/IEC13818-1 Second Edition
//  Mt. Fuji Commands for Multimedia Devices
//  https://www.cs.cmu.edu/~dst/DeCSS/Kesden/
//  http://groups.csail.mit.edu/mac/users/hal/css/css.html
//  http://www.staroceans.org/e-book/css/css_auth.html
//  libdvdcpxm (https://offog.org/git/dvdaexplorer/src/libdvdcpxm/)
//  libdvdcss (https://www.videolan.org/developers/libdvdcss.html)

using System;
using System.Linq;
using Aaru.Decoders.DVD;

namespace Aaru.Decryption.DVD;

public class CSS
{
    static readonly byte[,] _playerKeys =
    {
        {
            0x01, 0xaf, 0xe3, 0x12, 0x80
        },
        {
            0x12, 0x11, 0xca, 0x04, 0x3b
        },
        {
            0x14, 0x0c, 0x9e, 0xd0, 0x09
        },
        {
            0x14, 0x71, 0x35, 0xba, 0xe2
        },
        {
            0x1a, 0xa4, 0x33, 0x21, 0xa6
        },
        {
            0x26, 0xec, 0xc4, 0xa7, 0x4e
        },
        {
            0x2c, 0xb2, 0xc1, 0x09, 0xee
        },
        {
            0x2f, 0x25, 0x9e, 0x96, 0xdd
        },
        {
            0x33, 0x2f, 0x49, 0x6c, 0xe0
        },
        {
            0x35, 0x5b, 0xc1, 0x31, 0x0f
        },
        {
            0x36, 0x67, 0xb2, 0xe3, 0x85
        },
        {
            0x39, 0x3d, 0xf1, 0xf1, 0xbd
        },
        {
            0x3b, 0x31, 0x34, 0x0d, 0x91
        },
        {
            0x45, 0xed, 0x28, 0xeb, 0xd3
        },
        {
            0x48, 0xb7, 0x6c, 0xce, 0x69
        },
        {
            0x4b, 0x65, 0x0d, 0xc1, 0xee
        },
        {
            0x4c, 0xbb, 0xf5, 0x5b, 0x23
        },
        {
            0x51, 0x67, 0x67, 0xc5, 0xe0
        },
        {
            0x53, 0x94, 0xe1, 0x75, 0xbf
        },
        {
            0x57, 0x2c, 0x8b, 0x31, 0xae
        },
        {
            0x63, 0xdb, 0x4c, 0x5b, 0x4a
        },
        {
            0x7b, 0x1e, 0x5e, 0x2b, 0x57
        },
        {
            0x85, 0xf3, 0x85, 0xa0, 0xe0
        },
        {
            0xab, 0x1e, 0xe7, 0x7b, 0x72
        },
        {
            0xab, 0x36, 0xe3, 0xeb, 0x76
        },
        {
            0xb1, 0xb8, 0xf9, 0x38, 0x03
        },
        {
            0xb8, 0x5d, 0xd8, 0x53, 0xbd
        },
        {
            0xbf, 0x92, 0xc3, 0xb0, 0xe2
        },
        {
            0xcf, 0x1a, 0xb2, 0xf8, 0x0a
        },
        {
            0xec, 0xa0, 0xcf, 0xb3, 0xff
        },
        {
            0xfc, 0x95, 0xa9, 0x87, 0x35
        }
    };

    static readonly byte[] _cssTable1 =
    {
        0x33, 0x73, 0x3b, 0x26, 0x63, 0x23, 0x6b, 0x76, 0x3e, 0x7e, 0x36, 0x2b, 0x6e, 0x2e, 0x66, 0x7b, 0xd3, 0x93,
        0xdb, 0x06, 0x43, 0x03, 0x4b, 0x96, 0xde, 0x9e, 0xd6, 0x0b, 0x4e, 0x0e, 0x46, 0x9b, 0x57, 0x17, 0x5f, 0x82,
        0xc7, 0x87, 0xcf, 0x12, 0x5a, 0x1a, 0x52, 0x8f, 0xca, 0x8a, 0xc2, 0x1f, 0xd9, 0x99, 0xd1, 0x00, 0x49, 0x09,
        0x41, 0x90, 0xd8, 0x98, 0xd0, 0x01, 0x48, 0x08, 0x40, 0x91, 0x3d, 0x7d, 0x35, 0x24, 0x6d, 0x2d, 0x65, 0x74,
        0x3c, 0x7c, 0x34, 0x25, 0x6c, 0x2c, 0x64, 0x75, 0xdd, 0x9d, 0xd5, 0x04, 0x4d, 0x0d, 0x45, 0x94, 0xdc, 0x9c,
        0xd4, 0x05, 0x4c, 0x0c, 0x44, 0x95, 0x59, 0x19, 0x51, 0x80, 0xc9, 0x89, 0xc1, 0x10, 0x58, 0x18, 0x50, 0x81,
        0xc8, 0x88, 0xc0, 0x11, 0xd7, 0x97, 0xdf, 0x02, 0x47, 0x07, 0x4f, 0x92, 0xda, 0x9a, 0xd2, 0x0f, 0x4a, 0x0a,
        0x42, 0x9f, 0x53, 0x13, 0x5b, 0x86, 0xc3, 0x83, 0xcb, 0x16, 0x5e, 0x1e, 0x56, 0x8b, 0xce, 0x8e, 0xc6, 0x1b,
        0xb3, 0xf3, 0xbb, 0xa6, 0xe3, 0xa3, 0xeb, 0xf6, 0xbe, 0xfe, 0xb6, 0xab, 0xee, 0xae, 0xe6, 0xfb, 0x37, 0x77,
        0x3f, 0x22, 0x67, 0x27, 0x6f, 0x72, 0x3a, 0x7a, 0x32, 0x2f, 0x6a, 0x2a, 0x62, 0x7f, 0xb9, 0xf9, 0xb1, 0xa0,
        0xe9, 0xa9, 0xe1, 0xf0, 0xb8, 0xf8, 0xb0, 0xa1, 0xe8, 0xa8, 0xe0, 0xf1, 0x5d, 0x1d, 0x55, 0x84, 0xcd, 0x8d,
        0xc5, 0x14, 0x5c, 0x1c, 0x54, 0x85, 0xcc, 0x8c, 0xc4, 0x15, 0xbd, 0xfd, 0xb5, 0xa4, 0xed, 0xad, 0xe5, 0xf4,
        0xbc, 0xfc, 0xb4, 0xa5, 0xec, 0xac, 0xe4, 0xf5, 0x39, 0x79, 0x31, 0x20, 0x69, 0x29, 0x61, 0x70, 0x38, 0x78,
        0x30, 0x21, 0x68, 0x28, 0x60, 0x71, 0xb7, 0xf7, 0xbf, 0xa2, 0xe7, 0xa7, 0xef, 0xf2, 0xba, 0xfa, 0xb2, 0xaf,
        0xea, 0xaa, 0xe2, 0xff
    };

    static readonly byte[] _cssTable2 =
    {
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x09, 0x08, 0x0b, 0x0a, 0x0d, 0x0c, 0x0f, 0x0e, 0x12, 0x13,
        0x10, 0x11, 0x16, 0x17, 0x14, 0x15, 0x1b, 0x1a, 0x19, 0x18, 0x1f, 0x1e, 0x1d, 0x1c, 0x24, 0x25, 0x26, 0x27,
        0x20, 0x21, 0x22, 0x23, 0x2d, 0x2c, 0x2f, 0x2e, 0x29, 0x28, 0x2b, 0x2a, 0x36, 0x37, 0x34, 0x35, 0x32, 0x33,
        0x30, 0x31, 0x3f, 0x3e, 0x3d, 0x3c, 0x3b, 0x3a, 0x39, 0x38, 0x49, 0x48, 0x4b, 0x4a, 0x4d, 0x4c, 0x4f, 0x4e,
        0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x5b, 0x5a, 0x59, 0x58, 0x5f, 0x5e, 0x5d, 0x5c, 0x52, 0x53,
        0x50, 0x51, 0x56, 0x57, 0x54, 0x55, 0x6d, 0x6c, 0x6f, 0x6e, 0x69, 0x68, 0x6b, 0x6a, 0x64, 0x65, 0x66, 0x67,
        0x60, 0x61, 0x62, 0x63, 0x7f, 0x7e, 0x7d, 0x7c, 0x7b, 0x7a, 0x79, 0x78, 0x76, 0x77, 0x74, 0x75, 0x72, 0x73,
        0x70, 0x71, 0x92, 0x93, 0x90, 0x91, 0x96, 0x97, 0x94, 0x95, 0x9b, 0x9a, 0x99, 0x98, 0x9f, 0x9e, 0x9d, 0x9c,
        0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x89, 0x88, 0x8b, 0x8a, 0x8d, 0x8c, 0x8f, 0x8e, 0xb6, 0xb7,
        0xb4, 0xb5, 0xb2, 0xb3, 0xb0, 0xb1, 0xbf, 0xbe, 0xbd, 0xbc, 0xbb, 0xba, 0xb9, 0xb8, 0xa4, 0xa5, 0xa6, 0xa7,
        0xa0, 0xa1, 0xa2, 0xa3, 0xad, 0xac, 0xaf, 0xae, 0xa9, 0xa8, 0xab, 0xaa, 0xdb, 0xda, 0xd9, 0xd8, 0xdf, 0xde,
        0xdd, 0xdc, 0xd2, 0xd3, 0xd0, 0xd1, 0xd6, 0xd7, 0xd4, 0xd5, 0xc9, 0xc8, 0xcb, 0xca, 0xcd, 0xcc, 0xcf, 0xce,
        0xc0, 0xc1, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 0xff, 0xfe, 0xfd, 0xfc, 0xfb, 0xfa, 0xf9, 0xf8, 0xf6, 0xf7,
        0xf4, 0xf5, 0xf2, 0xf3, 0xf0, 0xf1, 0xed, 0xec, 0xef, 0xee, 0xe9, 0xe8, 0xeb, 0xea, 0xe4, 0xe5, 0xe6, 0xe7,
        0xe0, 0xe1, 0xe2, 0xe3
    };

    static readonly byte[] _cssTable3 =
    {
        0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24,
        0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d,
        0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6,
        0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff,
        0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24,
        0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d,
        0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6,
        0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff,
        0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24,
        0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d,
        0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6,
        0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff,
        0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24,
        0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d,
        0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6,
        0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff,
        0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24,
        0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d,
        0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6,
        0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff,
        0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24,
        0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d,
        0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6,
        0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff,
        0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24,
        0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d,
        0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6,
        0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff, 0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff,
        0x00, 0x24, 0x49, 0x6d, 0x92, 0xb6, 0xdb, 0xff
    };

    static readonly byte[] _cssTable4 =
    {
        0x00, 0x80, 0x40, 0xc0, 0x20, 0xa0, 0x60, 0xe0, 0x10, 0x90, 0x50, 0xd0, 0x30, 0xb0, 0x70, 0xf0, 0x08, 0x88,
        0x48, 0xc8, 0x28, 0xa8, 0x68, 0xe8, 0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8, 0x04, 0x84, 0x44, 0xc4,
        0x24, 0xa4, 0x64, 0xe4, 0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4, 0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac,
        0x6c, 0xec, 0x1c, 0x9c, 0x5c, 0xdc, 0x3c, 0xbc, 0x7c, 0xfc, 0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2,
        0x12, 0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2, 0x0a, 0x8a, 0x4a, 0xca, 0x2a, 0xaa, 0x6a, 0xea, 0x1a, 0x9a,
        0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa, 0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6, 0x16, 0x96, 0x56, 0xd6,
        0x36, 0xb6, 0x76, 0xf6, 0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee, 0x1e, 0x9e, 0x5e, 0xde, 0x3e, 0xbe,
        0x7e, 0xfe, 0x01, 0x81, 0x41, 0xc1, 0x21, 0xa1, 0x61, 0xe1, 0x11, 0x91, 0x51, 0xd1, 0x31, 0xb1, 0x71, 0xf1,
        0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9, 0x19, 0x99, 0x59, 0xd9, 0x39, 0xb9, 0x79, 0xf9, 0x05, 0x85,
        0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5, 0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 0xf5, 0x0d, 0x8d, 0x4d, 0xcd,
        0x2d, 0xad, 0x6d, 0xed, 0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd, 0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3,
        0x63, 0xe3, 0x13, 0x93, 0x53, 0xd3, 0x33, 0xb3, 0x73, 0xf3, 0x0b, 0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb,
        0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb, 0x07, 0x87, 0x47, 0xc7, 0x27, 0xa7, 0x67, 0xe7, 0x17, 0x97,
        0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7, 0x0f, 0x8f, 0x4f, 0xcf, 0x2f, 0xaf, 0x6f, 0xef, 0x1f, 0x9f, 0x5f, 0xdf,
        0x3f, 0xbf, 0x7f, 0xff
    };

    static readonly byte[] _cssTable5 =
    {
        0xff, 0x7f, 0xbf, 0x3f, 0xdf, 0x5f, 0x9f, 0x1f, 0xef, 0x6f, 0xaf, 0x2f, 0xcf, 0x4f, 0x8f, 0x0f, 0xf7, 0x77,
        0xb7, 0x37, 0xd7, 0x57, 0x97, 0x17, 0xe7, 0x67, 0xa7, 0x27, 0xc7, 0x47, 0x87, 0x07, 0xfb, 0x7b, 0xbb, 0x3b,
        0xdb, 0x5b, 0x9b, 0x1b, 0xeb, 0x6b, 0xab, 0x2b, 0xcb, 0x4b, 0x8b, 0x0b, 0xf3, 0x73, 0xb3, 0x33, 0xd3, 0x53,
        0x93, 0x13, 0xe3, 0x63, 0xa3, 0x23, 0xc3, 0x43, 0x83, 0x03, 0xfd, 0x7d, 0xbd, 0x3d, 0xdd, 0x5d, 0x9d, 0x1d,
        0xed, 0x6d, 0xad, 0x2d, 0xcd, 0x4d, 0x8d, 0x0d, 0xf5, 0x75, 0xb5, 0x35, 0xd5, 0x55, 0x95, 0x15, 0xe5, 0x65,
        0xa5, 0x25, 0xc5, 0x45, 0x85, 0x05, 0xf9, 0x79, 0xb9, 0x39, 0xd9, 0x59, 0x99, 0x19, 0xe9, 0x69, 0xa9, 0x29,
        0xc9, 0x49, 0x89, 0x09, 0xf1, 0x71, 0xb1, 0x31, 0xd1, 0x51, 0x91, 0x11, 0xe1, 0x61, 0xa1, 0x21, 0xc1, 0x41,
        0x81, 0x01, 0xfe, 0x7e, 0xbe, 0x3e, 0xde, 0x5e, 0x9e, 0x1e, 0xee, 0x6e, 0xae, 0x2e, 0xce, 0x4e, 0x8e, 0x0e,
        0xf6, 0x76, 0xb6, 0x36, 0xd6, 0x56, 0x96, 0x16, 0xe6, 0x66, 0xa6, 0x26, 0xc6, 0x46, 0x86, 0x06, 0xfa, 0x7a,
        0xba, 0x3a, 0xda, 0x5a, 0x9a, 0x1a, 0xea, 0x6a, 0xaa, 0x2a, 0xca, 0x4a, 0x8a, 0x0a, 0xf2, 0x72, 0xb2, 0x32,
        0xd2, 0x52, 0x92, 0x12, 0xe2, 0x62, 0xa2, 0x22, 0xc2, 0x42, 0x82, 0x02, 0xfc, 0x7c, 0xbc, 0x3c, 0xdc, 0x5c,
        0x9c, 0x1c, 0xec, 0x6c, 0xac, 0x2c, 0xcc, 0x4c, 0x8c, 0x0c, 0xf4, 0x74, 0xb4, 0x34, 0xd4, 0x54, 0x94, 0x14,
        0xe4, 0x64, 0xa4, 0x24, 0xc4, 0x44, 0x84, 0x04, 0xf8, 0x78, 0xb8, 0x38, 0xd8, 0x58, 0x98, 0x18, 0xe8, 0x68,
        0xa8, 0x28, 0xc8, 0x48, 0x88, 0x08, 0xf0, 0x70, 0xb0, 0x30, 0xd0, 0x50, 0x90, 0x10, 0xe0, 0x60, 0xa0, 0x20,
        0xc0, 0x40, 0x80, 0x00
    };

    static readonly byte[] _encryptTable0 =
    {
        0xB7, 0xF4, 0x82, 0x57, 0xDA, 0x4D, 0xDB, 0xE2, 0x2F, 0x52, 0x1A, 0xA8, 0x68, 0x5A, 0x8A, 0xFF, 0xFB, 0x0E,
        0x6D, 0x35, 0xF7, 0x5C, 0x76, 0x12, 0xCE, 0x25, 0x79, 0x29, 0x39, 0x62, 0x08, 0x24, 0xA5, 0x85, 0x7B, 0x56,
        0x01, 0x23, 0x68, 0xCF, 0x0A, 0xE2, 0x5A, 0xED, 0x3D, 0x59, 0xB0, 0xA9, 0xB0, 0x2C, 0xF2, 0xB8, 0xEF, 0x32,
        0xA9, 0x40, 0x80, 0x71, 0xAF, 0x1E, 0xDE, 0x8F, 0x58, 0x88, 0xB8, 0x3A, 0xD0, 0xFC, 0xC4, 0x1E, 0xB5, 0xA0,
        0xBB, 0x3B, 0x0F, 0x01, 0x7E, 0x1F, 0x9F, 0xD9, 0xAA, 0xB8, 0x3D, 0x9D, 0x74, 0x1E, 0x25, 0xDB, 0x37, 0x56,
        0x8F, 0x16, 0xBA, 0x49, 0x2B, 0xAC, 0xD0, 0xBD, 0x95, 0x20, 0xBE, 0x7A, 0x28, 0xD0, 0x51, 0x64, 0x63, 0x1C,
        0x7F, 0x66, 0x10, 0xBB, 0xC4, 0x56, 0x1A, 0x04, 0x6E, 0x0A, 0xEC, 0x9C, 0xD6, 0xE8, 0x9A, 0x7A, 0xCF, 0x8C,
        0xDB, 0xB1, 0xEF, 0x71, 0xDE, 0x31, 0xFF, 0x54, 0x3E, 0x5E, 0x07, 0x69, 0x96, 0xB0, 0xCF, 0xDD, 0x9E, 0x47,
        0xC7, 0x96, 0x8F, 0xE4, 0x2B, 0x59, 0xC6, 0xEE, 0xB9, 0x86, 0x9A, 0x64, 0x84, 0x72, 0xE2, 0x5B, 0xA2, 0x96,
        0x58, 0x99, 0x50, 0x03, 0xF5, 0x38, 0x4D, 0x02, 0x7D, 0xE7, 0x7D, 0x75, 0xA7, 0xB8, 0x67, 0x87, 0x84, 0x3F,
        0x1D, 0x11, 0xE5, 0xFC, 0x1E, 0xD3, 0x83, 0x16, 0xA5, 0x29, 0xF6, 0xC7, 0x15, 0x61, 0x29, 0x1A, 0x43, 0x4F,
        0x9B, 0xAF, 0xC5, 0x87, 0x34, 0x6C, 0x0F, 0x3B, 0xA8, 0x1D, 0x45, 0x58, 0x25, 0xDC, 0xA8, 0xA3, 0x3B, 0xD1,
        0x79, 0x1B, 0x48, 0xF2, 0xE9, 0x93, 0x1F, 0xFC, 0xDB, 0x2A, 0x90, 0xA9, 0x8A, 0x3D, 0x39, 0x18, 0xA3, 0x8E,
        0x58, 0x6C, 0xE0, 0x12, 0xBB, 0x25, 0xCD, 0x71, 0x22, 0xA2, 0x64, 0xC6, 0xE7, 0xFB, 0xAD, 0x94, 0x77, 0x04,
        0x9A, 0x39, 0xCF, 0x7C
    };

    static readonly byte[] _encryptTable1 =
    {
        0x8C, 0x47, 0xB0, 0xE1, 0xEB, 0xFC, 0xEB, 0x56, 0x10, 0xE5, 0x2C, 0x1A, 0x5D, 0xEF, 0xBE, 0x4F, 0x08, 0x75,
        0x97, 0x4B, 0x0E, 0x25, 0x8E, 0x6E, 0x39, 0x5A, 0x87, 0x53, 0xC4, 0x1F, 0xF4, 0x5C, 0x4E, 0xE6, 0x99, 0x30,
        0xE0, 0x42, 0x88, 0xAB, 0xE5, 0x85, 0xBC, 0x8F, 0xD8, 0x3C, 0x54, 0xC9, 0x53, 0x47, 0x18, 0xD6, 0x06, 0x5B,
        0x41, 0x2C, 0x67, 0x1E, 0x41, 0x74, 0x33, 0xE2, 0xB4, 0xE0, 0x23, 0x29, 0x42, 0xEA, 0x55, 0x0F, 0x25, 0xB4,
        0x24, 0x2C, 0x99, 0x13, 0xEB, 0x0A, 0x0B, 0xC9, 0xF9, 0x63, 0x67, 0x43, 0x2D, 0xC7, 0x7D, 0x07, 0x60, 0x89,
        0xD1, 0xCC, 0xE7, 0x94, 0x77, 0x74, 0x9B, 0x7E, 0xD7, 0xE6, 0xFF, 0xBB, 0x68, 0x14, 0x1E, 0xA3, 0x25, 0xDE,
        0x3A, 0xA3, 0x54, 0x7B, 0x87, 0x9D, 0x50, 0xCA, 0x27, 0xC3, 0xA4, 0x50, 0x91, 0x27, 0xD4, 0xB0, 0x82, 0x41,
        0x97, 0x79, 0x94, 0x82, 0xAC, 0xC7, 0x8E, 0xA5, 0x4E, 0xAA, 0x78, 0x9E, 0xE0, 0x42, 0xBA, 0x28, 0xEA, 0xB7,
        0x74, 0xAD, 0x35, 0xDA, 0x92, 0x60, 0x7E, 0xD2, 0x0E, 0xB9, 0x24, 0x5E, 0x39, 0x4F, 0x5E, 0x63, 0x09, 0xB5,
        0xFA, 0xBF, 0xF1, 0x22, 0x55, 0x1C, 0xE2, 0x25, 0xDB, 0xC5, 0xD8, 0x50, 0x03, 0x98, 0xC4, 0xAC, 0x2E, 0x11,
        0xB4, 0x38, 0x4D, 0xD0, 0xB9, 0xFC, 0x2D, 0x3C, 0x08, 0x04, 0x5A, 0xEF, 0xCE, 0x32, 0xFB, 0x4C, 0x92, 0x1E,
        0x4B, 0xFB, 0x1A, 0xD0, 0xE2, 0x3E, 0xDA, 0x6E, 0x7C, 0x4D, 0x56, 0xC3, 0x3F, 0x42, 0xB1, 0x3A, 0x23, 0x4D,
        0x6E, 0x84, 0x56, 0x68, 0xF4, 0x0E, 0x03, 0x64, 0xD0, 0xA9, 0x92, 0x2F, 0x8B, 0xBC, 0x39, 0x9C, 0xAC, 0x09,
        0x5E, 0xEE, 0xE5, 0x97, 0xBF, 0xA5, 0xCE, 0xFA, 0x28, 0x2C, 0x6D, 0x4F, 0xEF, 0x77, 0xAA, 0x1B, 0x79, 0x8E,
        0x97, 0xB4, 0xC3, 0xF4
    };

    static readonly byte[] _encryptTable2 =
    {
        0xB7, 0x75, 0x81, 0xD5, 0xDC, 0xCA, 0xDE, 0x66, 0x23, 0xDF, 0x15, 0x26, 0x62, 0xD1, 0x83, 0x77, 0xE3, 0x97,
        0x76, 0xAF, 0xE9, 0xC3, 0x6B, 0x8E, 0xDA, 0xB0, 0x6E, 0xBF, 0x2B, 0xF1, 0x19, 0xB4, 0x95, 0x34, 0x48, 0xE4,
        0x37, 0x94, 0x5D, 0x7B, 0x36, 0x5F, 0x65, 0x53, 0x07, 0xE2, 0x89, 0x11, 0x98, 0x85, 0xD9, 0x12, 0xC1, 0x9D,
        0x84, 0xEC, 0xA4, 0xD4, 0x88, 0xB8, 0xFC, 0x2C, 0x79, 0x28, 0xD8, 0xDB, 0xB3, 0x1E, 0xA2, 0xF9, 0xD0, 0x44,
        0xD7, 0xD6, 0x60, 0xEF, 0x14, 0xF4, 0xF6, 0x31, 0xD2, 0x41, 0x46, 0x67, 0x0A, 0xE1, 0x58, 0x27, 0x43, 0xA3,
        0xF8, 0xE0, 0xC8, 0xBA, 0x5A, 0x5C, 0x80, 0x6C, 0xC6, 0xF2, 0xE8, 0xAD, 0x7D, 0x04, 0x0D, 0xB9, 0x3C, 0xC2,
        0x25, 0xBD, 0x49, 0x63, 0x8C, 0x9F, 0x51, 0xCE, 0x20, 0xC5, 0xA1, 0x50, 0x92, 0x2D, 0xDD, 0xBC, 0x8D, 0x4F,
        0x9A, 0x71, 0x2F, 0x30, 0x1D, 0x73, 0x39, 0x13, 0xFB, 0x1A, 0xCB, 0x24, 0x59, 0xFE, 0x05, 0x96, 0x57, 0x0F,
        0x1F, 0xCF, 0x54, 0xBE, 0xF5, 0x06, 0x1B, 0xB2, 0x6D, 0xD3, 0x4D, 0x32, 0x56, 0x21, 0x33, 0x0B, 0x52, 0xE7,
        0xAB, 0xEB, 0xA6, 0x74, 0x00, 0x4C, 0xB1, 0x7F, 0x82, 0x99, 0x87, 0x0E, 0x5E, 0xC0, 0x8F, 0xEE, 0x6F, 0x55,
        0xF3, 0x7E, 0x08, 0x90, 0xFA, 0xB6, 0x64, 0x70, 0x47, 0x4A, 0x17, 0xA7, 0xB5, 0x40, 0x8A, 0x38, 0xE5, 0x68,
        0x3E, 0x8B, 0x69, 0xAA, 0x9B, 0x42, 0xA5, 0x10, 0x01, 0x35, 0xFD, 0x61, 0x9E, 0xE6, 0x16, 0x9C, 0x86, 0xED,
        0xCD, 0x2E, 0xFF, 0xC4, 0x5B, 0xA0, 0xAE, 0xCC, 0x4B, 0x3B, 0x03, 0xBB, 0x1C, 0x2A, 0xAC, 0x0C, 0x3F, 0x93,
        0xC7, 0x72, 0x7A, 0x09, 0x22, 0x3D, 0x45, 0x78, 0xA9, 0xA8, 0xEA, 0xC9, 0x6A, 0xF7, 0x29, 0x91, 0xF0, 0x02,
        0x18, 0x3A, 0x4E, 0x7C
    };

    static readonly byte[] _encryptTable3 =
    {
        0x73, 0x51, 0x95, 0xE1, 0x12, 0xE4, 0xC0, 0x58, 0xEE, 0xF2, 0x08, 0x1B, 0xA9, 0xFA, 0x98, 0x4C, 0xA7, 0x33,
        0xE2, 0x1B, 0xA7, 0x6D, 0xF5, 0x30, 0x97, 0x1D, 0xF3, 0x02, 0x60, 0x5A, 0x82, 0x0F, 0x91, 0xD0, 0x9C, 0x10,
        0x39, 0x7A, 0x83, 0x85, 0x3B, 0xB2, 0xB8, 0xAE, 0x0C, 0x09, 0x52, 0xEA, 0x1C, 0xE1, 0x8D, 0x66, 0x4F, 0xF3,
        0xDA, 0x92, 0x29, 0xB9, 0xD5, 0xC5, 0x77, 0x47, 0x22, 0x53, 0x14, 0xF7, 0xAF, 0x22, 0x64, 0xDF, 0xC6, 0x72,
        0x12, 0xF3, 0x75, 0xDA, 0xD7, 0xD7, 0xE5, 0x02, 0x9E, 0xED, 0xDA, 0xDB, 0x4C, 0x47, 0xCE, 0x91, 0x06, 0x06,
        0x6D, 0x55, 0x8B, 0x19, 0xC9, 0xEF, 0x8C, 0x80, 0x1A, 0x0E, 0xEE, 0x4B, 0xAB, 0xF2, 0x08, 0x5C, 0xE9, 0x37,
        0x26, 0x5E, 0x9A, 0x90, 0x00, 0xF3, 0x0D, 0xB2, 0xA6, 0xA3, 0xF7, 0x26, 0x17, 0x48, 0x88, 0xC9, 0x0E, 0x2C,
        0xC9, 0x02, 0xE7, 0x18, 0x05, 0x4B, 0xF3, 0x39, 0xE1, 0x20, 0x02, 0x0D, 0x40, 0xC7, 0xCA, 0xB9, 0x48, 0x30,
        0x57, 0x67, 0xCC, 0x06, 0xBF, 0xAC, 0x81, 0x08, 0x24, 0x7A, 0xD4, 0x8B, 0x19, 0x8E, 0xAC, 0xB4, 0x5A, 0x0F,
        0x73, 0x13, 0xAC, 0x9E, 0xDA, 0xB6, 0xB8, 0x96, 0x5B, 0x60, 0x88, 0xE1, 0x81, 0x3F, 0x07, 0x86, 0x37, 0x2D,
        0x79, 0x14, 0x52, 0xEA, 0x73, 0xDF, 0x3D, 0x09, 0xC8, 0x25, 0x48, 0xD8, 0x75, 0x60, 0x9A, 0x08, 0x27, 0x4A,
        0x2C, 0xB9, 0xA8, 0x8B, 0x8A, 0x73, 0x62, 0x37, 0x16, 0x02, 0xBD, 0xC1, 0x0E, 0x56, 0x54, 0x3E, 0x14, 0x5F,
        0x8C, 0x8F, 0x6E, 0x75, 0x1C, 0x07, 0x39, 0x7B, 0x4B, 0xDB, 0xD3, 0x4B, 0x1E, 0xC8, 0x7E, 0xFE, 0x3E, 0x72,
        0x16, 0x83, 0x7D, 0xEE, 0xF5, 0xCA, 0xC5, 0x18, 0xF9, 0xD8, 0x68, 0xAB, 0x38, 0x85, 0xA8, 0xF0, 0xA1, 0x73,
        0x9F, 0x5D, 0x19, 0x0B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x33, 0x72, 0x39, 0x25, 0x67, 0x26,
        0x6D, 0x71, 0x36, 0x77, 0x3C, 0x20, 0x62, 0x23, 0x68, 0x74, 0xC3, 0x82, 0xC9, 0x15, 0x57, 0x16, 0x5D, 0x81
    };

    static readonly byte[,] _permutationChallenge =
    {
        {
            1, 3, 0, 7, 5, 2, 9, 6, 4, 8
        },
        {
            6, 1, 9, 3, 8, 5, 7, 4, 0, 2
        },
        {
            4, 0, 3, 5, 7, 2, 8, 6, 1, 9
        }
    };

    static readonly byte[,] _permutationVariant =
    {
        {
            0x0a, 0x08, 0x0e, 0x0c, 0x0b, 0x09, 0x0f, 0x0d, 0x1a, 0x18, 0x1e, 0x1c, 0x1b, 0x19, 0x1f, 0x1d, 0x02, 0x00,
            0x06, 0x04, 0x03, 0x01, 0x07, 0x05, 0x12, 0x10, 0x16, 0x14, 0x13, 0x11, 0x17, 0x15
        },
        {
            0x12, 0x1a, 0x16, 0x1e, 0x02, 0x0a, 0x06, 0x0e, 0x10, 0x18, 0x14, 0x1c, 0x00, 0x08, 0x04, 0x0c, 0x13, 0x1b,
            0x17, 0x1f, 0x03, 0x0b, 0x07, 0x0f, 0x11, 0x19, 0x15, 0x1d, 0x01, 0x09, 0x05, 0x0d
        }
    };

    static readonly byte[] _variants =
    {
        0xB7, 0x74, 0x85, 0xD0, 0xCC, 0xDB, 0xCA, 0x73, 0x03, 0xFE, 0x31, 0x03, 0x52, 0xE0, 0xB7, 0x42, 0x63, 0x16,
        0xF2, 0x2A, 0x79, 0x52, 0xFF, 0x1B, 0x7A, 0x11, 0xCA, 0x1A, 0x9B, 0x40, 0xAD, 0x01
    };

    static readonly byte[] _secret =
    {
        0x55, 0xD6, 0xC4, 0xC5, 0x28
    };

    /// <summary>
    ///     The disc key returned by the logical unit is encoded with the bus key to prevent man-in-the-middle attacks.
    ///     This method returns a structure with the decoded key included.
    /// </summary>
    /// <param name="response">The encoded key from the logical unit.</param>
    /// <param name="busKey">The bus key from the logical unit.</param>
    /// <returns>A DiscKey struct with the decoded key.</returns>
    public static CSS_CPRM.DiscKey? DecodeDiscKey(byte[] response, byte[] busKey)
    {
        if(response.Length != 2052 ||
           busKey.Length   != 5)
            return null;

        byte[] key = response.Skip(4).Take(2048).ToArray();

        for(uint i = 0; i < key.Length; i++)
            key[i] ^= busKey[4 - (i % busKey.Length)];

        return new CSS_CPRM.DiscKey
        {
            DataLength = (ushort)((response[0] << 8) + response[1]),
            Reserved1  = response[2],
            Reserved2  = response[3],
            Key        = key
        };
    }

    /// <summary>
    ///     The title key returned by the logical unit is encoded with the bus key to prevent man-in-the-middle attacks.
    ///     This method returns a structure with the decoded key included.
    /// </summary>
    /// <param name="response">The encoded key from the logical unit.</param>
    /// <param name="busKey">The bus key from the logical unit.</param>
    /// <returns>A TitleKey struct with the decoded key.</returns>
    public static CSS_CPRM.TitleKey? DecodeTitleKey(byte[] response, byte[] busKey)
    {
        if(response.Length != 12 ||
           busKey.Length   != 5)
            return null;

        byte[] key = response.Skip(5).Take(5).ToArray();

        for(uint i = 0; i < key.Length; i++)
            key[i] ^= busKey[4 - (i % busKey.Length)];

        return new CSS_CPRM.TitleKey
        {
            DataLength = (ushort)((response[0] << 8) + response[1]),
            Reserved1  = response[2],
            Reserved2  = response[3],
            CMI        = response[4],
            Key        = key,
            Reserved3  = response[10],
            Reserved4  = response[11]
        };
    }

    /// <summary>Takes a challenge and a variant and encrypts it according to the key type.</summary>
    /// <param name="keyType">The type of key to encrypt.</param>
    /// <param name="variant"></param>
    /// <param name="challenge">The challenge sent to the logical unit.</param>
    /// <param name="key">The encrypted key.</param>
    /// <returns>The encrypted key.</returns>
    public static void EncryptKey(DvdCssKeyType keyType, uint variant, byte[] challenge, out byte[] key)
    {
        byte[] bits    = new byte[30];
        byte[] scratch = new byte[10];
        byte   index   = sizeof(byte) * 30;
        byte[] temp1   = new byte[5];
        byte[] temp2   = new byte[5];
        byte   carry   = 0;
        key = new byte[5];

        for(int i = 9; i >= 0; --i)
            scratch[i] = challenge[_permutationChallenge[(uint)keyType, i]];

        byte cssVariant = (byte)(keyType == 0 ? variant : _permutationVariant[(uint)keyType - 1, variant]);

        for(int i = 5; --i >= 0;)
            temp1[i] = (byte)(scratch[5 + i] ^ _secret[i] ^ _encryptTable2[i]);

        uint lfsr0 = (uint)((temp1[0] << 17) | (temp1[1] << 9) | ((temp1[2] & ~7) << 1) | 8 | (temp1[2] & 7));
        uint lfsr1 = (uint)((temp1[3] << 9)  | 0x100           | temp1[4]);

        do
        {
            byte val = 0;

            for(int bit = 0; bit < 8; ++bit)
            {
                byte oLfsr0 = (byte)(((lfsr0 >> 24) ^ (lfsr0 >> 21) ^ (lfsr0 >> 20) ^ (lfsr0 >> 12)) & 1);
                lfsr0 = (lfsr0 << 1) | oLfsr0;

                byte oLfsr1 = (byte)(((lfsr1 >> 16) ^ (lfsr1 >> 2)) & 1);
                lfsr1 = (lfsr1 << 1) | oLfsr1;

                byte combined = (byte)(Convert.ToByte(oLfsr1 == 0) + carry + Convert.ToByte(oLfsr0 == 0));
                carry =  (byte)((combined >> 1) & 1);
                val   |= (byte)((combined & 1) << bit);
            }

            bits[--index] = val;
        } while(index > 0);

        byte cse  = (byte)(_variants[cssVariant] ^ _encryptTable2[cssVariant]);
        int  term = 0;

        for(int i = 5; --i >= 0; term = scratch[i])
        {
            index = (byte)(bits[25 + i]          ^ scratch[i]);
            index = (byte)(_encryptTable1[index] ^ ~_encryptTable2[index] ^ cse);

            temp1[i] = (byte)(_encryptTable2[index] ^ _encryptTable3[index] ^ term);
        }

        temp1[4] ^= temp1[0];
        term     =  0;

        for(int i = 5; --i >= 0; term = temp1[i])
        {
            index = (byte)(bits[20 + i]          ^ temp1[i]);
            index = (byte)(_encryptTable1[index] ^ ~_encryptTable2[index] ^ cse);

            temp2[i] = (byte)(_encryptTable2[index] ^ _encryptTable3[index] ^ term);
        }

        temp2[4] ^= temp2[0];
        term     =  0;

        for(int i = 5; --i >= 0; term = temp2[i])
        {
            index = (byte)(bits[15 + i]          ^ temp2[i]);
            index = (byte)(_encryptTable1[index] ^ ~_encryptTable2[index] ^ cse);
            index = (byte)(_encryptTable2[index] ^ _encryptTable3[index]  ^ term);

            temp1[i] = (byte)(_encryptTable0[index] ^ _encryptTable2[index]);
        }

        temp1[4] ^= temp1[0];
        term     =  0;

        for(int i = 5; --i >= 0; term = temp1[i])
        {
            index = (byte)(bits[10 + i]          ^ temp1[i]);
            index = (byte)(_encryptTable1[index] ^ ~_encryptTable2[index] ^ cse);
            index = (byte)(_encryptTable2[index] ^ _encryptTable3[index]  ^ term);

            temp2[i] = (byte)(_encryptTable0[index] ^ _encryptTable2[index]);
        }

        temp2[4] ^= temp2[0];
        term     =  0;

        for(int i = 5; --i >= 0; term = temp2[i])
        {
            index = (byte)(bits[5 + i]           ^ temp2[i]);
            index = (byte)(_encryptTable1[index] ^ ~_encryptTable2[index] ^ cse);

            temp1[i] = (byte)(_encryptTable2[index] ^ _encryptTable3[index] ^ term);
        }

        temp1[4] ^= temp1[0];
        term     =  0;

        for(int i = 5; --i >= 0; term = temp1[i])
        {
            index = (byte)(bits[i]               ^ temp1[i]);
            index = (byte)(_encryptTable1[index] ^ ~_encryptTable2[index] ^ cse);

            key[i] = (byte)(_encryptTable2[index] ^ _encryptTable3[index] ^ term);
        }
    }

    /// <summary>Takes an encrypted key and its crypto and returns the key decrypted.</summary>
    /// <param name="invert"></param>
    /// <param name="cryptoKey">The key used to encrypt the data.</param>
    /// <param name="encryptedKey">The encrypted data.</param>
    /// <param name="decryptedKey">The decrypted data.</param>
    public static void DecryptKey(byte invert, byte[] cryptoKey, byte[] encryptedKey, out byte[] decryptedKey)
    {
        decryptedKey = new byte[5];
        byte[] k = new byte[5];

        uint lfsr1Lo = (uint)(cryptoKey[0] | 0x100);
        uint lfsr1Hi = cryptoKey[1];

        uint lfsr0 = (uint)(((cryptoKey[4] << 17) | (cryptoKey[3] << 9) | (cryptoKey[2] << 1)) + 8 -
                            (cryptoKey[2] & 7));

        lfsr0 = (uint)((_cssTable4[lfsr0 & 0xff] << 24) | (_cssTable4[(lfsr0 >> 8) & 0xff] << 16) |
                       (_cssTable4[(lfsr0 >> 16) & 0xff] << 8) | _cssTable4[(lfsr0 >> 24) & 0xff]);

        uint combined = 0;

        for(uint i = 0; i < 5; i++)
        {
            byte oLfsr1 = (byte)(_cssTable2[lfsr1Hi] ^ _cssTable3[lfsr1Lo]);
            lfsr1Hi = lfsr1Lo >> 1;
            lfsr1Lo = ((lfsr1Lo & 1) << 8) ^ oLfsr1;
            oLfsr1  = _cssTable4[oLfsr1];
            byte oLfsr0 = (byte)(((((((lfsr0 >> 8) ^ lfsr0) >> 1) ^ lfsr0) >> 3) ^ lfsr0) >> 7);
            lfsr0    =   (lfsr0 >> 8) | ((uint)oLfsr0 << 24);
            combined +=  (uint)((oLfsr0 ^ invert) + oLfsr1);
            k[i]     =   (byte)(combined & 0xff);
            combined >>= 8;
        }

        decryptedKey[4] = (byte)(k[4] ^ _cssTable1[encryptedKey[4]] ^ encryptedKey[3]);
        decryptedKey[3] = (byte)(k[3] ^ _cssTable1[encryptedKey[3]] ^ encryptedKey[2]);
        decryptedKey[2] = (byte)(k[2] ^ _cssTable1[encryptedKey[2]] ^ encryptedKey[1]);
        decryptedKey[1] = (byte)(k[1] ^ _cssTable1[encryptedKey[1]] ^ encryptedKey[0]);
        decryptedKey[0] = (byte)(k[0] ^ _cssTable1[encryptedKey[0]] ^ decryptedKey[4]);

        decryptedKey[4] = (byte)(k[4] ^ _cssTable1[decryptedKey[4]] ^ decryptedKey[3]);
        decryptedKey[3] = (byte)(k[3] ^ _cssTable1[decryptedKey[3]] ^ decryptedKey[2]);
        decryptedKey[2] = (byte)(k[2] ^ _cssTable1[decryptedKey[2]] ^ decryptedKey[1]);
        decryptedKey[1] = (byte)(k[1] ^ _cssTable1[decryptedKey[1]] ^ decryptedKey[0]);
        decryptedKey[0] = (byte)(k[0] ^ _cssTable1[decryptedKey[0]]);
    }

    public static void DecryptTitleKey(byte invert, byte[] cryptoKey, byte[] encryptedKey, out byte[] decryptedKey) =>
        DecryptKey(invert, cryptoKey, encryptedKey, out decryptedKey);

    /// <summary>Takes an bytearray of encrypted keys, decrypts them and returns the correctly decrypted key.</summary>
    /// <param name="encryptedKeys">Encrypted keys to try to decrypt.</param>
    /// <param name="decryptedKey">The decrypted key if found.</param>
    public static void DecryptDiscKey(byte[] encryptedKeys, out byte[]? decryptedKey)
    {
        decryptedKey = new byte[5];
        byte[] verificationKey = encryptedKeys.Take(5).ToArray();

        for(uint n = 0; n < _playerKeys.GetLength(0); n++)
        {
            byte[] currentPlayerKey = Enumerable.Range(0, _playerKeys.GetLength(1)).Select(x => _playerKeys[n, x]).
                                                 ToArray();

            for(uint i = 1; i < 409; i++)
            {
                DecryptKey(0, currentPlayerKey, encryptedKeys.Skip(5 * (int)i).Take(5).ToArray(), out decryptedKey);

                // The first key in the structure is the key encrypted with itself, so we can use it to verify
                // we found the correct key.
                DecryptKey(0, decryptedKey, verificationKey, out byte[] verify);

                if(decryptedKey.SequenceEqual(verify))
                    return;
            }
        }

        // No correct key was found.
        decryptedKey = null;
    }

    /// <summary>Takes a sector and a decrypted title key and returns the decrypted sector.</summary>
    /// <param name="sectorData">Encrypted sector data.</param>
    /// <param name="cmiData">The Copyright Management Information.</param>
    /// <param name="keyData">The encryption keys.</param>
    /// <param name="blocks">Number of sectors in <c>sectorData</c>.</param>
    /// <param name="blockSize">Size of one sector.</param>
    /// <returns>The decrypted sector.</returns>
    public static byte[] DecryptSector(byte[] sectorData, byte[] cmiData, byte[] keyData, uint blocks = 1,
                                       uint blockSize = 2048)
    {
        if(cmiData.All(cmi => (cmi & 0x80) >> 7 == 0) ||
           keyData.All(k => k                   == 0))
            return sectorData;

        byte[] decryptedBuffer = new byte[sectorData.Length];

        for(uint j = 0; j < blocks; j++)
        {
            byte[] currentKey    = keyData.Skip((int)(j    * 5)).Take(5).ToArray();
            byte[] currentSector = sectorData.Skip((int)(j * blockSize)).Take((int)blockSize).ToArray();

            // If the CMI tells use the sector isn't encrypted or
            // if the key is all zeroes or
            // if the MPEG Packetized Elementary Stream scrambling control value tells us the packet is not scrambled
            if((cmiData[j] & 0x80) >> 7 == 0 ||
               currentKey.All(k => k == 0)   ||
               (currentSector[20] & 0x30) >> 4 == 0)
            {
                // Sector is not encrypted
                Array.Copy(currentSector, 0, decryptedBuffer, (int)(j * blockSize), blockSize);

                continue;
            }

            uint lfsr1Lo = (uint)(currentKey[0] ^ currentSector[0x54]) | 0x100;
            uint lfsr1Hi = (uint)currentKey[1] ^ currentSector[0x55];

            uint lfsr0 = (uint)((currentKey[2]    | (currentKey[3]    << 8) | (currentKey[4]    << 16)) ^
                                (sectorData[0x56] | (sectorData[0x57] << 8) | (sectorData[0x58] << 16)));

            uint oLfsr1 = lfsr0 & 7;
            lfsr0 = (lfsr0 * 2) + 8 - oLfsr1;

            uint combined = 0;

            for(uint i = 128; i < blockSize; i++)
            {
                oLfsr1  = (uint)(_cssTable2[lfsr1Hi] ^ _cssTable3[lfsr1Lo]);
                lfsr1Hi = lfsr1Lo >> 1;
                lfsr1Lo = ((lfsr1Lo & 1) << 8) ^ oLfsr1;
                oLfsr1  = _cssTable5[oLfsr1];
                uint oLfsr0 = (((((((lfsr0 >> 3) ^ lfsr0) >> 1) ^ lfsr0) >> 8) ^ lfsr0) >> 5) & 0xff;
                lfsr0            =   (lfsr0 >> 8) | (oLfsr0 << 24);
                lfsr0            =   (lfsr0 << 8) | oLfsr0;
                oLfsr0           =   _cssTable4[oLfsr0];
                combined         +=  oLfsr0 + oLfsr1;
                currentSector[i] =   (byte)(_cssTable1[currentSector[i]] ^ (combined & 0xff));
                combined         >>= 8;
            }

            Array.Copy(currentSector, 0, decryptedBuffer, (int)(j * blockSize), blockSize);
        }

        return decryptedBuffer;
    }

    /// <summary>Takes an RPC state from the drive and a CMI from a disc and checks if the regions are compatible.</summary>
    /// <param name="rpc">The <c>RegionalPlaybackControlState</c> from drive.</param>
    /// <param name="cmi">The <c>LeadInCopyright</c> from disc.</param>
    /// <returns><c>true</c> if the regions are compatible, else <c>false</c></returns>
    public static bool CheckRegion(CSS_CPRM.RegionalPlaybackControlState rpc, CSS_CPRM.LeadInCopyright cmi)
    {
        // if disc region is all or none, we cannot do anything but try to read it as is
        if(cmi.RegionInformation is 0xFF or 0x00)
            return true;

        return ((rpc.RegionMask & 0x01) == (cmi.RegionInformation & 0x01) && (rpc.RegionMask & 0x01) != 0x01) ||
               ((rpc.RegionMask & 0x02) == (cmi.RegionInformation & 0x02) && (rpc.RegionMask & 0x02) != 0x02) ||
               ((rpc.RegionMask & 0x04) == (cmi.RegionInformation & 0x04) && (rpc.RegionMask & 0x04) != 0x04) ||
               ((rpc.RegionMask & 0x08) == (cmi.RegionInformation & 0x08) && (rpc.RegionMask & 0x08) != 0x08) ||
               ((rpc.RegionMask & 0x10) == (cmi.RegionInformation & 0x10) && (rpc.RegionMask & 0x10) != 0x10) ||
               ((rpc.RegionMask & 0x20) == (cmi.RegionInformation & 0x20) && (rpc.RegionMask & 0x20) != 0x20) ||
               ((rpc.RegionMask & 0x40) == (cmi.RegionInformation & 0x40) && (rpc.RegionMask & 0x40) != 0x40) ||
               ((rpc.RegionMask & 0x80) == (cmi.RegionInformation & 0x80) && (rpc.RegionMask & 0x80) != 0x80);
    }
}