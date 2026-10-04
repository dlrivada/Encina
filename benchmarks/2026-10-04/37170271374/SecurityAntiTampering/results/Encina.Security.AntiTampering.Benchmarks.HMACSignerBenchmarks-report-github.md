```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | 3           |  5.195 μs | 0.0310 μs | 0.0185 μs |  5.190 μs |  1.29 |    0.01 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.946 μs | 0.0246 μs | 0.0163 μs |  4.941 μs |  1.23 |    0.00 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.913 μs | 0.0126 μs | 0.0084 μs |  4.915 μs |  1.22 |    0.00 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 3           | 80.152 μs | 0.0168 μs | 0.0088 μs | 80.155 μs | 19.95 |    0.05 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.018 μs | 0.0175 μs | 0.0104 μs |  4.016 μs |  1.00 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | 3           |  8.561 μs | 0.0382 μs | 0.0227 μs |  8.566 μs |  2.13 |    0.01 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | 3           |  4.518 μs | 0.0071 μs | 0.0042 μs |  4.518 μs |  1.12 |    0.00 | 0.0687 |   1.81 KB |        1.03 |
|                            |            |                |             |             |           |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | MediumRun  | 15             | 2           | 10          |  5.164 μs | 0.0133 μs | 0.0194 μs |  5.161 μs |  1.28 |    0.02 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.953 μs | 0.0072 μs | 0.0104 μs |  4.954 μs |  1.23 |    0.02 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.896 μs | 0.0136 μs | 0.0196 μs |  4.895 μs |  1.21 |    0.02 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | MediumRun  | 15             | 2           | 10          | 80.249 μs | 0.0704 μs | 0.0987 μs | 80.270 μs | 19.91 |    0.31 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.031 μs | 0.0438 μs | 0.0641 μs |  3.991 μs |  1.00 |    0.02 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | MediumRun  | 15             | 2           | 10          |  8.327 μs | 0.0087 μs | 0.0125 μs |  8.326 μs |  2.07 |    0.03 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | MediumRun  | 15             | 2           | 10          |  4.380 μs | 0.0268 μs | 0.0392 μs |  4.402 μs |  1.09 |    0.02 | 0.0687 |   1.81 KB |        1.03 |
