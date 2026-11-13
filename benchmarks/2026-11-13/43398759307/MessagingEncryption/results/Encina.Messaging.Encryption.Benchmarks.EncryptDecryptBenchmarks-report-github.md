```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.658 μs |  0.0370 μs | 0.0194 μs |  1.10 |    0.02 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     |  9.581 μs |  1.0839 μs | 0.7170 μs |  3.97 |    0.29 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  1.815 μs |  0.0687 μs | 0.0454 μs |  0.75 |    0.02 |  0.1431 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     |  6.925 μs |  0.6119 μs | 0.4047 μs |  2.87 |    0.16 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  1.576 μs |  0.0452 μs | 0.0299 μs |  0.65 |    0.01 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.416 μs |  0.0450 μs | 0.0298 μs |  1.00 |    0.02 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  4.047 μs |  0.1022 μs | 0.0608 μs |  1.68 |    0.03 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  2.772 μs |  0.5480 μs | 0.0300 μs |  1.18 |    0.01 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 10.280 μs |  8.8381 μs | 0.4844 μs |  4.39 |    0.18 | 11.7645 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.396 μs | 15.6344 μs | 0.8570 μs |  1.02 |    0.32 |  0.1431 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           |  7.014 μs |  2.1572 μs | 0.1182 μs |  2.99 |    0.05 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  1.610 μs |  0.5478 μs | 0.0300 μs |  0.69 |    0.01 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  2.344 μs |  0.3301 μs | 0.0181 μs |  1.00 |    0.01 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  3.984 μs |  0.6870 μs | 0.0377 μs |  1.70 |    0.02 |  0.0687 |      - |    1184 B |        1.61 |
