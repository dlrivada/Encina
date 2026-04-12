```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.701 μs | 0.0247 μs | 0.0163 μs |  5.701 μs |  1.18 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 31.777 μs | 0.4188 μs | 0.2770 μs | 31.738 μs |  6.59 |    0.05 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.927 μs | 0.0271 μs | 0.0179 μs |  3.922 μs |  0.81 |    0.00 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 26.284 μs | 0.1828 μs | 0.1209 μs | 26.311 μs |  5.45 |    0.02 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.552 μs | 0.0097 μs | 0.0064 μs |  3.552 μs |  0.74 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.825 μs | 0.0065 μs | 0.0039 μs |  4.826 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.204 μs | 0.0335 μs | 0.0175 μs |  8.197 μs |  1.70 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.297 μs | 0.0216 μs | 0.0303 μs |  5.302 μs |  1.07 |    0.04 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 32.139 μs | 0.4126 μs | 0.6048 μs | 32.111 μs |  6.47 |    0.24 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.621 μs | 0.0093 μs | 0.0134 μs |  3.622 μs |  0.73 |    0.02 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 26.324 μs | 0.1550 μs | 0.2320 μs | 26.368 μs |  5.30 |    0.18 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.228 μs | 0.0032 μs | 0.0046 μs |  3.228 μs |  0.65 |    0.02 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.975 μs | 0.1236 μs | 0.1649 μs |  4.846 μs |  1.00 |    0.05 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.205 μs | 0.0112 μs | 0.0161 μs |  8.202 μs |  1.65 |    0.05 |  0.0610 |      - |    1184 B |        1.61 |
