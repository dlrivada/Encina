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
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.393 μs | 0.1502 μs | 0.0994 μs |  1.10 |    0.02 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 32.839 μs | 1.4643 μs | 0.8714 μs |  6.70 |    0.17 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.672 μs | 0.0325 μs | 0.0194 μs |  0.75 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 26.710 μs | 0.3795 μs | 0.2510 μs |  5.45 |    0.05 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.276 μs | 0.0050 μs | 0.0030 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.901 μs | 0.0189 μs | 0.0125 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.282 μs | 0.0109 μs | 0.0065 μs |  1.69 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.523 μs | 0.1172 μs | 0.1681 μs |  1.14 |    0.03 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 33.537 μs | 0.4261 μs | 0.6245 μs |  6.90 |    0.13 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.752 μs | 0.0318 μs | 0.0456 μs |  0.77 |    0.01 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 29.125 μs | 0.5276 μs | 0.7896 μs |  5.99 |    0.16 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.416 μs | 0.0566 μs | 0.0775 μs |  0.70 |    0.02 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.860 μs | 0.0070 μs | 0.0098 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.311 μs | 0.0172 μs | 0.0236 μs |  1.71 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
