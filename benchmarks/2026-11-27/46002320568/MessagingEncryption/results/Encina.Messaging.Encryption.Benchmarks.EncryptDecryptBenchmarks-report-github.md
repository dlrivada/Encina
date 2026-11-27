```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.878 μs | 0.0364 μs | 0.0241 μs |  1.17 |    0.01 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 16.853 μs | 0.1240 μs | 0.0738 μs |  6.83 |    0.04 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.177 μs | 0.0154 μs | 0.0092 μs |  0.88 |    0.00 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 12.935 μs | 0.3332 μs | 0.2204 μs |  5.25 |    0.09 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  1.870 μs | 0.0044 μs | 0.0023 μs |  0.76 |    0.00 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.466 μs | 0.0141 μs | 0.0093 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  4.438 μs | 0.0242 μs | 0.0144 μs |  1.80 |    0.01 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  2.931 μs | 0.2795 μs | 0.0153 μs |  1.18 |    0.01 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 16.955 μs | 0.7430 μs | 0.0407 μs |  6.85 |    0.03 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.176 μs | 0.0726 μs | 0.0040 μs |  0.88 |    0.00 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 12.525 μs | 1.5669 μs | 0.0859 μs |  5.06 |    0.04 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  1.893 μs | 0.1290 μs | 0.0071 μs |  0.77 |    0.00 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  2.474 μs | 0.1846 μs | 0.0101 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  4.490 μs | 0.9617 μs | 0.0527 μs |  1.82 |    0.02 | 0.0076 |      - |    1184 B |        1.61 |
