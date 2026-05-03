```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.361 μs | 0.0232 μs | 0.0153 μs |  5.360 μs |  1.14 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 33.731 μs | 0.4739 μs | 0.2820 μs | 33.779 μs |  7.17 |    0.06 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.956 μs | 0.0168 μs | 0.0111 μs |  3.954 μs |  0.84 |    0.00 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 28.157 μs | 1.0666 μs | 0.7055 μs | 28.328 μs |  5.98 |    0.14 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.170 μs | 0.0082 μs | 0.0054 μs |  3.169 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.706 μs | 0.0168 μs | 0.0100 μs |  4.704 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.137 μs | 0.0298 μs | 0.0197 μs |  8.129 μs |  1.73 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.548 μs | 0.1222 μs | 0.1791 μs |  5.404 μs |  1.17 |    0.04 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 33.201 μs | 0.2474 μs | 0.3548 μs | 33.143 μs |  7.02 |    0.08 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.784 μs | 0.0839 μs | 0.1203 μs |  3.871 μs |  0.80 |    0.03 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 27.819 μs | 0.1999 μs | 0.2992 μs | 27.782 μs |  5.88 |    0.07 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.463 μs | 0.0069 μs | 0.0097 μs |  3.468 μs |  0.73 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.727 μs | 0.0125 μs | 0.0175 μs |  4.731 μs |  1.00 |    0.01 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.451 μs | 0.1721 μs | 0.2413 μs |  8.267 μs |  1.79 |    0.05 |  0.0610 |      - |    1184 B |        1.61 |
