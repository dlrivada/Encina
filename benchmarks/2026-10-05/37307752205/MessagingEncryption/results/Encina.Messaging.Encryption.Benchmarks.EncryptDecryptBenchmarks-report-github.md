```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.359 μs | 0.0213 μs | 0.0141 μs |  1.13 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 26.646 μs | 1.4826 μs | 0.9806 μs |  5.60 |    0.20 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.746 μs | 0.0090 μs | 0.0059 μs |  0.79 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 19.972 μs | 0.3850 μs | 0.2547 μs |  4.20 |    0.05 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.330 μs | 0.0122 μs | 0.0081 μs |  0.70 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.756 μs | 0.0098 μs | 0.0058 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.419 μs | 0.0944 μs | 0.0625 μs |  1.77 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.418 μs | 0.1875 μs | 0.0103 μs |  1.12 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 26.753 μs | 9.3798 μs | 0.5141 μs |  5.51 |    0.09 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.715 μs | 0.2546 μs | 0.0140 μs |  0.77 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 19.718 μs | 1.7228 μs | 0.0944 μs |  4.06 |    0.02 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.320 μs | 0.1745 μs | 0.0096 μs |  0.68 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.853 μs | 0.1521 μs | 0.0083 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.595 μs | 0.7578 μs | 0.0415 μs |  1.77 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
