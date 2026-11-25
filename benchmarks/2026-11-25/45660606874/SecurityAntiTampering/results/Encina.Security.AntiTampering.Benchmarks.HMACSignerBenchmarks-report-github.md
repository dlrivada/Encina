```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  2.482 μs | 0.0071 μs | 0.0043 μs |  1.26 |    0.00 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.769 μs | 0.0122 μs | 0.0073 μs |  1.41 |    0.00 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.741 μs | 0.0098 μs | 0.0065 μs |  1.39 |    0.00 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 37.063 μs | 0.0521 μs | 0.0310 μs | 18.85 |    0.05 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  1.967 μs | 0.0085 μs | 0.0050 μs |  1.00 |    0.00 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  4.173 μs | 0.0082 μs | 0.0054 μs |  2.12 |    0.01 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.195 μs | 0.0303 μs | 0.0200 μs |  1.12 |    0.01 | 0.0191 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  2.494 μs | 0.0904 μs | 0.0050 μs |  1.27 |    0.00 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  2.742 μs | 0.4369 μs | 0.0239 μs |  1.40 |    0.01 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  2.750 μs | 0.0475 μs | 0.0026 μs |  1.40 |    0.00 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 37.001 μs | 1.1057 μs | 0.0606 μs | 18.86 |    0.03 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  1.962 μs | 0.0332 μs | 0.0018 μs |  1.00 |    0.00 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  4.170 μs | 0.3879 μs | 0.0213 μs |  2.13 |    0.01 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  2.238 μs | 0.1188 μs | 0.0065 μs |  1.14 |    0.00 | 0.0191 |   1.81 KB |        1.03 |
