```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  4.748 μs | 0.1524 μs | 0.0907 μs |  1.12 |    0.03 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 19.695 μs | 0.4733 μs | 0.3131 μs |  4.63 |    0.11 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.135 μs | 0.0441 μs | 0.0262 μs |  0.74 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 14.316 μs | 0.4367 μs | 0.2888 μs |  3.37 |    0.09 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.846 μs | 0.0592 μs | 0.0352 μs |  0.67 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.255 μs | 0.1169 μs | 0.0773 μs |  1.00 |    0.02 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  7.181 μs | 0.1137 μs | 0.0677 μs |  1.69 |    0.03 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  4.774 μs | 3.5827 μs | 0.1964 μs |  1.13 |    0.05 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 19.527 μs | 4.8816 μs | 0.2676 μs |  4.63 |    0.11 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.151 μs | 0.8987 μs | 0.0493 μs |  0.75 |    0.02 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 15.242 μs | 5.0846 μs | 0.2787 μs |  3.61 |    0.09 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.939 μs | 0.3734 μs | 0.0205 μs |  0.70 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.222 μs | 1.8321 μs | 0.1004 μs |  1.00 |    0.03 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  7.118 μs | 2.2363 μs | 0.1226 μs |  1.69 |    0.04 |      - |      - |    1184 B |        1.61 |
