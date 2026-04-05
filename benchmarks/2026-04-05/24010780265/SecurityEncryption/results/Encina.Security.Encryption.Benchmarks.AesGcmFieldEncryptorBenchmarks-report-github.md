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
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.728 μs | 0.0177 μs | 0.0117 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.932 μs | 0.0113 μs | 0.0067 μs |  1.04 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.115 μs | 0.0341 μs | 0.0203 μs |  1.50 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.247 μs | 0.0026 μs | 0.0014 μs |  0.69 | 0.0191 |      - |     320 B |        0.71 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.004 μs | 0.0130 μs | 0.0068 μs |  1.69 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.589 μs | 0.0104 μs | 0.0062 μs |  0.97 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.798 μs | 0.0175 μs | 0.0116 μs |  1.01 | 0.0687 |      - |    1168 B |        2.61 |
|                         |            |                |             |             |              |             |               |           |           |       |        |        |           |             |
| EncryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,834.599 μs |        NA | 0.0000 μs |  1.00 |      - |      - |     672 B |        1.00 |
| EncryptString_Medium    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,793.152 μs |        NA | 0.0000 μs |  0.99 |      - |      - |    1392 B |        2.07 |
| EncryptString_Long      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,869.815 μs |        NA | 0.0000 μs |  1.01 |      - |      - |   12912 B |       19.21 |
| DecryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,566.940 μs |        NA | 0.0000 μs |  5.14 |      - |      - |     544 B |        0.81 |
| EncryptDecryptRoundtrip | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,406.918 μs |        NA | 0.0000 μs |  5.44 |      - |      - |     888 B |        1.32 |
| EncryptBytes_Short      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,008.625 μs |        NA | 0.0000 μs |  1.77 |      - |      - |     672 B |        1.00 |
| EncryptBytes_Medium     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,026.629 μs |        NA | 0.0000 μs |  1.77 |      - |      - |    1392 B |        2.07 |
