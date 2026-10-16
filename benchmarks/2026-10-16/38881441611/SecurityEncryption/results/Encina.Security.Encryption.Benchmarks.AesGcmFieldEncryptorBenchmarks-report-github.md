```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.955 μs | 0.0090 μs | 0.0054 μs |  1.06 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 8.045 μs | 0.0243 μs | 0.0127 μs |  1.72 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.646 μs | 0.0613 μs | 0.0405 μs |  0.99 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.914 μs | 0.0106 μs | 0.0063 μs |  1.05 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.460 μs | 0.0048 μs | 0.0025 μs |  0.74 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 7.557 μs | 0.0273 μs | 0.0162 μs |  1.61 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.679 μs | 0.0179 μs | 0.0106 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.936 μs | 0.2336 μs | 0.0128 μs |  1.05 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 8.550 μs | 0.5245 μs | 0.0287 μs |  1.81 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.648 μs | 0.1041 μs | 0.0057 μs |  0.99 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 5.212 μs | 0.1987 μs | 0.0109 μs |  1.11 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.226 μs | 0.0757 μs | 0.0041 μs |  0.68 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 7.525 μs | 0.1685 μs | 0.0092 μs |  1.60 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.715 μs | 0.1975 μs | 0.0108 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
