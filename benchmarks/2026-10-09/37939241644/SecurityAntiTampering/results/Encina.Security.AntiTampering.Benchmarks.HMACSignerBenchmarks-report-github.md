```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.85GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  5.154 μs | 0.0122 μs | 0.0073 μs |  1.26 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.910 μs | 0.0073 μs | 0.0043 μs |  1.20 |    0.00 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.932 μs | 0.0091 μs | 0.0054 μs |  1.21 |    0.00 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 80.178 μs | 0.1203 μs | 0.0796 μs | 19.66 |    0.04 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.079 μs | 0.0135 μs | 0.0080 μs |  1.00 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  8.523 μs | 0.0286 μs | 0.0170 μs |  2.09 |    0.01 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  4.421 μs | 0.0088 μs | 0.0052 μs |  1.08 |    0.00 | 0.0687 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  5.104 μs | 0.1314 μs | 0.0072 μs |  1.28 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.916 μs | 0.3475 μs | 0.0190 μs |  1.24 |    0.00 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.865 μs | 0.2769 μs | 0.0152 μs |  1.22 |    0.00 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 80.230 μs | 2.9486 μs | 0.1616 μs | 20.16 |    0.04 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.981 μs | 0.0698 μs | 0.0038 μs |  1.00 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  8.287 μs | 0.1791 μs | 0.0098 μs |  2.08 |    0.00 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  4.388 μs | 0.1617 μs | 0.0089 μs |  1.10 |    0.00 | 0.0687 |   1.81 KB |        1.03 |
