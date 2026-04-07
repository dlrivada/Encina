```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.307 μs | 0.0165 μs | 0.0109 μs |  1.11 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 31.227 μs | 0.2364 μs | 0.1564 μs |  6.54 |    0.03 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.653 μs | 0.0113 μs | 0.0075 μs |  0.76 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 26.633 μs | 0.2508 μs | 0.1659 μs |  5.58 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.215 μs | 0.0098 μs | 0.0058 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.776 μs | 0.0070 μs | 0.0037 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.839 μs | 0.0377 μs | 0.0224 μs |  1.85 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.265 μs | 0.2095 μs | 0.0115 μs |  1.09 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 32.996 μs | 1.4884 μs | 0.0816 μs |  6.86 |    0.02 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.620 μs | 0.0577 μs | 0.0032 μs |  0.75 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 26.288 μs | 2.9221 μs | 0.1602 μs |  5.46 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.499 μs | 0.2467 μs | 0.0135 μs |  0.73 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.812 μs | 0.2814 μs | 0.0154 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.246 μs | 0.3646 μs | 0.0200 μs |  1.71 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
