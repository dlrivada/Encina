```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  3.765 μs | 0.0063 μs | 0.0042 μs |  1.14 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.042 μs | 0.0108 μs | 0.0057 μs |  1.22 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.170 μs | 0.0472 μs | 0.0281 μs |  1.26 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 44.608 μs | 0.1675 μs | 0.1108 μs | 13.51 |    0.04 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.301 μs | 0.0119 μs | 0.0062 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  6.556 μs | 0.0067 μs | 0.0040 μs |  1.99 |    0.00 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  3.572 μs | 0.0127 μs | 0.0076 μs |  1.08 |    0.00 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  3.765 μs | 0.0974 μs | 0.0053 μs |  1.16 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.274 μs | 0.9347 μs | 0.0512 μs |  1.32 |    0.02 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.215 μs | 0.1185 μs | 0.0065 μs |  1.30 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 44.587 μs | 2.5035 μs | 0.1372 μs | 13.78 |    0.14 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.235 μs | 0.6706 μs | 0.0368 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  6.631 μs | 0.7279 μs | 0.0399 μs |  2.05 |    0.02 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  3.565 μs | 0.8547 μs | 0.0468 μs |  1.10 |    0.02 | 0.1106 |   1.81 KB |        1.03 |
