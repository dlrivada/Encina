```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  3.999 μs |  0.1680 μs | 0.1111 μs |  1.13 |    0.03 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.038 μs |  0.0814 μs | 0.0538 μs |  1.14 |    0.02 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.034 μs |  0.1648 μs | 0.0981 μs |  1.13 |    0.03 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 43.079 μs |  1.0688 μs | 0.7069 μs | 12.12 |    0.26 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.555 μs |  0.0813 μs | 0.0538 μs |  1.00 |    0.02 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  7.606 μs |  0.1637 μs | 0.0974 μs |  2.14 |    0.04 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  4.015 μs |  0.1757 μs | 0.1162 μs |  1.13 |    0.04 | 0.0153 |   1.81 KB |        1.03 |
|                            |            |                |             |           |            |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  3.933 μs |  0.2675 μs | 0.0147 μs |  1.10 |    0.04 | 0.0153 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  3.897 μs |  0.6822 μs | 0.0374 μs |  1.09 |    0.04 | 0.0153 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  3.998 μs |  0.7310 μs | 0.0401 μs |  1.12 |    0.04 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 43.737 μs | 12.9595 μs | 0.7104 μs | 12.27 |    0.48 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.570 μs |  2.8333 μs | 0.1553 μs |  1.00 |    0.05 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  7.567 μs |  3.5674 μs | 0.1955 μs |  2.12 |    0.09 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  4.169 μs |  4.5612 μs | 0.2500 μs |  1.17 |    0.07 | 0.0153 |   1.81 KB |        1.03 |
