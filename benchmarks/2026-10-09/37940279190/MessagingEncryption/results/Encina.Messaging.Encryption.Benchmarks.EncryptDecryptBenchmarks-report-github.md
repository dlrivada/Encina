```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.514 μs | 0.0174 μs | 0.0104 μs |  1.12 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 34.090 μs | 1.1217 μs | 0.7419 μs |  6.89 |    0.14 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.757 μs | 0.0475 μs | 0.0282 μs |  0.76 |    0.01 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 27.180 μs | 0.4323 μs | 0.2859 μs |  5.50 |    0.06 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.300 μs | 0.0127 μs | 0.0066 μs |  0.67 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.945 μs | 0.0081 μs | 0.0053 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.900 μs | 0.0168 μs | 0.0088 μs |  1.80 |    0.00 |  0.0610 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.852 μs | 0.1440 μs | 0.0079 μs |  1.12 |    0.00 |  0.2136 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 33.230 μs | 2.3732 μs | 0.1301 μs |  6.39 |    0.02 | 11.7188 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  4.002 μs | 0.2222 μs | 0.0122 μs |  0.77 |    0.00 |  0.1373 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 30.122 μs | 3.2841 μs | 0.1800 μs |  5.79 |    0.03 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.538 μs | 0.0943 μs | 0.0052 μs |  0.68 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  5.204 μs | 0.1831 μs | 0.0100 μs |  1.00 |    0.00 |  0.0381 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  9.131 μs | 2.0892 μs | 0.1145 μs |  1.75 |    0.02 |  0.0610 |      - |    1184 B |        1.61 |
