```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.566 μs | 0.0468 μs | 0.0278 μs |  1.13 |    0.01 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 17.858 μs | 0.2108 μs | 0.1394 μs |  5.63 |    0.05 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.356 μs | 0.0162 μs | 0.0096 μs |  0.74 |    0.00 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 12.823 μs | 0.1806 μs | 0.0945 μs |  4.05 |    0.03 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.204 μs | 0.1451 μs | 0.0960 μs |  0.70 |    0.03 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.169 μs | 0.0179 μs | 0.0106 μs |  1.00 |    0.00 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  5.803 μs | 0.6066 μs | 0.4012 μs |  1.83 |    0.12 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  3.767 μs | 4.3728 μs | 0.2397 μs |  1.06 |    0.09 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 17.648 μs | 1.9506 μs | 0.1069 μs |  4.96 |    0.34 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.651 μs | 4.0314 μs | 0.2210 μs |  0.75 |    0.07 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 13.175 μs | 7.5999 μs | 0.4166 μs |  3.70 |    0.27 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.171 μs | 0.1533 μs | 0.0084 μs |  0.61 |    0.04 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.572 μs | 5.1197 μs | 0.2806 μs |  1.00 |    0.10 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  5.819 μs | 9.5500 μs | 0.5235 μs |  1.64 |    0.17 | 0.0076 |      - |    1184 B |        1.61 |
