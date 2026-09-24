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
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.409 μs | 0.1463 μs | 0.0967 μs |  1.07 |    0.04 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 5.619 μs | 0.1187 μs | 0.0785 μs |  1.76 |    0.05 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.044 μs | 0.0423 μs | 0.0252 μs |  0.95 |    0.02 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.390 μs | 0.1828 μs | 0.1209 μs |  1.06 |    0.04 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.148 μs | 0.0298 μs | 0.0197 μs |  0.67 |    0.02 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 5.005 μs | 0.3307 μs | 0.2188 μs |  1.56 |    0.07 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.202 μs | 0.1191 μs | 0.0787 μs |  1.00 |    0.03 | 0.0038 |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.329 μs | 1.3480 μs | 0.0739 μs |  1.08 |    0.02 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 5.416 μs | 0.6164 μs | 0.0338 μs |  1.75 |    0.01 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.076 μs | 0.2036 μs | 0.0112 μs |  1.00 |    0.00 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.401 μs | 2.9379 μs | 0.1610 μs |  1.10 |    0.05 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.122 μs | 0.0478 μs | 0.0026 μs |  0.69 |    0.00 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 4.822 μs | 1.1399 μs | 0.0625 μs |  1.56 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.087 μs | 0.1457 μs | 0.0080 μs |  1.00 |    0.00 | 0.0038 |     448 B |        1.00 |
