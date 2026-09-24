```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  3.263 μs | 0.0555 μs | 0.0290 μs |  1.17 |    0.03 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.263 μs | 0.1233 μs | 0.0734 μs |  1.17 |    0.04 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.232 μs | 0.0707 μs | 0.0421 μs |  1.16 |    0.03 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 36.048 μs | 0.1275 μs | 0.0843 μs | 12.89 |    0.28 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.797 μs | 0.1105 μs | 0.0658 μs |  1.00 |    0.03 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  5.960 μs | 0.3662 μs | 0.2422 μs |  2.13 |    0.09 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.999 μs | 0.0239 μs | 0.0142 μs |  1.07 |    0.02 | 0.0191 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  3.362 μs | 1.6964 μs | 0.0930 μs |  1.17 |    0.03 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  3.210 μs | 0.6665 μs | 0.0365 μs |  1.12 |    0.02 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  3.228 μs | 0.5873 μs | 0.0322 μs |  1.13 |    0.02 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 36.562 μs | 8.1514 μs | 0.4468 μs | 12.76 |    0.25 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  2.865 μs | 0.9847 μs | 0.0540 μs |  1.00 |    0.02 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  5.933 μs | 2.0834 μs | 0.1142 μs |  2.07 |    0.05 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  3.167 μs | 0.1070 μs | 0.0059 μs |  1.11 |    0.02 | 0.0191 |   1.81 KB |        1.03 |
