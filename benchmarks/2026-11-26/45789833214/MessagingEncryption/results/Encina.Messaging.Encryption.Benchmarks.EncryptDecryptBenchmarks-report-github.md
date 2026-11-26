```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.776 μs | 0.0279 μs | 0.0166 μs |  1.11 |    0.01 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 16.944 μs | 0.6355 μs | 0.4203 μs |  4.98 |    0.12 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.460 μs | 0.0114 μs | 0.0068 μs |  0.72 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 12.247 μs | 0.1690 μs | 0.1118 μs |  3.60 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.087 μs | 0.0120 μs | 0.0079 μs |  0.61 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.405 μs | 0.0116 μs | 0.0077 μs |  1.00 |    0.00 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  5.386 μs | 0.0323 μs | 0.0214 μs |  1.58 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  3.647 μs | 0.4977 μs | 0.0273 μs |  1.15 |    0.01 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 16.721 μs | 2.5335 μs | 0.1389 μs |  5.28 |    0.04 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.369 μs | 0.6349 μs | 0.0348 μs |  0.75 |    0.01 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 12.361 μs | 2.8669 μs | 0.1571 μs |  3.90 |    0.04 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.083 μs | 0.1763 μs | 0.0097 μs |  0.66 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.169 μs | 0.2136 μs | 0.0117 μs |  1.00 |    0.00 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  5.813 μs | 0.1857 μs | 0.0102 μs |  1.83 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
