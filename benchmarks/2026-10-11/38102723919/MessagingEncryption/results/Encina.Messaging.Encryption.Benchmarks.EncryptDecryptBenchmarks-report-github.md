```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  4.521 μs | 0.0225 μs | 0.0149 μs |  1.20 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 17.705 μs | 0.1805 μs | 0.1194 μs |  4.71 |    0.04 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  2.743 μs | 0.0211 μs | 0.0140 μs |  0.73 |    0.01 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 12.811 μs | 0.0381 μs | 0.0227 μs |  3.41 |    0.02 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  2.337 μs | 0.0100 μs | 0.0066 μs |  0.62 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.756 μs | 0.0324 μs | 0.0215 μs |  1.00 |    0.01 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  6.168 μs | 0.0193 μs | 0.0127 μs |  1.64 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  4.229 μs | 0.0216 μs | 0.0323 μs |  1.10 |    0.04 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 18.019 μs | 0.2533 μs | 0.3713 μs |  4.71 |    0.20 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  2.827 μs | 0.0915 μs | 0.1369 μs |  0.74 |    0.04 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 13.273 μs | 0.1350 μs | 0.1979 μs |  3.47 |    0.14 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  2.321 μs | 0.0215 μs | 0.0308 μs |  0.61 |    0.02 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.835 μs | 0.1015 μs | 0.1456 μs |  1.00 |    0.05 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  6.206 μs | 0.0267 μs | 0.0391 μs |  1.62 |    0.06 |  0.0687 |      - |    1184 B |        1.61 |
