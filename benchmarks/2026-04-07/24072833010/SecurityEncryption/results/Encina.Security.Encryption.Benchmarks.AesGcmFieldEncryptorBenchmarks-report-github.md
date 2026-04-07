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
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.886 μs | 0.0150 μs | 0.0100 μs |  1.06 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.771 μs | 0.0320 μs | 0.0190 μs |  1.90 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.593 μs | 0.0150 μs | 0.0099 μs |  0.99 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.780 μs | 0.0209 μs | 0.0124 μs |  1.03 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.238 μs | 0.0071 μs | 0.0047 μs |  0.70 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.761 μs | 0.0708 μs | 0.0469 μs |  1.68 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.619 μs | 0.0069 μs | 0.0036 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |             |              |             |               |           |           |       |        |        |           |             |
| EncryptString_Medium    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,038.130 μs |        NA | 0.0000 μs |  0.98 |      - |      - |    1392 B |        2.07 |
| EncryptDecryptRoundtrip | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,760.438 μs |        NA | 0.0000 μs |  5.42 |      - |      - |     888 B |        1.32 |
| EncryptBytes_Short      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,512.834 μs |        NA | 0.0000 μs |  1.78 |      - |      - |     672 B |        1.00 |
| EncryptBytes_Medium     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,355.629 μs |        NA | 0.0000 μs |  1.73 |      - |      - |    1392 B |        2.07 |
| DecryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,487.562 μs |        NA | 0.0000 μs |  5.01 |      - |      - |     544 B |        0.81 |
| EncryptString_Long      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,098.154 μs |        NA | 0.0000 μs |  1.00 |      - |      - |   12912 B |       19.21 |
| EncryptString_Short     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,090.248 μs |        NA | 0.0000 μs |  1.00 |      - |      - |     672 B |        1.00 |
