```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.67GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.932 μs | 0.0266 μs | 0.0176 μs | 4.932 μs |  1.06 |    0.01 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 7.829 μs | 0.0382 μs | 0.0253 μs | 7.825 μs |  1.69 |    0.01 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 4.652 μs | 0.0253 μs | 0.0167 μs | 4.650 μs |  1.00 |    0.00 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 4.749 μs | 0.0111 μs | 0.0066 μs | 4.749 μs |  1.02 |    0.00 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 3.270 μs | 0.0064 μs | 0.0043 μs | 3.270 μs |  0.71 |    0.00 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 6.703 μs | 0.0955 μs | 0.0632 μs | 6.705 μs |  1.45 |    0.01 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 4.637 μs | 0.0270 μs | 0.0161 μs | 4.640 μs |  1.00 |    0.00 | 0.0153 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |          |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 4.924 μs | 0.0132 μs | 0.0198 μs | 4.930 μs |  1.04 |    0.01 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 8.020 μs | 0.0210 μs | 0.0314 μs | 8.010 μs |  1.69 |    0.02 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 4.671 μs | 0.0087 μs | 0.0122 μs | 4.667 μs |  0.98 |    0.01 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 4.873 μs | 0.0082 μs | 0.0117 μs | 4.872 μs |  1.03 |    0.01 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 3.214 μs | 0.0592 μs | 0.0829 μs | 3.259 μs |  0.68 |    0.02 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 7.007 μs | 0.0317 μs | 0.0475 μs | 7.005 μs |  1.48 |    0.02 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 4.745 μs | 0.0273 μs | 0.0408 μs | 4.751 μs |  1.00 |    0.01 | 0.0153 |      - |     448 B |        1.00 |
