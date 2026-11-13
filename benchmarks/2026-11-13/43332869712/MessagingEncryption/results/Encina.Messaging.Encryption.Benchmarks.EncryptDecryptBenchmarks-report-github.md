```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 2.757 μs | 0.1205 μs | 0.0717 μs |  1.05 |    0.03 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 9.831 μs | 0.5094 μs | 0.3369 μs |  3.74 |    0.13 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 1.881 μs | 0.0785 μs | 0.0467 μs |  0.72 |    0.02 |  0.1431 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 7.073 μs | 0.4592 μs | 0.2733 μs |  2.69 |    0.10 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 1.708 μs | 0.0212 μs | 0.0111 μs |  0.65 |    0.01 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 2.630 μs | 0.0598 μs | 0.0356 μs |  1.00 |    0.02 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 4.610 μs | 0.0953 μs | 0.0630 μs |  1.75 |    0.03 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |          |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           | 2.891 μs | 5.1629 μs | 0.2830 μs |  1.09 |    0.09 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 9.146 μs | 2.8882 μs | 0.1583 μs |  3.44 |    0.05 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           | 1.866 μs | 1.3085 μs | 0.0717 μs |  0.70 |    0.02 |  0.1431 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 6.744 μs | 2.5112 μs | 0.1376 μs |  2.53 |    0.05 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           | 1.722 μs | 0.0843 μs | 0.0046 μs |  0.65 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           | 2.662 μs | 0.3026 μs | 0.0166 μs |  1.00 |    0.01 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           | 4.483 μs | 0.2252 μs | 0.0123 μs |  1.68 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
