```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.938 μs | 0.0232 μs | 0.0138 μs |  1.05 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 8.079 μs | 0.0533 μs | 0.0352 μs |  1.72 |    0.01 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 4.644 μs | 0.0182 μs | 0.0120 μs |  0.99 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 5.177 μs | 0.0346 μs | 0.0206 μs |  1.10 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 3.524 μs | 0.0154 μs | 0.0080 μs |  0.75 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 7.437 μs | 0.1566 μs | 0.1036 μs |  1.58 |    0.02 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 4.704 μs | 0.0333 μs | 0.0174 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 4.985 μs | 0.0189 μs | 0.0272 μs |  1.06 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 8.170 μs | 0.0433 μs | 0.0621 μs |  1.74 |    0.01 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 4.720 μs | 0.0148 μs | 0.0207 μs |  1.00 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 4.868 μs | 0.0277 μs | 0.0406 μs |  1.04 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 3.242 μs | 0.0088 μs | 0.0124 μs |  0.69 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 7.604 μs | 0.0846 μs | 0.1239 μs |  1.62 |    0.03 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 4.698 μs | 0.0096 μs | 0.0135 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
