```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.738 μs | 0.0119 μs | 0.0071 μs | 4.737 μs |  1.02 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 7.896 μs | 0.0263 μs | 0.0174 μs | 7.889 μs |  1.69 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 4.637 μs | 0.0384 μs | 0.0228 μs | 4.630 μs |  0.99 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 4.782 μs | 0.0130 μs | 0.0086 μs | 4.781 μs |  1.02 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 3.303 μs | 0.0134 μs | 0.0089 μs | 3.301 μs |  0.71 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 7.181 μs | 0.0363 μs | 0.0240 μs | 7.182 μs |  1.54 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 4.665 μs | 0.0208 μs | 0.0138 μs | 4.669 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |          |       |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 4.955 μs | 0.0070 μs | 0.0102 μs | 4.953 μs |  1.05 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 7.927 μs | 0.0453 μs | 0.0664 μs | 7.961 μs |  1.68 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 4.758 μs | 0.0321 μs | 0.0460 μs | 4.737 μs |  1.01 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 4.867 μs | 0.0092 μs | 0.0133 μs | 4.862 μs |  1.03 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 3.198 μs | 0.0141 μs | 0.0198 μs | 3.208 μs |  0.68 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 7.298 μs | 0.0404 μs | 0.0592 μs | 7.322 μs |  1.55 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 4.719 μs | 0.0071 μs | 0.0104 μs | 4.717 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
