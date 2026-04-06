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
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.741 μs | 0.0221 μs | 0.0132 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.800 μs | 0.0112 μs | 0.0067 μs |  1.01 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.015 μs | 0.0364 μs | 0.0240 μs |  1.48 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.233 μs | 0.0071 μs | 0.0043 μs |  0.68 | 0.0114 |      - |     320 B |        0.71 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.927 μs | 0.0164 μs | 0.0097 μs |  1.67 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.684 μs | 0.0257 μs | 0.0170 μs |  0.99 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.808 μs | 0.0222 μs | 0.0132 μs |  1.01 | 0.0458 |      - |    1168 B |        2.61 |
|                         |            |                |             |             |              |             |               |           |           |       |        |        |           |             |
| EncryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,827.744 μs |        NA | 0.0000 μs |  1.00 |      - |      - |     672 B |        1.00 |
| EncryptString_Medium    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,842.883 μs |        NA | 0.0000 μs |  1.01 |      - |      - |    1392 B |        2.07 |
| EncryptString_Long      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,876.406 μs |        NA | 0.0000 μs |  1.02 |      - |      - |   12912 B |       19.21 |
| DecryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,322.602 μs |        NA | 0.0000 μs |  5.07 |      - |      - |     544 B |        0.81 |
| EncryptDecryptRoundtrip | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,356.489 μs |        NA | 0.0000 μs |  5.43 |      - |      - |     888 B |        1.32 |
| EncryptBytes_Short      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,231.706 μs |        NA | 0.0000 μs |  1.85 |      - |      - |     672 B |        1.00 |
| EncryptBytes_Medium     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,236.881 μs |        NA | 0.0000 μs |  1.85 |      - |      - |    1392 B |        2.07 |
