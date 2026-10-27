```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.414 μs |  0.0136 μs | 0.0090 μs |  1.10 |    0.01 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 23.398 μs |  0.3736 μs | 0.2471 μs |  4.77 |    0.05 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.626 μs |  0.0070 μs | 0.0042 μs |  0.74 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 16.739 μs |  0.2285 μs | 0.1360 μs |  3.41 |    0.03 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.287 μs |  0.0124 μs | 0.0082 μs |  0.67 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.904 μs |  0.0343 μs | 0.0227 μs |  1.00 |    0.01 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.323 μs |  0.0229 μs | 0.0120 μs |  1.70 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.348 μs |  0.2139 μs | 0.0117 μs |  1.12 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 23.589 μs | 13.1338 μs | 0.7199 μs |  4.95 |    0.13 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.683 μs |  0.1167 μs | 0.0064 μs |  0.77 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 16.490 μs |  1.9534 μs | 0.1071 μs |  3.46 |    0.02 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.301 μs |  0.4346 μs | 0.0238 μs |  0.69 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.762 μs |  0.2066 μs | 0.0113 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.335 μs |  0.3875 μs | 0.0212 μs |  1.75 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
