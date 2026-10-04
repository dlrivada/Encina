```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.374 μs | 0.0148 μs | 0.0077 μs |  1.10 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 32.239 μs | 0.3141 μs | 0.2077 μs |  6.62 |    0.05 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.662 μs | 0.0055 μs | 0.0036 μs |  0.75 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 26.434 μs | 0.1934 μs | 0.1279 μs |  5.43 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.253 μs | 0.0055 μs | 0.0029 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.872 μs | 0.0284 μs | 0.0188 μs |  1.00 |    0.01 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.296 μs | 0.0241 μs | 0.0144 μs |  1.70 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.350 μs | 0.0125 μs | 0.0183 μs |  1.10 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 32.174 μs | 0.1140 μs | 0.1671 μs |  6.60 |    0.07 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.812 μs | 0.0938 μs | 0.1404 μs |  0.78 |    0.03 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 27.116 μs | 0.1879 μs | 0.2812 μs |  5.56 |    0.07 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.268 μs | 0.0128 μs | 0.0184 μs |  0.67 |    0.01 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.874 μs | 0.0305 μs | 0.0427 μs |  1.00 |    0.01 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.982 μs | 0.0334 μs | 0.0457 μs |  1.84 |    0.02 |  0.0610 |      - |    1184 B |        1.61 |
