```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.309 μs | 0.0392 μs | 0.0259 μs |  1.09 |    0.02 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 31.861 μs | 0.3794 μs | 0.2258 μs |  6.57 |    0.11 | 2.3193 | 0.4883 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.785 μs | 0.0955 μs | 0.0632 μs |  0.78 |    0.02 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 22.436 μs | 0.2620 μs | 0.1733 μs |  4.62 |    0.08 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.289 μs | 0.0282 μs | 0.0187 μs |  0.68 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.854 μs | 0.1187 μs | 0.0785 μs |  1.00 |    0.02 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.338 μs | 0.1538 μs | 0.1017 μs |  1.72 |    0.03 |      - |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.364 μs | 0.4976 μs | 0.0273 μs |  1.12 |    0.01 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 32.247 μs | 3.5272 μs | 0.1933 μs |  6.71 |    0.05 | 2.3193 | 0.4883 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.672 μs | 0.1464 μs | 0.0080 μs |  0.76 |    0.00 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 22.949 μs | 1.1976 μs | 0.0656 μs |  4.78 |    0.02 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.333 μs | 0.1472 μs | 0.0081 μs |  0.69 |    0.00 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.805 μs | 0.4510 μs | 0.0247 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.148 μs | 0.5802 μs | 0.0318 μs |  1.70 |    0.01 |      - |      - |    1184 B |        1.61 |
