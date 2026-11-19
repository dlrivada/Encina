```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  3.162 μs | 0.0121 μs | 0.0072 μs |  1.24 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.306 μs | 0.0247 μs | 0.0147 μs |  1.29 |    0.01 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.352 μs | 0.0211 μs | 0.0139 μs |  1.31 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 38.751 μs | 0.1757 μs | 0.1162 μs | 15.16 |    0.07 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.557 μs | 0.0134 μs | 0.0089 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  5.178 μs | 0.0429 μs | 0.0255 μs |  2.03 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.826 μs | 0.0090 μs | 0.0053 μs |  1.11 |    0.00 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  3.007 μs | 0.0960 μs | 0.0053 μs |  1.24 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  3.184 μs | 0.1827 μs | 0.0100 μs |  1.31 |    0.01 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  3.203 μs | 0.1271 μs | 0.0070 μs |  1.32 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 38.781 μs | 3.1477 μs | 0.1725 μs | 15.97 |    0.09 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  2.429 μs | 0.1929 μs | 0.0106 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  5.319 μs | 0.5479 μs | 0.0300 μs |  2.19 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  2.792 μs | 0.7889 μs | 0.0432 μs |  1.15 |    0.02 | 0.1106 |   1.81 KB |        1.03 |
