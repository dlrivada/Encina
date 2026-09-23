```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.341 μs |  0.0384 μs | 0.0201 μs |  1.03 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 34.265 μs |  1.3030 μs | 0.8619 μs |  6.63 |    0.16 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.690 μs |  0.0338 μs | 0.0201 μs |  0.71 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 28.459 μs |  0.4373 μs | 0.2893 μs |  5.51 |    0.05 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.277 μs |  0.0081 μs | 0.0048 μs |  0.63 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  5.169 μs |  0.0153 μs | 0.0101 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.296 μs |  0.0205 μs | 0.0136 μs |  1.61 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.457 μs |  2.2439 μs | 0.1230 μs |  1.06 |    0.02 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 33.359 μs |  2.5833 μs | 0.1416 μs |  6.50 |    0.03 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.741 μs |  0.4108 μs | 0.0225 μs |  0.73 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 28.266 μs | 12.0539 μs | 0.6607 μs |  5.50 |    0.11 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.223 μs |  0.2222 μs | 0.0122 μs |  0.63 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  5.135 μs |  0.1946 μs | 0.0107 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.268 μs |  1.8237 μs | 0.1000 μs |  1.61 |    0.02 |  0.0610 |      - |    1184 B |        1.61 |
