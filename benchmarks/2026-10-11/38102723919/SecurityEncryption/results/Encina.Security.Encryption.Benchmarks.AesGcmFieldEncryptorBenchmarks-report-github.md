```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.986 μs | 0.0265 μs | 0.0175 μs | 4.980 μs |  1.05 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 8.048 μs | 0.0349 μs | 0.0231 μs | 8.039 μs |  1.70 |    0.00 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 5.020 μs | 0.0140 μs | 0.0092 μs | 5.019 μs |  1.06 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 5.251 μs | 0.0163 μs | 0.0097 μs | 5.249 μs |  1.11 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 3.216 μs | 0.0131 μs | 0.0078 μs | 3.213 μs |  0.68 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 7.220 μs | 0.1503 μs | 0.0994 μs | 7.205 μs |  1.52 |    0.02 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 4.738 μs | 0.0076 μs | 0.0045 μs | 4.736 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |          |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 5.349 μs | 0.0108 μs | 0.0152 μs | 5.352 μs |  1.13 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 8.051 μs | 0.0224 μs | 0.0321 μs | 8.068 μs |  1.70 |    0.01 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 4.920 μs | 0.1084 μs | 0.1520 μs | 4.809 μs |  1.04 |    0.03 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 4.896 μs | 0.0412 μs | 0.0591 μs | 4.877 μs |  1.03 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 3.241 μs | 0.0445 μs | 0.0639 μs | 3.243 μs |  0.68 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 7.542 μs | 0.0782 μs | 0.1170 μs | 7.514 μs |  1.59 |    0.03 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 4.741 μs | 0.0150 μs | 0.0210 μs | 4.749 μs |  1.00 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
