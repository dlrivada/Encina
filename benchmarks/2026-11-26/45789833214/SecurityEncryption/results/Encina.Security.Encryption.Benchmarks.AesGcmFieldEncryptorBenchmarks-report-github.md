```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.974 μs | 0.0159 μs | 0.0095 μs |  1.06 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 6.726 μs | 0.0366 μs | 0.0242 μs |  1.79 |    0.01 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.836 μs | 0.1091 μs | 0.0722 μs |  1.02 |    0.02 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.953 μs | 0.0191 μs | 0.0114 μs |  1.05 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.821 μs | 0.0095 μs | 0.0063 μs |  0.75 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 6.196 μs | 0.0327 μs | 0.0216 μs |  1.65 |    0.01 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.766 μs | 0.0136 μs | 0.0081 μs |  1.00 |    0.00 | 0.0267 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.988 μs | 0.3981 μs | 0.0218 μs |  0.98 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 6.704 μs | 0.2774 μs | 0.0152 μs |  1.66 |    0.00 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.761 μs | 0.1633 μs | 0.0090 μs |  0.93 |    0.00 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.895 μs | 0.4520 μs | 0.0248 μs |  0.96 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.060 μs | 0.4956 μs | 0.0272 μs |  0.76 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 5.959 μs | 0.8830 μs | 0.0484 μs |  1.47 |    0.01 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.051 μs | 0.1355 μs | 0.0074 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
