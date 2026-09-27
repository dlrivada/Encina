```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.702 μs | 0.0572 μs | 0.0299 μs |  1.11 |    0.04 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 18.627 μs | 0.7118 μs | 0.4708 μs |  5.60 |    0.26 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.528 μs | 0.0184 μs | 0.0096 μs |  0.76 |    0.03 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 13.051 μs | 0.1603 μs | 0.1060 μs |  3.92 |    0.16 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.297 μs | 0.1423 μs | 0.0941 μs |  0.69 |    0.04 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.333 μs | 0.2159 μs | 0.1428 μs |  1.00 |    0.06 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  5.526 μs | 0.0494 μs | 0.0259 μs |  1.66 |    0.07 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  3.779 μs | 0.2435 μs | 0.0133 μs |  1.17 |    0.01 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 18.216 μs | 2.0846 μs | 0.1143 μs |  5.65 |    0.06 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.498 μs | 0.3318 μs | 0.0182 μs |  0.78 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 13.533 μs | 6.6825 μs | 0.3663 μs |  4.20 |    0.11 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.216 μs | 0.0445 μs | 0.0024 μs |  0.69 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.224 μs | 0.6549 μs | 0.0359 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  5.755 μs | 4.4034 μs | 0.2414 μs |  1.79 |    0.07 | 0.0076 |      - |    1184 B |        1.61 |
