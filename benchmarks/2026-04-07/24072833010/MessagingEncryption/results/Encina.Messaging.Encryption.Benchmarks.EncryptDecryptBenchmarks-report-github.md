```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.824 μs | 0.0213 μs | 0.0141 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.569 μs | 0.0811 μs | 0.0483 μs |  1.15 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     37.050 μs | 2.3660 μs | 1.4079 μs |  7.68 |    0.28 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.327 μs | 0.0196 μs | 0.0129 μs |  0.69 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.062 μs | 0.0724 μs | 0.0479 μs |  0.84 |    0.01 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     30.749 μs | 0.8406 μs | 0.5002 μs |  6.37 |    0.10 |  7.8125 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.272 μs | 0.0365 μs | 0.0217 μs |  1.71 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |              |             |               |           |           |       |         |         |        |           |             |
| Encrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    431.453 μs |        NA | 0.0000 μs |  1.00 |    0.00 |       - |      - |     960 B |        1.00 |
| Encrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    428.337 μs |        NA | 0.0000 μs |  0.99 |    0.00 |       - |      - |    3912 B |        4.08 |
| Encrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    532.201 μs |        NA | 0.0000 μs |  1.23 |    0.00 |       - |      - |  197448 B |      205.68 |
| Decrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 17,020.716 μs |        NA | 0.0000 μs | 39.45 |    0.00 |       - |      - |     672 B |        0.70 |
| Decrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 18,769.051 μs |        NA | 0.0000 μs | 43.50 |    0.00 |       - |      - |    2640 B |        2.75 |
| Decrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,287.951 μs |        NA | 0.0000 μs | 37.75 |    0.00 |       - |      - |  131664 B |      137.15 |
| Roundtrip_Short | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,873.161 μs |        NA | 0.0000 μs | 36.79 |    0.00 |       - |      - |    1408 B |        1.47 |
