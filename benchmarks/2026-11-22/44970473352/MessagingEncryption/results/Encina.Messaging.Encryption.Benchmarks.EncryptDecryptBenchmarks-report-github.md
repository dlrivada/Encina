```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.108 μs | 0.0528 μs | 0.0349 μs |  5.110 μs |  1.23 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 30.666 μs | 0.2128 μs | 0.1266 μs | 30.640 μs |  7.36 |    0.04 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.191 μs | 0.0227 μs | 0.0150 μs |  3.198 μs |  0.77 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 26.207 μs | 0.1505 μs | 0.0895 μs | 26.179 μs |  6.29 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  2.773 μs | 0.0208 μs | 0.0138 μs |  2.775 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.166 μs | 0.0311 μs | 0.0185 μs |  4.158 μs |  1.00 |    0.01 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  7.079 μs | 0.0366 μs | 0.0242 μs |  7.074 μs |  1.70 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  4.785 μs | 0.0252 μs | 0.0377 μs |  4.775 μs |  1.10 |    0.04 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 30.893 μs | 0.1509 μs | 0.2258 μs | 30.853 μs |  7.12 |    0.24 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.218 μs | 0.0077 μs | 0.0113 μs |  3.222 μs |  0.74 |    0.02 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 26.614 μs | 0.1310 μs | 0.1920 μs | 26.632 μs |  6.13 |    0.21 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  2.858 μs | 0.0818 μs | 0.1173 μs |  2.858 μs |  0.66 |    0.03 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.345 μs | 0.1009 μs | 0.1478 μs |  4.255 μs |  1.00 |    0.05 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  7.121 μs | 0.0229 μs | 0.0335 μs |  7.122 μs |  1.64 |    0.06 |  0.0687 |      - |    1184 B |        1.61 |
