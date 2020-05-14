﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TerraFX.Interop;

namespace PIX.NET
{
    public readonly unsafe partial struct ScopedEvent : IDisposable
    {
        public static ScopedEvent Create(
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            PIXMethods.BeginEvent(color, formatString);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            PIXMethods.BeginEvent(color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14,
                t15);

            return new ScopedEvent(NoContext);
        }


        public static ScopedEvent Create(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            PIXMethods.BeginEvent(context, color, formatString);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12,
                t13);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12,
                t13, t14);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ID3D12CommandQueue* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12,
                t13, t14, t15);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString
        )
        {
            PIXMethods.BeginEvent(context, color, formatString);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12,
                t13);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12,
                t13, t14);

            return new ScopedEvent(context);
        }


        public static ScopedEvent Create<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ID3D12GraphicsCommandList* context,
            in PIXColor color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            PIXMethods.BeginEvent(context, color, formatString, t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12,
                t13, t14, t15);

            return new ScopedEvent(context);
        }
    }
}