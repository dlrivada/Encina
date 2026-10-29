```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  5.080 μs | 0.0158 μs | 0.0104 μs |  1.26 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.953 μs | 0.0155 μs | 0.0103 μs |  1.22 |    0.00 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.866 μs | 0.0159 μs | 0.0095 μs |  1.20 |    0.00 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 80.209 μs | 0.0859 μs | 0.0449 μs | 19.83 |    0.03 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.045 μs | 0.0104 μs | 0.0068 μs |  1.00 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  8.484 μs | 0.0479 μs | 0.0317 μs |  2.10 |    0.01 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  4.427 μs | 0.0103 μs | 0.0061 μs |  1.09 |    0.00 | 0.0687 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  5.113 μs | 0.1088 μs | 0.0060 μs |  1.29 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.885 μs | 0.1477 μs | 0.0081 μs |  1.24 |    0.00 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.811 μs | 0.1633 μs | 0.0090 μs |  1.22 |    0.00 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 80.200 μs | 2.7896 μs | 0.1529 μs | 20.30 |    0.04 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.951 μs | 0.0607 μs | 0.0033 μs |  1.00 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  8.257 μs | 0.4222 μs | 0.0231 μs |  2.09 |    0.01 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  4.420 μs | 0.0872 μs | 0.0048 μs |  1.12 |    0.00 | 0.0687 |   1.81 KB |        1.03 |
