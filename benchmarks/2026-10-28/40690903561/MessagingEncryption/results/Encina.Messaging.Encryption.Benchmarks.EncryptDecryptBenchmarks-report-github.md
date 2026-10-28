```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3.227 μs | 0.1028 μs | 0.0680 μs |  1.12 |    0.04 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 9.133 μs | 0.0881 μs | 0.0461 μs |  3.16 |    0.08 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 1.969 μs | 0.0924 μs | 0.0550 μs |  0.68 |    0.02 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 6.889 μs | 0.0762 μs | 0.0504 μs |  2.39 |    0.06 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 1.821 μs | 0.0495 μs | 0.0294 μs |  0.63 |    0.02 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 2.888 μs | 0.1162 μs | 0.0768 μs |  1.00 |    0.04 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 4.717 μs | 0.0293 μs | 0.0153 μs |  1.63 |    0.04 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |          |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           | 3.339 μs | 3.0742 μs | 0.1685 μs |  1.12 |    0.05 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 9.307 μs | 3.5477 μs | 0.1945 μs |  3.13 |    0.07 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           | 1.936 μs | 0.3181 μs | 0.0174 μs |  0.65 |    0.01 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 6.825 μs | 2.1169 μs | 0.1160 μs |  2.30 |    0.04 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           | 1.798 μs | 0.4118 μs | 0.0226 μs |  0.61 |    0.01 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           | 2.971 μs | 0.7760 μs | 0.0425 μs |  1.00 |    0.02 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           | 4.713 μs | 0.1850 μs | 0.0101 μs |  1.59 |    0.02 |  0.0687 |      - |    1184 B |        1.61 |
