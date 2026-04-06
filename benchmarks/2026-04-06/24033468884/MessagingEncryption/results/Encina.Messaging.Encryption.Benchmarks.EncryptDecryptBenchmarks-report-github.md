```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  5.137 μs | 0.0150 μs | 0.0099 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.614 μs | 0.0168 μs | 0.0111 μs |  1.09 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 31.539 μs | 0.2083 μs | 0.1378 μs |  6.14 |    0.03 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.302 μs | 0.0087 μs | 0.0052 μs |  0.64 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.668 μs | 0.0110 μs | 0.0065 μs |  0.71 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 28.260 μs | 0.4311 μs | 0.2851 μs |  5.50 |    0.05 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.321 μs | 0.0419 μs | 0.0219 μs |  1.62 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.776 μs | 0.0065 μs | 0.0096 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.340 μs | 0.0320 μs | 0.0458 μs |  1.12 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 31.940 μs | 0.2130 μs | 0.3055 μs |  6.69 |    0.06 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.399 μs | 0.0898 μs | 0.1289 μs |  0.71 |    0.03 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.644 μs | 0.0174 μs | 0.0255 μs |  0.76 |    0.01 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 26.282 μs | 0.2621 μs | 0.3674 μs |  5.50 |    0.08 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.258 μs | 0.0310 μs | 0.0435 μs |  1.73 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
