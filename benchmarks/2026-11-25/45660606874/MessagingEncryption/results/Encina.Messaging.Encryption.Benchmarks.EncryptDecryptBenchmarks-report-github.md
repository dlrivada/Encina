```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.813 μs | 0.0353 μs | 0.0234 μs |  1.16 |    0.01 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 16.667 μs | 0.0452 μs | 0.0269 μs |  6.88 |    0.03 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.091 μs | 0.0184 μs | 0.0110 μs |  0.86 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 12.190 μs | 0.0641 μs | 0.0424 μs |  5.03 |    0.02 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  1.877 μs | 0.0097 μs | 0.0064 μs |  0.77 |    0.00 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.424 μs | 0.0132 μs | 0.0088 μs |  1.00 |    0.00 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  4.432 μs | 0.0229 μs | 0.0151 μs |  1.83 |    0.01 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  2.735 μs | 0.1446 μs | 0.0079 μs |  1.10 |    0.01 | 0.0420 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 16.772 μs | 1.4307 μs | 0.0784 μs |  6.74 |    0.05 | 2.3499 | 0.5188 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.081 μs | 0.1260 μs | 0.0069 μs |  0.84 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 12.182 μs | 0.6680 μs | 0.0366 μs |  4.90 |    0.03 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  1.900 μs | 0.0367 μs | 0.0020 μs |  0.76 |    0.00 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  2.487 μs | 0.3314 μs | 0.0182 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  4.459 μs | 0.4698 μs | 0.0258 μs |  1.79 |    0.01 | 0.0076 |      - |    1184 B |        1.61 |
