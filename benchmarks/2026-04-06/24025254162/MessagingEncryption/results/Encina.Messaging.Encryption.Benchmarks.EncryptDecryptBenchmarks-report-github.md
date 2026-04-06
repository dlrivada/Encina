```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.781 μs | 0.0146 μs | 0.0087 μs |  1.00 |    0.00 |  0.0420 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  4.542 μs | 0.0453 μs | 0.0300 μs |  1.20 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 18.258 μs | 1.1527 μs | 0.7624 μs |  4.83 |    0.19 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.355 μs | 0.0033 μs | 0.0020 μs |  0.62 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.990 μs | 0.0187 μs | 0.0124 μs |  0.79 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 13.429 μs | 0.2851 μs | 0.1886 μs |  3.55 |    0.05 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  6.245 μs | 0.0093 μs | 0.0055 μs |  1.65 |    0.00 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.742 μs | 0.0351 μs | 0.0019 μs |  1.00 |    0.00 |  0.0420 |      - |     736 B |        1.00 |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  4.168 μs | 0.5330 μs | 0.0292 μs |  1.11 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 17.354 μs | 4.0717 μs | 0.2232 μs |  4.64 |    0.05 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.391 μs | 0.0681 μs | 0.0037 μs |  0.64 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.703 μs | 0.3132 μs | 0.0172 μs |  0.72 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 12.664 μs | 0.2862 μs | 0.0157 μs |  3.38 |    0.00 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  6.726 μs | 0.0764 μs | 0.0042 μs |  1.80 |    0.00 |  0.0687 |      - |    1184 B |        1.61 |
