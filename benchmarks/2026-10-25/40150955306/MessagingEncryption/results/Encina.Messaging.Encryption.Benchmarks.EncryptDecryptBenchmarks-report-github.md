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
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.339 μs | 0.0245 μs | 0.0162 μs |  1.12 |    0.01 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 26.624 μs | 3.5645 μs | 2.3577 μs |  5.56 |    0.47 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.742 μs | 0.0084 μs | 0.0056 μs |  0.78 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 21.180 μs | 0.9691 μs | 0.6410 μs |  4.43 |    0.14 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.261 μs | 0.0154 μs | 0.0091 μs |  0.68 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.787 μs | 0.0777 μs | 0.0514 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.117 μs | 0.0909 μs | 0.0541 μs |  1.70 |    0.02 |      - |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.451 μs | 0.0303 μs | 0.0454 μs |  1.15 |    0.01 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 27.415 μs | 1.7568 μs | 2.4628 μs |  5.76 |    0.51 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.648 μs | 0.0150 μs | 0.0215 μs |  0.77 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 18.180 μs | 0.4420 μs | 0.6616 μs |  3.82 |    0.14 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.227 μs | 0.0467 μs | 0.0699 μs |  0.68 |    0.02 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.761 μs | 0.0201 μs | 0.0301 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.295 μs | 0.1172 μs | 0.1754 μs |  1.74 |    0.04 |      - |      - |    1184 B |        1.61 |
