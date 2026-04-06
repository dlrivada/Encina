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
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.213 μs | 0.0172 μs | 0.0114 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.473 μs | 0.0128 μs | 0.0067 μs |  1.05 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     36.810 μs | 0.2778 μs | 0.1653 μs |  7.06 |    0.03 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.281 μs | 0.0052 μs | 0.0027 μs |  0.63 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.980 μs | 0.0254 μs | 0.0151 μs |  0.76 |    0.00 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     29.265 μs | 0.3144 μs | 0.1871 μs |  5.61 |    0.04 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.265 μs | 0.0161 μs | 0.0096 μs |  1.59 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |              |             |               |           |           |       |         |         |        |           |             |
| Encrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    411.591 μs |        NA | 0.0000 μs |  1.00 |    0.00 |       - |      - |     960 B |        1.00 |
| Encrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    450.905 μs |        NA | 0.0000 μs |  1.10 |    0.00 |       - |      - |    3912 B |        4.08 |
| Encrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    528.210 μs |        NA | 0.0000 μs |  1.28 |    0.00 |       - |      - |  197448 B |      205.68 |
| Decrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 17,113.992 μs |        NA | 0.0000 μs | 41.58 |    0.00 |       - |      - |     672 B |        0.70 |
| Decrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,770.409 μs |        NA | 0.0000 μs | 40.75 |    0.00 |       - |      - |    2640 B |        2.75 |
| Decrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,717.950 μs |        NA | 0.0000 μs | 40.62 |    0.00 |       - |      - |  131664 B |      137.15 |
| Roundtrip_Short | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 17,294.721 μs |        NA | 0.0000 μs | 42.02 |    0.00 |       - |      - |    1408 B |        1.47 |
