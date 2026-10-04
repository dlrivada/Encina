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
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.877 μs | 0.0079 μs | 0.0041 μs | 4.875 μs |  1.05 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 8.001 μs | 0.0331 μs | 0.0219 μs | 7.995 μs |  1.72 |    0.01 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 4.651 μs | 0.0179 μs | 0.0093 μs | 4.649 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 4.877 μs | 0.0171 μs | 0.0113 μs | 4.875 μs |  1.05 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 3.190 μs | 0.0058 μs | 0.0034 μs | 3.190 μs |  0.69 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 7.403 μs | 0.0602 μs | 0.0398 μs | 7.393 μs |  1.59 |    0.01 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 4.650 μs | 0.0134 μs | 0.0080 μs | 4.647 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |          |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 5.092 μs | 0.1228 μs | 0.1761 μs | 5.097 μs |  1.09 |    0.04 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 8.286 μs | 0.2538 μs | 0.3559 μs | 7.972 μs |  1.77 |    0.07 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 4.701 μs | 0.0126 μs | 0.0184 μs | 4.700 μs |  1.01 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 5.085 μs | 0.1415 μs | 0.2029 μs | 5.254 μs |  1.09 |    0.04 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 3.238 μs | 0.0084 μs | 0.0122 μs | 3.237 μs |  0.69 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 7.340 μs | 0.0733 μs | 0.1097 μs | 7.302 μs |  1.57 |    0.02 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 4.677 μs | 0.0098 μs | 0.0137 μs | 4.679 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
