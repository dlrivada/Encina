```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  4.041 μs |  0.1638 μs | 0.0975 μs |  1.12 |    0.05 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 21.010 μs |  2.4573 μs | 1.4623 μs |  5.82 |    0.45 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.744 μs |  0.0738 μs | 0.0439 μs |  0.76 |    0.03 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 15.772 μs |  1.6067 μs | 1.0627 μs |  4.37 |    0.33 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.474 μs |  0.0807 μs | 0.0534 μs |  0.68 |    0.03 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.618 μs |  0.2323 μs | 0.1536 μs |  1.00 |    0.06 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  5.971 μs |  0.1996 μs | 0.1188 μs |  1.65 |    0.07 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  4.111 μs |  0.7928 μs | 0.0435 μs |  1.14 |    0.02 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 19.579 μs |  5.4340 μs | 0.2979 μs |  5.43 |    0.12 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.834 μs |  0.6582 μs | 0.0361 μs |  0.79 |    0.02 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 16.056 μs | 18.2130 μs | 0.9983 μs |  4.45 |    0.25 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.483 μs |  1.1671 μs | 0.0640 μs |  0.69 |    0.02 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.606 μs |  1.4210 μs | 0.0779 μs |  1.00 |    0.03 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  6.217 μs |  1.6744 μs | 0.0918 μs |  1.72 |    0.04 | 0.0076 |      - |    1184 B |        1.61 |
