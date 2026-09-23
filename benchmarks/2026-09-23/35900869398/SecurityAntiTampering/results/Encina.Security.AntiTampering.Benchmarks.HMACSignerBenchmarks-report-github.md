```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  3.131 μs |  0.0285 μs | 0.0149 μs |  1.16 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.145 μs |  0.0306 μs | 0.0182 μs |  1.17 |    0.02 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.198 μs |  0.1923 μs | 0.1272 μs |  1.19 |    0.05 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 35.501 μs |  0.4131 μs | 0.2732 μs | 13.17 |    0.18 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.697 μs |  0.0501 μs | 0.0332 μs |  1.00 |    0.02 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  5.685 μs |  0.1299 μs | 0.0679 μs |  2.11 |    0.03 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.983 μs |  0.0622 μs | 0.0412 μs |  1.11 |    0.02 | 0.0191 |   1.81 KB |        1.03 |
|                            |            |                |             |           |            |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  3.157 μs |  0.4520 μs | 0.0248 μs |  1.18 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  3.167 μs |  1.5796 μs | 0.0866 μs |  1.18 |    0.03 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  3.310 μs |  2.4918 μs | 0.1366 μs |  1.24 |    0.04 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 37.796 μs | 35.4907 μs | 1.9454 μs | 14.11 |    0.63 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  2.678 μs |  0.2808 μs | 0.0154 μs |  1.00 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  5.802 μs |  5.1630 μs | 0.2830 μs |  2.17 |    0.09 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  3.001 μs |  3.0771 μs | 0.1687 μs |  1.12 |    0.05 | 0.0191 |   1.81 KB |        1.03 |
