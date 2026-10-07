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
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.860 μs | 0.0634 μs | 0.0419 μs |  1.04 |    0.01 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.997 μs | 0.2274 μs | 0.1504 μs |  1.70 |    0.03 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.570 μs | 0.1608 μs | 0.1063 μs |  0.97 |    0.02 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.786 μs | 0.0489 μs | 0.0291 μs |  1.02 |    0.01 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.248 μs | 0.0535 μs | 0.0354 μs |  0.69 |    0.01 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 6.760 μs | 0.0699 μs | 0.0462 μs |  1.44 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.691 μs | 0.0654 μs | 0.0433 μs |  1.00 |    0.01 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.878 μs | 0.7727 μs | 0.0424 μs |  1.06 |    0.01 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 7.979 μs | 2.1758 μs | 0.1193 μs |  1.74 |    0.02 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.514 μs | 0.9646 μs | 0.0529 μs |  0.98 |    0.01 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.839 μs | 0.7149 μs | 0.0392 μs |  1.06 |    0.01 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.245 μs | 0.3993 μs | 0.0219 μs |  0.71 |    0.01 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 6.782 μs | 1.4234 μs | 0.0780 μs |  1.48 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.586 μs | 0.5115 μs | 0.0280 μs |  1.00 |    0.01 |      - |     448 B |        1.00 |
