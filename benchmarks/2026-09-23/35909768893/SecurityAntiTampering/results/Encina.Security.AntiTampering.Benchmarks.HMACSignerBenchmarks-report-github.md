```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  4.060 μs | 0.0263 μs | 0.0157 μs |  1.18 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.472 μs | 0.0114 μs | 0.0076 μs |  1.30 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.295 μs | 0.0256 μs | 0.0169 μs |  1.25 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 44.849 μs | 0.1849 μs | 0.1223 μs | 13.06 |    0.04 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.434 μs | 0.0100 μs | 0.0066 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  7.617 μs | 0.0589 μs | 0.0350 μs |  2.22 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  3.785 μs | 0.0128 μs | 0.0085 μs |  1.10 |    0.00 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  4.076 μs | 0.1132 μs | 0.0062 μs |  1.16 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.358 μs | 0.1286 μs | 0.0071 μs |  1.24 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.330 μs | 0.3590 μs | 0.0197 μs |  1.23 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 45.083 μs | 3.2348 μs | 0.1773 μs | 12.79 |    0.07 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.524 μs | 0.3268 μs | 0.0179 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  7.185 μs | 0.2834 μs | 0.0155 μs |  2.04 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  3.983 μs | 0.2517 μs | 0.0138 μs |  1.13 |    0.01 | 0.1068 |   1.81 KB |        1.03 |
