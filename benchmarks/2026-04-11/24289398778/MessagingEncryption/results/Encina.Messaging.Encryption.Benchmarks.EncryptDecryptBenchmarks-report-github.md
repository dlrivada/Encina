```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.977 μs | 0.0313 μs | 0.0207 μs |  5.975 μs |  1.20 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 33.846 μs | 1.6999 μs | 1.1244 μs | 33.335 μs |  6.82 |    0.22 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.714 μs | 0.0470 μs | 0.0280 μs |  3.718 μs |  0.75 |    0.01 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 27.700 μs | 0.2578 μs | 0.1705 μs | 27.713 μs |  5.58 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.369 μs | 0.0037 μs | 0.0019 μs |  3.368 μs |  0.68 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.961 μs | 0.0122 μs | 0.0072 μs |  4.960 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.284 μs | 0.0113 μs | 0.0067 μs |  8.286 μs |  1.67 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.649 μs | 0.0298 μs | 0.0437 μs |  5.640 μs |  1.11 |    0.04 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 33.594 μs | 0.2675 μs | 0.3837 μs | 33.683 μs |  6.60 |    0.23 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.641 μs | 0.0108 μs | 0.0155 μs |  3.640 μs |  0.72 |    0.02 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 27.968 μs | 0.2643 μs | 0.3790 μs | 27.897 μs |  5.50 |    0.20 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.142 μs | 0.0297 μs | 0.0426 μs |  3.175 μs |  0.62 |    0.02 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  5.092 μs | 0.1188 μs | 0.1741 μs |  5.237 μs |  1.00 |    0.05 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.297 μs | 0.0379 μs | 0.0543 μs |  8.301 μs |  1.63 |    0.06 |  0.0610 |      - |    1184 B |        1.61 |
