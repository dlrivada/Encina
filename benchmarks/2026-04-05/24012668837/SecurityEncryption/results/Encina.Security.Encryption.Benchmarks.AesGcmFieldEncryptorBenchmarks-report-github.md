```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                  | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.696 μs | 0.0203 μs | 0.0121 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.799 μs | 0.0228 μs | 0.0135 μs |  1.02 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.156 μs | 0.0406 μs | 0.0269 μs |  1.52 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.287 μs | 0.0059 μs | 0.0039 μs |  0.70 | 0.0114 |      - |     320 B |        0.71 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.925 μs | 0.0315 μs | 0.0208 μs |  1.69 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.608 μs | 0.0130 μs | 0.0086 μs |  0.98 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.769 μs | 0.0130 μs | 0.0086 μs |  1.02 | 0.0458 |      - |    1168 B |        2.61 |
|                         |            |                |             |             |              |             |               |           |           |       |        |        |           |             |
| EncryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,869.822 μs |        NA | 0.0000 μs |  1.00 |      - |      - |     672 B |        1.00 |
| EncryptString_Medium    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,950.828 μs |        NA | 0.0000 μs |  1.03 |      - |      - |    1392 B |        2.07 |
| EncryptString_Long      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,928.176 μs |        NA | 0.0000 μs |  1.02 |      - |      - |   12912 B |       19.21 |
| DecryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,387.410 μs |        NA | 0.0000 μs |  5.01 |      - |      - |     544 B |        0.81 |
| EncryptDecryptRoundtrip | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,240.655 μs |        NA | 0.0000 μs |  5.31 |      - |      - |     888 B |        1.32 |
| EncryptBytes_Short      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,327.379 μs |        NA | 0.0000 μs |  1.86 |      - |      - |     672 B |        1.00 |
| EncryptBytes_Medium     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,190.682 μs |        NA | 0.0000 μs |  1.81 |      - |      - |    1392 B |        2.07 |
