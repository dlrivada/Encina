```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  4.200 μs |  0.1717 μs | 0.1135 μs |  1.12 |    0.04 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 25.160 μs |  3.0099 μs | 1.9909 μs |  6.70 |    0.53 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.895 μs |  0.0782 μs | 0.0517 μs |  0.77 |    0.02 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 23.272 μs |  0.3729 μs | 0.2219 μs |  6.20 |    0.14 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.554 μs |  0.0961 μs | 0.0636 μs |  0.68 |    0.02 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.755 μs |  0.1285 μs | 0.0850 μs |  1.00 |    0.03 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  6.581 μs |  0.1072 μs | 0.0560 μs |  1.75 |    0.04 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |            |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  4.245 μs |  1.6151 μs | 0.0885 μs |  1.11 |    0.02 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 32.502 μs |  1.8889 μs | 0.1035 μs |  8.54 |    0.03 | 2.3193 | 0.4883 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.887 μs |  0.0598 μs | 0.0033 μs |  0.76 |    0.00 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 24.513 μs | 26.6673 μs | 1.4617 μs |  6.44 |    0.33 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.529 μs |  0.4915 μs | 0.0269 μs |  0.66 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.807 μs |  0.1483 μs | 0.0081 μs |  1.00 |    0.00 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  6.210 μs |  0.8386 μs | 0.0460 μs |  1.63 |    0.01 | 0.0076 |      - |    1184 B |        1.61 |
