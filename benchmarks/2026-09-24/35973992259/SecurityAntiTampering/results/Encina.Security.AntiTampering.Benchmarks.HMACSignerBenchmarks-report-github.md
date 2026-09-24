```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  3.977 μs | 0.1989 μs | 0.1316 μs |  1.17 |    0.04 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.009 μs | 0.1840 μs | 0.1095 μs |  1.18 |    0.03 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.268 μs | 0.1479 μs | 0.0880 μs |  1.25 |    0.03 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 43.020 μs | 0.8025 μs | 0.5308 μs | 12.63 |    0.16 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.406 μs | 0.0375 μs | 0.0196 μs |  1.00 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  7.334 μs | 0.3763 μs | 0.2489 μs |  2.15 |    0.07 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  3.873 μs | 0.1133 μs | 0.0749 μs |  1.14 |    0.02 | 0.0191 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  4.114 μs | 0.0673 μs | 0.0037 μs |  1.18 |    0.02 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.055 μs | 1.3404 μs | 0.0735 μs |  1.16 |    0.03 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.021 μs | 1.3193 μs | 0.0723 μs |  1.15 |    0.03 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 43.927 μs | 6.8173 μs | 0.3737 μs | 12.62 |    0.25 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.483 μs | 1.3835 μs | 0.0758 μs |  1.00 |    0.03 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  7.233 μs | 2.9239 μs | 0.1603 μs |  2.08 |    0.06 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  3.921 μs | 0.4835 μs | 0.0265 μs |  1.13 |    0.02 | 0.0191 |   1.81 KB |        1.03 |
