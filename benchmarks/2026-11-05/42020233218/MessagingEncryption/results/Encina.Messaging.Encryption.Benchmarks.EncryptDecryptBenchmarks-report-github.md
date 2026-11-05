```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 2.782 μs | 0.1639 μs | 0.1084 μs |  1.10 |    0.04 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 9.501 μs | 0.3471 μs | 0.1815 μs |  3.77 |    0.08 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 1.929 μs | 0.0262 μs | 0.0156 μs |  0.76 |    0.01 |  0.1431 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 7.070 μs | 0.2452 μs | 0.1622 μs |  2.80 |    0.07 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 1.648 μs | 0.0330 μs | 0.0197 μs |  0.65 |    0.01 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 2.523 μs | 0.0534 μs | 0.0353 μs |  1.00 |    0.02 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 4.338 μs | 0.1261 μs | 0.0750 μs |  1.72 |    0.04 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |          |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           | 2.791 μs | 0.8225 μs | 0.0451 μs |  1.09 |    0.02 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 9.800 μs | 4.7670 μs | 0.2613 μs |  3.82 |    0.10 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           | 1.946 μs | 1.1115 μs | 0.0609 μs |  0.76 |    0.02 |  0.1431 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 7.266 μs | 1.9158 μs | 0.1050 μs |  2.83 |    0.05 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           | 1.798 μs | 1.2113 μs | 0.0664 μs |  0.70 |    0.02 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           | 2.565 μs | 0.7261 μs | 0.0398 μs |  1.00 |    0.02 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           | 4.298 μs | 0.4810 μs | 0.0264 μs |  1.68 |    0.02 |  0.0687 |      - |    1184 B |        1.61 |
