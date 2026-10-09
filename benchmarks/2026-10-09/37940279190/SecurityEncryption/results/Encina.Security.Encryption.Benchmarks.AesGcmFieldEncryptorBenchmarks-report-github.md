```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 2.863 μs | 0.0502 μs | 0.0263 μs |  0.97 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 4.696 μs | 0.1902 μs | 0.1258 μs |  1.60 |    0.04 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 2.885 μs | 0.1556 μs | 0.1030 μs |  0.98 |    0.03 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 2.932 μs | 0.0544 μs | 0.0360 μs |  1.00 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 1.814 μs | 0.0538 μs | 0.0356 μs |  0.62 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 4.104 μs | 0.1481 μs | 0.0980 μs |  1.39 |    0.03 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.943 μs | 0.0475 μs | 0.0248 μs |  1.00 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 2.836 μs | 0.3045 μs | 0.0167 μs |  1.01 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 4.583 μs | 1.3846 μs | 0.0759 μs |  1.63 |    0.02 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 2.759 μs | 0.1704 μs | 0.0093 μs |  0.98 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.011 μs | 1.9663 μs | 0.1078 μs |  1.07 |    0.03 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 1.961 μs | 0.6839 μs | 0.0375 μs |  0.70 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 3.974 μs | 0.5988 μs | 0.0328 μs |  1.42 |    0.01 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 2.804 μs | 0.3066 μs | 0.0168 μs |  1.00 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
