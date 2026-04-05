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
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.934 μs | 0.0143 μs | 0.0075 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.511 μs | 0.0266 μs | 0.0158 μs |  1.12 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     34.784 μs | 0.4619 μs | 0.3055 μs |  7.05 |    0.06 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.569 μs | 0.0077 μs | 0.0046 μs |  0.72 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.766 μs | 0.0102 μs | 0.0067 μs |  0.76 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     28.503 μs | 0.2967 μs | 0.1552 μs |  5.78 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.339 μs | 0.0147 μs | 0.0087 μs |  1.69 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |              |             |               |           |           |       |         |         |        |           |             |
| Encrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    426.302 μs |        NA | 0.0000 μs |  1.00 |    0.00 |       - |      - |     960 B |        1.00 |
| Encrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    389.753 μs |        NA | 0.0000 μs |  0.91 |    0.00 |       - |      - |    3912 B |        4.08 |
| Encrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    538.443 μs |        NA | 0.0000 μs |  1.26 |    0.00 |       - |      - |  197448 B |      205.68 |
| Decrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,659.995 μs |        NA | 0.0000 μs | 39.08 |    0.00 |       - |      - |     672 B |        0.70 |
| Decrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,668.452 μs |        NA | 0.0000 μs | 39.10 |    0.00 |       - |      - |    2640 B |        2.75 |
| Decrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,658.182 μs |        NA | 0.0000 μs | 39.08 |    0.00 |       - |      - |  131664 B |      137.15 |
| Roundtrip_Short | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,366.042 μs |        NA | 0.0000 μs | 38.39 |    0.00 |       - |      - |    1408 B |        1.47 |
