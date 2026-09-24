```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.360 μs | 0.0469 μs | 0.0310 μs |  1.02 |    0.03 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 5.435 μs | 0.2034 μs | 0.1210 μs |  1.65 |    0.06 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.058 μs | 0.0994 μs | 0.0591 μs |  0.93 |    0.03 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.261 μs | 0.0571 μs | 0.0340 μs |  0.99 |    0.03 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.257 μs | 0.0132 μs | 0.0078 μs |  0.69 |    0.02 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 4.567 μs | 0.1769 μs | 0.1170 μs |  1.39 |    0.05 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.295 μs | 0.1474 μs | 0.0975 μs |  1.00 |    0.04 | 0.0038 |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.348 μs | 2.0923 μs | 0.1147 μs |  1.08 |    0.03 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 5.456 μs | 0.3411 μs | 0.0187 μs |  1.76 |    0.01 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.177 μs | 0.1885 μs | 0.0103 μs |  1.03 |    0.01 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.450 μs | 2.6565 μs | 0.1456 μs |  1.12 |    0.04 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.179 μs | 1.3954 μs | 0.0765 μs |  0.70 |    0.02 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 4.468 μs | 0.3182 μs | 0.0174 μs |  1.44 |    0.01 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.093 μs | 0.4579 μs | 0.0251 μs |  1.00 |    0.01 | 0.0038 |     448 B |        1.00 |
