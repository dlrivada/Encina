```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  4.137 μs | 0.0323 μs | 0.0169 μs |  1.12 |    0.01 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 16.395 μs | 0.0517 μs | 0.0307 μs |  4.42 |    0.02 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.869 μs | 0.0055 μs | 0.0033 μs |  0.77 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 12.340 μs | 0.0252 μs | 0.0132 μs |  3.33 |    0.01 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.328 μs | 0.0028 μs | 0.0015 μs |  0.63 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.706 μs | 0.0271 μs | 0.0161 μs |  1.00 |    0.01 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  6.168 μs | 0.0123 μs | 0.0073 μs |  1.66 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  4.121 μs | 0.0257 μs | 0.0014 μs |  1.11 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 16.755 μs | 0.5125 μs | 0.0281 μs |  4.52 |    0.01 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.600 μs | 0.0918 μs | 0.0050 μs |  0.70 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 12.639 μs | 0.8265 μs | 0.0453 μs |  3.41 |    0.01 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.302 μs | 0.0833 μs | 0.0046 μs |  0.62 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.703 μs | 0.2065 μs | 0.0113 μs |  1.00 |    0.00 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  6.664 μs | 0.0696 μs | 0.0038 μs |  1.80 |    0.00 |  0.0687 |      - |    1184 B |        1.61 |
