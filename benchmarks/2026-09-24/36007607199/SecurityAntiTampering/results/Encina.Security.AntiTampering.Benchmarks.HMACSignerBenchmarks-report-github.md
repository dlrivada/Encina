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
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  4.485 μs | 0.0922 μs | 0.0610 μs |  1.15 |    0.03 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.359 μs | 0.0413 μs | 0.0216 μs |  1.12 |    0.03 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.479 μs | 0.0802 μs | 0.0530 μs |  1.15 |    0.03 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 48.737 μs | 0.2328 μs | 0.1540 μs | 12.53 |    0.28 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.891 μs | 0.1409 μs | 0.0932 μs |  1.00 |    0.03 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  8.245 μs | 0.3312 μs | 0.2191 μs |  2.12 |    0.07 | 0.0305 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  4.275 μs | 0.1245 μs | 0.0823 μs |  1.10 |    0.03 | 0.0153 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  4.600 μs | 0.4817 μs | 0.0264 μs |  1.19 |    0.03 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.417 μs | 0.6309 μs | 0.0346 μs |  1.14 |    0.03 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.413 μs | 0.9919 μs | 0.0544 μs |  1.14 |    0.03 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 48.794 μs | 2.3627 μs | 0.1295 μs | 12.59 |    0.36 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.879 μs | 2.3797 μs | 0.1304 μs |  1.00 |    0.04 | 0.0153 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  8.006 μs | 1.1346 μs | 0.0622 μs |  2.07 |    0.06 | 0.0305 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  4.313 μs | 0.9108 μs | 0.0499 μs |  1.11 |    0.03 | 0.0153 |   1.81 KB |        1.03 |
