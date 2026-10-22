```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.718 μs | 0.0168 μs | 0.0100 μs |  1.09 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 31.791 μs | 0.2677 μs | 0.1771 μs |  6.05 |    0.03 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.697 μs | 0.0119 μs | 0.0079 μs |  0.70 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 26.193 μs | 0.1125 μs | 0.0744 μs |  4.98 |    0.02 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.273 μs | 0.0047 μs | 0.0024 μs |  0.62 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  5.256 μs | 0.0169 μs | 0.0111 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.895 μs | 0.0230 μs | 0.0137 μs |  1.69 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.382 μs | 0.1384 μs | 0.0076 μs |  1.12 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 31.599 μs | 3.3338 μs | 0.1827 μs |  6.57 |    0.04 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.691 μs | 0.1256 μs | 0.0069 μs |  0.77 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 25.902 μs | 2.1783 μs | 0.1194 μs |  5.38 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.298 μs | 1.7188 μs | 0.0942 μs |  0.69 |    0.02 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.811 μs | 0.2844 μs | 0.0156 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.355 μs | 0.6248 μs | 0.0342 μs |  1.74 |    0.01 |  0.0610 |      - |    1184 B |        1.61 |
