```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  4.426 μs | 0.0510 μs | 0.0337 μs |  1.11 |    0.03 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.460 μs | 0.0985 μs | 0.0651 μs |  1.12 |    0.03 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.435 μs | 0.0852 μs | 0.0564 μs |  1.12 |    0.03 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 48.503 μs | 0.4725 μs | 0.3125 μs | 12.21 |    0.30 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.974 μs | 0.1495 μs | 0.0989 μs |  1.00 |    0.03 | 0.0153 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  8.134 μs | 0.1847 μs | 0.1222 μs |  2.05 |    0.06 | 0.0305 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  4.227 μs | 0.0787 μs | 0.0411 μs |  1.06 |    0.03 | 0.0153 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  4.474 μs | 2.0183 μs | 0.1106 μs |  1.16 |    0.03 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.496 μs | 1.8160 μs | 0.0995 μs |  1.17 |    0.03 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.474 μs | 1.0111 μs | 0.0554 μs |  1.16 |    0.02 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 48.527 μs | 6.3565 μs | 0.3484 μs | 12.58 |    0.15 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.859 μs | 0.8613 μs | 0.0472 μs |  1.00 |    0.01 | 0.0153 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  8.033 μs | 1.3665 μs | 0.0749 μs |  2.08 |    0.03 | 0.0305 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  4.228 μs | 0.6457 μs | 0.0354 μs |  1.10 |    0.01 | 0.0153 |   1.81 KB |        1.03 |
