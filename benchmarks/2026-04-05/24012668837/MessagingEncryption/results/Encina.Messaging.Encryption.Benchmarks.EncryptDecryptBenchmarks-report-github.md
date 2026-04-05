```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.861 μs | 0.0094 μs | 0.0056 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.605 μs | 0.0262 μs | 0.0156 μs |  1.15 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     35.781 μs | 1.1875 μs | 0.7854 μs |  7.36 |    0.15 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.269 μs | 0.0070 μs | 0.0046 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.756 μs | 0.0143 μs | 0.0095 μs |  0.77 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     29.719 μs | 0.9507 μs | 0.5657 μs |  6.11 |    0.11 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.962 μs | 0.0429 μs | 0.0255 μs |  1.84 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |              |             |               |           |           |       |         |         |        |           |             |
| Encrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    446.562 μs |        NA | 0.0000 μs |  1.00 |    0.00 |       - |      - |     960 B |        1.00 |
| Encrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    408.510 μs |        NA | 0.0000 μs |  0.91 |    0.00 |       - |      - |    3912 B |        4.08 |
| Encrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    585.570 μs |        NA | 0.0000 μs |  1.31 |    0.00 |       - |      - |  197448 B |      205.68 |
| Decrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 17,074.879 μs |        NA | 0.0000 μs | 38.24 |    0.00 |       - |      - |     672 B |        0.70 |
| Decrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 27,577.771 μs |        NA | 0.0000 μs | 61.76 |    0.00 |       - |      - |    2640 B |        2.75 |
| Decrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 17,077.734 μs |        NA | 0.0000 μs | 38.24 |    0.00 |       - |      - |  131664 B |      137.15 |
| Roundtrip_Short | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 17,714.629 μs |        NA | 0.0000 μs | 39.67 |    0.00 |       - |      - |    1408 B |        1.47 |
