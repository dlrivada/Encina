```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.478 μs |  0.0438 μs | 0.0229 μs |  1.09 |    0.01 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 17.462 μs |  0.1346 μs | 0.0801 μs |  5.49 |    0.03 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.408 μs |  0.0658 μs | 0.0392 μs |  0.76 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 12.585 μs |  0.0383 μs | 0.0253 μs |  3.95 |    0.01 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.171 μs |  0.0350 μs | 0.0183 μs |  0.68 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.183 μs |  0.0146 μs | 0.0087 μs |  1.00 |    0.00 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  5.453 μs |  0.0683 μs | 0.0452 μs |  1.71 |    0.01 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  3.482 μs |  0.4910 μs | 0.0269 μs |  1.07 |    0.03 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 18.464 μs | 15.3707 μs | 0.8425 μs |  5.67 |    0.28 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.365 μs |  0.3170 μs | 0.0174 μs |  0.73 |    0.02 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 13.068 μs | 12.0896 μs | 0.6627 μs |  4.02 |    0.21 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.163 μs |  0.2558 μs | 0.0140 μs |  0.66 |    0.02 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.256 μs |  2.0630 μs | 0.1131 μs |  1.00 |    0.04 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  5.443 μs |  0.5111 μs | 0.0280 μs |  1.67 |    0.05 | 0.0076 |      - |    1184 B |        1.61 |
