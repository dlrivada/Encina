```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 3.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  2.791 μs |  0.0248 μs | 0.0130 μs |  1.22 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.793 μs |  0.1318 μs | 0.0872 μs |  1.22 |    0.04 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.772 μs |  0.0458 μs | 0.0273 μs |  1.21 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 39.896 μs |  1.0818 μs | 0.6438 μs | 17.42 |    0.29 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.290 μs |  0.0258 μs | 0.0171 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  4.599 μs |  0.0726 μs | 0.0432 μs |  2.01 |    0.02 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.468 μs |  0.0413 μs | 0.0246 μs |  1.08 |    0.01 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |           |            |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  2.822 μs |  0.2597 μs | 0.0142 μs |  1.26 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  2.764 μs |  0.2670 μs | 0.0146 μs |  1.23 |    0.01 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  2.689 μs |  0.7274 μs | 0.0399 μs |  1.20 |    0.02 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 41.003 μs | 35.1144 μs | 1.9247 μs | 18.26 |    0.76 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  2.246 μs |  0.4297 μs | 0.0236 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  4.771 μs |  0.8406 μs | 0.0461 μs |  2.12 |    0.03 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  2.458 μs |  0.0909 μs | 0.0050 μs |  1.09 |    0.01 | 0.1106 |   1.81 KB |        1.03 |
