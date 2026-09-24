```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  5.196 μs | 0.0103 μs | 0.0054 μs |  1.28 |    0.01 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.940 μs | 0.0148 μs | 0.0088 μs |  1.22 |    0.01 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.964 μs | 0.0147 μs | 0.0097 μs |  1.22 |    0.01 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 80.167 μs | 0.0757 μs | 0.0451 μs | 19.75 |    0.08 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.059 μs | 0.0278 μs | 0.0165 μs |  1.00 |    0.01 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  8.676 μs | 0.0131 μs | 0.0087 μs |  2.14 |    0.01 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  4.492 μs | 0.0095 μs | 0.0050 μs |  1.11 |    0.00 | 0.0687 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  5.151 μs | 0.0835 μs | 0.0046 μs |  1.28 |    0.01 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.927 μs | 0.1550 μs | 0.0085 μs |  1.23 |    0.01 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.880 μs | 0.1041 μs | 0.0057 μs |  1.22 |    0.01 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 80.160 μs | 3.0319 μs | 0.1662 μs | 19.97 |    0.11 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  4.015 μs | 0.4363 μs | 0.0239 μs |  1.00 |    0.01 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  8.437 μs | 1.1499 μs | 0.0630 μs |  2.10 |    0.02 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  4.484 μs | 0.2929 μs | 0.0161 μs |  1.12 |    0.01 | 0.0687 |   1.81 KB |        1.03 |
