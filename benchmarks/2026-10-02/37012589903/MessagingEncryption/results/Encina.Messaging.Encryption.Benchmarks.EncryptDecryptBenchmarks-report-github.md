```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.453 μs |  0.0116 μs | 0.0069 μs |  1.12 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 34.026 μs |  0.9119 μs | 0.6032 μs |  6.99 |    0.12 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.686 μs |  0.0177 μs | 0.0105 μs |  0.76 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 26.745 μs |  0.4234 μs | 0.2520 μs |  5.49 |    0.05 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.498 μs |  0.0064 μs | 0.0038 μs |  0.72 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.871 μs |  0.0143 μs | 0.0085 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  9.037 μs |  0.0360 μs | 0.0238 μs |  1.86 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.436 μs |  0.0880 μs | 0.0048 μs |  1.06 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 33.966 μs | 30.2650 μs | 1.6589 μs |  6.60 |    0.28 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.710 μs |  0.1912 μs | 0.0105 μs |  0.72 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 27.220 μs |  4.0467 μs | 0.2218 μs |  5.29 |    0.04 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.570 μs |  0.2256 μs | 0.0124 μs |  0.69 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  5.146 μs |  0.2019 μs | 0.0111 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.339 μs |  0.4024 μs | 0.0221 μs |  1.62 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
