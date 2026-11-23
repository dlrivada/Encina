```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  2.424 μs |  0.1636 μs | 0.0973 μs |  1.16 |    0.07 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.399 μs |  0.0711 μs | 0.0423 μs |  1.15 |    0.05 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.372 μs |  0.0740 μs | 0.0440 μs |  1.13 |    0.05 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 33.617 μs |  2.5123 μs | 1.6617 μs | 16.04 |    1.04 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.100 μs |  0.1493 μs | 0.0988 μs |  1.00 |    0.06 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  3.997 μs |  0.2256 μs | 0.1343 μs |  1.91 |    0.10 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.290 μs |  0.1286 μs | 0.0851 μs |  1.09 |    0.06 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |           |            |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  2.377 μs |  1.5885 μs | 0.0871 μs |  1.24 |    0.05 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  2.503 μs |  2.5110 μs | 0.1376 μs |  1.30 |    0.07 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  2.488 μs |  2.0733 μs | 0.1136 μs |  1.29 |    0.06 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 33.975 μs |  4.0633 μs | 0.2227 μs | 17.67 |    0.37 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  1.923 μs |  0.8151 μs | 0.0447 μs |  1.00 |    0.03 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  4.418 μs | 13.4635 μs | 0.7380 μs |  2.30 |    0.34 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  2.418 μs |  1.5335 μs | 0.0841 μs |  1.26 |    0.05 | 0.1106 |   1.81 KB |        1.03 |
