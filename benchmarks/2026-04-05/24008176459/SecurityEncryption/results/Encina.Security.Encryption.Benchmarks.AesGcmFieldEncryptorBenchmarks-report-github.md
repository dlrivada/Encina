```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                  | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.648 μs | 0.0399 μs | 0.0264 μs |  1.00 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.286 μs | 0.0264 μs | 0.0174 μs |  1.14 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.842 μs | 0.1041 μs | 0.0619 μs |  1.69 |    0.02 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.319 μs | 0.0075 μs | 0.0050 μs |  0.71 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.039 μs | 0.0336 μs | 0.0222 μs |  1.73 |    0.01 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.608 μs | 0.0134 μs | 0.0080 μs |  0.99 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.853 μs | 0.0178 μs | 0.0106 μs |  1.04 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
|                         |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| EncryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,858.300 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |     672 B |        1.00 |
| EncryptString_Medium    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,905.398 μs |        NA | 0.0000 μs |  1.02 |    0.00 |      - |      - |    1392 B |        2.07 |
| EncryptString_Long      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,857.078 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |   12912 B |       19.21 |
| DecryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,769.465 μs |        NA | 0.0000 μs |  5.17 |    0.00 |      - |      - |     544 B |        0.81 |
| EncryptDecryptRoundtrip | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,723.969 μs |        NA | 0.0000 μs |  5.50 |    0.00 |      - |      - |     888 B |        1.32 |
| EncryptBytes_Short      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,156.086 μs |        NA | 0.0000 μs |  1.80 |    0.00 |      - |      - |     672 B |        1.00 |
| EncryptBytes_Medium     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,199.657 μs |        NA | 0.0000 μs |  1.82 |    0.00 |      - |      - |    1392 B |        2.07 |
