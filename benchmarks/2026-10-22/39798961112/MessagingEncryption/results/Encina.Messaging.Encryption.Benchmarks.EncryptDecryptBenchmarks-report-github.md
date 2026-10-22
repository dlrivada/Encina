```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.007 μs | 0.0629 μs | 0.0416 μs |  1.18 |    0.02 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 29.677 μs | 1.4589 μs | 0.9650 μs |  6.98 |    0.23 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.374 μs | 0.0435 μs | 0.0259 μs |  0.79 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 21.001 μs | 1.1036 μs | 0.7300 μs |  4.94 |    0.17 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  2.887 μs | 0.0264 μs | 0.0157 μs |  0.68 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.249 μs | 0.0787 μs | 0.0468 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  7.136 μs | 0.1204 μs | 0.0717 μs |  1.68 |    0.02 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.002 μs | 0.0498 μs | 0.0745 μs |  1.16 |    0.03 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 30.021 μs | 0.8734 μs | 1.3072 μs |  6.96 |    0.34 | 2.3193 | 0.4883 |  197225 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.382 μs | 0.0643 μs | 0.0963 μs |  0.78 |    0.03 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 19.784 μs | 0.6517 μs | 0.9755 μs |  4.59 |    0.24 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  2.931 μs | 0.0318 μs | 0.0476 μs |  0.68 |    0.02 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.315 μs | 0.0675 μs | 0.0968 μs |  1.00 |    0.03 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  7.359 μs | 0.1082 μs | 0.1586 μs |  1.71 |    0.05 |      - |      - |    1184 B |        1.61 |
