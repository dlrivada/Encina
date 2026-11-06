```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  4.790 μs |  0.0326 μs | 0.0194 μs |  1.09 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 35.723 μs |  1.2722 μs | 0.8415 μs |  8.11 |    0.18 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.673 μs |  0.0082 μs | 0.0054 μs |  0.83 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 30.311 μs |  0.9723 μs | 0.6431 μs |  6.88 |    0.14 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.954 μs |  0.0056 μs | 0.0037 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.406 μs |  0.0173 μs | 0.0115 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  7.220 μs |  0.0316 μs | 0.0165 μs |  1.64 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  4.818 μs |  2.0979 μs | 0.1150 μs |  1.18 |    0.02 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 38.049 μs | 26.0344 μs | 1.4270 μs |  9.33 |    0.31 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.645 μs |  0.1199 μs | 0.0066 μs |  0.89 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 29.431 μs |  4.2332 μs | 0.2320 μs |  7.22 |    0.06 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.953 μs |  0.1020 μs | 0.0056 μs |  0.72 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.078 μs |  0.3727 μs | 0.0204 μs |  1.00 |    0.01 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  7.305 μs |  0.3764 μs | 0.0206 μs |  1.79 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
