```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.014 μs | 0.0114 μs | 0.0068 μs | 4.013 μs |  1.09 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 5.966 μs | 0.0604 μs | 0.0400 μs | 5.951 μs |  1.62 |    0.01 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 3.581 μs | 0.0100 μs | 0.0053 μs | 3.579 μs |  0.97 |    0.00 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 3.699 μs | 0.0356 μs | 0.0212 μs | 3.704 μs |  1.00 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 2.321 μs | 0.0043 μs | 0.0026 μs | 2.321 μs |  0.63 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 5.842 μs | 0.0779 μs | 0.0515 μs | 5.847 μs |  1.59 |    0.01 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 3.683 μs | 0.0109 μs | 0.0065 μs | 3.681 μs |  1.00 |    0.00 | 0.0267 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |          |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 3.885 μs | 0.0783 μs | 0.1147 μs | 3.979 μs |  1.03 |    0.05 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 5.969 μs | 0.0123 μs | 0.0176 μs | 5.971 μs |  1.59 |    0.07 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 3.604 μs | 0.0283 μs | 0.0405 μs | 3.607 μs |  0.96 |    0.04 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 3.725 μs | 0.0147 μs | 0.0220 μs | 3.720 μs |  0.99 |    0.04 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 2.427 μs | 0.0754 μs | 0.1082 μs | 2.517 μs |  0.65 |    0.04 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 6.034 μs | 0.1047 μs | 0.1502 μs | 5.957 μs |  1.61 |    0.08 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 3.760 μs | 0.1120 μs | 0.1570 μs | 3.623 μs |  1.00 |    0.06 | 0.0267 |      - |     448 B |        1.00 |
