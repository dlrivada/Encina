```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  5.650 μs | 0.0170 μs | 0.0101 μs |  5.653 μs |  1.15 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 32.710 μs | 0.1430 μs | 0.0851 μs | 32.733 μs |  6.64 |    0.02 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.723 μs | 0.0081 μs | 0.0054 μs |  3.723 μs |  0.76 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 27.569 μs | 0.2129 μs | 0.1114 μs | 27.564 μs |  5.60 |    0.02 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.377 μs | 0.0055 μs | 0.0033 μs |  3.377 μs |  0.69 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  4.924 μs | 0.0141 μs | 0.0074 μs |  4.924 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  8.230 μs | 0.0152 μs | 0.0090 μs |  8.228 μs |  1.67 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  5.783 μs | 0.1396 μs | 0.2003 μs |  5.672 μs |  1.17 |    0.04 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 32.855 μs | 0.3886 μs | 0.5574 μs | 32.785 μs |  6.65 |    0.12 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.757 μs | 0.1017 μs | 0.1490 μs |  3.679 μs |  0.76 |    0.03 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 27.591 μs | 0.2737 μs | 0.4012 μs | 27.834 μs |  5.58 |    0.09 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.306 μs | 0.0932 μs | 0.1337 μs |  3.302 μs |  0.67 |    0.03 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  4.942 μs | 0.0240 μs | 0.0328 μs |  4.944 μs |  1.00 |    0.01 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  8.841 μs | 0.0318 μs | 0.0446 μs |  8.846 μs |  1.79 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
