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
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.825 μs | 0.0265 μs | 0.0176 μs |  1.05 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.828 μs | 0.0370 μs | 0.0245 μs |  1.70 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.918 μs | 0.0095 μs | 0.0057 μs |  1.07 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.753 μs | 0.0177 μs | 0.0117 μs |  1.03 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.487 μs | 0.0055 μs | 0.0036 μs |  0.76 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.928 μs | 0.0553 μs | 0.0366 μs |  1.51 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.599 μs | 0.0164 μs | 0.0109 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |             |              |             |               |           |           |       |        |        |           |             |
| EncryptString_Medium    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,805.242 μs |        NA | 0.0000 μs |  0.99 |      - |      - |    1392 B |        2.07 |
| EncryptDecryptRoundtrip | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,077.538 μs |        NA | 0.0000 μs |  5.34 |      - |      - |     888 B |        1.32 |
| EncryptBytes_Short      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,942.211 μs |        NA | 0.0000 μs |  1.75 |      - |      - |     672 B |        1.00 |
| EncryptBytes_Medium     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,963.771 μs |        NA | 0.0000 μs |  1.76 |      - |      - |    1392 B |        2.07 |
| DecryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,501.705 μs |        NA | 0.0000 μs |  5.13 |      - |      - |     544 B |        0.81 |
| EncryptString_Long      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,829.346 μs |        NA | 0.0000 μs |  1.00 |      - |      - |   12912 B |       19.21 |
| EncryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,824.237 μs |        NA | 0.0000 μs |  1.00 |      - |      - |     672 B |        1.00 |
