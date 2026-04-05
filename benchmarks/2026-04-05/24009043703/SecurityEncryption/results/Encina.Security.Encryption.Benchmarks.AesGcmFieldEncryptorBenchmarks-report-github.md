```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                  | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.630 μs | 0.0061 μs | 0.0032 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.877 μs | 0.0198 μs | 0.0131 μs |  1.05 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.045 μs | 0.0642 μs | 0.0382 μs |  1.52 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.233 μs | 0.0061 μs | 0.0036 μs |  0.70 | 0.0191 |      - |     320 B |        0.71 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.842 μs | 0.0111 μs | 0.0058 μs |  1.69 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.914 μs | 0.0074 μs | 0.0044 μs |  1.06 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.111 μs | 0.0153 μs | 0.0091 μs |  1.10 | 0.0687 |      - |    1168 B |        2.61 |
|                         |            |                |             |             |              |             |               |           |           |       |        |        |           |             |
| EncryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,908.612 μs |        NA | 0.0000 μs |  1.00 |      - |      - |     672 B |        1.00 |
| EncryptString_Medium    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,862.957 μs |        NA | 0.0000 μs |  0.98 |      - |      - |    1392 B |        2.07 |
| EncryptString_Long      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,902.501 μs |        NA | 0.0000 μs |  1.00 |      - |      - |   12912 B |       19.21 |
| DecryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,018.567 μs |        NA | 0.0000 μs |  5.16 |      - |      - |     544 B |        0.81 |
| EncryptDecryptRoundtrip | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,226.475 μs |        NA | 0.0000 μs |  5.23 |      - |      - |     888 B |        1.32 |
| EncryptBytes_Short      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,115.856 μs |        NA | 0.0000 μs |  1.76 |      - |      - |     672 B |        1.00 |
| EncryptBytes_Medium     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,002.706 μs |        NA | 0.0000 μs |  1.72 |      - |      - |    1392 B |        2.07 |
