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
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.363 μs | 0.0338 μs | 0.0223 μs |  5.360 μs |  1.10 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 31.698 μs | 0.3823 μs | 0.2529 μs | 31.675 μs |  6.49 |    0.05 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.903 μs | 0.0068 μs | 0.0040 μs |  3.903 μs |  0.80 |    0.00 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 26.469 μs | 0.3631 μs | 0.2161 μs | 26.402 μs |  5.42 |    0.04 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.517 μs | 0.0025 μs | 0.0013 μs |  3.516 μs |  0.72 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.884 μs | 0.0149 μs | 0.0089 μs |  4.883 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.869 μs | 0.0316 μs | 0.0209 μs |  8.865 μs |  1.82 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.412 μs | 0.0131 μs | 0.0196 μs |  5.411 μs |  1.11 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 32.853 μs | 0.3674 μs | 0.5499 μs | 32.862 μs |  6.73 |    0.13 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.807 μs | 0.1024 μs | 0.1468 μs |  3.896 μs |  0.78 |    0.03 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 27.484 μs | 0.2290 μs | 0.3210 μs | 27.431 μs |  5.63 |    0.08 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.326 μs | 0.0333 μs | 0.0467 μs |  3.292 μs |  0.68 |    0.01 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.882 μs | 0.0322 μs | 0.0441 μs |  4.887 μs |  1.00 |    0.01 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.358 μs | 0.0384 μs | 0.0512 μs |  8.379 μs |  1.71 |    0.02 |  0.0610 |      - |    1184 B |        1.61 |
