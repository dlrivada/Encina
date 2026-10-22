```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.553 μs | 0.0715 μs | 0.0473 μs |  1.02 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 7.166 μs | 0.1022 μs | 0.0608 μs |  1.61 |    0.02 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 4.683 μs | 0.0723 μs | 0.0478 μs |  1.05 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 4.881 μs | 0.0759 μs | 0.0502 μs |  1.10 |    0.02 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 2.716 μs | 0.0365 μs | 0.0241 μs |  0.61 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 7.074 μs | 0.1422 μs | 0.0941 μs |  1.59 |    0.03 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 4.445 μs | 0.0702 μs | 0.0464 μs |  1.00 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 4.642 μs | 0.0350 μs | 0.0512 μs |  1.08 |    0.02 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 7.488 μs | 0.1677 μs | 0.2511 μs |  1.74 |    0.06 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 4.545 μs | 0.1186 μs | 0.1774 μs |  1.06 |    0.04 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 4.549 μs | 0.0357 μs | 0.0534 μs |  1.06 |    0.02 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 2.904 μs | 0.1100 μs | 0.1647 μs |  0.68 |    0.04 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 6.873 μs | 0.1973 μs | 0.2829 μs |  1.60 |    0.07 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 4.296 μs | 0.0367 μs | 0.0538 μs |  1.00 |    0.02 | 0.0229 |      - |     448 B |        1.00 |
