```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.567 μs | 0.1078 μs | 0.0713 μs |  1.07 |    0.03 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.467 μs | 0.2335 μs | 0.1389 μs |  1.76 |    0.05 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.216 μs | 0.1994 μs | 0.1319 μs |  0.99 |    0.04 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.463 μs | 0.0841 μs | 0.0556 μs |  1.05 |    0.03 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.049 μs | 0.0503 μs | 0.0332 μs |  0.72 |    0.02 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 6.657 μs | 0.1763 μs | 0.1166 μs |  1.57 |    0.04 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.251 μs | 0.1582 μs | 0.0942 μs |  1.00 |    0.03 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.494 μs | 1.1968 μs | 0.0656 μs |  1.03 |    0.02 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 7.407 μs | 1.6693 μs | 0.0915 μs |  1.71 |    0.02 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.180 μs | 0.5057 μs | 0.0277 μs |  0.96 |    0.01 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.562 μs | 1.8512 μs | 0.1015 μs |  1.05 |    0.02 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.014 μs | 1.6078 μs | 0.0881 μs |  0.69 |    0.02 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 6.661 μs | 1.7352 μs | 0.0951 μs |  1.53 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.343 μs | 0.7304 μs | 0.0400 μs |  1.00 |    0.01 |      - |     448 B |        1.00 |
