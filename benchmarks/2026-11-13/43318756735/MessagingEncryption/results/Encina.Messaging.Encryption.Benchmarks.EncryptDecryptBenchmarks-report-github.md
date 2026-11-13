```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 2.674 μs | 0.0371 μs | 0.0221 μs |  1.11 |    0.02 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 9.316 μs | 0.3140 μs | 0.2077 μs |  3.88 |    0.11 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 1.813 μs | 0.0201 μs | 0.0105 μs |  0.76 |    0.01 |  0.1431 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 6.913 μs | 0.1937 μs | 0.1153 μs |  2.88 |    0.07 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 1.615 μs | 0.0363 μs | 0.0240 μs |  0.67 |    0.02 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 2.400 μs | 0.0722 μs | 0.0429 μs |  1.00 |    0.02 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 4.022 μs | 0.1225 μs | 0.0810 μs |  1.68 |    0.04 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |          |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           | 2.702 μs | 0.3344 μs | 0.0183 μs |  1.13 |    0.02 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 9.769 μs | 4.3829 μs | 0.2402 μs |  4.08 |    0.11 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           | 1.813 μs | 0.4002 μs | 0.0219 μs |  0.76 |    0.02 |  0.1431 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 7.853 μs | 8.2357 μs | 0.4514 μs |  3.28 |    0.17 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           | 1.552 μs | 0.2878 μs | 0.0158 μs |  0.65 |    0.01 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           | 2.396 μs | 0.8742 μs | 0.0479 μs |  1.00 |    0.02 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           | 3.965 μs | 0.7770 μs | 0.0426 μs |  1.66 |    0.03 |  0.0687 |      - |    1184 B |        1.61 |
