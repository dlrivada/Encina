```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3.215 μs | 0.0333 μs | 0.0174 μs |  1.15 |    0.03 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 9.258 μs | 0.3873 μs | 0.2562 μs |  3.32 |    0.12 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 1.973 μs | 0.0152 μs | 0.0091 μs |  0.71 |    0.02 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 7.015 μs | 0.1475 μs | 0.0975 μs |  2.51 |    0.07 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 1.844 μs | 0.0846 μs | 0.0560 μs |  0.66 |    0.03 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 2.794 μs | 0.1094 μs | 0.0724 μs |  1.00 |    0.03 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 4.880 μs | 0.0691 μs | 0.0411 μs |  1.75 |    0.04 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |          |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           | 3.293 μs | 2.0144 μs | 0.1104 μs |  1.14 |    0.06 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 9.323 μs | 2.0910 μs | 0.1146 μs |  3.23 |    0.14 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           | 2.008 μs | 0.9183 μs | 0.0503 μs |  0.70 |    0.03 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 6.803 μs | 3.1639 μs | 0.1734 μs |  2.36 |    0.11 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           | 1.816 μs | 0.1254 μs | 0.0069 μs |  0.63 |    0.03 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           | 2.889 μs | 2.5917 μs | 0.1421 μs |  1.00 |    0.06 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           | 4.860 μs | 3.9657 μs | 0.2174 μs |  1.68 |    0.10 |  0.0687 |      - |    1184 B |        1.61 |
