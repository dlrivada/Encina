```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  4.450 μs | 0.0525 μs | 0.0347 μs |  1.15 |    0.02 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.405 μs | 0.0834 μs | 0.0552 μs |  1.14 |    0.02 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.410 μs | 0.0824 μs | 0.0545 μs |  1.14 |    0.02 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 48.147 μs | 0.3593 μs | 0.2138 μs | 12.44 |    0.15 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.872 μs | 0.0799 μs | 0.0476 μs |  1.00 |    0.02 | 0.0153 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  8.105 μs | 0.1456 μs | 0.0867 μs |  2.09 |    0.03 | 0.0305 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  4.213 μs | 0.1195 μs | 0.0711 μs |  1.09 |    0.02 | 0.0153 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  4.379 μs | 0.5975 μs | 0.0328 μs |  1.11 |    0.04 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.391 μs | 2.0704 μs | 0.1135 μs |  1.11 |    0.04 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.480 μs | 0.3550 μs | 0.0195 μs |  1.14 |    0.04 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 47.966 μs | 3.0928 μs | 0.1695 μs | 12.16 |    0.40 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.949 μs | 2.7056 μs | 0.1483 μs |  1.00 |    0.05 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  8.234 μs | 5.4932 μs | 0.3011 μs |  2.09 |    0.09 | 0.0305 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  4.316 μs | 0.5289 μs | 0.0290 μs |  1.09 |    0.04 | 0.0153 |   1.81 KB |        1.03 |
