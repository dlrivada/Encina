```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method          | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  4.432 μs | 0.0160 μs | 0.0095 μs |  1.16 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 26.355 μs | 1.6713 μs | 1.1054 μs |  6.87 |    0.28 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     | 3           |  3.105 μs | 0.0071 μs | 0.0047 μs |  0.81 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 3           | 18.712 μs | 0.8764 μs | 0.5797 μs |  4.88 |    0.14 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  2.716 μs | 0.0111 μs | 0.0066 μs |  0.71 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     | 3           |  3.836 μs | 0.0128 μs | 0.0067 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     | 3           |  6.662 μs | 0.0364 μs | 0.0217 μs |  1.74 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | MediumRun  | 15             | 2           | 10          |  4.390 μs | 0.0088 μs | 0.0126 μs |  1.14 |    0.01 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | MediumRun  | 15             | 2           | 10          | 24.178 μs | 0.4600 μs | 0.6742 μs |  6.30 |    0.17 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | MediumRun  | 15             | 2           | 10          |  3.146 μs | 0.0335 μs | 0.0491 μs |  0.82 |    0.01 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | MediumRun  | 15             | 2           | 10          | 18.741 μs | 0.5892 μs | 0.8818 μs |  4.88 |    0.23 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | MediumRun  | 15             | 2           | 10          |  2.746 μs | 0.0112 μs | 0.0161 μs |  0.72 |    0.01 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | MediumRun  | 15             | 2           | 10          |  3.838 μs | 0.0108 μs | 0.0159 μs |  1.00 |    0.01 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | MediumRun  | 15             | 2           | 10          |  6.867 μs | 0.0702 μs | 0.1028 μs |  1.79 |    0.03 | 0.0458 |      - |    1184 B |        1.61 |
