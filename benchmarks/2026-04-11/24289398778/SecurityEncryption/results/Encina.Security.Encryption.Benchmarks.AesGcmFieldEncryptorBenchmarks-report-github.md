```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                  | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3           | 4.811 μs | 0.0160 μs | 0.0095 μs | 4.810 μs |  1.04 |    0.00 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 3           | 7.869 μs | 0.0201 μs | 0.0120 μs | 7.870 μs |  1.70 |    0.00 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3           | 4.661 μs | 0.0254 μs | 0.0168 μs | 4.661 μs |  1.01 |    0.00 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3           | 4.831 μs | 0.0091 μs | 0.0054 μs | 4.831 μs |  1.04 |    0.00 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 3.315 μs | 0.0061 μs | 0.0040 μs | 3.315 μs |  0.72 |    0.00 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3           | 6.865 μs | 0.1255 μs | 0.0830 μs | 6.905 μs |  1.48 |    0.02 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3           | 4.635 μs | 0.0098 μs | 0.0058 μs | 4.635 μs |  1.00 |    0.00 | 0.0153 |      - |     448 B |        1.00 |
|                         |            |                |             |             |          |           |           |          |       |         |        |        |           |             |
| EncryptString_Medium    | MediumRun  | 15             | 2           | 10          | 4.927 μs | 0.0088 μs | 0.0129 μs | 4.925 μs |  1.04 |    0.01 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | MediumRun  | 15             | 2           | 10          | 7.851 μs | 0.0676 μs | 0.0991 μs | 7.789 μs |  1.65 |    0.02 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | MediumRun  | 15             | 2           | 10          | 4.627 μs | 0.0563 μs | 0.0808 μs | 4.628 μs |  0.98 |    0.02 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | MediumRun  | 15             | 2           | 10          | 4.853 μs | 0.0144 μs | 0.0212 μs | 4.859 μs |  1.02 |    0.01 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | MediumRun  | 15             | 2           | 10          | 3.220 μs | 0.0075 μs | 0.0108 μs | 3.218 μs |  0.68 |    0.00 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | MediumRun  | 15             | 2           | 10          | 7.220 μs | 0.0459 μs | 0.0686 μs | 7.231 μs |  1.52 |    0.02 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | MediumRun  | 15             | 2           | 10          | 4.745 μs | 0.0154 μs | 0.0221 μs | 4.744 μs |  1.00 |    0.01 | 0.0153 |      - |     448 B |        1.00 |
