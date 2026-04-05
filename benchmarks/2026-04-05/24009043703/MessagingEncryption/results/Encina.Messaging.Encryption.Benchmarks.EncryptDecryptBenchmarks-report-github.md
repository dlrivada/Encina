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
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.829 μs | 0.0182 μs | 0.0108 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5.270 μs | 0.0116 μs | 0.0069 μs |  1.09 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     31.136 μs | 0.6636 μs | 0.4389 μs |  6.45 |    0.09 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.248 μs | 0.0080 μs | 0.0047 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3.867 μs | 0.0185 μs | 0.0122 μs |  0.80 |    0.00 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     25.702 μs | 0.1759 μs | 0.1047 μs |  5.32 |    0.02 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      8.154 μs | 0.0099 μs | 0.0052 μs |  1.69 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |              |             |               |           |           |       |         |         |        |           |             |
| Encrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    338.303 μs |        NA | 0.0000 μs |  1.00 |    0.00 |       - |      - |     960 B |        1.00 |
| Encrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    355.957 μs |        NA | 0.0000 μs |  1.05 |    0.00 |       - |      - |    3912 B |        4.08 |
| Encrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    480.761 μs |        NA | 0.0000 μs |  1.42 |    0.00 |       - |      - |  197448 B |      205.68 |
| Decrypt_Short   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,974.392 μs |        NA | 0.0000 μs | 47.22 |    0.00 |       - |      - |     672 B |        0.70 |
| Decrypt_Medium  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,809.594 μs |        NA | 0.0000 μs | 46.73 |    0.00 |       - |      - |    2640 B |        2.75 |
| Decrypt_Large   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,134.372 μs |        NA | 0.0000 μs | 47.69 |    0.00 |       - |      - |  131664 B |      137.15 |
| Roundtrip_Short | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,868.324 μs |        NA | 0.0000 μs | 46.91 |    0.00 |       - |      - |    1408 B |        1.47 |
